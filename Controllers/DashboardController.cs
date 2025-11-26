using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.DTOs;
using MarcaAi.Backend.Models;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====================================================================
        // DASHBOARD PRINCIPAL (Admin e Funcionário)
        // ====================================================================
        [HttpGet("principal/{idClienteMaster}")]
        public async Task<IActionResult> GetDashboardPrincipal(int idClienteMaster, [FromQuery] int? idFuncionario)
        {
            var hoje = DateTime.Today;
            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var fimMes = inicioMes.AddMonths(1);

            // Filtro base
            var queryBase = _context.Agendamentos
                .Include(a => a.Servico)
                .Where(a => a.ClienteMasterId == idClienteMaster);

            // Se funcionário foi passado, filtra por ele
            if (idFuncionario.HasValue)
            {
                queryBase = queryBase.Where(a => a.FuncionarioId == idFuncionario.Value);
            }

            // 1. Agendamentos Hoje
            var agendamentosHoje = await queryBase
                .Where(a => a.DataHora.Date == hoje)
                .CountAsync();

            // 2. Agendamentos no mês realizados
            var agendamentosMesRealizados = await queryBase
                .Where(a => a.DataHora >= inicioMes && a.DataHora < fimMes && a.Realizado)
                .CountAsync();

            // 3. Pendentes
            var agendamentosPendentes = await queryBase
                .Where(a => !a.Realizado)
                .CountAsync();

            // 4. Lucro do Dia
            var lucroDia = await queryBase
                .Where(a => a.DataHora.Date == hoje && a.Realizado)
                .SumAsync(a => a.Servico.Preco);

            // 5. Lucro do Mês
            var lucroMes = await queryBase
                .Where(a => a.DataHora >= inicioMes && a.DataHora < fimMes && a.Realizado)
                .SumAsync(a => a.Servico.Preco);

            var dashboardData = new DashboardDto
            {
                AgendamentosHoje = agendamentosHoje,
                AgendamentosMesRealizados = agendamentosMesRealizados,
                AgendamentosPendentes = agendamentosPendentes,
                LucroDia = lucroDia,
                LucroMes = lucroMes
            };

            return Ok(dashboardData);
        }


        // ====================================================================
        // ANALYTICS ADMINISTRATIVO (Total Funcionários / Clientes / Ganhos)
        // ====================================================================
        [HttpGet("admin-analytics/{idClienteMaster}")]
        public async Task<IActionResult> GetAdminAnalytics(int idClienteMaster)
        {
            var anoAtual = DateTime.Today.Year;

            // 1. Total de Funcionários
            var totalFuncionarios = await _context.Funcionarios
                .Where(f => f.ClienteMasterId == idClienteMaster)
                .CountAsync();

            // 2. Total de Clientes
            var totalClientes = await _context.Clientes
                .Where(c => c.ClienteMasterId == idClienteMaster)
                .CountAsync();

            // 3. Ganhos do ano agrupados por mês
            var ganhosPorMes = await _context.Agendamentos
                .Include(a => a.Servico)
                .Where(a => a.ClienteMasterId == idClienteMaster &&
                            a.Realizado &&
                            a.DataHora.Year == anoAtual)
                .GroupBy(a => a.DataHora.Month)
                .Select(g => new GanhoMensalDto
                {
                    Mes = g.Key,
                    GanhoTotal = g.Sum(a => Convert.ToDecimal(a.Servico.Preco))
                })
                .OrderBy(g => g.Mes)
                .ToListAsync();

            // 4. Preenche meses sem dados
            var ganhosAnuais = new List<GanhoMensalDto>();
            for (int mes = 1; mes <= 12; mes++)
            {
                var ganho = ganhosPorMes.FirstOrDefault(g => g.Mes == mes);

                ganhosAnuais.Add(new GanhoMensalDto
                {
                    Mes = mes,
                    NomeMes = new DateTime(anoAtual, mes, 1).ToString("MMM"),
                    GanhoTotal = ganho?.GanhoTotal ?? 0
                });
            }

            var analyticsData = new AdminAnalyticsDto
            {
                TotalFuncionarios = totalFuncionarios,
                TotalClientes = totalClientes,
                GanhosAnuais = ganhosAnuais
            };

            return Ok(analyticsData);
        }
    }
}
