using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.DTOs.Auth;
using MarcaAi.Backend.Services;
using MarcaAi.Backend.Models; // Adicionado para SolicitacaoResetSenha
using BCrypt.Net; // Adicionado para BCrypt.Net.BCrypt

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly TokenService _token;
        private readonly WhatsAppService _whatsAppService;
        private readonly AgendamentoService _agendamentoService;

        public AuthController(ApplicationDbContext ctx, TokenService token, WhatsAppService whatsAppService, AgendamentoService agendamentoService)
        {
            _ctx = ctx;
            _token = token;
            _whatsAppService = whatsAppService;
            _agendamentoService = agendamentoService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var celular = req.Celular?.Trim();
            var senha = req.Senha ?? "";

            var master = await _ctx.ClientesMaster
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Celular == celular);

            if (master != null && !master.Ativo)
            {
                return Unauthorized(new { message = "Sua conta de Cliente Master está inativa. Por favor, acione o suporte." });
            }

            var funcionario = await _ctx.Funcionarios
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Celular == celular);

            if (funcionario != null)
            {
                var masterFuncionario = await _ctx.ClientesMaster.AsNoTracking().FirstOrDefaultAsync(cm => cm.Id == funcionario.ClienteMasterId);
                if (masterFuncionario != null && !masterFuncionario.Ativo)
                {
                    return Unauthorized(new { message = "A conta Master associada a este funcionário está inativa. Por favor, acione o suporte." });
                }
            }

            if (master == null && funcionario == null)
            {
                return Unauthorized(new { message = "Celular ou senha inválidos." });
            }

            bool senhaConfereMaster = master != null && SenhaHelper.Verificar(senha, master.SenhaHash);
            bool senhaConfereFuncionario = funcionario != null && SenhaHelper.Verificar(senha, funcionario.SenhaHash);

            if (!senhaConfereMaster && !senhaConfereFuncionario)
            {
                return Unauthorized(new { message = "Senha inválida." });
            }

            // 5. Atualização automática de agendamentos
            if (senhaConfereMaster && master!.AtualizacaoAutomatica)
            {
                await _agendamentoService.AtualizarAgendamentosAutomaticamente(master.Id);
            }
            else if (senhaConfereFuncionario)
            {
                var masterFuncionario = await _ctx.ClientesMaster.AsNoTracking().FirstOrDefaultAsync(cm => cm.Id == funcionario!.ClienteMasterId);
                if (masterFuncionario != null && masterFuncionario.AtualizacaoAutomatica)
                {
                    await _agendamentoService.AtualizarAgendamentosAutomaticamente(masterFuncionario.Id);
                }
            }

            var result = new
            {
                master = senhaConfereMaster ? new
                {
                    token = _token.GenerateToken("ClienteMaster", master.Id, master.Nome, master.Celular),
                    role = "ClienteMaster",
                    id = master.Id,
                    nome = master.Nome,
                    celular = master.Celular,
                    slug = master.Slug
                } : null,

                funcionario = senhaConfereFuncionario ? new
                {
                    token = _token.GenerateToken("Funcionario", funcionario.Id, funcionario.Nome, funcionario.Celular),
                    role = "Funcionario",
                    id = funcionario.Id,
                    nome = funcionario.Nome,
                    celular = funcionario.Celular,
                    slug = ""
                } : null
            };

            return Ok(result);
        }

        [HttpPost("verificar-celular")]
        public async Task<IActionResult> VerificarCelular([FromBody] VerificarCelularDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Celular))
                return BadRequest(new { message = "Celular é obrigatório." });

            var celular = dto.Celular.Trim();
            var usuarios = new List<string>();

            var master = await _ctx.ClientesMaster.AsNoTracking().FirstOrDefaultAsync(c => c.Celular == celular);
            if (master != null)
                usuarios.Add("ClienteMaster");

            var funcionario = await _ctx.Funcionarios.AsNoTracking().FirstOrDefaultAsync(f => f.Celular == celular);
            if (funcionario != null)
                usuarios.Add("Funcionario");

            return Ok(new { usuarios });
        }

       [HttpPost("solicitar-reset-senha")]
