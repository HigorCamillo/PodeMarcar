namespace MarcaAi.Backend.DTOs
{
    public class DashboardDto
    {
        public int AgendamentosHoje { get; set; }
        public int AgendamentosMesRealizados { get; set; }
        public int AgendamentosPendentes { get; set; }
        public decimal LucroDia { get; set; }
        public decimal LucroMes { get; set; }
    }
}
