using HDG.Domain.Entidades;
using HDG.Domain.Enums;

namespace HDG.Domain.Interfaces.Repositories;

public interface IPecaRepository
{
    Task<IEnumerable<Peca>> ObterTodasAsync(bool apenasAtivas = true, string? busca = null, CategoriaPeca? categoria = null);
    Task<Peca?> ObterPorIdAsync(int id, bool incluirInativas = false);
    Task<Peca> AdicionarAsync(Peca peca);
    Task AtualizarAsync(Peca peca);
    Task RemoverAsync(int id);
    Task<bool> ExisteAsync(int id);
    Task AdicionarImagemAsync(PecaImagem imagem);
    Task RemoverImagemAsync(int imagemId);
    Task<PecaImagem?> ObterImagemPorIdAsync(int imagemId);
}
