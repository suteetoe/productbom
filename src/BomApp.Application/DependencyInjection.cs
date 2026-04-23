using BomApp.Application.Interfaces;
using BomApp.Application.Services;
using BomApp.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace BomApp.Application;

/// <summary>
/// Extension methods สำหรับลงทะเบียน Application layer services ใน DI container
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// ลงทะเบียน Application services ทั้งหมด
    /// เรียกใน Program.cs หรือ Startup
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IBomService, BomService>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<ICalculateSalesProductionUseCase, CalculateSalesProductionUseCase>();
        services.AddScoped<IBomAssignmentService, BomAssignmentService>();
        return services;
    }
}
