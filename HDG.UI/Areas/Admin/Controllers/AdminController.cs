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
    private readonly IUsuarioAdminService _usuarioAdminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IPecaService pecaService,
        IAvaliacaoService avaliacaoService,
        ICloudinaryService cloudinaryService,
        UserManager<ApplicationUser> userManager,
        IUsuarioAdminService usuarioAdminService,
        ILogger<AdminController> logger)
    {
        _pecaService = pecaService;
        _avaliacaoService = avaliacaoService;
        _cloudinaryService = cloudinaryService;
        _userManager = userManager;
        _usuarioAdminService = usuarioAdminService;
        _logger = logger;
    }

    public override async Task OnActionExecutionAsync(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context, Microsoft.AspNetCore.Mvc.Filters.ActionExecutionDelegate next)
    {
        try
        {
            var todas = await _avaliacaoService.ObterTodasParaModeracaoAsync();
            ViewBag.AvaliacoesPendentesBadge = todas.Count(a => !a.Aprovada);
        }
        catch
        {
            ViewBag.AvaliacoesPendentesBadge = 0;
        }

        await base.OnActionExecutionAsync(context, next);
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
    // GESTÃO DE PEÇAS
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
    // MODERAÇÃO DE AVALIAÇÕES
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Avaliacoes()
    {
        var avaliacoes = await _avaliacaoService.ObterTodasParaModeracaoAsync();
        return View(avaliacoes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("AvaliacaoAprovar")]
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
    [ActionName("AprovarAvaliacao")]
    public Task<IActionResult> AprovarAvaliacaoAlias(int id) => AvaliacaoAprovar(id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("AvaliacaoDesaprovar")]
    public async Task<IActionResult> AvaliacaoDesaprovar(int id)
    {
        try
        {
            await _avaliacaoService.DesaprovarAsync(id);
            TempData["MensagemSucesso"] = "Avaliação despublicada e retornada para moderação!";
        }
        catch (Exception ex)
        {
            TempData["MensagemErro"] = "Erro ao despublicar avaliação: " + ex.Message;
        }

        return RedirectToAction(nameof(Avaliacoes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("DesaprovarAvaliacao")]
    public Task<IActionResult> DesaprovarAvaliacaoAlias(int id) => AvaliacaoDesaprovar(id);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("AvaliacaoExcluir")]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("ExcluirAvaliacao")]
    public Task<IActionResult> ExcluirAvaliacaoAlias(int id) => AvaliacaoExcluir(id);

    // ==========================================
    // GESTÃO DE USUÁRIOS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Usuarios([FromQuery] FiltroUsuariosDto filtro)
    {
        filtro ??= new FiltroUsuariosDto();
        var usuarios = await _usuarioAdminService.ObterUsuariosAsync(filtro);
        ViewBag.Filtro = filtro;
        ViewBag.AdminLogadoId = _userManager.GetUserId(User) ?? string.Empty;
        return View(usuarios);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsuarioEditar(EditarUsuarioDto dto)
    {
        if (!ModelState.IsValid)
        {
            var erros = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["MensagemErro"] = string.IsNullOrWhiteSpace(erros) ? "Dados inválidos para edição de usuário." : erros;
            return RedirectToAction(nameof(Usuarios));
        }

        var adminLogadoId = _userManager.GetUserId(User) ?? string.Empty;
        var resultado = await _usuarioAdminService.EditarDadosAsync(dto, adminLogadoId);

        if (resultado.Sucesso)
        {
            TempData["MensagemSucesso"] = "Dados do usuário atualizados com sucesso!";
        }
        else
        {
            TempData["MensagemErro"] = string.Join(" ", resultado.Erros);
        }

        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsuarioAlternarStatus(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["MensagemErro"] = "Identificador de usuário não informado.";
            return RedirectToAction(nameof(Usuarios));
        }

        var adminLogadoId = _userManager.GetUserId(User) ?? string.Empty;
        var resultado = await _usuarioAdminService.AlternarStatusAsync(id, adminLogadoId);

        if (resultado.Sucesso)
        {
            TempData["MensagemSucesso"] = resultado.NovoStatus
                ? "Usuário reativado com sucesso!"
                : "Usuário desativado com sucesso. As sessões foram encerradas.";
        }
        else
        {
            TempData["MensagemErro"] = string.Join(" ", resultado.Erros);
        }

        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsuarioAlterarSenha(AlterarSenhaAdminDto dto)
    {
        if (!ModelState.IsValid)
        {
            var erros = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["MensagemErro"] = string.IsNullOrWhiteSpace(erros) ? "Dados inválidos para redefinição de senha." : erros;
            return RedirectToAction(nameof(Usuarios));
        }

        var adminLogadoId = _userManager.GetUserId(User) ?? string.Empty;
        var resultado = await _usuarioAdminService.AlterarSenhaAsync(dto, adminLogadoId);

        if (resultado.Sucesso)
        {
            TempData["MensagemSucesso"] = "Senha redefinida com sucesso! Sessões anteriores foram invalidadas.";
        }
        else
        {
            TempData["MensagemErro"] = string.Join(" ", resultado.Erros);
        }

        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsuarioAlterarPerfil(AlterarPerfilDto dto)
    {
        if (!ModelState.IsValid)
        {
            var erros = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["MensagemErro"] = string.IsNullOrWhiteSpace(erros) ? "Dados inválidos para alteração de perfil." : erros;
            return RedirectToAction(nameof(Usuarios));
        }

        var adminLogadoId = _userManager.GetUserId(User) ?? string.Empty;
        var resultado = await _usuarioAdminService.AlterarPerfilAsync(dto, adminLogadoId);

        if (resultado.Sucesso)
        {
            TempData["MensagemSucesso"] = $"Perfil do usuário atualizado para {dto.NovaRole}!";
        }
        else
        {
            TempData["MensagemErro"] = string.Join(" ", resultado.Erros);
        }

        return RedirectToAction(nameof(Usuarios));
    }
}
