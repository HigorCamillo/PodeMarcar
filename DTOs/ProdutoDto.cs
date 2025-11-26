using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MarcaAi.Backend.DTOs
{
    public class ProdutoDto
    {
        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        public decimal Preco { get; set; }

        public int Estoque { get; set; }

        [Required]
        public int ClienteMasterId { get; set; }

        public IFormFile? Imagem { get; set; }
    }
}
