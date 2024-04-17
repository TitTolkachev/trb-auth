using System.Net;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using trb_auth.Common;

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
        // See: https://www.pollydocs.org/strategies/retry.html
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            // Customize and configure the retry logic.
            BackoffType = DelayBackoffType.Exponential,
            MaxRetryAttempts = 5,
            UseJitter = true
        });

        // See: https://www.pollydocs.org/strategies/circuit-breaker.html
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            // Customize and configure the circuit breaker logic.
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.7,
            MinimumThroughput = 10,
            ShouldHandle = static args => ValueTask.FromResult(args is
            {
                Outcome.Result.StatusCode:
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests or 
                HttpStatusCode.InternalServerError
            })
        });

        // See: https://www.pollydocs.org/strategies/timeout.html
        builder.AddTimeout(TimeSpan.FromSeconds(10));
    });

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();