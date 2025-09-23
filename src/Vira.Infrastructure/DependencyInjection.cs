using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Vira.Application.Abstractions.Auth;
using Vira.Application.Abstractions.Files;
using Vira.Application.Abstractions.Persistence;
using Vira.Application.Abstractions.Repositories;
using Vira.Infrastructure.Auth;
using Vira.Infrastructure.Files;
using Vira.Infrastructure.Persistence.Read;
using Vira.Infrastructure.Repositories;

namespace Vira.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Default"),
                o => o.UseNetTopologySuite()
            )
        );

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<GeometryFactory>(_ => NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));

        services.AddScoped<IFileStorage, LocalFileStorage>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IReadDb, ReadDb>();

        return services;
    }
}
