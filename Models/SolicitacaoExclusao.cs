using System;

namespace MarcaAi.Backend.Models
{
    public class SolicitacaoExclusao
    {
        public int Id { get; set; }

        public Guid Codigo { get; set; }

        public DateTime CriadoEm { get; set; }

        public string Status { get; set; }

        public int? AgendamentoId { get; set; }
        public Agendamento? Agendamento { get; set; }
    }
}
