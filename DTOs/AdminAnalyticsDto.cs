using System.Collections.Generic;

namespace MarcaAi.Backend.DTOs
{
    public class GanhoMensalDto
    {
        public int Mes { get; set; }
        public string NomeMes { get; set; } = string.Empty;
        public decimal GanhoTotal { get; set; }
    }

    public class AdminAnalyticsDto
{
    public int TotalFuncionarios { get; set; }
    public int TotalClientes { get; set; }
    public List<GanhoMensalDto> GanhosAnuais { get; set; }
}

}
