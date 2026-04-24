using BomApp.Application.Interfaces.Repositories;
using BomApp.Infrastructure.Auth;
using BomApp.Infrastructure.Erp;
using BomApp.Infrastructure.Persistence;
using BomApp.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BomApp.Infrastructure;

/// <summary>
/// Extension methods สำหรับลงทะเบียน Infrastructure layer services ใน DI container
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ลงทะเบียน Infrastructure services ทั้งหมด — DbContexts และ Repositories
    /// เรียกใน Program.cs หรือ Startup
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // BOM database — schema "bom" (migrations history อยู่ใน public schema)
        services.AddDbContext<BomDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("bom-database"),
                o => o.MigrationsHistoryTable("__EFMigrationsHistory", "public")));

        // Authentication database — read-only
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("authentication-database")));

        // ERP database — read-only
        services.AddDbContext<ErpDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("erp-database")));

        // BOM Repositories
        services.AddScoped<IBomRepository, BomRepository>();
        services.AddScoped<IBomAssignmentRepository, BomAssignmentRepository>();
        services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();

        // Auth Repository
        services.AddScoped<IAuthRepository, AuthRepository>();

        // ERP Repositories
        services.AddScoped<IErpItemRepository, ErpItemRepository>();
        services.AddScoped<IErpSalesOrderRepository, ErpSalesOrderRepository>();

        return services;
    }
}
