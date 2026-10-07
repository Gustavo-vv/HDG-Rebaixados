using HDG.Application.Extensions;
using HDG.Infrastructure.Data;
using HDG.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// SERVICES CONFIGURATION
// ==========================================

// Infrastructure (DbContext, Identity, Repositórios)
builder.Services.AddInfrastructure(builder.Configuration);

// Application (AutoMapper, Serviços de Negócio)
builder.Services.AddApplication();

// MVC com Controllers e Views
builder.Services.AddControllersWithViews();

// Configuração do validador de carimbo de segurança (Security Stamp)
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// Configuração do Cookie do Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "HDGRebaixados.Auth";
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnValidatePrincipal = async context =>
    {
        if (context.Principal == null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }

        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<HDG.Domain.Entidades.ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user == null || !user.Ativo)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// ==========================================
// SEED AUTOMÁTICO DE DADOS NO INÍCIO
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<HdgDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao inicializar o banco de dados e aplicar o Seed na HDG.UI.");
    }
}

// ==========================================
// HTTP REQUEST PIPELINE
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Atalhos para o painel Admin: /Admin/Usuarios, /Admin/Pecas, etc.
app.MapControllerRoute(
    name: "adminActions",
    pattern: "Admin/{action=Index}/{id?}",
    defaults: new { area = "Admin", controller = "Admin" });

// Rota de Áreas (Painel Admin completa)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

// Rota padrão do Catálogo e Páginas Públicas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
