using System;

namespace MarcaAi.Backend.Models
{
    public class Agendamento
    {
        public int Id { get; set; }

        public int ClienteMasterId { get; set; }
        public ClienteMaster ClienteMaster { get; set; } = null!;

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public int ServicoId { get; set; }
        public Servico Servico { get; set; } = null!;

        public int FuncionarioId { get; set; }
        public Funcionario Funcionario { get; set; } = null!;

        // ✅ Agora armazenamos DataHora diretamente
        public DateTime DataHora { get; set; }

        // ✅ Campo novo para observações do cliente
        public string? Observacao { get; set; }

        // Continua existindo, caso já esteja sendo usado
        public bool Realizado { get; set; } = false;
    }
}
