using System;
using System.ComponentModel.DataAnnotations;

namespace MarcaAi.Backend.DTOs
{
    public class ClienteMasterDadosUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Phone]
        [MaxLength(20)]
        public string Celular { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DataVencimento { get; set; }

        [Range(0, double.MaxValue)]
public decimal? ValorMensalidade { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
