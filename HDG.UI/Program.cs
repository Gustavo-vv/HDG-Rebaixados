using HDG.Application.Extensions;
using HDG.Infrastructure.Data;
using HDG.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;

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

// Autenticação baseada em Cookies para a aplicação Web MVC
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "HDGRebaixados.Auth";
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AcessoNegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
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

// Rota de Áreas (Painel Admin)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Admin}/{action=Index}/{id?}");

// Atalho /Admin -> Area Admin
app.MapControllerRoute(
    name: "adminShortcut",
    pattern: "Admin",
    defaults: new { area = "Admin", controller = "Admin", action = "Index" });

// Rota padrão do Catálogo e Páginas Públicas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
