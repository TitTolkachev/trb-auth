using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trb_auth.Common;
using trb_auth.Entities;
using trb_auth.Models;

namespace trb_auth.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory,
        ApplicationDbContext context)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    public async Task<IActionResult> Index(string? app = null, string? deviceId = null)
    {
        if (!Request.Cookies.ContainsKey("auth-token")) return View();

        var token = Request.Cookies["auth-token"];
        var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
        decoded.Claims.TryGetValue("id", out var userId);

        if (deviceId != null && userId != null)
        {
            await SaveDeviceId(deviceId, userId.ToString(), app);
        }

        return Redirect(app == "client"
            ? $"trust-bank-client://sign-in/{token}"
            : $"trust-bank://sign-in/{token}");
    }

    public IActionResult Logout()
    {
        return View();
    }

    public async Task<IActionResult> PerformLogout(string? app, string? deviceId)
    {
        var entity = await _context.Devices.FirstOrDefaultAsync(x => x.DeviceId == deviceId && x.App == app);
        if (entity != null)
        {
            _context.Devices.Remove(entity);
            await _context.SaveChangesAsync();
        }

        Response.Cookies.Delete("auth-token");

        return Redirect(app == "client"
            ? "trust-bank-client://logout"
            : "trust-bank://logout");
    }

    public async Task<IActionResult> Login(Credentials credentials, string? app, string? deviceId)
    {
        try
        {
            var token = await GetSignInToken(credentials, app, deviceId);
            Response.Cookies.Append("auth-token", token);
            return Redirect(app == "client"
                ? $"trust-bank-client://sign-in/{token}"
                : $"trust-bank://sign-in/{token}");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Login FAILED: {Response}", e.Message);
            return Unauthorized();
        }
    }

    private async Task<string> GetSignInToken(Credentials credentials, string? app, string? deviceId)
    {
        var httpClient = _httpClientFactory.CreateClient(Constants.UserHttpClient);
        var response = await httpClient.PostAsJsonAsync("users/ident-user", credentials);
        if (!response.IsSuccessStatusCode)
            _logger.LogInformation("IdentUser FAILED: {Response}", response.ToString());
        response.EnsureSuccessStatusCode();

        var userId = await response.Content.ReadFromJsonAsync<string>();
        if (userId == null)
            throw new Exception("IdentUser FAILED: userId == null");

        var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(credentials.Email);

        if (deviceId != null)
        {
            await SaveDeviceId(deviceId, userId, app);
        }

        return await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(user.Uid);
    }

    private async Task SaveDeviceId(string deviceId, string? userId, string? app)
    {
        await _context.Devices.AddAsync(new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            UserId = userId,
            App = app
        });
        await _context.SaveChangesAsync();
    }
}