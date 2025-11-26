using System.Collections.Generic;

namespace MarcaAi.Backend.DTOs
{
    public record ServicoWithFuncionariosDto(
        int Id,
        string Nome,
        decimal Preco,
        int DuracaoMinutos,
        bool Ativo,
        string? ImagemUrl,
        List<FuncionarioMinDto> Funcionarios
    );
}
