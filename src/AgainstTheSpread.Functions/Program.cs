using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        // Register application services
        services.AddSingleton<IExcelService, ExcelService>();
        services.AddSingleton<IStorageService>(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";
            var excelService = sp.GetRequiredService<IExcelService>();
            return new StorageService(connectionString, excelService);
        });
    })
    .Build();

host.Run();