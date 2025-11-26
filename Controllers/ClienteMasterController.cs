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
        // GET BY ID
        // ===========================
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

        // ===========================
        // GERAR QR CODE MENUIA
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

            if (string.IsNullOrWhiteSpace(admin.AuthKey))
                return BadRequest(new { Message = "AuthKey do Admin Geral não configurada." });

var baseUrl = _configuration["PublicBaseUrl"];

if (string.IsNullOrEmpty(baseUrl))
{
    var host = Request.Host.Host;  // pega somente hostname
    var port = Request.Host.Port;  // pega porta (pode ser null)

    baseUrl = port.HasValue
        ? $"https://{host}:{port}"
        : $"https://{host}";
}
            // WEBHOOK DO MENUIA (SEM EVENTO NA URL! )
            var webhookUrl = $"{baseUrl}/api/ClienteMaster/webhook/{id}";

            var deviceName = $"Dispositivo-{cliente.Slug}";

            try
            {
                var response = await menuiaService.AdicionarDispositivoQrCodeAsync(
                    admin.AuthKey,
                    deviceName,
                    webhookUrl
                );

                if (response.Status != 200)
                    return BadRequest(new { Message = "Erro ao solicitar QR Code.", Response = response });

                if (string.IsNullOrEmpty(response.QrCodeBase64))
                    return BadRequest(new { Message = "QR Code vazio retornado pelo Menuia." });

                return Ok(new
                {
                    Message = "QR Code gerado com sucesso.",
                    QrCodeBase64 = response.QrCodeBase64,
                    DeviceId = response.DeviceId,
                    WebhookUrl = webhookUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar QR Code");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // ===========================
        // VERIFICAR DISPOSITIVO CONECTADO
        // ===========================
        [HttpPost("verificar-dispositivo/{id}")]
        public async Task<IActionResult> VerificarDispositivo(int id, [FromServices] MenuiaService menuiaService)
        {
            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
                return NotFound(new { Message = "Cliente Master não encontrado." });

            var admin = await _ctx.AdministradoresGerais.FirstOrDefaultAsync();
            if (admin == null)
                return StatusCode(500, new { Message = "Configuração do Administrador Geral não encontrada." });

            if (string.IsNullOrWhiteSpace(admin.AuthKey))
                return BadRequest(new { Message = "AuthKey do Admin Geral não configurada." });

            var deviceName = $"Dispositivo-{cliente.Slug}";

            try
            {
                // Verifica se o dispositivo está conectado via API do Menuia
                var response = await menuiaService.VerificarDispositivoAsync(
                    admin.AuthKey,
                    deviceName
                );

                _logger.LogInformation($"Verificação do dispositivo - IsConnected: {response.IsConnected}, Message: {response.Message}");

                // Se o dispositivo está conectado
                if (response.IsConnected)
                {
                    // Verifica se as chaves já foram salvas (via webhook ou tentativa anterior)
                    if (!string.IsNullOrWhiteSpace(cliente.AppKey) && 
                        !string.IsNullOrWhiteSpace(cliente.AuthKey))
                    {
                        _logger.LogInformation($"Dispositivo conectado e chaves já salvas para Cliente={id}");

                        return Ok(new
                        {
                            Connected = true,
                            Message = "Dispositivo conectado e chaves disponíveis.",
                            AppKey = cliente.AppKey,
                            AuthKey = cliente.AuthKey
                        });
                    }
                    
                    // Se as chaves não foram salvas, tenta obter ativamente (APENAS UMA VEZ)
                    _logger.LogInformation($"Dispositivo conectado. Tentando obter chaves ativamente para Cliente={id}...");

                    var appResponse = await menuiaService.ObterChavesAppAsync(
                        admin.AuthKey,
                        deviceName,
                        deviceName // O Menuia usa o nome do dispositivo como identificador
                    );

                    // O log mostrou que a resposta do Menuia só tem a appkey, não a authkey.
                    // Usaremos a authkey do Admin Geral para o cliente, conforme o fluxo esperado.
                    if (!string.IsNullOrWhiteSpace(appResponse.AppKey))
                    {
                        // Chaves obtidas com sucesso! Salva no banco.
                        cliente.AppKey = appResponse.AppKey;
                        // A authkey do cliente é a mesma do Admin Geral, que foi usada para criar o app.
                        cliente.AuthKey = admin.AuthKey; 
                        cliente.UsaApiLembrete = true;

                        _ctx.Update(cliente);
                        await _ctx.SaveChangesAsync();

                        _logger.LogInformation($"Chaves obtidas ativamente e salvas para Cliente={id}");

                        return Ok(new
                        {
                            Connected = true,
                            Message = "Dispositivo conectado e chaves obtidas ativamente.",
                            AppKey = cliente.AppKey,
                            AuthKey = cliente.AuthKey
                        });
                    }
                    else
                    {
                        // Não conseguiu obter as chaves ativamente (erro ou atraso)
                        _logger.LogWarning($"Falha ao obter chaves ativamente para Cliente={id}. Resposta: {appResponse.Message}. AppKey obtida: {appResponse.AppKey}");

                        return Ok(new
                        {
                            Connected = true,
                            Message = "Dispositivo conectado. Falha ao obter chaves ativamente. Aguardando webhook..."
                        });
                    }
                }

                // Dispositivo ainda não conectado
                return Ok(new
                {
                    Connected = false,
                    Message = response.Message ?? "Dispositivo ainda não conectado."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar dispositivo");
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // ===========================
        // WEBHOOK OFICIAL MENUIA (SEM EVENTO NA URL!)
        // ===========================
        [HttpPost("webhook/{id}")]
        public async Task<IActionResult> Webhook(int id, [FromBody] MenuiaWebhook payload)
        {
            _logger.LogInformation($"Webhook recebido do Menuia para Cliente={id}: {payload.Event}");

            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
                return NotFound(new { Message = "Cliente Master não encontrado." });

            // EVENTO: Dispositivo conectado
            if (payload.Event == "device.connected")
            {
                if (string.IsNullOrWhiteSpace(payload?.Data?.AppKey) ||
                    string.IsNullOrWhiteSpace(payload?.Data?.AuthKey))
                {
                    return BadRequest(new { Message = "AppKey e AuthKey são obrigatórias." });
                }

                cliente.AppKey = payload.Data.AppKey;
                cliente.AuthKey = payload.Data.AuthKey;
                cliente.UsaApiLembrete = true;

                _ctx.Update(cliente);
                await _ctx.SaveChangesAsync();

                _logger.LogInformation($"Chaves salvas para Cliente={id}");
            }

            return Ok(new { Success = true });
        }
    }

    // ===========================
    // MODELOS PARA O WEBHOOK MENUIA
    // ===========================
    public class MenuiaWebhook
    {
        public string Event { get; set; } = string.Empty;
        public MenuiaWebhookData Data { get; set; } = new();
    }

    public class MenuiaWebhookData
    {
        public string? AppKey { get; set; }
        public string? AuthKey { get; set; }
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
