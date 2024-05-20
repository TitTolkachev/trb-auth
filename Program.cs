using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using trb_auth;
using trb_auth.Common;
using trb_auth.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Firebase
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.GetApplicationDefault(),
    ProjectId = "trb-officer-android",
});

// Logger
builder.Services.AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultLogger"));

// Http Clients
builder.Services
    .AddHttpClient(
        Constants.UserHttpClient,
        client => { client.BaseAddress = new Uri(Constants.UserHost); })
    .AddResilienceHandler(
        "CustomPipeline",
        static builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = 50,
                UseJitter = true
            });
        });

builder.Services
    .AddHttpClient(
        Constants.CoreHttpClient,
        client => { client.BaseAddress = new Uri(Constants.CoreHost); })
    .AddResilienceHandler(
        "CustomPipeline",
        static builder =>
        {
            builder.AddRetry(new HttpRetryStrategyOptions
            {
                BackoffType = DelayBackoffType.Constant,
                MaxRetryAttempts = 50,
                UseJitter = true
            });
        });

builder.Services.AddHostedService<TransactionHandler>();

// DB
var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connection));

// -----------------------------------------------------------------------------------------------------

var app = builder.Build();

// Auto Migration
using var serviceScope = app.Services.CreateScope();
var context = serviceScope.ServiceProvider.GetService<ApplicationDbContext>();
context?.Database.Migrate();

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{app?}/{deviceId?}");

app.Run();