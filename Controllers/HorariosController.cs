using MarcaAi.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.DTOs;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HorariosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HorariosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ GET: Obter todas as disponibilidades e bloqueios de um funcionário
        [HttpGet("funcionario/{idFuncionario}")]
        public async Task<IActionResult> GetHorariosFuncionario(int idFuncionario)
        {
            var disponibilidades = await _context.Disponibilidades
                .Where(d => d.FuncionarioId == idFuncionario)
                .ToListAsync();

            var bloqueios = await _context.Bloqueios
                .Where(b => b.FuncionarioId == idFuncionario)
                .ToListAsync();

            return Ok(new { Disponibilidades = disponibilidades, Bloqueios = bloqueios });
        }

        // ✅ POST: Criar nova Disponibilidade (Horário Padrão ou Extra)
        [HttpPost("disponibilidade")]
        public async Task<IActionResult> CreateDisponibilidade([FromBody] DisponibilidadeDto dto)
        {
            var disponibilidade = new Disponibilidade
            {
                FuncionarioId = dto.FuncionarioId,
                DiaSemana = dto.DiaSemana,
                DataEspecifica = dto.DataEspecifica,
                HoraInicio = dto.HoraInicio,
                HoraFim = dto.HoraFim,
                Tipo = dto.Tipo,
                Almoço = dto.Almoço,
                DtInicioAlmoco = dto.DtInicioAlmoco,
                DtFimAlmoco = dto.DtFimAlmoco
            };

            _context.Disponibilidades.Add(disponibilidade);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHorariosFuncionario), new { idFuncionario = disponibilidade.FuncionarioId }, disponibilidade);
        }

        // ✅ PUT: Atualizar Disponibilidade
        [HttpPut("disponibilidade/{id}")]
        public async Task<IActionResult> UpdateDisponibilidade(int id, [FromBody] DisponibilidadeDto dto)
        {
            var disponibilidade = await _context.Disponibilidades.FindAsync(id);
            if (disponibilidade == null)
            {
                return NotFound("Disponibilidade não encontrada.");
            }

            disponibilidade.DiaSemana = dto.DiaSemana;
            disponibilidade.DataEspecifica = dto.DataEspecifica;
            disponibilidade.HoraInicio = dto.HoraInicio;
            disponibilidade.HoraFim = dto.HoraFim;
            disponibilidade.Tipo = dto.Tipo;
            disponibilidade.Almoço = dto.Almoço;
            disponibilidade.DtInicioAlmoco = dto.DtInicioAlmoco;
            disponibilidade.DtFimAlmoco = dto.DtFimAlmoco;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Disponibilidade atualizada com sucesso!" });
        }

        // ✅ DELETE: Excluir Disponibilidade
        [HttpDelete("disponibilidade/{id}")]
        public async Task<IActionResult> DeleteDisponibilidade(int id)
        {
            var disponibilidade = await _context.Disponibilidades.FindAsync(id);
            if (disponibilidade == null)
            {
                return NotFound("Disponibilidade não encontrada.");
            }

            _context.Disponibilidades.Remove(disponibilidade);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Disponibilidade excluída com sucesso!" });
        }

