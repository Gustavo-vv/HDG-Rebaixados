using System.Diagnostics;
using System.Security.Claims;
using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Entidades;
using HDG.Domain.Enums;
using HDG.UI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HDG.UI.Controllers;

public class HomeController : Controller
{
    private readonly IPecaService _pecaService;
    private readonly IAvaliacaoService _avaliacaoService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IPecaService pecaService,
        IAvaliacaoService avaliacaoService,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<HomeController> logger)
    {
        _pecaService = pecaService;
        _avaliacaoService = avaliacaoService;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca = null, CategoriaPeca? categoria = null)
    {
        var pecas = await _pecaService.ObterCatalogoAsync(busca, categoria);
        var whatsAppNum = _configuration["WhatsApp:Numero"] ?? "5511999999999";

        ViewBag.Busca = busca;
        ViewBag.CategoriaSelecionada = categoria;
        ViewBag.WhatsAppNumero = whatsAppNum;

        return View(pecas);
    }

    [HttpGet]
    public async Task<IActionResult> Detalhes(int id)
    {
        var peca = await _pecaService.ObterPorIdAsync(id);
        if (peca == null || !peca.Ativo)
            return NotFound();

        var avaliacoes = await _avaliacaoService.ObterPorPecaAsync(id);
        var whatsAppNum = _configuration["WhatsApp:Numero"] ?? "5511999999999";

        ViewBag.Avaliacoes = avaliacoes;
        ViewBag.WhatsAppNumero = whatsAppNum;

        // Se o usuário estiver autenticado, disponibilizar o NomeCompleto
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UsuarioNomeCompleto = user?.NomeCompleto ?? User.Identity.Name ?? "Cliente";
        }

        return View(peca);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarAvaliacao([FromForm] CriarAvaliacaoDto dto)
    {
        try
        {
            if (dto.Nota < 1 || dto.Nota > 5)
            {
                TempData["ErroAvaliacao"] = "A nota deve ser entre 1 e 5 estrelas.";
                return RedirectToAction(nameof(Detalhes), "Home", new { id = dto.PecaId }, "avaliacoes");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.Ativo)
            {
                TempData["ErroAvaliacao"] = "Sua conta não possui permissão para enviar avaliações.";
                return RedirectToAction(nameof(Detalhes), "Home", new { id = dto.PecaId }, "avaliacoes");
            }

            // O nome do cliente NÃO vem do formulário (qualquer valor forjado é ignorado)
            var nomeCliente = !string.IsNullOrWhiteSpace(user.NomeCompleto) ? user.NomeCompleto.Trim() : (user.UserName ?? "Cliente");

            await _avaliacaoService.CriarParaUsuarioAsync(dto, nomeCliente, user.Id);

            var primeiroNome = nomeCliente.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nomeCliente;
            TempData["SucessoAvaliacao"] = $"Obrigado, {primeiroNome}! Sua avaliação foi enviada e será publicada após a análise da nossa equipe.";
            TempData["PrimeiroNomeAvaliacao"] = primeiroNome;
        }
        catch (ArgumentException ex)
        {
            TempData["ErroAvaliacao"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErroAvaliacao"] = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar avaliação para peça {PecaId}", dto.PecaId);
            TempData["ErroAvaliacao"] = "Não foi possível registrar sua avaliação. Tente novamente mais tarde.";
        }

        return RedirectToAction(nameof(Detalhes), "Home", new { id = dto.PecaId }, "avaliacoes");
    }


    [HttpGet]
    public IActionResult Sobre()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
