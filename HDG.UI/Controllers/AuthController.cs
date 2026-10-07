using HDG.Application.DTOs;
using HDG.Domain.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HDG.UI.Controllers;

public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin", new { area = "Admin" });

            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("AuthRateLimitPolicy")]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.Ativo)
        {
            // Mensagem genérica para evitar enumeração de contas e proteção contra contas inativas
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(model);
        }

        // lockoutOnFailure: true garante a trava de segurança por tentativas excessivas (brute-force)
        var result = await _signInManager.PasswordSignInAsync(user.UserName ?? model.Email, model.Senha, isPersistent: true, lockoutOnFailure: true);
        
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde.");
            return View(model);
        }

        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Admin", new { area = "Admin" });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Registro(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("AuthRateLimitPolicy")]
    public async Task<IActionResult> Registro(RegistroDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        if (model.Senha != model.ConfirmarSenha)
        {
            ModelState.AddModelError(nameof(model.ConfirmarSenha), "As senhas não coincidem.");
            return View(model);
        }

        var userExistente = await _userManager.FindByEmailAsync(model.Email);
        if (userExistente != null)
        {
            ModelState.AddModelError(string.Empty, "Já existe um cadastro com este e-mail.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            NomeCompleto = model.NomeCompleto.Trim(),
            Ativo = true,
            DataCadastro = DateTime.UtcNow
        };

        var resultado = await _userManager.CreateAsync(user, model.Senha);
        if (resultado.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Cliente");
            await _signInManager.SignInAsync(user, isPersistent: true);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AcessoNegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
