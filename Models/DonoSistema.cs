using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarcaAi.Backend.Models
{
    public class DonoSistema
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string SenhaHash { get; set; }

        // Configurações de Vencimento
        public DateTime? DataVencimento { get; set; }
        public int DiasAvisoVencimento { get; set; } = 7; // Padrão de 7 dias

        // Configurações do Menuia (WhatsApp)
        [MaxLength(255)]
        public string AppKey { get; set; }

        [MaxLength(255)]
        public string AuthKey { get; set; }

        // Valor da mensalidade padrão para novos clientes (opcional, para dashboard)
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorMensalidadePadrao { get; set; } = 0.00m;
    }
}
