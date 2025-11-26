using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ServicosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ServicosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/Servicos/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetServicoById(int id)
        {
            var servico = await _context.Servicos
                .Include(s => s.FuncionariosServicos)
                    .ThenInclude(fs => fs.Funcionario)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (servico == null)
                return NotFound("Serviço não encontrado.");

            return Ok(servico);
        }

        // GET: /api/Servicos/search?query=&idClienteMaster=
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] int idClienteMaster, [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<Servico>());

            var servicos = await _context.Servicos
                .Where(s => s.ClienteMasterId == idClienteMaster && s.Nome.Contains(query) && s.Ativo)
                .Select(s => new { s.Id, s.Nome, s.DuracaoMinutos })
                .Take(10)
                .ToListAsync();

            return Ok(servicos);
        }

        // GET: /api/Servicos/admin?idClienteMaster=
        [HttpGet("admin")]
        public async Task<IActionResult> GetServicosAdmin([FromQuery] int idClienteMaster)
        {
            var servicos = await _context.Servicos
                .Include(s => s.FuncionariosServicos)
                    .ThenInclude(fs => fs.Funcionario)
                .Where(s => s.ClienteMasterId == idClienteMaster)
                .Select(s => new
                {
                    s.Id,
                    s.Nome,
                    s.Preco,
                    s.DuracaoMinutos,
                    s.ImagemUrl,
                    s.Ativo,
                    Funcionarios = s.FuncionariosServicos.Select(fs => new
                    {
                        fs.Funcionario.Id,
                        fs.Funcionario.Nome,
                        fs.Funcionario.ImagemUrl
                    }).ToList()
                })
                .ToListAsync();

            return Ok(servicos);
        }

        // GET: /api/Servicos?IdClienteMaster=
        [HttpGet]
        public async Task<IActionResult> GetServicos([FromQuery] int idClienteMaster)
        {
            var servicos = await _context.Servicos
                .Include(s => s.FuncionariosServicos)
                    .ThenInclude(fs => fs.Funcionario)
                .Where(s => s.ClienteMasterId == idClienteMaster && s.Ativo)
                .Select(s => new
                {
                    s.Id,
                    s.Nome,
                    s.Preco,
                    s.DuracaoMinutos,
                    s.ImagemUrl,
                    Funcionarios = s.FuncionariosServicos.Select(fs => new
                    {
                        fs.Funcionario.Id,
                        fs.Funcionario.Nome,
                        fs.Funcionario.ImagemUrl
                    }).ToList()
                })
                .ToListAsync();

            return Ok(servicos);
        }

        // POST: /api/Servicos
        [HttpPost]
        public async Task<IActionResult> CreateServico([FromBody] ServicoDto dto)
        {
            var servico = new Servico
            {
                Nome = dto.Nome,
                Preco = dto.Preco,
                DuracaoMinutos = dto.DuracaoMinutos,
                ImagemUrl = dto.ImagemUrl,
                Ativo = dto.Ativo,
                ClienteMasterId = dto.ClienteMasterId
            };

            _context.Servicos.Add(servico);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetServicoById), new { id = servico.Id }, servico);
        }

        // PUT: /api/Servicos/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateServico(int id, [FromBody] ServicoDto dto)
        {
            var servico = await _context.Servicos.FindAsync(id);
            if (servico == null)
                return NotFound("Serviço não encontrado.");

            servico.Nome = dto.Nome;
            servico.Preco = dto.Preco;
            servico.DuracaoMinutos = dto.DuracaoMinutos;
            servico.ImagemUrl = dto.ImagemUrl;
            servico.Ativo = dto.Ativo;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Serviço atualizado com sucesso!" });
        }

        // DELETE: /api/Servicos/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServico(int id)
        {
            var servico = await _context.Servicos.FindAsync(id);
            if (servico == null)
                return NotFound("Serviço não encontrado.");

            _context.Servicos.Remove(servico);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Serviço excluído com sucesso!" });
        }

        [HttpGet("funcionario/{idFuncionario}")]
public async Task<IActionResult> GetServicosByFuncionario(int idFuncionario)
{
    var funcionario = await _context.Funcionarios
        .Include(f => f.FuncionariosServicos)
            .ThenInclude(fs => fs.Servico)
        .FirstOrDefaultAsync(f => f.Id == idFuncionario);

    if (funcionario == null)
        return NotFound(new { message = "Funcionário não encontrado." });

    var servicos = funcionario.FuncionariosServicos
        .Where(fs => fs.Servico.Ativo)
        .Select(fs => new
        {
            fs.Servico.Id,
            fs.Servico.Nome,
            fs.Servico.Preco,
            fs.Servico.DuracaoMinutos,
            fs.Servico.ImagemUrl
        })
        .ToList();

    return Ok(servicos);
}
        // POST: /api/Servicos/{id}/upload-image
        [HttpPost("{id}/upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(int id, [FromForm] UploadImageServiceDto dto)
        {
            var servico = await _context.Servicos.FindAsync(id);
            if (servico == null)
                return NotFound(new { message = "Serviço não encontrado." });

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest(new { message = "Nenhum arquivo enviado." });

            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "services");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileExtension = Path.GetExtension(dto.File.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.File.CopyToAsync(stream);
                }

                var relativePath = $"/images/services/{uniqueFileName}";
                servico.ImagemUrl = relativePath;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    servico.Id,
                    servico.Nome,
                    ImagemUrl = relativePath,
                    message = "Imagem do serviço atualizada com sucesso!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao fazer upload da imagem.", error = ex.Message });
            }
        }
    }

    public class ServicoDto
    {
        public string Nome { get; set; } = string.Empty;
        public decimal Preco { get; set; }
        public int DuracaoMinutos { get; set; }
        public string? ImagemUrl { get; set; }
        public bool Ativo { get; set; }
        public int ClienteMasterId { get; set; }
    }

    public class UploadImageServiceDto
    {
        [Required]
        public IFormFile File { get; set; }
    }
}
