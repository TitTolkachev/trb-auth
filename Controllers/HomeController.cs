using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using trb_auth.Common;
using trb_auth.Models;

namespace trb_auth.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index(string? app = null)
    {
        if (!Request.Cookies.ContainsKey("auth-token")) return View();

        var token = Request.Cookies["auth-token"];
        return Redirect(app == "client"
            ? $"trust-bank-client://sign-in/{token}"
            : $"trust-bank://sign-in/{token}");
    }

    public IActionResult Logout()
    {
        return View();
    }

    public IActionResult PerformLogout(string? app)
    {
        Response.Cookies.Delete("auth-token");
        return Redirect(app == "client"
            ? "trust-bank-client://logout"
            : "trust-bank://logout");
    }

    public async Task<IActionResult> Login(Credentials credentials, string? app)
    {
        try
        {
            var token = await GetSignInToken(credentials);
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

    private async Task<string> GetSignInToken(Credentials credentials)
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
        return await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(user.Uid);
    }
}