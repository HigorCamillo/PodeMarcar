namespace MarcaAi.Backend.Models
{
    public class Bloqueio
    {
        public int Id { get; set; }

        public int ClienteMasterId { get; set; }
        public ClienteMaster ClienteMaster { get; set; } = null!;

        public int FuncionarioId { get; set; }
        public Funcionario Funcionario { get; set; } = null!;

        // Dia específico do bloqueio
        public DateTime Data { get; set; }

        // Intervalo no mesmo dia
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
    }
}
