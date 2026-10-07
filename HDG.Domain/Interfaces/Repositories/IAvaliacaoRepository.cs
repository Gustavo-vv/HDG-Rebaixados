using HDG.Domain.Entidades;

namespace HDG.Domain.Interfaces.Repositories;

public interface IAvaliacaoRepository
{
    Task<IEnumerable<Avaliacao>> ObterPorPecaAsync(int pecaId, bool apenasAprovadas = true);
    Task<IEnumerable<Avaliacao>> ObterTodasAsync(bool? apenasAprovadas = null);
    Task<Avaliacao?> ObterPorIdAsync(int id);
    Task<Avaliacao> AdicionarAsync(Avaliacao avaliacao);
    Task AprovarAsync(int id);
    Task DesaprovarAsync(int id);
    Task RemoverAsync(int id);
    Task<double> ObterMediaNotaPorPecaAsync(int pecaId);
    Task<int> ObterTotalAvaliacoesPorPecaAsync(int pecaId);
    Task<bool> ExisteDuplicadaRecenteAsync(int pecaId, string nomeCliente, string comentario, TimeSpan janela);
}
