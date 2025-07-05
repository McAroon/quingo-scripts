using Amazon.S3;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml;
using Quingo.Infrastructure;
using Quingo.Infrastructure.Database;
using Quingo.Infrastructure.Database.Repos;
using Quingo.Infrastructure.Files;
using Quingo.Scripts;
using Quingo.Scripts.ApiFootball;
using Quingo.Scripts.Excel;
using Quingo.Scripts.Footbingo;
using Quingo.Scripts.Transfermarkt;
using StackExchange.Redis;

var configuration = new ConfigurationBuilder().AddJsonFile("scriptsAppsettings.json").Build();
var connectionString = configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
    services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
    services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
    services.AddTransient<GenerateStandardBingo>();

    services.AddTransient<ApiFootballClient>();

    // S3
    var s3Settings = configuration.GetSection(nameof(S3Settings)).Get<S3Settings>() ?? new S3Settings();
    services.AddSingleton<IAmazonS3>(_ =>
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = s3Settings.Endpoint,
            ForcePathStyle = true,
        };

        return new AmazonS3Client(s3Settings.AccessKey, s3Settings.SecretKey, s3Config);
    });

    // Redis
    services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost"));
    services.AddTransient<RedisCacheService>();
    
    services.AddScoped<FileStoreService>();
    services.Configure<FileStoreSettings>(configuration.GetSection(nameof(FileStoreSettings)));
    services.Configure<ScriptsSettings>(configuration.GetSection(nameof(ScriptsSettings)));

    services.AddTransient<TransfermarktClient>();
    services.AddTransient<GenerateFootballBingoTm>();
    services.AddTransient<FbPicUpdate>();
    services.AddTransient<GoogleClient>();
    services.AddTransient<PackRepo>();
    services.AddTransient<ICacheService, NoopCache>();
    services.AddTransient<FileService>();
    services.AddTransient<ExcelService>();
    services.AddTransient<ITransfermarktService, TransfermarktRedisService>();
    services.AddTransient<QuingoImportService>();
    services.AddTransient<FootbingoUpdater>();
});
var host = builder.Build();

ExcelPackage.License.SetNonCommercialPersonal("QuingoScripts");

var svc = host.Services.GetRequiredService<FootbingoUpdater>();
// var svc = host.Services.GetRequiredService<GenerateFootballBingoTm>();
// var svc = host.Services.GetRequiredService<FbPicUpdate>();

await svc.Execute();