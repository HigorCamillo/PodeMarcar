using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.Dtos;

namespace MarcaAi.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracaoCoresController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;

        public ConfiguracaoCoresController(ApplicationDbContext ctx)
        {
            _ctx = ctx;
        }

        // GET: api/ConfiguracaoCores/{clienteMasterId}
        [HttpGet("{clienteMasterId:int}")]
        public async Task<IActionResult> GetByClienteMasterId(int clienteMasterId)
        {
            var cores = await _ctx.ConfiguracoesCores
                .AsNoTracking()
                .FirstOrDefaultAsync(cc => cc.ClienteMasterId == clienteMasterId);

            if (cores == null)
            {
                var defaultCores = new ConfiguracaoCores
                {
                    ClienteMasterId = clienteMasterId,
                    PrimaryColor = "#007bff",
                    SecondaryColor = "#6c757d",
                    TextColor = "#212529",
                    TextColorLight = "#f8f9fa",
                    ButtonColor = "#007bff",
                    ButtonTextColor = "#f8f9fa",
                    CardBackgroundColor = "#ffffff",
                    CardTextColor = "#212529",
                    BackgroundColor = "#f5f6fa" // ⭐ ADICIONADO
                };

                try
                {
                    _ctx.ConfiguracoesCores.Add(defaultCores);
                    await _ctx.SaveChangesAsync();
                }
                catch (Exception)
                {
                    // Ignora erros e retorna só o padrão
                }

                return Ok(defaultCores);
            }

            return Ok(cores);
        }

        // PUT: api/ConfiguracaoCores/{clienteMasterId}
        [HttpPut("{clienteMasterId:int}")]
        public async Task<IActionResult> UpdateConfiguracaoCores(int clienteMasterId, [FromBody] ConfiguracaoCoresDto dto)
        {
            var cores = await _ctx.ConfiguracoesCores
                .FirstOrDefaultAsync(cc => cc.ClienteMasterId == clienteMasterId);

            if (cores == null)
            {
                cores = new ConfiguracaoCores
                {
                    ClienteMasterId = clienteMasterId,
                    PrimaryColor = dto.PrimaryColor,
                    SecondaryColor = dto.SecondaryColor,
                    TextColor = dto.TextColor,
                    TextColorLight = dto.TextColorLight,
                    ButtonColor = dto.ButtonColor,
                    ButtonTextColor = dto.ButtonTextColor,
                    CardBackgroundColor = dto.CardBackgroundColor,
                    CardTextColor = dto.CardTextColor,
                    BackgroundColor = dto.BackgroundColor // ⭐ ADICIONADO
                };

                _ctx.ConfiguracoesCores.Add(cores);
            }
            else
            {
                cores.PrimaryColor = dto.PrimaryColor;
                cores.SecondaryColor = dto.SecondaryColor;
                cores.TextColor = dto.TextColor;
                cores.TextColorLight = dto.TextColorLight;
                cores.ButtonColor = dto.ButtonColor;
                cores.ButtonTextColor = dto.ButtonTextColor;
                cores.CardBackgroundColor = dto.CardBackgroundColor;
                cores.CardTextColor = dto.CardTextColor;
                cores.BackgroundColor = dto.BackgroundColor; // ⭐ ADICIONADO

                _ctx.ConfiguracoesCores.Update(cores);
            }

            await _ctx.SaveChangesAsync();

            return Ok(new { Message = "Configurações de cores atualizadas com sucesso!" });
        }
    }
}
