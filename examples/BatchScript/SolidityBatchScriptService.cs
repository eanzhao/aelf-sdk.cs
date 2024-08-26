using AElf.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp;

namespace BatchScript;

public class SolidityBatchScriptService : IHostedService
{
    private IAbpApplicationWithInternalServiceProvider _abpApplication;

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private ISolidityService _solidityService;
    private IBatchScriptHelper _batchScriptHelper;
    private  TestContractOptions  _testContractOptions;
    private ILogger<SolidityBatchScriptService> Logger { get; set; }

    public SolidityBatchScriptService(IConfiguration configuration, 
        IHostEnvironment hostEnvironment,ILogger<SolidityBatchScriptService> logger)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        Logger = logger;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _abpApplication = await AbpApplicationFactory.CreateAsync<BatchScriptModule>(options =>
        {
            var builder = new ConfigurationBuilder()
                .AddConfiguration(_configuration)
                .AddJsonFile($"appsettings.json")
                .AddJsonFile($"appsettings.local.json", true);
            options.Services.ReplaceConfiguration(builder.Build());
            options.Services.AddSingleton(_hostEnvironment);

            options.UseAutofac();
            options.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());
        });
        
        await _abpApplication.InitializeAsync();

        _batchScriptHelper = _abpApplication.ServiceProvider.GetRequiredService<IBatchScriptHelper>();
        _testContractOptions = _abpApplication.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestContractOptions>>().Value;
        
        if (_testContractOptions.TestAddress == "")
        {
            await _batchScriptHelper.DeployContract();
        }
        else
        {
            await _batchScriptHelper.SetContract(Address.FromBase58(_testContractOptions.TestAddress));
        }
        
        _solidityService = _abpApplication.ServiceProvider.GetRequiredService<ISolidityService>();
        // await _solidityService.TransferTokenForFee();
        await _solidityService.Initialize();
        await _solidityService.Mint();
        ExecuteTransactionsWithoutResultTask();
        
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _abpApplication.ShutdownAsync();
    }

    private void ExecuteTransactionsWithoutResultTask()
    {
        Logger.LogInformation("START");

        var txCts = new CancellationTokenSource();
        var txToken = txCts.Token;
        txCts.CancelAfter( _testContractOptions.TestDuration * 1000);

       var task = Task.Run(async () => await _solidityService.ExecuteBatchTransactionTask(txCts, txToken), txToken);

       Task.WhenAll(task);
        Logger.LogInformation("END");
    }
}