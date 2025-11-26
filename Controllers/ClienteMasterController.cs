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

        // ===========================
        // GETs básicos
        // ===========================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var dados = await _ctx.ClientesMaster
                .Include(c => c.ConfiguracaoCores)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            return dados == null ? NotFound() : Ok(dados);
        }

        [HttpGet("by-celular/{celular}")]
        public async Task<IActionResult> GetByCelular(string celular)
        {
            var dados = await _ctx.ClientesMaster
                .Include(c => c.ConfiguracaoCores)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Celular == celular);

            return dados == null ? NotFound() : Ok(dados);
        }

        [HttpGet("check-slug/{slug}")]
        public async Task<IActionResult> CheckSlug(string slug)
        {
            bool exists = await _ctx.ClientesMaster.AnyAsync(c => c.Slug == slug);
            return Ok(new { exists });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var dados = await _ctx.ClientesMaster
                .Include(c => c.ConfiguracaoCores)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug);

            return dados == null ? NotFound() : Ok(dados);
        }

        // ===========================
        // UPDATE
        // ===========================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClienteMasterUpdateDto dto)
        {
            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null) return NotFound("Cliente Master não encontrado.");

            cliente.UsaApiLembrete = dto.UsaApiLembrete;
            cliente.AppKey = dto.AppKey;
            cliente.AuthKey = dto.AuthKey;
            cliente.TempoLembrete = dto.TempoLembrete;
            cliente.Ativo = dto.Ativo;
            cliente.AtualizacaoAutomatica = dto.AtualizacaoAutomatica;

            _ctx.Update(cliente);
            await _ctx.SaveChangesAsync();

            return Ok(new { Message = "Configurações atualizadas com sucesso!" });
        }

        // ===========================
        // GERAR QR CODE
        // ===========================
        [HttpPost("gerar-qrcode/{id}")]
        public async Task<IActionResult> GerarQrCode(int id, [FromServices] MenuiaService menuiaService)
        {
            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
                return NotFound(new { Message = "Cliente Master não encontrado." });

            var admin = await _ctx.AdministradoresGerais.FirstOrDefaultAsync();
            if (admin == null)
                return StatusCode(500, new { Message = "Configuração do Administrador Geral não encontrada." });

            if (string.IsNullOrEmpty(admin.AuthKey))
                return BadRequest(new { Message = "AuthKey do Admin Geral não está configurada." });

            // BASE URL
            var baseUrl = _configuration["PublicBaseUrl"];
            if (string.IsNullOrEmpty(baseUrl))
            {
                var host = Request.Host.ToString();
                baseUrl = $"https://{host}";
            }

            // WEBHOOK CORRETO PARA MENUIA
            var webhookUrl = $"{baseUrl}/api/ClienteMaster/webhook/{id}/on-whatsapp-connected";
            var deviceName = $"Dispositivo-{cliente.Slug}";

            try
            {
                var response = await menuiaService.AdicionarDispositivoQrCodeAsync(
                    admin.AuthKey,
                    deviceName,
                    webhookUrl
                );

                if (response.Status != 200)
                {
                    return BadRequest(new
                    {
                        Message = "Erro ao solicitar QR Code do Menuia",
                        Response = response
                    });
                }

                if (string.IsNullOrEmpty(response.QrCodeBase64))
                {
                    return BadRequest(new
                    {
                        Message = "Menuia retornou QR Code vazio.",
                        Response = response
                    });
                }

                return Ok(new
                {
                    Message = "QR Code gerado com sucesso.",
                    QrCodeBase64 = response.QrCodeBase64,
                    WebhookUrl = webhookUrl,
                    DeviceName = deviceName,
                    DeviceId = response.DeviceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar QR Code");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // ===========================
        // WEBHOOK UNIVERSAL MENUIA
        // ===========================
        [HttpPost("webhook/{id}/{evento}")]
        public async Task<IActionResult> Webhook(int id, string evento, [FromBody] MenuiaResponse body)
        {
            _logger.LogInformation($"Webhook recebido. Cliente={id}, Evento={evento}");

            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
                return NotFound(new { Message = "Cliente Master não encontrado." });

            if (evento == "on-whatsapp-connected")
            {
                _logger.LogInformation("WhatsApp conectado. Salvando chaves...");

                if (string.IsNullOrEmpty(body?.AppKey) || string.IsNullOrEmpty(body?.AuthKey))
                    return BadRequest(new { Message = "AppKey e AuthKey são obrigatórias." });

                cliente.AppKey = body.AppKey;
                cliente.AuthKey = body.AuthKey;
                cliente.UsaApiLembrete = true;

                _ctx.Update(cliente);
                await _ctx.SaveChangesAsync();

                _logger.LogInformation("Chaves salvas com sucesso!");
            }

            return Ok(new { Success = true });
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