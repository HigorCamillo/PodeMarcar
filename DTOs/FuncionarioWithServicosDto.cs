using System.Collections.Generic;

namespace MarcaAi.Backend.DTOs
{
    public record FuncionarioWithServicosDto(
        int Id,
        string Nome,
        
        string Celular,
        int ClienteMasterId,
        List<ServicoMinDto> Servicos
    );
}
