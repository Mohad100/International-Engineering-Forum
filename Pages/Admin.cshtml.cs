using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Fourm.Models;
using Fourm.Services;

namespace Fourm.Pages;

[Authorize]
public class AdminModel : PageModel
{
    private readonly IForumService _forumService;
    private readonly IUserService _userService;

    public List<ForumThread> Threads { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<ForumCategory> Categories { get; set; } = new();

    public int TotalThreads => Threads.Count;
    public int ViolatedThreads => Threads.Count(t => t.IsViolation);
    public int ViolatedReplies => Threads.SelectMany(t => t.Replies).Count(r => r.IsViolation);
    public int TotalUsers => Users.Count;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public AdminModel(IForumService forumService, IUserService userService)
    {
        _forumService = forumService;
        _userService = userService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is admin
        var currentUser = await _userService.GetUserByUsernameAsync(User.Identity?.Name ?? "");
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return RedirectToPage("/Index");
        }

        await LoadDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMarkViolationAsync(Guid threadId, string violationReason)
    {
        var currentUser = await _userService.GetUserByUsernameAsync(User.Identity?.Name ?? "");
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return RedirectToPage("/Index");
        }

        var success = await _forumService.MarkThreadAsViolationAsync(threadId, violationReason, User.Identity?.Name ?? "Admin");
        
        if (success)
        {
            SuccessMessage = "Thread marked as violation successfully!";
        }
        else
        {
            ErrorMessage = "Failed to mark thread as violation.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveViolationAsync(Guid threadId)
    {
        var currentUser = await _userService.GetUserByUsernameAsync(User.Identity?.Name ?? "");
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return RedirectToPage("/Index");
        }

        var success = await _forumService.RemoveViolationFromThreadAsync(threadId);
        
        if (success)
        {
            SuccessMessage = "Violation removed successfully!";
        }
        else
        {
            ErrorMessage = "Failed to remove violation.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteThreadAsync(Guid threadId)
    {
        var currentUser = await _userService.GetUserByUsernameAsync(User.Identity?.Name ?? "");
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return RedirectToPage("/Index");
        }

        var success = await _forumService.AdminDeleteThreadAsync(threadId);
        
        if (success)
        {
            SuccessMessage = "Thread deleted successfully!";
        }
        else
        {
            ErrorMessage = "Failed to delete thread.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMakeAdminAsync(string userId)
    {
        var currentUser = await _userService.GetUserByUsernameAsync(User.Identity?.Name ?? "");
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return RedirectToPage("/Index");
        }

        var targetUser = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == userId);
        if (targetUser == null)
        {
            ErrorMessage = "User not found.";
            return RedirectToPage();
        }

        if (targetUser.IsAdmin)
        {
            ErrorMessage = "User is already an admin.";
            return RedirectToPage();
        }

        targetUser.IsAdmin = true;
        var success = await _userService.UpdateUserAsync(targetUser);

        if (success)
        {
            SuccessMessage = $"{targetUser.Username} is now an admin!";
        }
        else
        {
            ErrorMessage = "Failed to make user an admin.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAdminAsync(string userId)
    {
        var currentUser = await _userService.GetUserByUsernameAsync(User.Identity?.Name ?? "");
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return RedirectToPage("/Index");
        }

        var targetUser = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == userId);
        if (targetUser == null)
        {
            ErrorMessage = "User not found.";
            return RedirectToPage();
        }

        if (!targetUser.IsAdmin)
        {
            ErrorMessage = "User is not an admin.";
            return RedirectToPage();
        }

        if (targetUser.Username == User.Identity?.Name)
        {
            ErrorMessage = "You cannot remove your own admin privileges.";
            return RedirectToPage();
        }

        targetUser.IsAdmin = false;
        var success = await _userService.UpdateUserAsync(targetUser);

        if (success)
        {
            SuccessMessage = $"Admin privileges removed from {targetUser.Username}.";
        }
        else
        {
            ErrorMessage = "Failed to remove admin privileges.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        var currentUser = await _userService.GetUserByUsernameAsync(User.Identity?.Name ?? "");
        if (currentUser == null || !currentUser.IsAdmin)
        {
            return RedirectToPage("/Index");
        }

        var targetUser = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Id == userId);
        if (targetUser == null)
        {
            ErrorMessage = "User not found.";
            return RedirectToPage();
        }

        if (targetUser.Username == User.Identity?.Name)
        {
            ErrorMessage = "You cannot delete your own account.";
            return RedirectToPage();
        }

        var success = await _userService.DeleteUserAsync(userId);

        if (success)
        {
            SuccessMessage = $"User {targetUser.Username} has been deleted.";
        }
        else
        {
            ErrorMessage = "Failed to delete user.";
        }

        return RedirectToPage();
    }

    private async Task LoadDataAsync()
    {
        Threads = await _forumService.GetAllThreadsWithViolationsAsync();
        Users = await _userService.GetAllUsersAsync();
        
        // Define categories
        Categories = new List<ForumCategory>
        {
            new ForumCategory { Id = "general", Name = "General Discussion", Icon = "💬", Description = "Talk about anything" },
            new ForumCategory { Id = "education", Name = "Education", Icon = "📚", Description = "Learning and academic topics" },
            new ForumCategory { Id = "science", Name = "Science & Technology", Icon = "🔬", Description = "Scientific discoveries and tech innovations" },
            new ForumCategory { Id = "entertainment", Name = "Entertainment", Icon = "🎬", Description = "Movies, TV, music, and more" },
            new ForumCategory { Id = "sports", Name = "Sports & Fitness", Icon = "⚽", Description = "Athletic activities and health" },
            new ForumCategory { Id = "gaming", Name = "Gaming", Icon = "🎮", Description = "Video games and esports" },
            new ForumCategory { Id = "programming", Name = "Programming", Icon = "💻", Description = "Coding and software development" },
            new ForumCategory { Id = "arts", Name = "Arts & Creativity", Icon = "🎨", Description = "Art, design, and creative projects" }
        };
    }
}
