using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.DTOs; // Presumo que DTOs serão necessários para o CRUD completo
using System.IO;
using Microsoft.AspNetCore.Hosting; // Para IWebHostEnvironment
using Microsoft.AspNetCore.Http; // Para IFormFile

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env; // Para o caminho do wwwroot

        public ProdutosController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // =========================
        // CRUD de Produtos
        // =========================

        [HttpPost]
        public async Task<IActionResult> CriarProduto([FromForm] ProdutoDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var produto = new Produto
            {
                Nome = dto.Nome,
                Preco = dto.Preco,
                Estoque = dto.Estoque,
                ClienteMasterId = dto.ClienteMasterId,
                ImagemUrl = null // Será preenchido após o upload
            };

            if (dto.Imagem != null)
            {
                produto.ImagemUrl = await SalvarImagem(dto.Imagem);
            }

            _db.Produtos.Add(produto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = produto.Id }, produto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> AtualizarProduto(int id, [FromForm] ProdutoDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var produto = await _db.Produtos.FindAsync(id);
            if (produto == null) return NotFound();

            produto.Nome = dto.Nome;
            produto.Preco = dto.Preco;
            produto.Estoque = dto.Estoque;

            if (dto.Imagem != null)
            {
                // Remove a imagem antiga se existir
                if (!string.IsNullOrEmpty(produto.ImagemUrl))
                {
                    ExcluirImagem(produto.ImagemUrl);
                }
                produto.ImagemUrl = await SalvarImagem(dto.Imagem);
            }

            _db.Produtos.Update(produto);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> ExcluirProduto(int id)
        {
            var produto = await _db.Produtos.FindAsync(id);
            if (produto == null) return NotFound();

            // Remove a imagem
            if (!string.IsNullOrEmpty(produto.ImagemUrl))
            {
                ExcluirImagem(produto.ImagemUrl);
            }

            _db.Produtos.Remove(produto);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // =========================
        // Métodos de Suporte
        // =========================

        private async Task<string> SalvarImagem(IFormFile imagem)
        {
            var extensao = Path.GetExtension(imagem.FileName);
            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
            var caminho = Path.Combine(_env.WebRootPath, "images", "produtos", nomeArquivo);

            // Cria o diretório se não existir
            var diretorio = Path.GetDirectoryName(caminho);
            if (!Directory.Exists(diretorio))
            {
                Directory.CreateDirectory(diretorio);
            }

            using (var stream = new FileStream(caminho, FileMode.Create))
            {
                await imagem.CopyToAsync(stream);
            }

            return $"/images/produtos/{nomeArquivo}";
        }

        private void ExcluirImagem(string url)
        {
            var nomeArquivo = Path.GetFileName(url);
            var caminho = Path.Combine(_env.WebRootPath, "images", "produtos", nomeArquivo);
            if (System.IO.File.Exists(caminho))
            {
                System.IO.File.Delete(caminho);
            }
        }

        // =========================
        // Endpoints Existentes
        // =========================

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int idClienteMaster)
        {
            var produtos = await _db.Produtos
                .Where(p => idClienteMaster == 0 || p.ClienteMasterId == idClienteMaster)
                .ToListAsync();

            return Ok(produtos);
        }

        public class MovEstoqueDto
        {
            public int IdClienteMaster { get; set; }
            public int IdProduto { get; set; }
            public int Quantidade { get; set; }
            public string Tipo { get; set; } = "entrada"; // 'entrada'|'saida'
        }

        [HttpPost("movimentar")]
        public async Task<IActionResult> Movimentar([FromBody] MovEstoqueDto dto)
        {
            var prod = await _db.Produtos.FindAsync(dto.IdProduto);
            if (prod is null) return NotFound("Produto não encontrado");

            if (dto.Tipo == "entrada")
                prod.Estoque += dto.Quantidade;
            else if (dto.Tipo == "saida")
                prod.Estoque -= dto.Quantidade;
            else
                return BadRequest("Tipo inválido (use 'entrada' ou 'saida').");

            _db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                ClienteMasterId = dto.IdClienteMaster,
                ProdutoId = dto.IdProduto,
                Quantidade = dto.Quantidade,
                Tipo = dto.Tipo,
                Data = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok(new { prod.Id, prod.Estoque });
        }

        [HttpGet("vendas")]
        public async Task<IActionResult> ListarVendas([FromQuery] int idClienteMaster)
        {
            var vendas = await _db.VendasProdutos
                .Include(v => v.Produto)
                .Include(v => v.Cliente)
                .Where(v => v.ClienteMasterId == idClienteMaster)
                .OrderByDescending(v => v.DataVenda)
                .ToListAsync();

            return Ok(vendas);
        }

        public class VendaDto
        {
            public int IdClienteMaster { get; set; }
            public int IdCliente { get; set; }
            public int IdProduto { get; set; }
            public int Quantidade { get; set; }
            public decimal PrecoUnitario { get; set; }
        }

        [HttpPost("vender")]
        public async Task<IActionResult> Vender([FromBody] VendaDto dto)
        {
            var prod = await _db.Produtos.FindAsync(dto.IdProduto);
            if (prod is null) return NotFound("Produto não encontrado");

            if (prod.Estoque < dto.Quantidade)
                return BadRequest("Estoque insuficiente");

            prod.Estoque -= dto.Quantidade;

            _db.VendasProdutos.Add(new VendaProduto
            {
                ClienteMasterId = dto.IdClienteMaster,
                ClienteId = dto.IdCliente,
                ProdutoId = dto.IdProduto,
                Quantidade = dto.Quantidade,
                PrecoUnitario = dto.PrecoUnitario,
                DataVenda = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return Ok(new { prod.Id, prod.Estoque });
        }
    }
}
