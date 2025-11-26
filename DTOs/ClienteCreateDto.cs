using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MarcaAi.Backend.DTOs
{
    public class ClienteCreateDto
    {
        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public string Telefone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        [ValidateNever]
        public int ClienteMasterId { get; set; }
    }
}

