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
            var dados = await _ctx.ClientesMaster
                .Include(c => c.ConfiguracaoCores)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            return dados == null ? NotFound() : Ok(dados);
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
            // WEBHOOK DO MENUIA (SEM EVENTO NA URL!)
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
}
