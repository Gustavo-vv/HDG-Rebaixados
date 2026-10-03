using System.Diagnostics;
using HDG.Application.DTOs;
using HDG.Application.Servicos.Services;
using HDG.Domain.Enums;
using HDG.UI.Models;
using Microsoft.AspNetCore.Mvc;

namespace HDG.UI.Controllers;

public class HomeController : Controller
{
    private readonly IPecaService _pecaService;
    private readonly IAvaliacaoService _avaliacaoService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IPecaService pecaService,
        IAvaliacaoService avaliacaoService,
        IConfiguration configuration,
        ILogger<HomeController> logger)
    {
        _pecaService = pecaService;
        _avaliacaoService = avaliacaoService;
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

        return View(peca);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarAvaliacao([FromForm] CriarAvaliacaoDto dto)
    {
        try
        {
            if (dto.Nota < 1 || dto.Nota > 5)
            {
                TempData["ErroAvaliacao"] = "A nota deve ser entre 1 e 5 estrelas.";
                return RedirectToAction(nameof(Detalhes), new { id = dto.PecaId });
            }

            if (string.IsNullOrWhiteSpace(dto.NomeCliente))
            {
                TempData["ErroAvaliacao"] = "Informe seu nome para enviar a avaliação.";
                return RedirectToAction(nameof(Detalhes), new { id = dto.PecaId });
            }

            await _avaliacaoService.CriarAsync(dto);
            TempData["SucessoAvaliacao"] = "Obrigado! Sua avaliação foi enviada e será exibida após moderação.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar avaliação para peça {PecaId}", dto.PecaId);
            TempData["ErroAvaliacao"] = "Não foi possível registrar sua avaliação. Tente novamente mais tarde.";
        }

        return RedirectToAction(nameof(Detalhes), new { id = dto.PecaId });
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
