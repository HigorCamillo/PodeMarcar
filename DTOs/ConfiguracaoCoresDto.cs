using System.ComponentModel.DataAnnotations;

namespace MarcaAi.Backend.Dtos
{
    public class ConfiguracaoCoresDto
    {
        [Required]
        [MaxLength(7)]
        public string PrimaryColor { get; set; } = "#007bff";

        [Required]
        [MaxLength(7)]
        public string SecondaryColor { get; set; } = "#6c757d";

        [Required]
        [MaxLength(7)]
        public string TextColor { get; set; } = "#212529";

        [Required]
        [MaxLength(7)]
        public string TextColorLight { get; set; } = "#f8f9fa";

        [Required]
        [MaxLength(7)]
        public string ButtonColor { get; set; } = "#007bff";

        [Required]
        [MaxLength(7)]
        public string ButtonTextColor { get; set; } = "#f8f9fa";

        [Required]
        [MaxLength(7)]
        public string CardBackgroundColor { get; set; } = "#ffffff";

        [Required]
        [MaxLength(7)]
        public string CardTextColor { get; set; } = "#212529";

        // ⭐ NOVO CAMPO
        [Required]
        [MaxLength(7)]
        public string BackgroundColor { get; set; } = "#f5f6fa";
    }
}
