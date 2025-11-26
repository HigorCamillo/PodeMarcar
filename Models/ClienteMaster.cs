using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarcaAi.Backend.Models
{
    public class ClienteMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required] public string Nome { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        // novo
        [Required, MaxLength(20)]
        public string Celular { get; set; } = string.Empty;

        // novo - guarda o hash
        [Required]
        public string SenhaHash { get; set; } = string.Empty;

        [Required] public string Slug { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        public bool UsaApiLembrete { get; set; } = false;
        public string? AppKey { get; set; }
        public string? AuthKey { get; set; }
        public int? TempoLembrete { get; set; }

        // Propriedade de navegação para a configuração de cores (1:1)
        public ConfiguracaoCores ConfiguracaoCores { get; set; }
        public bool AtualizacaoAutomatica { get; set; } = false;

        // Campos de controle de mensalidade
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorMensalidade { get; set; } = 0;

        public DateTime? DataVencimento { get; set; }

        public int DiasAvisoVencimento { get; set; } = 7;

        public void GenerateAndSetSlug()
        {
            Slug = Services.SlugService.GenerateSlug(Nome);
        }
    }
}
