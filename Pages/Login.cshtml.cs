using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Fourm.Models;
using Fourm.Services;

namespace Fourm.Pages;

/// <summary>
/// Page model for user login
/// </summary>
public class LoginPageModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty]
    public LoginModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public LoginPageModel(IUserService userService)
    {
        _userService = userService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // Validate the model
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Validate user credentials
        var user = await _userService.ValidateUserAsync(Input.Username, Input.Password);

        if (user == null)
        {
            ErrorMessage = "Invalid username or password. Please try again!";
            return Page();
        }

        // Create claims for the authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Add admin role claim if user is admin
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Sign in the user
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            claimsPrincipal,
            new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = Input.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(2)
            });

        // Redirect to the Forum page after successful login
        return RedirectToPage("/Forum");
    }
}
