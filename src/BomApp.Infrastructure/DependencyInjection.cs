using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Infrastructure.Auth;
using BomApp.Infrastructure.Configuration;
using BomApp.Infrastructure.Erp;
using BomApp.Infrastructure.Persistence;
using BomApp.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BomApp.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure services: runtime configuration, DbContexts, and repositories.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IRuntimeConfigurationService>(_ =>
        {
            var service = new RuntimeConfigurationService(configuration);
            service.Load();
            return service;
        });

        services.AddDbContext<BomDbContext>((sp, options) =>
            options.UseNpgsql(
                sp.GetRequiredService<IRuntimeConfigurationService>().GetConnectionString("erp-database"),
                o => o.MigrationsHistoryTable("__EFMigrationsHistory", "public")));

        services.AddDbContext<AuthDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<IRuntimeConfigurationService>().GetConnectionString("authentication-database")));

        services.AddDbContext<ErpDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<IRuntimeConfigurationService>().GetConnectionString("erp-database")));
        services.AddDbContextFactory<ErpDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<IRuntimeConfigurationService>().GetConnectionString("erp-database")));

        services.AddScoped<IBomRepository, BomRepository>();
        services.AddScoped<IBomAssignmentRepository, BomAssignmentRepository>();
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();
        services.AddScoped<IBomProductionRepository, BomProductionRepository>();

        services.AddScoped<IAuthRepository, AuthRepository>();

        services.AddScoped<IErpItemRepository>(sp =>
            new ErpItemRepository(sp.GetRequiredService<IDbContextFactory<ErpDbContext>>()));
        services.AddScoped<IErpSalesOrderRepository, ErpSalesOrderRepository>();
        services.AddScoped<IErpProductionRepository, ErpProductionRepository>();
        services.AddSingleton<HttpClient>();
        services.AddScoped<IErpStockRequestProcessor, ErpStockRequestProcessor>();

        return services;
    }
}
