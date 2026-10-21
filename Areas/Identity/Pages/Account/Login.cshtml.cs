using System.ComponentModel.DataAnnotations;
using Blog.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Areas.Identity.Pages.Account;

public class LoginModel(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        public string Login { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return Page();

        var user = await users.FindByNameAsync(Input.Login)
            ?? await users.FindByEmailAsync(Input.Login);
        if (user is not null)
        {
            var result = await signInManager.PasswordSignInAsync(
                user, Input.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.Succeeded)
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        ModelState.AddModelError(string.Empty, "Неверное имя пользователя, почта или пароль.");
        return Page();
    }
}