// ✅ POST: Criar novo Bloqueio
[HttpPost("bloqueio")]
public async Task<IActionResult> CreateBloqueio([FromBody] BloqueioDto dto)
{
    try
    {
        // ⚙️ Garante que a Data seja tratada como UTC
        var dataUtc = DateTime.SpecifyKind(dto.Data, DateTimeKind.Utc);

        var bloqueio = new Bloqueio
        {
            ClienteMasterId = dto.ClienteMasterId,
            FuncionarioId = dto.FuncionarioId,
            Data = dataUtc,
            HoraInicio = dto.HoraInicio,
            HoraFim = dto.HoraFim
        };

        _context.Bloqueios.Add(bloqueio);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHorariosFuncionario), new { idFuncionario = bloqueio.FuncionarioId }, bloqueio);
    }
    catch (Exception ex)
    {
        // Loga o erro detalhado no console e retorna mensagem amigável
        Console.WriteLine($"Erro ao criar bloqueio: {ex}");
        return StatusCode(500, new { message = "Erro ao criar bloqueio.", detalhe = ex.Message });
    }
}

        // ✅ DELETE: Excluir Bloqueio
        [HttpDelete("bloqueio/{id}")]
        public async Task<IActionResult> DeleteBloqueio(int id)
        {
            var bloqueio = await _context.Bloqueios.FindAsync(id);
            if (bloqueio == null)
            {
                return NotFound("Bloqueio não encontrado.");
            }

            _context.Bloqueios.Remove(bloqueio);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Bloqueio excluído com sucesso!" });
        }

        [HttpGet("disponiveis")]
        public async Task<IActionResult> GetDisponiveis(
            int idClienteMaster,
            int idFuncionario,
            DateTime from,
            DateTime to,
            int? idServico = null)
        {
            try
            {
                from = DateTime.SpecifyKind(from, DateTimeKind.Unspecified);
                to = DateTime.SpecifyKind(to, DateTimeKind.Unspecified);

                // ✅ Agora a duração só existe se idServico for informado
                if (!idServico.HasValue)
                {
                    return BadRequest("É necessário informar o idServico para calcular a duração correta.");
                }

                var servico = await _context.Servicos
                    .FirstOrDefaultAsync(s =>
                        s.Id == idServico.Value &&
                        s.ClienteMasterId == idClienteMaster);

                if (servico == null)
                {
                    return NotFound("Serviço não encontrado ou não pertence ao cliente master informado.");
                }

                int duracaoServico = servico.DuracaoMinutos;
                var horariosDisponiveis = new List<object>();

                for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
                {
                    var disponibilidades = await _context.Disponibilidades
                        .Where(d =>
                            d.FuncionarioId == idFuncionario &&
                            (
                                (d.DiaSemana.HasValue && d.DiaSemana.Value == date.DayOfWeek) ||
                                (d.DataEspecifica.HasValue && d.DataEspecifica.Value.Date == date)
                            )
                        )
                        .ToListAsync();

                    if (!disponibilidades.Any())
                        continue;

                    foreach (var disp in disponibilidades)
                    {
                        var horaInicio = date.Add(disp.HoraInicio);
                        var horaFim = date.Add(disp.HoraFim);

                        for (var hora = horaInicio; hora.AddMinutes(duracaoServico) <= horaFim; hora = hora.AddMinutes(duracaoServico))
                        {
                            var horaFimServico = hora.AddMinutes(duracaoServico);

                            // ✅ Validação de Almoço
                            if (disp.Almoço && disp.DtInicioAlmoco.HasValue && disp.DtFimAlmoco.HasValue)
                            {
                                var inicioAlmoco = date.Add(disp.DtInicioAlmoco.Value);
                                var fimAlmoco = date.Add(disp.DtFimAlmoco.Value);

                                // Verifica se o horário do serviço se sobrepõe ao horário de almoço
                                if ((hora < fimAlmoco && horaFimServico > inicioAlmoco))
                                {
                                    continue; // Pula este horário se houver conflito com o almoço
                                }
                            }

                            bool conflito = await _context.Agendamentos
                                .Include(a => a.Servico)
                                .AnyAsync(a =>
                                    a.FuncionarioId == idFuncionario &&
                                    a.DataHora < horaFimServico &&
                                    a.DataHora.AddMinutes(a.Servico.DuracaoMinutos) > hora
                                );

                            if (!conflito)
                            {
                                horariosDisponiveis.Add(new
                                {
                                    Data = date.ToString("yyyy-MM-dd"),
                                    Hora = hora.ToString("HH:mm")
                                });
                            }
                        }
                    }
                }

                return Ok(horariosDisponiveis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno: {ex.Message}");
            }
        }
    }
}
