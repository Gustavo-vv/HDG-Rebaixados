using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HDG.Application.Servicos.Implementacoes;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<UsuarioTokenDto?> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !user.Ativo)
            return null;

        var senhaValida = await _userManager.CheckPasswordAsync(user, dto.Senha);
        if (!senhaValida)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var token = GerarJwtToken(user, roles, out var expiracao);

        return new UsuarioTokenDto
        {
            Id = user.Id,
            NomeCompleto = user.NomeCompleto,
            Email = user.Email ?? string.Empty,
            Token = token,
            Expiracao = expiracao,
            Roles = roles
        };
    }

    public async Task<(bool Sucesso, string[] Erros)> RegistrarAsync(RegistroDto dto)
    {
        if (dto.Senha != dto.ConfirmarSenha)
            return (false, new[] { "As senhas não conferem." });

        var usuarioExistente = await _userManager.FindByEmailAsync(dto.Email);
        if (usuarioExistente != null)
            return (false, new[] { "Já existe um usuário cadastrado com este e-mail." });

        var novoUsuario = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            NomeCompleto = dto.NomeCompleto.Trim(),
            Ativo = true,
            DataCadastro = DateTime.UtcNow
        };

        var resultado = await _userManager.CreateAsync(novoUsuario, dto.Senha);
        if (!resultado.Succeeded)
        {
            return (false, resultado.Errors.Select(e => e.Description).ToArray());
        }

        // Adiciona role padrão "Cliente"
        await _userManager.AddToRoleAsync(novoUsuario, "Cliente");

        return (true, Array.Empty<string>());
    }

    private string GerarJwtToken(ApplicationUser user, IList<string> roles, out DateTime expiracao)
    {
        var chave = _configuration["Jwt:Chave"] ?? "HDGRebaixadosChaveSuperSecreta2026ParaAutenticacaoComMaisDe32Bytes!";
        var emissor = _configuration["Jwt:Emissor"] ?? "HDGRebaixadosAPI";
        var audiencia = _configuration["Jwt:Audiencia"] ?? "HDGRebaixadosApp";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        expiracao = DateTime.UtcNow.AddHours(8);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.NomeCompleto)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiracao,
            Issuer = emissor,
            Audience = audiencia,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}