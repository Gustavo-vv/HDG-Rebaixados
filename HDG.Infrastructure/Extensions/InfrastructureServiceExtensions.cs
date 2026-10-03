using HDG.Domain.Entidades;
using HDG.Domain.Interfaces.Repositories;
using HDG.Infrastructure.Data;
using HDG.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HDG.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. DbContext (SQL Server)
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=(localdb)\\mssqllocaldb;Database=HDGRebaixadosDb;Trusted_Connection=True;MultipleActiveResultSets=true";

        services.AddDbContext<HdgDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 2. Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<HdgDbContext>()
        .AddDefaultTokenProviders();

        // 3. Repositories
        services.AddScoped<IPecaRepository, PecaRepository>();
        services.AddScoped<IAvaliacaoRepository, AvaliacaoRepository>();

        return services;
    }
}
