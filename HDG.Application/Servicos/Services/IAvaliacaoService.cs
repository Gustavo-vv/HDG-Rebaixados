using HDG.Application.DTOs;

namespace HDG.Application.Servicos.Services;

public interface IAvaliacaoService
{
    Task<IEnumerable<AvaliacaoDto>> ObterPorPecaAsync(int pecaId);
    Task<IEnumerable<AvaliacaoDto>> ObterTodasParaModeracaoAsync();
    Task<AvaliacaoDto> CriarAsync(CriarAvaliacaoDto dto);
    Task AprovarAsync(int id);
    Task ExcluirAsync(int id);
}