public async Task<IActionResult> SolicitarResetSenha([FromBody] SolicitarResetSenhaDto dto)
{
    var celular = dto.Celular.Trim();
    var tipoUsuario = dto.TipoUsuario;

    ClienteMaster? master = null;
    Funcionario? funcionario = null;

    if (tipoUsuario == "ClienteMaster")
    {
        master = await _ctx.ClientesMaster.FirstOrDefaultAsync(c => c.Celular == celular);
    }
    else if (tipoUsuario == "Funcionario")
    {
        funcionario = await _ctx.Funcionarios
            .Include(f => f.ClienteMaster) // Inclui o ClienteMaster para pegar AppKey/AuthKey
            .FirstOrDefaultAsync(f => f.Celular == celular);
    }
    else
    {
        return BadRequest("Tipo de usuário inválido.");
    }

    if (master == null && funcionario == null)
        return NotFound("Nenhum usuário encontrado com esse celular.");

    Guid codigo = Guid.NewGuid();
    var solicitacao = new SolicitacaoResetSenha
    {
        ClienteMasterId = master?.Id,
        FuncionarioId = funcionario?.Id,
        Codigo = codigo,
        CriadoEm = DateTime.UtcNow,
        ExpiraEm = DateTime.UtcNow.AddHours(1),
        Status = "Pendente"
    };

    _ctx.SolicitacoesResetSenha.Add(solicitacao);
    await _ctx.SaveChangesAsync();

    string resetLink = $"https://marca-nv7defgj7-higors-projects-e4b93cd4.vercel.app/resetar-senha?codigo={codigo}";
    string msgConf = $"*Reset de Senha*\n\nClique no link abaixo para redefinir sua senha:\n{resetLink}\n\nO link expira em 1 hora.";

    try
    {
        if (master != null)
        {
            // Envia mensagem usando o próprio ClienteMaster
            await _whatsAppService.SendMessage(master.Celular, msgConf, master.AppKey!, master.AuthKey!);
        }
        else if (funcionario != null)
        {
            // Pega AppKey/AuthKey do ClienteMaster dono do funcionário
            var clienteMaster = funcionario.ClienteMaster;

            if (clienteMaster == null || string.IsNullOrEmpty(clienteMaster.AppKey) || string.IsNullOrEmpty(clienteMaster.AuthKey))
                return BadRequest("O ClienteMaster associado ao funcionário não possui credenciais de WhatsApp configuradas.");

            await _whatsAppService.SendMessage(funcionario.Celular, msgConf, clienteMaster.AppKey, clienteMaster.AuthKey);
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Erro ao enviar mensagem pelo WhatsApp.", error = ex.Message });
    }

    return Ok(new { message = "Link de reset gerado e enviado via WhatsApp.", resetLink });
}



        [HttpGet("validar-reset-token")]
        public async Task<IActionResult> ValidarResetToken([FromQuery] Guid codigo)
        {
            var solicitacao = await _ctx.SolicitacoesResetSenha.FirstOrDefaultAsync(s => s.Codigo == codigo && s.Status == "Pendente" && s.ExpiraEm > DateTime.UtcNow);

            if (solicitacao == null)
            {
                return BadRequest(new { Message = "Token inválido ou expirado." });
            }

            return Ok(new { Message = "Token válido." });
        }

        [HttpPost("resetar-senha")]
        public async Task<IActionResult> ResetarSenha([FromBody] ResetarSenhaDto dto)
        {
            var solicitacao = await _ctx.SolicitacoesResetSenha
                .FirstOrDefaultAsync(s => s.Codigo == dto.Codigo && s.Status == "Pendente" && s.ExpiraEm > DateTime.UtcNow);

            if (solicitacao == null)
                return BadRequest("Token inválido ou expirado.");

            if (solicitacao.ClienteMasterId.HasValue)
            {
                var master = await _ctx.ClientesMaster.FindAsync(solicitacao.ClienteMasterId.Value);
                if (master == null) return NotFound("ClienteMaster não encontrado.");

                master.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
            }
            else if (solicitacao.FuncionarioId.HasValue)
            {
                var funcionario = await _ctx.Funcionarios.FindAsync(solicitacao.FuncionarioId.Value);
                if (funcionario == null) return NotFound("Funcionário não encontrado.");

                funcionario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);
            }

            solicitacao.Status = "Usado";
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Senha redefinida com sucesso." });
        }
    }

    public class SolicitarResetSenhaDto
{
    public string Celular { get; set; } = null!;
    public string TipoUsuario { get; set; } = null!; // "ClienteMaster" ou "Funcionario"
}

// DTO para o endpoint
public class VerificarCelularDto
{
    public string Celular { get; set; }
}
    public class ResetarSenhaDto
    {
        public Guid Codigo { get; set; }
        public string NovaSenha { get; set; }
        public string ConfirmacaoNovaSenha { get; set; }
    }
}
