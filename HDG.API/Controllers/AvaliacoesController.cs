using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDG.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvaliacoesController : ControllerBase
{
    private readonly IAvaliacaoService _avaliacaoService;

    public AvaliacoesController(IAvaliacaoService avaliacaoService)
    {
        _avaliacaoService = avaliacaoService;
    }

    /// <summary>
    /// Avaliações aprovadas de uma peça
    /// </summary>
    [HttpGet("peca/{pecaId}")]
    public async Task<IActionResult> ObterPorPeca(int pecaId)
    {
        var avaliacoes = await _avaliacaoService.ObterPorPecaAsync(pecaId);
        return Ok(avaliacoes);
    }

    /// <summary>
    /// Todas as avaliações para moderação no painel admin
    /// </summary>
    [HttpGet("admin/moderacao")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ObterParaModeracao()
    {
        var avaliacoes = await _avaliacaoService.ObterTodasParaModeracaoAsync();
        return Ok(avaliacoes);
    }

    /// <summary>
    /// Enviar uma avaliação de cliente
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Criar([FromBody] CriarAvaliacaoDto dto)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

            var nomeUsuario = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            if (string.IsNullOrWhiteSpace(nomeUsuario))
                nomeUsuario = User.Identity?.Name ?? "Cliente";

            var criada = await _avaliacaoService.CriarParaUsuarioAsync(dto, nomeUsuario, userId);
            return Ok(new { mensagem = "Avaliação enviada com sucesso! Ela será exibida após moderação.", avaliacao = criada });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
    }

    /// <summary>
    /// Aprovar avaliação
    /// </summary>
    [HttpPatch("{id}/aprovar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Aprovar(int id)
    {
        await _avaliacaoService.AprovarAsync(id);
        return Ok(new { mensagem = "Avaliação aprovada com sucesso." });
    }

    /// <summary>
    /// Desaprovar avaliação (voltar para moderação)
    /// </summary>
    [HttpPatch("{id}/desaprovar")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Desaprovar(int id)
    {
        await _avaliacaoService.DesaprovarAsync(id);
        return Ok(new { mensagem = "Avaliação desaprovada com sucesso." });
    }

    /// <summary>
    /// Excluir avaliação
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Excluir(int id)
    {
        await _avaliacaoService.ExcluirAsync(id);
        return NoContent();
    }
}
