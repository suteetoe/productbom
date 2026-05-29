using System.CommandLine;
using BomApp.Application;
using BomApp.Application.Interfaces;
using BomApp.Infrastructure;
using BomApp.Shared.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Build configuration from appsettings.json + appsettings.Development.json + env vars
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// Build DI container
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddApplicationServices();
services.AddInfrastructureServices(config);
var sp = services.BuildServiceProvider();

// Root command
var rootCommand = new RootCommand("BOM Production Calculator CLI");

// Define 'calculate' sub-command
var calculateCommand = new Command("calculate", "คำนวณวัตถุดิบจากรายการขาย");

var fromOption = new Option<string>("--from")
{
    Description = "วันที่เริ่มต้น (yyyy-MM-dd)",
};
var toOption = new Option<string>("--to")
{
    Description = "วันที่สิ้นสุด (yyyy-MM-dd)",
};
var modeOption = new Option<string>("--mode")
{
    Description = "รูปแบบบันทึก: daily|per-document",
};
var dryRunOption = new Option<bool>("--dry-run")
{
    Description = "คำนวณแต่ไม่บันทึก",
};
var outputOption = new Option<string>("--output")
{
    Description = "csv|json|none",
};

calculateCommand.Add(fromOption);
calculateCommand.Add(toOption);
calculateCommand.Add(modeOption);
calculateCommand.Add(dryRunOption);
calculateCommand.Add(outputOption);

calculateCommand.SetAction(parseResult =>
{
    var fromStr  = parseResult.GetValue(fromOption) ?? "";
    var toStr    = parseResult.GetValue(toOption) ?? "";

    if (!DateOnly.TryParse(fromStr, out var dateFrom))
    {
        Console.Error.WriteLine($"Invalid --from date '{fromStr}'. Use yyyy-MM-dd");
        Environment.Exit(4);
        return;
    }

    if (!DateOnly.TryParse(toStr, out var dateTo))
    {
        Console.Error.WriteLine($"Invalid --to date '{toStr}'. Use yyyy-MM-dd");
        Environment.Exit(4);
        return;
    }

    var modeStr = parseResult.GetValue(modeOption) ?? "daily";
    var mode    = modeStr == "per-document" ? SaveMode.PerDocument : SaveMode.Daily;
    var dryRun  = parseResult.GetValue(dryRunOption);

    var useCase = sp.GetRequiredService<ICalculateSalesProductionUseCase>();
    var request = new CalculateSalesProductionRequest(
        DateFrom:   dateFrom,
        DateTo:     dateTo,
        Mode:       mode,
        DryRun:     dryRun,
        CreatedBy:  "SYSTEM",
        CreatedVia: "CLI");

    var calcResult = useCase.CalculateAsync(request).GetAwaiter().GetResult();

    if (!calcResult.IsSuccess)
    {
        Console.Error.WriteLine($"Error: {calcResult.Error}");
        // Exit code 1 = no sales transactions in date range
        Environment.Exit(1);
        return;
    }

    var productionResult = calcResult.Value!;
    Console.WriteLine($"Items processed:          {productionResult.Items.Count}");
    Console.WriteLine($"Items skipped (no BOM):   {productionResult.SkippedItemCount}");
    Console.WriteLine($"Total materials:          {productionResult.Materials.Count}");

    if (productionResult.SkippedItemCount > 0)
    {
        Console.WriteLine($"Skipped item codes: {string.Join(", ", productionResult.SkippedItemCodes)}");
    }

    if (dryRun)
    {
        Console.WriteLine("Dry-run mode — no production documents written.");
        return;
    }

    var saveResult = useCase.SaveAsync(request).GetAwaiter().GetResult();

    if (!saveResult.IsSuccess)
    {
        Console.Error.WriteLine($"Save error: {saveResult.Error}");
        Environment.Exit(3);
        return;
    }

    Console.WriteLine($"Production documents created: {saveResult.Value!.Count}");
});

rootCommand.Add(calculateCommand);
return await rootCommand.Parse(args).InvokeAsync();
