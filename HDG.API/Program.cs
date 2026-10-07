using System.Text;
using HDG.Application.Extensions;
using HDG.Infrastructure.Data;
using HDG.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// SERVICES
// ==========================================

// Infrastructure: DbContext + Identity (com Roles) + Repositories
builder.Services.AddInfrastructure(builder.Configuration);

// Application: AutoMapper + Serviços de Domínio/Aplicação
builder.Services.AddApplication();

// Controllers
builder.Services.AddControllers();

// Autenticação JWT
var chaveJwt = builder.Configuration["Jwt:Chave"] ?? "HDGRebaixadosChaveSuperSecreta2026ParaAutenticacaoComMaisDe32Bytes!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveJwt)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Emissor"] ?? "HDGRebaixadosAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audiencia"] ?? "HDGRebaixadosApp",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("HDGCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HDG Rebaixados API",
        Version = "v1",
        Description = "API REST do catálogo e painel administrativo da HDG Rebaixados",
        Contact = new OpenApiContact { Name = "HDG Rebaixados Suporte", Email = "contato@hdgrebaixados.com.br" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT retornado no login. Exemplo: 'eyJhbGci...'"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

var app = builder.Build();

// ==========================================
// SEED AUTOMÁTICO NA INICIALIZAÇÃO
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<HdgDbContext>();
        await db.Database.EnsureCreatedAsync(); // Garante banco e tabelas criados
        await DbSeeder.SeedAsync(services);     // Roles, admin e peças de demonstração
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao aplicar Seed inicial no banco de dados.");
    }
}

// ==========================================
// PIPELINE
// ==========================================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HDG Rebaixados API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("HDGCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
