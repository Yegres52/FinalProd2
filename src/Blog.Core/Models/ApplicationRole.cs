using Microsoft.AspNetCore.Identity;

namespace Blog.Models;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }

    public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}
