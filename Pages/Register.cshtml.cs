using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Fourm.Models;
using Fourm.Services;

namespace Fourm.Pages;

/// <summary>
/// Page model for user registration
/// </summary>
public class RegisterPageModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty]
    public RegisterModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public RegisterPageModel(IUserService userService)
    {
        _userService = userService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validate the model
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if user already exists
        if (await _userService.UserExistsAsync(Input.Username))
        {
            ErrorMessage = "This username is already taken. Please choose another one!";
            return Page();
        }

        // Check if this is the first user (auto-make admin)
        var allUsers = await _userService.GetAllUsersAsync();
        bool isFirstUser = allUsers.Count == 0;

        // Register the user
        var success = await _userService.RegisterUserAsync(
            Input.Username,
            Input.Email ?? string.Empty,
            Input.Password
        );

        if (success)
        {
            // If first user, make them admin
            if (isFirstUser)
            {
                var newUser = await _userService.GetUserByUsernameAsync(Input.Username);
                if (newUser != null)
                {
                    newUser.IsAdmin = true;
                    await _userService.UpdateUserAsync(newUser);
                    TempData["SuccessMessage"] = "Account created successfully! You are now an admin. Please log in.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Account created successfully! Please log in to continue.";
            }
            
            return RedirectToPage("/Login");
        }
        else
        {
            ErrorMessage = "Something went wrong. Please try again!";
            return Page();
        }
    }
}
