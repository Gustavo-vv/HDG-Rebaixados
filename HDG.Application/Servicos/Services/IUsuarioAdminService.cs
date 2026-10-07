using HDG.Application.DTOs;

namespace HDG.Application.Servicos.Services;

public interface IUsuarioAdminService
{
    Task<IReadOnlyList<UsuarioListaDto>> ObterUsuariosAsync(FiltroUsuariosDto filtro);
    Task<UsuarioListaDto?> ObterPorIdAsync(string id);
    Task<(bool Sucesso, string[] Erros)> EditarDadosAsync(EditarUsuarioDto dto, string adminLogadoId);
    Task<(bool Sucesso, string[] Erros, bool NovoStatus)> AlternarStatusAsync(string id, string adminLogadoId);
    Task<(bool Sucesso, string[] Erros)> AlterarSenhaAsync(AlterarSenhaAdminDto dto, string adminLogadoId);
    Task<(bool Sucesso, string[] Erros)> AlterarPerfilAsync(AlterarPerfilDto dto, string adminLogadoId);
}
