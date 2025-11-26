using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.Services;
using MarcaAi.Backend.DTOs;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgendamentosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly WhatsAppService _whatsAppService;
        private readonly AgendamentoService _agendamentoService;
        private readonly ILogger<AgendamentosController> _logger;

        public AgendamentosController(
            ApplicationDbContext context,
            WhatsAppService whatsAppService,
            AgendamentoService agendamentoService,
            ILogger<AgendamentosController> logger)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _agendamentoService = agendamentoService;
            _logger = logger;
        }

        // =========================
        // Criar agendamento
        // =========================
        [HttpPost]
public async Task<IActionResult> CriarAgendamento([FromBody] AgendamentoDto dto)
{   
    Console.WriteLine(dto.DataHora);
    try
    {   
        // 0️⃣ Valida DataHora
        if (dto.DataHora == default || dto.DataHora < DateTime.MinValue.AddDays(1))
            return BadRequest("DataHora inválida.");

        // Opcional: converte para UTC ou para horário local do servidor
        var dataHora = DateTime.SpecifyKind(dto.DataHora, DateTimeKind.Local);

        // 1️⃣ Verifica ClienteMaster
        var clienteMaster = await _context.ClientesMaster.FindAsync(dto.ClienteMasterId);
        if (clienteMaster == null) return BadRequest("Cliente Master inválido.");
        if (!clienteMaster.Ativo) return Unauthorized("A conta Master está inativa. Não é possível realizar agendamentos.");

        // 2️⃣ Verifica serviço
        var servico = await _context.Servicos.FindAsync(dto.ServicoId);
        if (servico == null) return BadRequest("Serviço inválido.");

        var inicio = dataHora;
        var fim = inicio.AddMinutes(servico.DuracaoMinutos);

        // 3️⃣ Verifica conflito de horários
        bool conflito = await _context.Agendamentos.AnyAsync(a =>
            a.FuncionarioId == dto.FuncionarioId &&
            a.DataHora < fim &&
            a.DataHora.AddMinutes(a.Servico.DuracaoMinutos) > inicio
        );

        if (conflito)
            return Conflict("Esse horário já está reservado para esse funcionário.");

        // 4️⃣ Cria agendamento
        var agendamento = new Agendamento
        {
            ClienteMasterId = dto.ClienteMasterId,
            ClienteId = dto.ClienteId,
            ServicoId = dto.ServicoId,
            FuncionarioId = dto.FuncionarioId,
            DataHora = dataHora,
            Realizado = false,
            Observacao = dto.Observacao
        };

        _context.Agendamentos.Add(agendamento);
        await _context.SaveChangesAsync();

        // 5️⃣ Busca cliente e funcionário
        var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
        var funcionario = await _context.Funcionarios.FindAsync(dto.FuncionarioId);

        if (clienteMaster.UsaApiLembrete && cliente != null && funcionario != null)
        {
            // Mensagem de confirmação
            string msgConf = $"*Confirmação de Agendamento*\n\nOlá! Seu agendamento foi confirmado:\n\n" +
                             $"👤 Profissional: {funcionario.Nome}\n✂️ Serviço: {servico.Nome}\n" +
                             $"📅 Data: {dataHora:dd/MM/yyyy}\n⏰ Horário: {dataHora:HH:mm}";

            await _whatsAppService.SendMessage(cliente.Telefone, msgConf, clienteMaster.AppKey!, clienteMaster.AuthKey!);

            // Agenda lembrete, se configurado
            if (clienteMaster.TempoLembrete > 0)
            {
                var codigoCancelamento = Guid.NewGuid();
                var solicitacaoCancelamento = new SolicitacaoExclusao
                {
                    AgendamentoId = agendamento.Id,
                    Codigo = codigoCancelamento,
                    Status = "Pendente",
                    CriadoEm = DateTime.Now
                };

                _context.SolicitacoesExclusao.Add(solicitacaoCancelamento);
                await _context.SaveChangesAsync();

                string cancelamentoLink = $"https://marcaai-nine.vercel.app/confirmar-exclusao?codigo={codigoCancelamento}";
                string msgLembrete = $"*Lembrete de Agendamento*\n\nSeu horário está próximo!\n\n" +
                                     $"👤 Profissional: {funcionario.Nome}\n✂️ Serviço: {servico.Nome}\n" +
                                     $"📅 Data: {dataHora:dd/MM/yyyy}\n⏰ Horário: {dataHora:HH:mm}\n\n" +
                                     $"Se precisar cancelar, clique aqui: {cancelamentoLink}";

                DateTime agendar = dataHora.AddMinutes(-clienteMaster.TempoLembrete.Value);

                await _whatsAppService.ScheduleReminder(
                    cliente.Telefone,
                    msgLembrete,
                    clienteMaster.AppKey!,
                    clienteMaster.AuthKey!,
                    agendar
                );
            }
        }

        return Ok(new { Message = "Agendamento criado com sucesso!" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao criar agendamento");
        return StatusCode(500, $"Erro interno: {ex.Message}");
    }
}

        // =========================
        // Listar agendamentos
        // =========================
        [HttpGet]
        public async Task<IActionResult> ListarAgendamentos(int idClienteMaster)
        {
            var lista = await _context.Agendamentos
                .Include(a => a.Servico)
                .Include(a => a.Funcionario)
                .Include(a => a.Cliente)
                .Where(a => a.ClienteMasterId == idClienteMaster)
                .ToListAsync();

            return Ok(lista);
        }

        // =========================
        // Solicitar exclusão (WhatsApp)
        // =========================
        [HttpPost("solicitar-exclusao")]
        public async Task<IActionResult> SolicitarExclusao([FromBody] SolicitarExclusaoDto dto)
        {
            var enviado = await _agendamentoService.SolicitarExclusaoAsync(dto.AgendamentoId);

            return Ok(new
            {
                enviado,
                message = enviado ? "Mensagem enviada ao WhatsApp." : "Falha ao enviar mensagem."
            });
        }

        [HttpGet("status-exclusao")]
public async Task<IActionResult> StatusExclusao([FromQuery] Guid codigo)
{
    var solicitacao = await _context.SolicitacoesExclusao
        .Include(s => s.Agendamento)
        .ThenInclude(a => a.ClienteMaster)
        .FirstOrDefaultAsync(s => s.Codigo == codigo);

    if (solicitacao == null)
        return NotFound(new { status = "Não encontrada" });

    return Ok(new
    {
        status = solicitacao.Status,
        slug = solicitacao.Agendamento?.ClienteMaster?.Slug

    });
}
        // =========================
        // Links de exclusão únicos
        // =========================
[HttpGet("confirmar")]
public async Task<IActionResult> ConfirmarExclusao([FromQuery] Guid codigo)
{
    var solicitacao = await _context.SolicitacoesExclusao
        .Include(s => s.Agendamento)
        .ThenInclude(a => a.ClienteMaster)
        .Include(s => s.Agendamento)
        .ThenInclude(a => a.Cliente)
        .FirstOrDefaultAsync(s => s.Codigo == codigo);

    if (solicitacao == null)
        return NotFound(new { sucesso = false, mensagem = "Código não encontrado." });

    if (solicitacao.Status != "Pendente")
        return BadRequest(new { sucesso = false, mensagem = $"Essa solicitação já foi {solicitacao.Status}." });

    // 🔹 Armazena dados antes de excluir
    string slug = solicitacao.Agendamento?.ClienteMaster?.Slug ?? "";
    string telefone = solicitacao.Agendamento?.Cliente?.Telefone ?? "";
    string appKey = solicitacao.Agendamento?.ClienteMaster?.AppKey ?? "";
    string authKey = solicitacao.Agendamento?.ClienteMaster?.AuthKey ?? "";

    try
    {
        // Atualiza como confirmada e exclui agendamento
        await _agendamentoService.ProcessarConfirmacaoAsync(codigo, "SIM");

        // Envia mensagem via WhatsApp
        if (!string.IsNullOrEmpty(telefone))
        {
            string msg = "A Solicitação de exclusão foi concluída com sucesso!";
            await _whatsAppService.SendMessage(telefone, msg, appKey, authKey);
        }

        // Retorna sucesso para o front-end
        return Ok(new
        {
            sucesso = true,
            mensagem = "Exclusão confirmada com sucesso!",
            slug = slug,
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao confirmar exclusão");
        return StatusCode(500, new { sucesso = false, mensagem = "Erro interno ao processar exclusão." });
    }
}

    // =========================
    // Cancelar exclusão
    // =========================
    [HttpGet("cancelar")]
    public async Task<IActionResult> CancelarExclusao([FromQuery] Guid codigo)
    {
        var solicitacao = await _context.SolicitacoesExclusao
            .Include(s => s.Agendamento)
            .ThenInclude(a => a.ClienteMaster)
            .Include(s => s.Agendamento)
            .ThenInclude(a => a.Cliente)
            .FirstOrDefaultAsync(s => s.Codigo == codigo);

        if (solicitacao == null)
            return Content("<h2>Código não encontrado.</h2>", "text/html; charset=utf-8");

        if (solicitacao.Status != "Pendente")
            return Content($"<h2>Essa solicitação já foi {solicitacao.Status}.</h2>", "text/html; charset=utf-8");

        // Atualiza como negada
        await _agendamentoService.ProcessarConfirmacaoAsync(codigo, "NÃO");

        // Envia mensagem de cancelamento via WhatsApp
        if (solicitacao.Agendamento?.ClienteMaster != null)
        {
            string telefone = solicitacao.Agendamento.Cliente?.Telefone ?? "";
            string appKey = solicitacao.Agendamento.ClienteMaster.AppKey!;
            string authKey = solicitacao.Agendamento.ClienteMaster.AuthKey!;
            string msg = $"A solicitação de exclusão do seu agendamento foi cancelada com sucesso.";

            await _whatsAppService.SendMessage(telefone, msg, appKey, authKey);
        }

        // Redireciona para página principal + slug do cliente master
        string slug = solicitacao.Agendamento?.ClienteMaster?.Slug ?? "";
        string urlRedirect = $"https://marcaai-nine.vercel.app/{slug}";

        string html = $@"
            <html>
                <head>
                    <meta charset='utf-8'>
                    <meta http-equiv='refresh' content='5;url={urlRedirect}' />
                </head>
                <body>
                    <h2>Exclusão cancelada com sucesso!</h2>
                    <p>Você será redirecionado em alguns segundos...</p>
                </body>
            </html>";

        return Content(html, "text/html; charset=utf-8");
    }
        // =========================
        // Marcar agendamento como realizado manualmente
        // =========================
        [HttpPut("realizado/{id}")]
        public async Task<IActionResult> MarcarComoRealizado(int id)
        {
            try
            {
                var sucesso = await _agendamentoService.MarcarComoRealizado(id);
                if (!sucesso)
                    return NotFound("Agendamento não encontrado.");

                return Ok(new { Message = "Agendamento marcado como realizado com sucesso!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao marcar agendamento como realizado");
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }

        // =========================
        // Excluir agendamento manualmente
        // =========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirAgendamento(int id)
        {
            try
            {
                var agendamento = await _context.Agendamentos.FindAsync(id);
                if (agendamento == null)
                    return NotFound("Agendamento não encontrado.");

                _context.Agendamentos.Remove(agendamento);
                await _context.SaveChangesAsync();

                return Ok(new { Message = "Agendamento excluído com sucesso!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }
    }

    // =========================
    // DTOs
    // =========================
    public class AgendamentoDto
    {
        public int ClienteMasterId { get; set; }
        public int ClienteId { get; set; }
        public int ServicoId { get; set; }
        public int FuncionarioId { get; set; }
        public DateTime DataHora { get; set; }
        public string? Observacao { get; set; }
    }
}
