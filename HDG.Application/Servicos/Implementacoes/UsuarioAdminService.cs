using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HDG.Application.Servicos.Implementacoes;

public class UsuarioAdminService : IUsuarioAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsuarioAdminService> _logger;

    public UsuarioAdminService(
        UserManager<ApplicationUser> userManager,
        ILogger<UsuarioAdminService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UsuarioListaDto>> ObterUsuariosAsync(FiltroUsuariosDto filtro)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var termo = filtro.Busca.Trim();
            query = query.Where(u =>
                (u.NomeCompleto != null && EF.Functions.Like(u.NomeCompleto, $"%{termo}%")) ||
                (u.Email != null && EF.Functions.Like(u.Email, $"%{termo}%")) ||
                (u.PhoneNumber != null && EF.Functions.Like(u.PhoneNumber, $"%{termo}%")));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Status))
        {
            if (filtro.Status.Equals("ativos", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => u.Ativo);
            else if (filtro.Status.Equals("inativos", StringComparison.OrdinalIgnoreCase))
                query = query.Where(u => !u.Ativo);
        }

        var usuarios = await query
            .OrderByDescending(u => u.DataCadastro)
            .ToListAsync();

        var lista = new List<UsuarioListaDto>(usuarios.Count);
        var fusoBrasilia = ObterFusoHorarioBrasilia();

        foreach (var user in usuarios)
        {
            var roles = await _userManager.GetRolesAsync(user);

            // Filtragem por role em memória (Identity armazena papéis em tabela associativa)
            if (!string.IsNullOrWhiteSpace(filtro.Role) && !filtro.Role.Equals("todos", StringComparison.OrdinalIgnoreCase))
            {
                if (!roles.Contains(filtro.Role, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var dataBrasilia = TimeZoneInfo.ConvertTimeFromUtc(user.DataCadastro, fusoBrasilia);

            lista.Add(new UsuarioListaDto
            {
                Id = user.Id,
                NomeCompleto = user.NomeCompleto,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                DataCadastroUtc = user.DataCadastro,
                DataCadastroBrasilia = dataBrasilia,
                Ativo = user.Ativo,
                Roles = roles
            });
        }

        return lista;
    }

    public async Task<UsuarioListaDto?> ObterPorIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var fusoBrasilia = ObterFusoHorarioBrasilia();
        var dataBrasilia = TimeZoneInfo.ConvertTimeFromUtc(user.DataCadastro, fusoBrasilia);

        return new UsuarioListaDto
        {
            Id = user.Id,
            NomeCompleto = user.NomeCompleto,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            DataCadastroUtc = user.DataCadastro,
            DataCadastroBrasilia = dataBrasilia,
            Ativo = user.Ativo,
            Roles = roles
        };
    }

    public async Task<(bool Sucesso, string[] Erros)> EditarDadosAsync(EditarUsuarioDto dto, string adminLogadoId)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            return (false, new[] { "Identificador de usuário inválido." });

        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null)
            return (false, new[] { "Usuário não encontrado." });

        user.NomeCompleto = dto.NomeCompleto.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.Telefone) ? null : dto.Telefone.Trim();

        var resultado = await _userManager.UpdateAsync(user);
        if (!resultado.Succeeded)
        {
            return (false, resultado.Errors.Select(TraduzirErroIdentity).ToArray());
        }

        _logger.LogInformation("Admin {AdminId} editou dados cadastrais do usuário {UserId} ({Nome})",
            adminLogadoId, user.Id, user.NomeCompleto);

        return (true, Array.Empty<string>());
    }

    public async Task<(bool Sucesso, string[] Erros, bool NovoStatus)> AlternarStatusAsync(string id, string adminLogadoId)
    {
        if (string.IsNullOrWhiteSpace(id))
            return (false, new[] { "Identificador de usuário inválido." }, false);

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, new[] { "Usuário não encontrado." }, false);

        // REGRA DE SEGURANÇA: Admin logado não pode desativar a si mesmo
        if (user.Id == adminLogadoId)
        {
            return (false, new[] { "Você não pode desativar o seu próprio usuário de administrador." }, user.Ativo);
        }

        var novoStatus = !user.Ativo;

        // Se estiver desativando, checar se não é o último Admin ativo
        if (!novoStatus)
        {
            var isUserAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isUserAdmin)
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var adminsAtivos = admins.Count(a => a.Ativo);
                if (adminsAtivos <= 1)
                {
                    return (false, new[] { "Não é permitido desativar o último administrador ativo do sistema." }, user.Ativo);
                }
            }
        }

        user.Ativo = novoStatus;
        var resultado = await _userManager.UpdateAsync(user);
        if (!resultado.Succeeded)
        {
            return (false, resultado.Errors.Select(TraduzirErroIdentity).ToArray(), !novoStatus);
        }

        // Ao desativar ou alterar status, invalida o security stamp para derrubar sessões ativas
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Admin {AdminId} alterou status do usuário {UserId} ({Email}) para Ativo={NovoStatus}",
            adminLogadoId, user.Id, user.Email, novoStatus);

        return (true, Array.Empty<string>(), novoStatus);
    }

    public async Task<(bool Sucesso, string[] Erros)> AlterarSenhaAsync(AlterarSenhaAdminDto dto, string adminLogadoId)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            return (false, new[] { "Identificador de usuário inválido." });

        if (dto.NovaSenha != dto.ConfirmarSenha)
            return (false, new[] { "As senhas informadas não conferem." });

        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null)
            return (false, new[] { "Usuário não encontrado." });

        // Gera token de redefinição e aplica a nova senha
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resultado = await _userManager.ResetPasswordAsync(user, resetToken, dto.NovaSenha);

        if (!resultado.Succeeded)
        {
            return (false, resultado.Errors.Select(TraduzirErroIdentity).ToArray());
        }

        // Garante a invalidação das sessões ativas com o token antigo
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Admin {AdminId} redefiniu com sucesso a senha do usuário {UserId} ({Email})",
            adminLogadoId, user.Id, user.Email);

        return (true, Array.Empty<string>());
    }

    public async Task<(bool Sucesso, string[] Erros)> AlterarPerfilAsync(AlterarPerfilDto dto, string adminLogadoId)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            return (false, new[] { "Identificador de usuário inválido." });

        var roleDestino = dto.NovaRole?.Trim();
        if (roleDestino != "Admin" && roleDestino != "Cliente")
            return (false, new[] { "Perfil inválido. O usuário deve ser Admin ou Cliente." });

        var user = await _userManager.FindByIdAsync(dto.Id);
        if (user == null)
            return (false, new[] { "Usuário não encontrado." });

        var rolesAtuais = await _userManager.GetRolesAsync(user);
        var jaTemRole = rolesAtuais.Contains(roleDestino, StringComparer.OrdinalIgnoreCase);
        if (jaTemRole)
        {
            return (true, Array.Empty<string>());
        }

        // REGRA DE SEGURANÇA: Admin logado não pode remover o próprio perfil de Admin
        if (user.Id == adminLogadoId && roleDestino != "Admin")
        {
            return (false, new[] { "Você não pode revogar o seu próprio perfil de administrador." });
        }

        // REGRA DE SEGURANÇA: Se estiver rebaixando de Admin para Cliente, checar se não é o último Admin ativo
        if (rolesAtuais.Contains("Admin") && roleDestino == "Cliente")
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminsAtivos = admins.Count(a => a.Ativo);
            if (adminsAtivos <= 1 && user.Ativo)
            {
                return (false, new[] { "Não é permitido remover o perfil do último administrador ativo do sistema." });
            }
        }

        // Remove papéis antigos e adiciona o novo papel
        if (rolesAtuais.Any())
        {
            var remocao = await _userManager.RemoveFromRolesAsync(user, rolesAtuais);
            if (!remocao.Succeeded)
            {
                return (false, remocao.Errors.Select(TraduzirErroIdentity).ToArray());
            }
        }

        var adicao = await _userManager.AddToRoleAsync(user, roleDestino);
        if (!adicao.Succeeded)
        {
            return (false, adicao.Errors.Select(TraduzirErroIdentity).ToArray());
        }

        // Atualiza carimbo de segurança para renovar permissões em claims/cookies
        await _userManager.UpdateSecurityStampAsync(user);

        _logger.LogInformation("Admin {AdminId} alterou o perfil do usuário {UserId} ({Email}) para {Role}",
            adminLogadoId, user.Id, user.Email, roleDestino);

        return (true, Array.Empty<string>());
    }

    private static TimeZoneInfo ObterFusoHorarioBrasilia()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }
    }

    private static string TraduzirErroIdentity(IdentityError erro)
    {
        return erro.Code switch
        {
            "PasswordTooShort" => "A senha deve ter no mínimo 8 caracteres.",
            "PasswordRequiresNonAlphanumeric" => "A senha deve conter ao menos um caractere especial (ex: !@#$%&*).",
            "PasswordRequiresDigit" => "A senha deve conter ao menos um número.",
            "PasswordRequiresLower" => "A senha deve conter ao menos uma letra minúscula.",
            "PasswordRequiresUpper" => "A senha deve conter ao menos uma letra maiúscula.",
            "PasswordRequiresUniqueChars" => "A senha requer caracteres distintos.",
            "DuplicateUserName" => "Já existe um usuário cadastrado com este nome de usuário.",
            "DuplicateEmail" => "Já existe um usuário cadastrado com este e-mail.",
            "InvalidEmail" => "O formato do e-mail é inválido.",
            "InvalidUserName" => "O nome de usuário é inválido.",
            "UserAlreadyHasPassword" => "O usuário já possui uma senha definida.",
            "UserLockoutNotEnabled" => "Bloqueio não habilitado para este usuário.",
            "ConcurrencyFailure" => "Os dados foram alterados por outro processo. Atualize a página e tente novamente.",
            _ => erro.Description
        };
    }
}
