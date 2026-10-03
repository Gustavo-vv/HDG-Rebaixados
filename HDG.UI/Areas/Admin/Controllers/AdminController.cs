using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Entidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HDG.UI.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IPecaService _pecaService;
    private readonly IAvaliacaoService _avaliacaoService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IPecaService pecaService,
        IAvaliacaoService avaliacaoService,
        ICloudinaryService cloudinaryService,
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _pecaService = pecaService;
        _avaliacaoService = avaliacaoService;
        _cloudinaryService = cloudinaryService;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // DASHBOARD OVERVIEW
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pecas = (await _pecaService.ObterTodasAdminAsync()).ToList();
        var avaliacoes = (await _avaliacaoService.ObterTodasParaModeracaoAsync()).ToList();
        var totalUsuarios = await _userManager.Users.CountAsync();

        ViewBag.TotalPecas = pecas.Count;
        ViewBag.PecasAtivas = pecas.Count(p => p.Ativo);
        ViewBag.PecasInativas = pecas.Count(p => !p.Ativo);
        ViewBag.AvaliacoesPendentes = avaliacoes.Count(a => !a.Aprovada);
        ViewBag.ValorTotalEstoque = pecas.Where(p => p.Ativo).Sum(p => p.Preco);
        ViewBag.TotalUsuarios = totalUsuarios;

        ViewBag.UltimasPecas = pecas.Take(5).ToList();
        ViewBag.UltimasAvaliacoesPendentes = avaliacoes.Where(a => !a.Aprovada).Take(5).ToList();

        return View();
    }

    // ==========================================
    // GESTÃƒO DE PEÃ‡AS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Pecas()
    {
        var pecas = await _pecaService.ObterTodasAdminAsync();
        return View(pecas);
    }

    [HttpGet]
    public IActionResult PecaNova()
    {
        return View("PecaForm", new CriarPecaDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PecaNova(CriarPecaDto dto, IFormFile? imagemArquivo, string? imagemUrlManual)
    {
        if (!ModelState.IsValid)
            return View("PecaForm", dto);

        try
        {
            var urls = new List<string>();

            // Upload Cloudinary se arquivo fornecido
            if (imagemArquivo != null && imagemArquivo.Length > 0)
            {
                using var stream = imagemArquivo.OpenReadStream();
                var upload = await _cloudinaryService.UploadImagemAsync(stream, imagemArquivo.FileName);
                if (upload.Sucesso && !string.IsNullOrWhiteSpace(upload.Url))
                {
                    urls.Add(upload.Url);
                }
            }

            // URL manual opcional
            if (!string.IsNullOrWhiteSpace(imagemUrlManual))
            {
                urls.Add(imagemUrlManual.Trim());
            }

            // Fallback para imagem automotiva se nenhuma fornecida
            if (!urls.Any())
            {
                urls.Add("https://images.unsplash.com/photo-1486006920555-c77dce18193b?auto=format&fit=crop&w=1000&q=80");
            }

            dto.ImagensUrls = urls;
            await _pecaService.CriarAsync(dto);

            TempData["MensagemSucesso"] = "Peça cadastrada com sucesso no catálogo!";
            return RedirectToAction(nameof(Pecas));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cadastrar peça");
            ModelState.AddModelError(string.Empty, "Erro ao cadastrar peça: " + ex.Message);
            return View("PecaForm", dto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> PecaEditar(int id)
    {
        var peca = await _pecaService.ObterPorIdAsync(id);
        if (peca == null)
            return NotFound();

        var dto = new AtualizarPecaDto
        {
            Id = peca.Id,
            Nome = peca.Nome,
            Descricao = peca.Descricao,
            Preco = peca.Preco,
            Categoria = peca.Categoria,
            Ativo = peca.Ativo
        };

        ViewBag.PecaExistente = peca;
        return View("PecaEditar", dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PecaEditar(AtualizarPecaDto dto, IFormFile? novaImagemArquivo, string? novaImagemUrlManual)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PecaExistente = await _pecaService.ObterPorIdAsync(dto.Id);
            return View("PecaEditar", dto);
        }

        try
        {
            var novasUrls = new List<string>();

            if (novaImagemArquivo != null && novaImagemArquivo.Length > 0)
            {
                using var stream = novaImagemArquivo.OpenReadStream();
                var upload = await _cloudinaryService.UploadImagemAsync(stream, novaImagemArquivo.FileName);
                if (upload.Sucesso && !string.IsNullOrWhiteSpace(upload.Url))
                {
                    novasUrls.Add(upload.Url);
                }
            }

            if (!string.IsNullOrWhiteSpace(novaImagemUrlManual))
            {
                novasUrls.Add(novaImagemUrlManual.Trim());
            }

            dto.NovasImagensUrls = novasUrls;
            await _pecaService.AtualizarAsync(dto);

            TempData["MensagemSucesso"] = "Peça atualizada com sucesso!";
            return RedirectToAction(nameof(Pecas));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar peça {Id}", dto.Id);
            ModelState.AddModelError(string.Empty, "Erro ao atualizar: " + ex.Message);
            ViewBag.PecaExistente = await _pecaService.ObterPorIdAsync(dto.Id);
            return View("PecaEditar", dto);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DefinirPrincipal(int pecaId, int imagemId)
    {
        try
        {
            await _pecaService.DefinirImagemPrincipalAsync(pecaId, imagemId);
            TempData["MensagemSucesso"] = "Imagem principal atualizada com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["MensagemErro"] = "Erro ao definir imagem principal: " + ex.Message;
        }

        return RedirectToAction(nameof(PecaEditar), new { id = pecaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverFoto(int pecaId, int imagemId)
    {
        try
        {
            await _pecaService.RemoverImagemAsync(imagemId);
            TempData["MensagemSucesso"] = "Foto removida com sucesso!";
        }
        catch (Exception ex)
        {
            TempData["MensagemErro"] = "Erro ao remover foto: " + ex.Message;
        }

        return RedirectToAction(nameof(PecaEditar), new { id = pecaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PecaAlternarStatus(int id)
    {
        try
        {
            await _pecaService.AlternarStatusAsync(id);
            TempData["MensagemSucesso"] = "Status da peça atualizado!";
        }
        catch (Exception ex)
        {
            TempData["MensagemErro"] = "Erro ao alterar status: " + ex.Message;
        }

        return RedirectToAction(nameof(Pecas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PecaExcluir(int id)
    {
        try
        {
            await _pecaService.ExcluirAsync(id);
            TempData["MensagemSucesso"] = "Peça removida do catálogo.";
        }
        catch (Exception ex)
        {
            TempData["MensagemErro"] = "Erro ao excluir peça: " + ex.Message;
        }

        return RedirectToAction(nameof(Pecas));
    }

    // ==========================================
    // MODERAÃ‡ÃƒO DE AVALIAÃ‡Ã•ES
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Avaliacoes()
    {
        var avaliacoes = await _avaliacaoService.ObterTodasParaModeracaoAsync();
        return View(avaliacoes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AvaliacaoAprovar(int id)
    {
        try
        {
            await _avaliacaoService.AprovarAsync(id);
            TempData["MensagemSucesso"] = "Avaliação aprovada e publicada no catálogo!";
        }
        catch (Exception ex)
        {
            TempData["MensagemErro"] = "Erro ao aprovar avaliação: " + ex.Message;
        }

        return RedirectToAction(nameof(Avaliacoes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AvaliacaoExcluir(int id)
    {
        try
        {
            await _avaliacaoService.ExcluirAsync(id);
            TempData["MensagemSucesso"] = "Avaliação excluída com sucesso.";
        }
        catch (Exception ex)
        {
            TempData["MensagemErro"] = "Erro ao excluir avaliação: " + ex.Message;
        }

        return RedirectToAction(nameof(Avaliacoes));
    }

    // ==========================================
    // USUÃRIOS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _userManager.Users.OrderByDescending(u => u.DataCadastro).ToListAsync();
        return View(usuarios);
    }
}
