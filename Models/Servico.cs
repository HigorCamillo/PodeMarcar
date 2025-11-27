using System.Collections.Generic;

namespace MarcaAi.Backend.Models
{
    public class Servico
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int DuracaoMinutos { get; set; }
        public bool Ativo { get; set; }
        public int ClienteMasterId { get; set; }
        public ClienteMaster ClienteMaster { get; set; } = null!;

        // Propriedades para armazenar a imagem no banco de dados
        public byte[]? Imagem { get; set; }
        public string? ContentType { get; set; }

        public ICollection<FuncionarioServico> FuncionariosServicos { get; set; } = new List<FuncionarioServico>();
    }
}
