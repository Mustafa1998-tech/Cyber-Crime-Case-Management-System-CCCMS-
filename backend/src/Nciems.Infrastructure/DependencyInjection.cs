using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nciems.Application.Interfaces;
using Nciems.Infrastructure.Options;
using Nciems.Infrastructure.Persistence;
using Nciems.Infrastructure.Security;
using Nciems.Infrastructure.Services;

namespace Nciems.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Missing connection string: DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EvidenceStorageOptions>(configuration.GetSection(EvidenceStorageOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEvidenceFileService, EvidenceFileService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<DbSeeder>();

        return services;
    }
}
