using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using Microsoft.AspNetCore.Mvc;

namespace HDG.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var resultado = await _authService.LoginAsync(dto);
        if (resultado == null)
            return Unauthorized(new { mensagem = "Credenciais inválidas ou conta inativa." });

        return Ok(resultado);
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registro([FromBody] RegistroDto dto)
    {
        var (sucesso, erros) = await _authService.RegistrarAsync(dto);
        if (!sucesso)
            return BadRequest(new { erros });

        return Ok(new { mensagem = "Usuário registrado com sucesso." });
    }
}
