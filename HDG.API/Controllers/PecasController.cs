using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HDG.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PecasController : ControllerBase
{
    private readonly IPecaService _pecaService;
    private readonly ICloudinaryService _cloudinaryService;

    public PecasController(IPecaService pecaService, ICloudinaryService cloudinaryService)
    {
        _pecaService = pecaService;
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Catálogo público de peças ativas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ObterCatalogo([FromQuery] string? busca, [FromQuery] CategoriaPeca? categoria)
    {
        var pecas = await _pecaService.ObterCatalogoAsync(busca, categoria);
        return Ok(pecas);
    }

    /// <summary>
    /// Listagem administrativa completa (ativas e inativas)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ObterTodasAdmin()
    {
        var pecas = await _pecaService.ObterTodasAdminAsync();
        return Ok(pecas);
    }

    /// <summary>
    /// Detalhes de uma peça pelo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var peca = await _pecaService.ObterPorIdAsync(id);
        if (peca == null)
            return NotFound(new { mensagem = "Peça não encontrada." });

        return Ok(peca);
    }

    /// <summary>
    /// Cadastro de peça com upload de imagem opcional direto no Cloudinary
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Criar([FromBody] CriarPecaDto dto)
    {
        var criada = await _pecaService.CriarAsync(dto);
        return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
    }

    /// <summary>
    /// Atualizar dados da peça
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarPecaDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { mensagem = "ID divergente." });

        await _pecaService.AtualizarAsync(dto);
        return NoContent();
    }

    /// <summary>
    /// Alternar status (ativo/inativo)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AlternarStatus(int id)
    {
        await _pecaService.AlternarStatusAsync(id);
        return Ok(new { mensagem = "Status alterado com sucesso." });
    }

    /// <summary>
    /// Excluir peça
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Excluir(int id)
    {
        await _pecaService.ExcluirAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Upload de imagens para o Cloudinary vinculado à peça
    /// </summary>
    [HttpPost("{id}/imagens")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImagens(int id, [FromForm] List<IFormFile> arquivos)
    {
        if (arquivos == null || !arquivos.Any())
            return BadRequest(new { mensagem = "Nenhum arquivo enviado." });

        var peca = await _pecaService.ObterPorIdAsync(id);
        if (peca == null)
            return NotFound(new { mensagem = "Peça não encontrada." });

        var resultados = new List<CloudinaryUploadResult>();

        foreach (var arquivo in arquivos)
        {
            using var stream = arquivo.OpenReadStream();
            var uploadRes = await _cloudinaryService.UploadImagemAsync(stream, arquivo.FileName);
            
            if (uploadRes.Sucesso && !string.IsNullOrEmpty(uploadRes.Url))
            {
                await _pecaService.AdicionarImagemAsync(id, uploadRes.Url, uploadRes.PublicId, principal: !peca.Imagens.Any());
            }

            resultados.Add(uploadRes);
        }

        return Ok(resultados);
    }

    /// <summary>
    /// Remover imagem da peça
    /// </summary>
    [HttpDelete("imagens/{imagemId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoverImagem(int imagemId)
    {
        await _pecaService.RemoverImagemAsync(imagemId);
        return NoContent();
    }
}
