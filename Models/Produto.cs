using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarcaAi.Backend.Models
{
    public class Produto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        public string? ImagemUrl { get; set; } // Novo campo para a imagem

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Preco { get; set; }

        public int Estoque { get; set; }

        public int ClienteMasterId { get; set; }
        public ClienteMaster ClienteMaster { get; set; } = null!;
    }
}
