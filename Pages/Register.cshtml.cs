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
    private readonly IEmailService _emailService;

    [BindProperty]
    public RegisterModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public RegisterPageModel(IUserService userService, IEmailService emailService)
    {
        _userService = userService;
        _emailService = emailService;
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
            Input.Email,
            Input.Password,
            Input.Major
        );

        if (success)
        {
            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(Input.Email, Input.Username, Input.Major);
            }
            catch (Exception ex)
            {
                // Log error but don't fail registration
                Console.WriteLine($"Failed to send welcome email: {ex.Message}");
            }

            // If first user, make them admin
            if (isFirstUser)
            {
                var newUser = await _userService.GetUserByUsernameAsync(Input.Username);
                if (newUser != null)
                {
                    newUser.IsAdmin = true;
                    await _userService.UpdateUserAsync(newUser);
                    TempData["SuccessMessage"] = "Account created successfully! You are now an admin. Please check your email for confirmation. Please log in.";
                }
            }
            else
            {
                TempData["SuccessMessage"] = "Account created successfully! Please check your email for confirmation and log in to continue.";
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
