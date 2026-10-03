using HDG.Application.DTOs;
using HDG.Domain.Enums;

namespace HDG.Application.Servicos.Services;

public interface IPecaService
{
    Task<IEnumerable<PecaDto>> ObterCatalogoAsync(string? busca = null, CategoriaPeca? categoria = null);
    Task<IEnumerable<PecaDto>> ObterTodasAdminAsync();
    Task<PecaDto?> ObterPorIdAsync(int id);
    Task<PecaDto> CriarAsync(CriarPecaDto dto);
    Task AtualizarAsync(AtualizarPecaDto dto);
    Task AlternarStatusAsync(int id);
    Task ExcluirAsync(int id);
    Task AdicionarImagemAsync(int pecaId, string url, string? publicId, bool principal);
    Task RemoverImagemAsync(int imagemId);
    Task DefinirImagemPrincipalAsync(int pecaId, int imagemId);
}