using Microsoft.EntityFrameworkCore;
using MarcaAi.Backend.Data;
using MarcaAi.Backend.Models;
using MarcaAi.Backend.Services;

public class AgendamentoService
{
    private readonly ApplicationDbContext _db;
    private readonly WhatsAppService _whats;
  
    public AgendamentoService(
        ApplicationDbContext db,
        WhatsAppService whats,
        IConfiguration config)
    {
        _db = db;
        _whats = whats;
    }

    public async Task<bool> SolicitarExclusaoAsync(int agendamentoId)
{
    var agendamento = await _db.Agendamentos
        .Include(a => a.Cliente)
        .Include(a => a.ClienteMaster) // para pegar AppKey/AuthKey
        .FirstOrDefaultAsync(a => a.Id == agendamentoId);

    if (agendamento == null)
        throw new Exception("Agendamento não encontrado.");

    if (agendamento.Cliente == null || agendamento.ClienteMaster == null)
        throw new Exception("Cliente ou ClienteMaster não encontrados.");

    var codigo = Guid.NewGuid();

    var solicitacao = new SolicitacaoExclusao
    {
        AgendamentoId = agendamentoId,
        Codigo = codigo,
        Status = "Pendente",
        CriadoEm = DateTime.UtcNow
    };

    _db.SolicitacoesExclusao.Add(solicitacao);
    await _db.SaveChangesAsync();

    // 🔹 Mensagem com links clicáveis
    string mensagem =
        $"Olá, {agendamento.Cliente.Nome}!\n\n" +
        $"Você confirma a exclusão do seu agendamento?\n\n" +
        $"Código: *{codigo}*\n\n" +
        $"✅ Confirmar:  http://localhost:3000/confirmar-exclusao?codigo={codigo}";

    var result = await _whats.SendMessage(
        to: agendamento.Cliente.Telefone,
        message: mensagem,
        appKey: agendamento.ClienteMaster.AppKey!,
        authKey: agendamento.ClienteMaster.AuthKey!
    );

    return result;
}


    public async Task<bool> ProcessarConfirmacaoAsync(Guid codigo, string resposta)
    {
        var solicitacao = await _db.SolicitacoesExclusao
            .FirstOrDefaultAsync(s => s.Codigo == codigo);

        if (solicitacao == null)
            return false;

        resposta = resposta.Trim().ToUpper();

        if (resposta == "SIM")
    {
        // Remove o agendamento da tabela Agendamentos
        var agendamento = await _db.Agendamentos
            .FirstOrDefaultAsync(a => a.Id == solicitacao.AgendamentoId);

        if (agendamento != null)
            _db.Agendamentos.Remove(agendamento);

        // Atualiza a solicitação de exclusão para Aprovado
        solicitacao.Status = "Aprovada";
    }
    else
    {
        // Caso o usuário cancele a exclusão
        solicitacao.Status = "Negada";
    }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarcarComoRealizado(int agendamentoId)
    {
        var agendamento = await _db.Agendamentos.FindAsync(agendamentoId);

        if (agendamento == null)
            return false;

        agendamento.Realizado = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task AtualizarAgendamentosAutomaticamente(int clienteMasterId)
    {
        // 1. Buscar a configuração de atualização automática (se houver)
        // Por enquanto, vamos assumir que se o método for chamado, a atualização automática está ativa.
        // A lógica de verificação da flag será implementada no Controller.

        // 2. Buscar todos os agendamentos pendentes (Realizado = false)
        // que deveriam ter ocorrido até o momento atual (Data <= DateTime.Now)
        // para o ClienteMaster específico.
        var agendamentosPendentes = await _db.Agendamentos
            .Where(a => a.ClienteMasterId == clienteMasterId &&
                        a.Realizado == false &&
                        a.DataHora <= DateTimeOffset.UtcNow) // Usando UtcNow para consistência com o banco
            .ToListAsync();

        // 3. Atualizar o status para Realizado = true
        foreach (var agendamento in agendamentosPendentes)
        {
            agendamento.Realizado = true;
        }

        // 4. Salvar as alterações no banco de dados
        if (agendamentosPendentes.Any())
        {
            await _db.SaveChangesAsync();
        }
    }
}
