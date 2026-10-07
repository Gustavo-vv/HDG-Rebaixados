using HDG.Application.Mapeamentos;
using HDG.Application.Servicos.Implementacoes;
using HDG.Application.Servicos.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HDG.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        // Application Services
        services.AddScoped<IPecaService, PecaService>();
        services.AddScoped<IAvaliacaoService, AvaliacaoService>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioAdminService, UsuarioAdminService>();

        return services;
    }
}
