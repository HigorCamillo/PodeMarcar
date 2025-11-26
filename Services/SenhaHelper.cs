namespace MarcaAi.Backend.Services
{
    
    public static class SenhaHelper
    {
        public static string GerarHash(string senha)
        {
            return BCrypt.Net.BCrypt.HashPassword(senha);
        }

        public static bool Verificar(string senhaPura, string hashSalvo)
        {
            return BCrypt.Net.BCrypt.Verify(senhaPura, hashSalvo);
        }
    }
}
