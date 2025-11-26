namespace MarcaAi.Backend.DTOs;

public record ServicoDto(int IdServico, string Nome, decimal Preco, int DuracaoMinutos, bool Ativo);
public record FuncionarioDto(int IdFuncionario, string Nome);
public record HorarioDto(int IdHorario, string Data, string Hora);

public record CriarAgendamentoRequest(int IdClienteMaster, int IdCliente, int IdServico, int IdFuncionario, int IdHorario);

public record DisponibilidadeDto(
    int FuncionarioId,
    DayOfWeek? DiaSemana,
    DateTime? DataEspecifica,
    TimeSpan HoraInicio,
    TimeSpan HoraFim,
    string Tipo, // "Padrao" ou "Excecao"
    bool Almoço,
    TimeSpan? DtInicioAlmoco,
    TimeSpan? DtFimAlmoco
);

public record BloqueioDto(
    int ClienteMasterId,
    int FuncionarioId,
    DateTime Data,
    TimeSpan HoraInicio,
    TimeSpan HoraFim
);
