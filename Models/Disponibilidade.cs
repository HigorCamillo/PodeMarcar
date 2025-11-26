using System;

namespace MarcaAi.Backend.Models
{
    public class Disponibilidade
    {
        public int Id { get; set; }

        public int FuncionarioId { get; set; }
        public Funcionario Funcionario { get; set; } = null!;

        // Se for uma disponibilidade recorrente (ex.: toda segunda-feira)
        public DayOfWeek? DiaSemana { get; set; }

        // Se for uma data específica (ex.: 30/11/2025 das 11h às 14h)
        public DateTime? DataEspecifica { get; set; }

        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }

        // "Padrao" = recorrente, "Excecao" = data específica
        public string Tipo { get; set; } = "Padrao";

        // Campos para horário de almoço
        public bool Almoço { get; set; } = false;
        public TimeSpan? DtInicioAlmoco { get; set; }
        public TimeSpan? DtFimAlmoco { get; set; }
    }
}
