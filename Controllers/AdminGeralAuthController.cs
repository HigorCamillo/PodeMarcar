using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.Services;
using BCrypt.Net;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/admin-geral/auth")]
    public class AdminGeralAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        private readonly TokenService _token;

        public AdminGeralAuthController(ApplicationDbContext ctx, TokenService token)
        {
            _ctx = ctx;
            _token = token;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminGeralLoginRequest req)
        {
            var celular = req.Celular?.Trim();
            var senha = req.Senha ?? "";

            var admin = await _ctx.AdministradoresGerais
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Celular == celular);

            if (admin == null)
            {
                return Unauthorized(new { message = "Celular ou senha inválidos." });
            }

            if (!admin.Ativo)
            {
                return Unauthorized(new { message = "Sua conta está inativa." });
            }

            bool senhaConfere = SenhaHelper.Verificar(senha, admin.SenhaHash);

            if (!senhaConfere)
            {
                return Unauthorized(new { message = "Senha inválida." });
            }

            var result = new
            {
                token = _token.GenerateToken("AdministradorGeral", admin.Id, admin.Nome, admin.Celular),
                role = "AdministradorGeral",
                id = admin.Id,
                nome = admin.Nome,
                celular = admin.Celular,
                email = admin.Email
            };

            return Ok(result);
        }

        [HttpPost("criar-primeiro-admin")]
        public async Task<IActionResult> CriarPrimeiroAdmin([FromBody] CriarAdminRequest req)
        {
            // Verifica se já existe algum administrador
            var existeAdmin = await _ctx.AdministradoresGerais.AnyAsync();
            if (existeAdmin)
            {
                return BadRequest(new { message = "Já existe um administrador cadastrado." });
            }

            var admin = new AdministradorGeral
            {
                Nome = req.Nome,
                Email = req.Email,
                Celular = req.Celular,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha),
                AppKey = req.AppKey,
                AuthKey = req.AuthKey,
                Ativo = true,
                CriadoEm = DateTime.Now
            };

            _ctx.AdministradoresGerais.Add(admin);
            await _ctx.SaveChangesAsync();

            return Ok(new { message = "Administrador Geral criado com sucesso.", id = admin.Id });
        }
    }

    public class AdminGeralLoginRequest
    {
        public string Celular { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }

    public class CriarAdminRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string? AppKey { get; set; }
        public string? AuthKey { get; set; }
    }
}
