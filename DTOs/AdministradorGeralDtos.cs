namespace MarcaAi.Backend.DTOs
{
    // DTO para Login do Dono do Sistema
    public class AdministradorGeralLoginRequest
    {
        public string Email { get; set; }
        public string Senha { get; set; }
    }

    // DTO para retorno de Login (Token e Configurações)
    public class AdministradorGeralLoginResponse
    {
        public string Token { get; set; }
        public AdministradorGeralConfigDto Config { get; set; }
    }

    // DTO para Configurações do Dono do Sistema
    public class AdministradorGeralConfigDto
    {
        public string Email { get; set; }
        public int DiasAvisoVencimento { get; set; }
        public string AppKey { get; set; }
        public string AuthKey { get; set; }
        public decimal ValorMensalidadePadrao { get; set; }
    }

    // DTO para o Dashboard do Dono do Sistema
    public class AdministradorGeralDashboardDto
    {
        public int TotalClientesMaster { get; set; }
        public int ClientesMasterAtivos { get; set; }
        public decimal FaturamentoMensalEstimado { get; set; }
        public int ClientesProximosVencimento { get; set; }
        public List<ClienteMasterMinDto> ProximosVencimentos { get; set; }
    }

    // DTO mínimo para listagem de clientes master
    public class ClienteMasterMinDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Celular { get; set; }
        public bool Ativo { get; set; }
        public DateTime? DataVencimento { get; set; }
        public decimal ValorMensalidade { get; set; }
    }

    // DTO para criação/atualização de Cliente Master
    public class ClienteMasterUpdateDto
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Celular { get; set; }
        public bool Ativo { get; set; }
        public DateTime? DataVencimento { get; set; }
        public decimal ValorMensalidade { get; set; }
    }
}
