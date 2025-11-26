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
        private readonly MenuiaService _menuia;

        public ClienteMasterController(
            ApplicationDbContext ctx,
            ILogger<ClienteMasterController> logger,
            IConfiguration configuration,
            MenuiaService menuia)
        {
            _ctx = ctx;
            _logger = logger;
            _configuration = configuration;
            _menuia = menuia;
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
public async Task<IActionResult> GerarQrCode(int id)
{
    var cliente = await _ctx.ClientesMaster.FindAsync(id);
    if (cliente == null)
        return NotFound(new { Message = "Cliente Master não encontrado." });

    var admin = await _ctx.AdministradoresGerais.FirstOrDefaultAsync();
    if (admin == null)
        return StatusCode(500, new { Message = "Admin Geral não encontrado." });

    if (string.IsNullOrWhiteSpace(admin.AuthKey))
        return BadRequest(new { Message = "AuthKey do Admin não configurada." });

    // BASE URL
    var baseUrl = _configuration["PublicBaseUrl"];
    if (string.IsNullOrEmpty(baseUrl))
    {
        var host = Request.Host.Host;
        baseUrl = $"https://{host}";
    }

    var webhookUrl = $"{baseUrl}/api/ClienteMaster/webhook/{id}";
    var deviceName = $"Dispositivo-{cliente.Slug}";

    try
    {
        var response = await _menuia.AdicionarDispositivoQrCodeAsync(
            admin.AuthKey,
            deviceName,
            webhookUrl
        );

        if (response.Status != 200)
            return BadRequest(new { Message = "Erro ao solicitar QR Code", Response = response });

        if (string.IsNullOrEmpty(response.DeviceId))
            return BadRequest(new { Message = "Menuia não retornou DeviceId." });



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
        // WEBHOOK
        // ===========================
        [HttpPost("webhook/{id}")]
public async Task<IActionResult> Webhook(int id, [FromBody] MenuiaWebhook payload)
{
    _logger.LogInformation($"Webhook recebido para Cliente={id}: {payload.Event}");

    var cliente = await _ctx.ClientesMaster.FindAsync(id);
    if (cliente == null)
        return NotFound(new { Message = "Cliente Master não encontrado." });

    if (payload.Event == "device.connected")
    {
        if (string.IsNullOrWhiteSpace(payload.Data?.AppKey) ||
            string.IsNullOrWhiteSpace(payload.Data?.AuthKey))
        {
            return BadRequest(new { Message = "AppKey e AuthKey são obrigatórias." });
        }

        cliente.AppKey = payload.Data.AppKey;
        cliente.AuthKey = payload.Data.AuthKey;
        cliente.UsaApiLembrete = true;

        await _ctx.SaveChangesAsync();

        // CHAMADA ADICIONADA: Validar o dispositivo após a conexão bem-sucedida
        var admin = await _ctx.AdministradoresGerais.FirstOrDefaultAsync();
        if (admin != null && !string.IsNullOrWhiteSpace(admin.AuthKey) && !string.IsNullOrWhiteSpace(payload.Data.Dispositivo))
        {
            var validacaoSucesso = await _menuia.ValidarDispositivoAsync(admin.AuthKey, payload.Data.Dispositivo);
            if (!validacaoSucesso)
            {
                _logger.LogWarning($"Falha ao validar dispositivo {payload.Data.Dispositivo} após conexão no Webhook.");
            }
        }
    }

    return Ok(new { Success = true });
}
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
    public string? Dispositivo { get; set; } // Device ID que o Menuia manda
}

