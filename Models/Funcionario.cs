using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarcaAi.Backend.Models
{
    public class Funcionario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Celular { get; set; } = string.Empty;

        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        // Propriedades para armazenar a imagem no banco de dados
        public byte[]? Imagem { get; set; }
        public string? ContentType { get; set; }

        [Required]
        public int ClienteMasterId { get; set; }
        public ClienteMaster ClienteMaster { get; set; } = null!;
        public List<FuncionarioServico>? FuncionariosServicos { get; set; }

        //public List<FuncionarioServico> FuncionariosServicos { get; set; } = new();
    }
}
