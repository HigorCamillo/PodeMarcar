namespace MarcaAi.Backend.DTOs
{
    public class SolicitarExclusaoDto
    {
        public int AgendamentoId { get; set; }
    }

    public class ConfirmacaoWhatsAppDto
    {
        public Guid Codigo { get; set; }
        public string Resposta { get; set; } // SIM / NAO
    }
}