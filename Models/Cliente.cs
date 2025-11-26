using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarcaAi.Backend.Models
{
    public class Cliente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Telefone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        public int ClienteMasterId { get; set; }
    }
}

