using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace MarcaAi.Backend.Services
{
    public class AdministradorGeralService
    {
        private readonly ApplicationDbContext _context;
        private readonly TokenService _tokenService;
        private readonly WhatsAppService _whatsAppService;

        public AdministradorGeralService(ApplicationDbContext context, TokenService tokenService, WhatsAppService whatsAppService)
        {
            _context = context;
            _tokenService = tokenService;
            _whatsAppService = whatsAppService;
        }

        // 1. Inicialização
        public async Task InitializeAdministradorGeralAsync(string email, string senha)
        {
            if (!await _context.AdministradoresGerais.AnyAsync())
            {
                var admin = new AdministradorGeral
                {
                    Email = email,
                    SenhaHash = SenhaHelper.GerarHash(senha),
                    DiasAvisoVencimento = 7,
                    ValorMensalidadePadrao = 99.90m,
                    AppKey = "",
                    AuthKey = ""
                };
                _context.AdministradoresGerais.Add(admin);
                await _context.SaveChangesAsync();
            }
        }

        // 2. Autenticação
        public async Task<AdministradorGeralLoginResponse> AuthenticateAsync(AdministradorGeralLoginRequest request)
{
    var admin = await _context.AdministradoresGerais.FirstOrDefaultAsync(d => d.Email == request.Email);

    if (admin == null || !SenhaHelper.Verificar(request.Senha, admin.SenhaHash))
        return null;

    // Chamada corrigida para GenerateToken
    var token = _tokenService.GenerateToken(
        role: "AdministradorGeral",
        userId: admin.Id,
        name: admin.Nome,
        celular: admin.Celular
    );

    var configDto = new AdministradorGeralConfigDto
    {
        Email = admin.Email,
        DiasAvisoVencimento = admin.DiasAvisoVencimento, // Certifique-se de que essa propriedade exista
        AppKey = admin.AppKey,
        AuthKey = admin.AuthKey,
        ValorMensalidadePadrao = admin.ValorMensalidadePadrao // Certifique-se de que essa propriedade exista
    };

    return new AdministradorGeralLoginResponse 
    { 
        Token = token, 
        Config = configDto 
    };
}


        // 3. Dashboard
        public async Task<AdministradorGeralDashboardDto> GetDashboardDataAsync()
        {
            var totalClientes = await _context.ClientesMaster.CountAsync();
            var clientesAtivos = await _context.ClientesMaster.CountAsync(c => c.Ativo);

            var faturamentoMensal = await _context.ClientesMaster
                .Where(c => c.Ativo)
                .SumAsync(c => c.ValorMensalidade);

            var diasAviso = (await _context.AdministradoresGerais.FirstOrDefaultAsync())?.DiasAvisoVencimento ?? 7;
            var dataLimite = DateTime.Now.AddDays(diasAviso);

            var proximosVencimentos = await _context.ClientesMaster
                .Where(c => c.Ativo && c.DataVencimento.HasValue && c.DataVencimento.Value <= dataLimite)
                .OrderBy(c => c.DataVencimento)
                .Select(c => new ClienteMasterMinDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Celular = c.Celular,
                    Ativo = c.Ativo,
                    DataVencimento = c.DataVencimento,
                    ValorMensalidade = c.ValorMensalidade
                })
                .ToListAsync();

            return new AdministradorGeralDashboardDto
            {
                TotalClientesMaster = totalClientes,
                ClientesMasterAtivos = clientesAtivos,
                FaturamentoMensalEstimado = faturamentoMensal,
                ClientesProximosVencimento = proximosVencimentos.Count,
                ProximosVencimentos = proximosVencimentos
            };
        }

        // 4. Gestão de Clientes Master
        public async Task<List<ClienteMasterMinDto>> GetAllClientesMasterAsync()
        {
            return await _context.ClientesMaster
                .Select(c => new ClienteMasterMinDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Email = c.Email,
                    Celular = c.Celular,
                    Ativo = c.Ativo,
                    DataVencimento = c.DataVencimento,
                    ValorMensalidade = c.ValorMensalidade
                })
                .ToListAsync();
        }

        public async Task<ClienteMaster> GetClienteMasterByIdAsync(int id)
        {
            return await _context.ClientesMaster.FindAsync(id);
        }

        public async Task<ClienteMaster> CreateClienteMasterAsync(ClienteMasterUpdateDto dto)
        {
            var cliente = new ClienteMaster
            {
                Nome = dto.Nome,
                Email = dto.Email,
                Celular = dto.Celular,
                Ativo = dto.Ativo,
                DataVencimento = dto.DataVencimento,
                ValorMensalidade = dto.ValorMensalidade,
                SenhaHash = SenhaHelper.GerarHash("senha_padrao") // Alterar para fluxo seguro
            };
            cliente.GenerateAndSetSlug();

            _context.ClientesMaster.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        public async Task<bool> UpdateClienteMasterAsync(int id, ClienteMasterUpdateDto dto)
        {
            var cliente = await _context.ClientesMaster.FindAsync(id);
            if (cliente == null) return false;

            cliente.Nome = dto.Nome;
            cliente.Email = dto.Email;
            cliente.Celular = dto.Celular;
            cliente.Ativo = dto.Ativo;
            cliente.DataVencimento = dto.DataVencimento;
            cliente.ValorMensalidade = dto.ValorMensalidade;

            _context.ClientesMaster.Update(cliente);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleAtivoClienteMasterAsync(int id)
        {
            var cliente = await _context.ClientesMaster.FindAsync(id);
            if (cliente == null) return false;

            cliente.Ativo = !cliente.Ativo;
            _context.ClientesMaster.Update(cliente);
            await _context.SaveChangesAsync();
            return true;
        }

        // 5. Renovação de Mensalidade
        public async Task<bool> RenovarMensalidadeAsync(int clienteMasterId, int meses = 1)
        {
            var cliente = await _context.ClientesMaster.FindAsync(clienteMasterId);
            if (cliente == null) return false;

            var dataBase = cliente.DataVencimento.HasValue && cliente.DataVencimento.Value > DateTime.Now
                ? cliente.DataVencimento.Value
                : DateTime.Now;

            cliente.DataVencimento = dataBase.AddMonths(meses);
            cliente.Ativo = true;

            _context.ClientesMaster.Update(cliente);
            await _context.SaveChangesAsync();

            await ScheduleRenovationMessageAsync(cliente);

            return true;
        }

        // Método auxiliar para envio/agendamento de mensagens
        private async Task ScheduleRenovationMessageAsync(ClienteMaster cliente)
        {
            var adminConfig = await GetAdministradorGeralConfigAsync();
            if (adminConfig == null || string.IsNullOrEmpty(adminConfig.AppKey) || string.IsNullOrEmpty(adminConfig.AuthKey))
                return;

            var dataVencimento = cliente.DataVencimento.GetValueOrDefault();
            var dataAviso = dataVencimento.AddDays(-adminConfig.DiasAvisoVencimento);

            var mensagemRenovacao = $"Olá {cliente.Nome}, sua mensalidade foi renovada com sucesso! Seu novo vencimento é {dataVencimento:dd/MM/yyyy}. Agradecemos a preferência!";
            await _whatsAppService.SendMessage(cliente.Celular, mensagemRenovacao, adminConfig.AppKey, adminConfig.AuthKey);

            var mensagemAviso = $"🚨 Lembrete: Sua mensalidade do sistema {cliente.Nome} vencerá em {dataVencimento:dd/MM/yyyy}. Não se esqueça de renovar para manter seu acesso ativo!";
            await _whatsAppService.ScheduleReminder(cliente.Celular, mensagemAviso, adminConfig.AppKey, adminConfig.AuthKey, dataAviso);
        }

        // 6. Configurações do Administrador Geral
        public async Task<bool> UpdateConfigAsync(AdministradorGeralConfigDto dto)
        {
            var admin = await _context.AdministradoresGerais.FirstOrDefaultAsync();
            if (admin == null) return false;

            admin.DiasAvisoVencimento = dto.DiasAvisoVencimento;
            admin.AppKey = dto.AppKey;
            admin.AuthKey = dto.AuthKey;
            admin.ValorMensalidadePadrao = dto.ValorMensalidadePadrao;

            _context.AdministradoresGerais.Update(admin);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AdministradorGeral> GetAdministradorGeralConfigAsync()
        {
            return await _context.AdministradoresGerais.FirstOrDefaultAsync();
        }
    }
}
