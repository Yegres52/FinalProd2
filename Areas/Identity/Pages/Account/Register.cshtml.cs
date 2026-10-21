using System.ComponentModel.DataAnnotations;
using Blog.Models;
using Blog.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Areas.Identity.Pages.Account;

public class RegisterModel(
    IUserService users,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, StringLength(256)]
        public string UserName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true
        ? LocalRedirect(Url.Content("~/"))
        : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.Identity?.IsAuthenticated == true) return Forbid();
        if (!ModelState.IsValid) return Page();

        var result = await users.RegisterAsync(
            new RegisterUserRequest(Input.UserName, Input.Email, Input.Password,
                Input.FirstName, Input.LastName));
        if (result.Status != OperationStatus.Success)
        {
            foreach (var error in result.Errors ?? [])
                ModelState.AddModelError(string.Empty, error);
            return Page();
        }

        var user = await userManager.FindByIdAsync(result.Value!.Id);
        await signInManager.SignInAsync(user!, isPersistent: false);
        return LocalRedirect(Url.Content("~/"));
    }
}
