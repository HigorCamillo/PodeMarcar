namespace MarcaAi.Backend.DTOs
{
    public class FuncionarioCreateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public int ClienteMasterId { get; set; }
        public string? ImagemUrl { get; set; }  // ✅ opcional
    }
}
