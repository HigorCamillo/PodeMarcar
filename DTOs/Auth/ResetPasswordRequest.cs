namespace MarcaAi.Backend.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        public string Celular { get; set; }
        public string ResetToken { get; set; }
        public string NovaSenha { get; set; }
    }
}
