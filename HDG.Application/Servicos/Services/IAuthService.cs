using HDG.Application.DTOs;

namespace HDG.Application.Servicos.Services;

public interface IAuthService
{
    Task<UsuarioTokenDto?> LoginAsync(LoginDto dto);
    Task<(bool Sucesso, string[] Erros)> RegistrarAsync(RegistroDto dto);
}
