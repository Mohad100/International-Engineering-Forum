using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Fourm.Services;

namespace Fourm.Pages;

public class SetupAdminModel : PageModel
{
    private readonly IUserService _userService;

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public SetupAdminModel(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "يرجى إدخال اسم المستخدم وكلمة المرور";
            return Page();
        }

        // Check if user exists
        var user = await _userService.GetUserByUsernameAsync(Username);
        if (user == null)
        {
            ErrorMessage = "المستخدم غير موجود";
            return Page();
        }

        // Verify password
        if (user.Password != Password) // In production, use proper password hashing
        {
            ErrorMessage = "كلمة المرور غير صحيحة";
            return Page();
        }

        // Check if already admin
        if (user.IsAdmin)
        {
            Message = "أنت مدير بالفعل! يمكنك الوصول إلى صفحة الإدارة";
            return Page();
        }

        // Make user admin
        user.IsAdmin = true;
        await _userService.UpdateUserAsync(user);

        Message = "تم! أنت الآن مدير. يمكنك الوصول إلى صفحة الإدارة.";
        return Page();
    }
}
