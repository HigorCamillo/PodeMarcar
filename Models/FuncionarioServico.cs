using System.Text.Json.Serialization;

namespace MarcaAi.Backend.Models
{
    public class FuncionarioServico
    {
        public int FuncionarioId { get; set; }

        // ✅ Pode deixar SEM JsonIgnore, porque queremos mostrar o funcionário
        public Funcionario Funcionario { get; set; } = null!;

        public int ServicoId { get; set; }

        [JsonIgnore] // ✅ Ignoramos APENAS o lado do Serviço
        public Servico Servico { get; set; } = null!;
    }
}
