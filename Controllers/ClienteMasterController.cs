using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.Services;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteMasterController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<ClienteMasterController> _logger;
        private readonly IConfiguration _configuration;

       public ClienteMasterController(ApplicationDbContext ctx, ILogger<ClienteMasterController> logger, IConfiguration configuration)
        {
            _ctx = ctx;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var clienteMaster = await _ctx.ClientesMaster
                .Include(cm => cm.ConfiguracaoCores)
                .AsNoTracking()
                .FirstOrDefaultAsync(cm => cm.Id == id);

            if (clienteMaster == null)
            {
                return NotFound();
            }

            return Ok(clienteMaster);
        }

        [HttpGet("by-celular/{celular}")]
        public async Task<IActionResult> GetByCelular(string celular)
        {
            var clienteMaster = await _ctx.ClientesMaster
                .Include(cm => cm.ConfiguracaoCores)
                .AsNoTracking()
                .FirstOrDefaultAsync(cm => cm.Celular == celular);

            if (clienteMaster == null)
            {
                return NotFound();
            }

            return Ok(clienteMaster);
        }

        [HttpGet("check-slug/{slug}")]
        public async Task<IActionResult> CheckSlug(string slug)
        {
            var exists = await _ctx.ClientesMaster
                .AnyAsync(cm => cm.Slug == slug);

            return Ok(new { exists });
        }
        
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var clienteMaster = await _ctx.ClientesMaster
                .Include(cm => cm.ConfiguracaoCores)
                .AsNoTracking()
                .FirstOrDefaultAsync(cm => cm.Slug == slug);

            if (clienteMaster == null)
            {
                return NotFound();
            }

            return Ok(clienteMaster);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClienteMaster(int id, [FromBody] ClienteMasterUpdateDto dto)
        {
            var clienteMaster = await _ctx.ClientesMaster.FindAsync(id);
            if (clienteMaster == null)
            {
                return NotFound("Cliente Master não encontrado.");
            }

            clienteMaster.UsaApiLembrete = dto.UsaApiLembrete;
            clienteMaster.AppKey = dto.AppKey;
            clienteMaster.AuthKey = dto.AuthKey;
            clienteMaster.TempoLembrete = dto.TempoLembrete;

            clienteMaster.AtualizacaoAutomatica = dto.AtualizacaoAutomatica;
            clienteMaster.Ativo = dto.Ativo;

            _ctx.ClientesMaster.Update(clienteMaster);
            await _ctx.SaveChangesAsync();

            return Ok(new { Message = "Configurações atualizadas com sucesso!" });
        }

        /// <summary>
        /// Endpoint para gerar QR Code para integração com Menuia.
        /// Este endpoint chama a API do Menuia para adicionar um dispositivo com QR Code
        /// e retorna a imagem do QR Code em Base64 para exibição no frontend.
        /// </summary>
        [HttpPost("gerar-qrcode/{id}")]
        public async Task<IActionResult> GerarQrCode(int id, [FromServices] MenuiaService menuiaService)
        {
            var clienteMaster = await _ctx.ClientesMaster.FindAsync(id);
            if (clienteMaster == null)
            {
                return NotFound(new { Message = "Cliente Master não encontrado." });
            }

            // 1. Buscar a AuthKey do Administrador Geral
            var administradorGeral = await _ctx.AdministradoresGerais.FirstOrDefaultAsync();
            if (administradorGeral == null)
            {
                return StatusCode(500, new { Message = "Configuração do Administrador Geral não encontrada." });
            }

            var masterAuthKey = administradorGeral.AuthKey;

            if (string.IsNullOrEmpty(masterAuthKey))
            {
                return BadRequest(new { Message = "Auth API Key do Administrador Geral não está configurada." });
            }

            // 1. Adicionar Dispositivo (QR CODE) no Menuia
            var publicBaseUrl = _configuration["PublicBaseUrl"];
            if (string.IsNullOrEmpty(publicBaseUrl))
            {
                _logger.LogWarning("Configuração 'PublicBaseUrl' não encontrada. Usando Request.Host, o webhook pode falhar.");
                publicBaseUrl = $"{Request.Scheme}://{Request.Host}";
            }
            var baseUrl = publicBaseUrl;
            var webhookUrl = $"{baseUrl}/api/ClienteMaster/salvar-chaves/{id}";
            var deviceName = $"Dispositivo-{clienteMaster.Slug}";

            try
            {
                _logger.LogInformation($"Iniciando geração de QR Code para cliente {id}");
                var response = await menuiaService.AdicionarDispositivoQrCodeAsync(masterAuthKey, deviceName, webhookUrl);

                _logger.LogInformation($"Resposta do MenuiaService - Status: {response.Status}, QrCodeBase64: {(string.IsNullOrEmpty(response.QrCodeBase64) ? "VAZIO" : "PREENCHIDO")}");

                if (response.Status != 200)
                {
                    return BadRequest(new 
                    { 
                        Message = $"Erro ao solicitar QR Code do Menuia: {response.Message}",
                        Status = response.Status,
                        FullResponse = response
                    });
                }

                // Verifica se conseguiu extrair a imagem Base64 do QR Code
                if (string.IsNullOrEmpty(response.QrCodeBase64))
                {
                    _logger.LogError($"QrCodeBase64 vazio. Resposta completa: {response.Message}");
                    return BadRequest(new 
                    { 
                        Message = "Resposta do Menuia não contém imagem do QR Code válida.",
                        FullResponse = response.Message
                    });
                }

                // Retorna a imagem do QR Code como Base64
                return Ok(new
                {
                    Message = "QR Code gerado com sucesso. Escaneie para conectar o dispositivo.",
                    QrCodeBase64 = response.QrCodeBase64,
                    WebhookUrl = webhookUrl,
                    DeviceName = deviceName,
                    DeviceId = response.DeviceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao gerar QR Code: {ex.Message}");
                return StatusCode(500, new { Message = $"Erro ao gerar QR Code: {ex.Message}", Details = ex.StackTrace });
            }
        }

        /// <summary>
        /// Endpoint para salvar as chaves AppKey e AuthKey após a leitura do QR Code.
        /// Este endpoint é chamado pelo webhook do Menuia quando o dispositivo é conectado.
        /// </summary>
        [HttpPost("salvar-chaves/{id}")]
        public async Task<IActionResult> SalvarChaves(int id, [FromBody] MenuiaResponse menuiaResponse)
        {
            var clienteMaster = await _ctx.ClientesMaster.FindAsync(id);
            if (clienteMaster == null)
            {
                return NotFound(new { Message = "Cliente Master não encontrado." });
            }

            if (string.IsNullOrEmpty(menuiaResponse?.AppKey) || string.IsNullOrEmpty(menuiaResponse?.AuthKey))
            {
                return BadRequest(new { Message = "AppKey e AuthKey são obrigatórios." });
            }

            clienteMaster.AppKey = menuiaResponse.AppKey;
            clienteMaster.AuthKey = menuiaResponse.AuthKey;
            clienteMaster.UsaApiLembrete = true;

            _ctx.ClientesMaster.Update(clienteMaster);
            await _ctx.SaveChangesAsync();

            return Ok(new { Message = "Chaves AppKey e AuthKey salvas com sucesso!" });
        }
    }

    public class ClienteMasterUpdateDto
    {
        public bool UsaApiLembrete { get; set; }
        public string? AppKey { get; set; }
        public string? AuthKey { get; set; }
        public int? TempoLembrete { get; set; }

        public bool AtualizacaoAutomatica { get; set; }
        public bool Ativo { get; set; }
    }
}
