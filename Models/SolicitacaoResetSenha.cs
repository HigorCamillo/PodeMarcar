using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarcaAi.Backend.Models
{
    public class SolicitacaoResetSenha
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? ClienteMasterId { get; set; }
    public ClienteMaster? ClienteMaster { get; set; }

    public int? FuncionarioId { get; set; }
    public Funcionario? Funcionario { get; set; }

    [Required]
    public Guid Codigo { get; set; }

    [Required]
    public string Status { get; set; } = "Pendente";

    public DateTime CriadoEm { get; set; }
    public DateTime ExpiraEm { get; set; }
}

}
