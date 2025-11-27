using MarcaAi.Backend.Data;
using MarcaAi.Backend.DTOs;
using MarcaAi.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using BCrypt.Net;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FuncionariosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public FuncionariosController(ApplicationDbContext db)
        {
            _db = db;
        }

        // ✅ POST: Upload de imagem do funcionário
        [HttpPost("{id}/upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(int id, [FromForm] UploadImageDto dto)
        {
            var funcionario = await _db.Funcionarios.FindAsync(id);
            if (funcionario == null)
                return NotFound(new { message = "Funcionário não encontrado." });

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "Nenhum arquivo enviado." });

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await dto.File.CopyToAsync(memoryStream);
                    funcionario.Imagem = memoryStream.ToArray();
                    funcionario.ContentType = dto.File.ContentType;
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    funcionario.Id,
                    funcionario.Nome,
                    message = "Imagem atualizada com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao fazer upload da imagem.", error = ex.Message });
            }
        }

        // ✅ GET: obter funcionário por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var funcionario = await _db.Funcionarios
                .Include(f => f.FuncionariosServicos)
                    .ThenInclude(fs => fs.Servico)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (funcionario == null)
                return NotFound(new { message = "Funcionário não encontrado." });

            var dto = new FuncionarioWithServicosDto(
                funcionario.Id,
                funcionario.Nome,
                funcionario.Celular,
                funcionario.ClienteMasterId,
                funcionario.FuncionariosServicos.Select(fs => new ServicoMinDto(
                    fs.Servico.Id,
                    fs.Servico.Nome,
                    fs.Servico.Preco,
                    fs.Servico.DuracaoMinutos
                )).ToList()
            );

            return Ok(dto);
        }

        // ✅ GET: lista funcionários (com filtro opcional por ClienteMasterId)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int idClienteMaster)
        {
            var funcionarios = await _db.Funcionarios
                .Where(f => idClienteMaster == 0 || f.ClienteMasterId == idClienteMaster)
                .Include(f => f.FuncionariosServicos)
                    .ThenInclude(fs => fs.Servico)
                .ToListAsync();

            var dto = funcionarios.Select(f => new FuncionarioWithServicosDto(
                f.Id,
                f.Nome,
                f.Celular,
                f.ClienteMasterId,
                f.FuncionariosServicos.Select(fs => new ServicoMinDto(
                    fs.Servico.Id,
                    fs.Servico.Nome,
                    fs.Servico.Preco,
                    fs.Servico.DuracaoMinutos
                )).ToList()
            ));

            return Ok(dto);
        }

        // ✅ POST: cria funcionário com senha criptografada
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FuncionarioCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Dados inválidos." });

            if (_db.Funcionarios.Any(f => f.Celular == dto.Celular))
                return Conflict(new { message = "Esse celular já está cadastrado." });

            var funcionario = new Funcionario
            {
                Nome = dto.Nome,
                Celular = dto.Celular,
                ClienteMasterId = dto.ClienteMasterId,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha)
            };

            await _db.Funcionarios.AddAsync(funcionario);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                funcionario.Id,
                funcionario.Nome,
                funcionario.Celular,
                funcionario.ClienteMasterId,
            });
        }

        // ✅ PUT: atualizar funcionário
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FuncionarioUpdateDto dto)
        {
            var funcionario = await _db.Funcionarios.FindAsync(id);
            if (funcionario == null)
                return NotFound(new { message = "Funcionário não encontrado." });

            funcionario.Nome = dto.Nome;
            funcionario.Celular = dto.Celular;

            if (!string.IsNullOrWhiteSpace(dto.Senha))
                funcionario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

            // Atualiza serviços
            var currentServices = await _db.FuncionariosServicos
                .Where(fs => fs.FuncionarioId == id)
                .ToListAsync();

            _db.FuncionariosServicos.RemoveRange(currentServices);

            if (dto.ServicosIds?.Any() == true)
            {
                var newLinks = dto.ServicosIds.Select(servicoId => new FuncionarioServico
                {
                    FuncionarioId = id,
                    ServicoId = servicoId
                });

                await _db.FuncionariosServicos.AddRangeAsync(newLinks);
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Funcionário atualizado com sucesso!" });
        }

        // ✅ DELETE: excluir funcionário
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var funcionario = await _db.Funcionarios.FindAsync(id);
            if (funcionario == null)
                return NotFound(new { message = "Funcionário não encontrado." });

            _db.Funcionarios.Remove(funcionario);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Funcionário excluído com sucesso!" });
        }

        // ✅ GET: obter imagem do funcionário
        [HttpGet("{id}/image")]
        public async Task<IActionResult> GetImage(int id)
        {
            var funcionario = await _db.Funcionarios.FindAsync(id);

            if (funcionario == null || funcionario.Imagem == null || string.IsNullOrEmpty(funcionario.ContentType))
            {
                return NotFound();
            }

            return File(funcionario.Imagem, funcionario.ContentType);
        }
    }

    // DTOs auxiliares (mantidos para evitar erros de compilação)
    public class UploadImageDto
    {
        [Required]
        public IFormFile File { get; set; }
    }

    public class FuncionarioUpdateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string? Senha { get; set; }
        public List<int>? ServicosIds { get; set; }
    }
}
