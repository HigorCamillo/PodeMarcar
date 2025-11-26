using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarcaAi.Backend.Models
{
    public class AdministradorGeral
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Informações básicas
        [Required, MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Celular { get; set; } = string.Empty;

        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        // Configurações do sistema
        public int DiasAvisoVencimento { get; set; } = 7;

        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorMensalidadePadrao { get; set; } = 99.90m;

        // Integrações
        [MaxLength(200)]
        public string? AppKey { get; set; }

        [MaxLength(200)]
        public string? AuthKey { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
