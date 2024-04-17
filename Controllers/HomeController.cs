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

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Login(Credentials credentials)
    {
        try
        {
            var token = await GetSignInToken(credentials);
            return Redirect($"trust-bank://sign-in/{token}");
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