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

// MVC com Controllers e Views com validação global de Anti-Forgery Token
builder.Services.AddControllersWithViews(options =>
{
    // Aplica validação automática de CSRF em todas as ações de escrita (POST, PUT, DELETE, PATCH)
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Rate Limiting para rotas críticas (Login e Registro)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AuthRateLimitPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// Configuração do validador de carimbo de segurança (Security Stamp)
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// Configuração do Cookie do Identity com endurecimento de segurança
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "HDGRebaixados.Auth";
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Proteções avançadas de cookie
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

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

// Middleware de cabeçalhos de segurança (Security Headers)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();

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
