using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.Services;
using BCrypt.Net;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/admin-geral")]
    public class AdminGeralController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly WhatsAppService _whatsAppService;

        public AdminGeralController(ApplicationDbContext ctx, WhatsAppService whatsAppService)
        {
            _ctx = ctx;
            _whatsAppService = whatsAppService;
        }

        // Dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalClientes = await _ctx.ClientesMaster.CountAsync();
            var clientesAtivos = await _ctx.ClientesMaster.CountAsync(c => c.Ativo);
            
            // Valor faturado mensal (soma das mensalidades dos clientes ativos)
            var valorFaturadoMensal = await _ctx.ClientesMaster
                .Where(c => c.Ativo)
                .SumAsync(c => c.ValorMensalidade);

            // Clientes próximos ao vencimento
            var hoje = DateTime.Now.Date;
            var clientesProximosVencimento = await _ctx.ClientesMaster
                .Where(c => c.Ativo && c.DataVencimento.HasValue)
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Celular,
                    c.DataVencimento,
                    c.DiasAvisoVencimento,
                    DiasRestantes = (c.DataVencimento.Value - hoje).Days
                })
                .ToListAsync();

            var proximosVencimento = clientesProximosVencimento
                .Where(c => c.DiasRestantes <= c.DiasAvisoVencimento && c.DiasRestantes >= 0)
                .OrderBy(c => c.DiasRestantes)
                .ToList();

            return Ok(new
            {
                totalClientes,
                clientesAtivos,
                valorFaturadoMensal,
                qtdClientesProximosVencimento = proximosVencimento.Count,
                clientesProximosVencimento = proximosVencimento
            });
        }

        // Obter Administrador Geral por ID (Perfil)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAdminGeral(int id)
        {
            var admin = await _ctx.AdministradoresGerais
                .Where(a => a.Id == id)
                .Select(a => new
                {
                    a.Id,
                    a.Nome,
                    a.Email,
                    a.Celular,
                    a.AppKey,
                    a.AuthKey
                })
                .FirstOrDefaultAsync();

            if (admin == null)
            {
                return NotFound(new { message = "Administrador Geral não encontrado." });
            }

            return Ok(admin);
        }

        // Atualizar Administrador Geral por ID (Perfil)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdminGeral(int id, [FromBody] UpdateAdminGeralRequest req)
        {
            var admin = await _ctx.AdministradoresGerais.FindAsync(id);
            if (admin == null)
            {
                return NotFound(new { message = "Administrador Geral não encontrado." });
            }

            // Verifica se o celular já está em uso por outro administrador
            if (req.Celular != admin.Celular)
            {
                var existeCelular = await _ctx.AdministradoresGerais.AnyAsync(a => a.Celular == req.Celular && a.Id != id);
                if (existeCelular)
                {
                    return BadRequest(new { message = "Já existe outro administrador com este celular." });
                }
            }

            admin.Nome = req.Nome;
            admin.Email = req.Email;
            admin.Celular = req.Celular;
            admin.AppKey = req.AppKey;
            admin.AuthKey = req.AuthKey;

            if (!string.IsNullOrEmpty(req.NovaSenha))
            {
                admin.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha);
            }

            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Configurações atualizadas com sucesso." });
        }

        // Listar todos os clientes master
        [HttpGet("clientes-master")]
        public async Task<IActionResult> GetClientesMaster([FromQuery] bool? ativo = null)
        {
            var query = _ctx.ClientesMaster.AsQueryable();

            if (ativo.HasValue)
            {
                query = query.Where(c => c.Ativo == ativo.Value);
            }

            var clientes = await query
                .OrderByDescending(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Celular,
                    c.Slug,
                    c.Ativo,
                    c.ValorMensalidade,
                    c.DataVencimento,
                    c.DiasAvisoVencimento,
                    c.UsaApiLembrete,
                    c.AppKey,
                    c.AuthKey,
                    c.TempoLembrete,
                    c.AtualizacaoAutomatica
                })
                .ToListAsync();

            return Ok(clientes);
        }

        // Obter cliente master por ID
        [HttpGet("clientes-master/{id}")]
        public async Task<IActionResult> GetClienteMaster(int id)
        {
            var cliente = await _ctx.ClientesMaster
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Celular,
                    c.Slug,
                    c.Ativo,
                    c.ValorMensalidade,
                    c.DataVencimento,
                    c.DiasAvisoVencimento,
                    c.UsaApiLembrete,
                    c.AppKey,
                    c.AuthKey,
                    c.TempoLembrete,
                    c.AtualizacaoAutomatica
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
            {
                return NotFound(new { message = "Cliente Master não encontrado." });
            }

            return Ok(cliente);
        }

        // Criar novo cliente master
        [HttpPost("clientes-master")]
        public async Task<IActionResult> CreateClienteMaster([FromBody] CreateClienteMasterRequest req)
        {
            // Verifica se já existe cliente com o mesmo celular
            var existeCelular = await _ctx.ClientesMaster.AnyAsync(c => c.Celular == req.Celular);
            if (existeCelular)
            {
                return BadRequest(new { message = "Já existe um cliente com este celular." });
            }

            var cliente = new ClienteMaster
            {
                Nome = req.Nome,
                Email = req.Email,
                Celular = req.Celular,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha),
                Ativo = true,
                ValorMensalidade = req.ValorMensalidade,
                DataVencimento = req.DataVencimento,
                DiasAvisoVencimento = req.DiasAvisoVencimento,
                UsaApiLembrete = req.UsaApiLembrete,
                AppKey = req.AppKey,
                AuthKey = req.AuthKey,
                TempoLembrete = req.TempoLembrete,
                AtualizacaoAutomatica = req.AtualizacaoAutomatica
            };

            cliente.GenerateAndSetSlug();

            _ctx.ClientesMaster.Add(cliente);
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Cliente Master criado com sucesso.", id = cliente.Id });
        }

        // Atualizar cliente master
        [HttpPut("clientes-master/{id}")]
        public async Task<IActionResult> UpdateClienteMaster(int id, [FromBody] UpdateClienteMasterRequest req)
        {
            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
            {
                return NotFound(new { message = "Cliente Master não encontrado." });
            }

            // Verifica se o celular já está em uso por outro cliente
            if (req.Celular != cliente.Celular)
            {
                var existeCelular = await _ctx.ClientesMaster.AnyAsync(c => c.Celular == req.Celular && c.Id != id);
                if (existeCelular)
                {
                    return BadRequest(new { message = "Já existe um cliente com este celular." });
                }
            }

            cliente.Nome = req.Nome;
            cliente.Email = req.Email;
            cliente.Celular = req.Celular;
            cliente.ValorMensalidade = req.ValorMensalidade;
            cliente.DataVencimento = req.DataVencimento;
            cliente.DiasAvisoVencimento = req.DiasAvisoVencimento;
            cliente.UsaApiLembrete = req.UsaApiLembrete;
            cliente.AppKey = req.AppKey;
            cliente.AuthKey = req.AuthKey;
            cliente.TempoLembrete = req.TempoLembrete;
            cliente.AtualizacaoAutomatica = req.AtualizacaoAutomatica;

            if (!string.IsNullOrEmpty(req.NovaSenha))
            {
                cliente.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha);
            }

            cliente.GenerateAndSetSlug();

            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Cliente Master atualizado com sucesso." });
        }

        // Ativar cliente master
        [HttpPatch("clientes-master/{id}/ativar")]
        public async Task<IActionResult> AtivarClienteMaster(int id)
        {
            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
            {
                return NotFound(new { message = "Cliente Master não encontrado." });
            }

            cliente.Ativo = true;
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Cliente Master ativado com sucesso." });
        }

        // Desativar cliente master
        [HttpPatch("clientes-master/{id}/desativar")]
        public async Task<IActionResult> DesativarClienteMaster(int id)
        {
            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
            {
                return NotFound(new { message = "Cliente Master não encontrado." });
            }

            cliente.Ativo = false;
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Cliente Master desativado com sucesso." });
        }

        // Listar clientes próximos ao vencimento
        [HttpGet("clientes-master/proximos-vencimento")]
        public async Task<IActionResult> GetClientesProximosVencimento()
        {
            var hoje = DateTime.Now.Date;
            
            var clientesProximosVencimento = await _ctx.ClientesMaster
                .Where(c => c.Ativo && c.DataVencimento.HasValue)
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.Email,
                    c.Celular,
                    c.DataVencimento,
                    c.DiasAvisoVencimento,
                    c.ValorMensalidade,
                    DiasRestantes = (c.DataVencimento.Value - hoje).Days
                })
                .ToListAsync();

            var proximosVencimento = clientesProximosVencimento
                .Where(c => c.DiasRestantes <= c.DiasAvisoVencimento && c.DiasRestantes >= 0)
                .OrderBy(c => c.DiasRestantes)
                .ToList();

            return Ok(proximosVencimento);
        }

        // Renovar mensalidade do cliente master
        [HttpPost("clientes-master/{id}/renovar")]
        public async Task<IActionResult> RenovarMensalidade(int id, [FromBody] RenovarMensalidadeRequest req)
        {
            var cliente = await _ctx.ClientesMaster.FindAsync(id);
            if (cliente == null)
            {
                return NotFound(new { message = "Cliente Master não encontrado." });
            }

            // Atualiza a data de vencimento
            var novaDataVencimento = req.NovaDataVencimento ?? DateTime.UtcNow.AddMonths(1);
            cliente.DataVencimento = novaDataVencimento;

            // Atualiza valor da mensalidade se fornecido
            if (req.NovoValorMensalidade.HasValue)
            {
                cliente.ValorMensalidade = req.NovoValorMensalidade.Value;
            }

            await _ctx.SaveChangesAsync();

            // Envia mensagem de confirmação via WhatsApp
            if (req.EnviarMensagemWhatsApp)
            {
                var admin = await _ctx.AdministradoresGerais.FirstOrDefaultAsync(a => a.Ativo);
                
                if (admin != null && !string.IsNullOrEmpty(admin.AppKey) && !string.IsNullOrEmpty(admin.AuthKey))
                {
                    var mensagem = $"*Renovação de Mensalidade*\n\n" +
                                   $"Olá {cliente.Nome}!\n\n" +
                                   $"Sua mensalidade foi renovada com sucesso.\n\n" +
                                   $"*Valor:* R$ {cliente.ValorMensalidade:N2}\n" +
                                   $"*Nova data de vencimento:* {novaDataVencimento:dd/MM/yyyy}\n\n" +
                                   $"Obrigado por continuar conosco!";

                    try
                    {
                        await _whatsAppService.SendMessage(cliente.Celular, mensagem, admin.AppKey, admin.AuthKey);
                    }
                    catch (Exception ex)
                    {
                        // Log do erro, mas não falha a operação
                        Console.WriteLine($"Erro ao enviar mensagem WhatsApp: {ex.Message}");
                    }
                }
            }

            return Ok(new { 
                message = "Mensalidade renovada com sucesso.", 
                novaDataVencimento = novaDataVencimento,
                valorMensalidade = cliente.ValorMensalidade
            });
        }
    }

    public class CreateClienteMasterRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public decimal ValorMensalidade { get; set; }
        public DateTime? DataVencimento { get; set; }
        public int DiasAvisoVencimento { get; set; } = 7;
        public bool UsaApiLembrete { get; set; } = false;
        public string? AppKey { get; set; }
        public string? AuthKey { get; set; }
        public int? TempoLembrete { get; set; }
        public bool AtualizacaoAutomatica { get; set; } = false;
    }

    public class UpdateClienteMasterRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string? NovaSenha { get; set; }
        public decimal ValorMensalidade { get; set; }
        public DateTime? DataVencimento { get; set; }
        public int DiasAvisoVencimento { get; set; } = 7;
        public bool UsaApiLembrete { get; set; } = false;
        public string? AppKey { get; set; }
        public string? AuthKey { get; set; }
        public int? TempoLembrete { get; set; }
        public bool AtualizacaoAutomatica { get; set; } = false;
    }

    public class UpdateAdminGeralRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string? AppKey { get; set; }
        public string? AuthKey { get; set; }
        public string? NovaSenha { get; set; }
    }

    public class RenovarMensalidadeRequest
    {
        public DateTime? NovaDataVencimento { get; set; }
        public decimal? NovoValorMensalidade { get; set; }
        public bool EnviarMensagemWhatsApp { get; set; } = true;
    }
}
