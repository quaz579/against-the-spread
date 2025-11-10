using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(AgainstTheSpread.Functions.Startup))]

namespace AgainstTheSpread.Functions;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        // Register application services
        builder.Services.AddSingleton<IExcelService, ExcelService>();
        builder.Services.AddSingleton<IStorageService>(sp =>
        {
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";
            var excelService = sp.GetRequiredService<IExcelService>();
            return new StorageService(connectionString, excelService);
        });
    }
}
