using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MarcaAi.Backend.Models
{
    public class ConfiguracaoCores
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Chave estrangeira
        public int ClienteMasterId { get; set; }

        [JsonIgnore] // evita loop no JSON
        public ClienteMaster ClienteMaster { get; set; }


        // ===== Cores Principais =====
        [Required, MaxLength(7)]
        public string PrimaryColor { get; set; } = "#007bff";

        [Required, MaxLength(7)]
        public string SecondaryColor { get; set; } = "#6c757d";


        // ===== Cores de Texto =====
        [Required, MaxLength(7)]
        public string TextColor { get; set; } = "#212529";

        [Required, MaxLength(7)]
        public string TextColorLight { get; set; } = "#f8f9fa";


        // ===== Cores de Botões =====
        [Required, MaxLength(7)]
        public string ButtonColor { get; set; } = "#007bff";

        [Required, MaxLength(7)]
        public string ButtonTextColor { get; set; } = "#f8f9fa";


        // ===== Cores de Card/Fundo =====
        [Required, MaxLength(7)]
        public string CardBackgroundColor { get; set; } = "#ffffff";

        [Required, MaxLength(7)]
        public string CardTextColor { get; set; } = "#212529";

        // ===== Cor de Fundo da Página =====
        [Required, MaxLength(7)]
        public string BackgroundColor { get; set; } = "#f5f5f5";
    }
}
