using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.ComponentModel.DataAnnotations;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.DTOs;
using MarcaAi.Backend.Models;
using BCrypt.Net;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FuncionariosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public FuncionariosController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ✅ POST: Upload de imagem do funcionário
        /// <summary>
        /// Faz o upload da imagem de um funcionário e atualiza o campo ImagemUrl.
        /// </summary>
        /// <param name="id">ID do funcionário</param>
        /// <param name="dto">Arquivo enviado (multipart/form-data)</param>
        /// <returns>URL da imagem atualizada</returns>
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
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "images", "employees");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileExtension = Path.GetExtension(dto.File.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                var relativePath = $"/images/employees/{uniqueFileName}";
                funcionario.ImagemUrl = relativePath;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    funcionario.Id,
                    funcionario.Nome,
                    ImagemUrl = relativePath,
                    message = "Imagem atualizada com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao fazer upload da imagem.", error = ex.Message });
            }
        }

        // ✅ GET: obter funcionário por ID
        /// <summary>
        /// Obtém um funcionário e seus serviços associados.
        /// </summary>
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
                funcionario.ImagemUrl,
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
        /// <summary>
        /// Lista todos os funcionários e seus serviços.  
        /// É possível filtrar pelo ID do cliente master.
        /// </summary>
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
                f.ImagemUrl,
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
        /// <summary>
        /// Cria um novo funcionário.
        /// </summary>
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
                ImagemUrl = dto.ImagemUrl,
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
                funcionario.ImagemUrl
            });
        }

        // ✅ PUT: atualizar funcionário
        /// <summary>
        /// Atualiza os dados de um funcionário, incluindo serviços associados.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FuncionarioUpdateDto dto)
        {
            var funcionario = await _db.Funcionarios.FindAsync(id);
            if (funcionario == null)
                return NotFound(new { message = "Funcionário não encontrado." });

            funcionario.Nome = dto.Nome;
            funcionario.Celular = dto.Celular;
            funcionario.ImagemUrl = dto.ImagemUrl;

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
        /// <summary>
        /// Exclui um funcionário do sistema.
        /// </summary>
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
    }

    // ✅ DTOs auxiliares para Swagger e uploads
    public class UploadImageDto
    {
        /// <example>arquivo.jpg</example>
        [Required]
        public IFormFile File { get; set; }
    }

    public class FuncionarioUpdateDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public string? Senha { get; set; }
        public List<int>? ServicosIds { get; set; }
    }
}
