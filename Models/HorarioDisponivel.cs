namespace MarcaAi.Backend.Models
{
    public class HorarioDisponivel
    {
        public int Id { get; set; }
        public DateTime Data { get; set; }   // sem timezone
        public string Hora { get; set; } = string.Empty;
        public bool Disponivel { get; set; } = true;

        public int DisponibilidadeId { get; set; }
        public Disponibilidade Disponibilidade { get; set; } = null!;
    }
}
