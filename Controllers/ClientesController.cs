using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Listar todos os clientes de um ClienteMaster
        [HttpGet]
        public async Task<IActionResult> GetAll(int idClienteMaster)
        {
            var clientes = await _context.Clientes
                .Where(c => c.ClienteMasterId == idClienteMaster)
                .ToListAsync();
            return Ok(clientes);
        }

        // ✅ Obter cliente por ID

        // ✅ Buscar clientes por nome (autocomplete)
        [HttpGet("search")]
public async Task<IActionResult> Search(int idClienteMaster, string query)
{
    if (string.IsNullOrWhiteSpace(query))
        return Ok(new List<Cliente>());

    query = query.ToLower();

    var clientes = await _context.Clientes
        .Where(c =>
            c.ClienteMasterId == idClienteMaster &&
            c.Nome.ToLower().Contains(query)
        )
        .OrderBy(c => c.Nome)
        .Take(20)
        .Select(c => new
        {
            c.Id,
            c.Nome,
            c.Telefone,
            c.Email
        })
        .ToListAsync();

    return Ok(clientes);
}


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound("Cliente não encontrado.");
            }
            return Ok(cliente);
        }

 // ✅ GET api/Clientes/by-phone?phone=...&idClienteMaster=...
[HttpGet("by-phone")]
public async Task<IActionResult> GetByPhone(
    [FromQuery] string phone,
    [FromQuery] int idClienteMaster)
{
    if (string.IsNullOrWhiteSpace(phone))
        return BadRequest("Telefone é obrigatório.");

    // remove tudo que não é dígito
    var digits = new string(phone.Where(char.IsDigit).ToArray());

    if (digits.Length == 0)
        return BadRequest("Telefone inválido.");

    // ✅ força incluir DDI 55 se não estiver presente
    if (!digits.StartsWith("55"))
        digits = "55" + digits;

    // pesquisa normal
    var cliente = _context.Clientes
        .Where(c => c.ClienteMasterId == idClienteMaster)
        .AsEnumerable() // comparação em memória
        .FirstOrDefault(c =>
        {
            var apenasDigitos = new string(c.Telefone.Where(char.IsDigit).ToArray());
            if (!apenasDigitos.StartsWith("55"))
                apenasDigitos = "55" + apenasDigitos;

            return apenasDigitos == digits;
        });

    if (cliente == null)
        return Ok(null); // ✅ não retorna erro

    return Ok(cliente);
}



 [HttpPost]
public async Task<ActionResult<Cliente>> Create(ClienteCreateDto clienteDto)
{
    var cliente = new Cliente
    {
        Nome = clienteDto.Nome,
        Telefone = clienteDto.Telefone,
        Email = clienteDto.Email,
        ClienteMasterId = clienteDto.ClienteMasterId
    };

    _context.Clientes.Add(cliente);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetByPhone), new { phone = cliente.Telefone, idClienteMaster = cliente.ClienteMasterId }, cliente);
}

        // ✅ Atualizar cliente
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, ClienteCreateDto clienteDto)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound("Cliente não encontrado.");
            }

            cliente.Nome = clienteDto.Nome;
            cliente.Telefone = clienteDto.Telefone;
            cliente.Email = clienteDto.Email;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cliente atualizado com sucesso!" });
        }

        // ✅ Excluir cliente
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound("Cliente não encontrado.");
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Cliente excluído com sucesso!" });
        }

    }
}

