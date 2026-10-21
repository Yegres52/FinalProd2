using Microsoft.AspNetCore.Identity;

namespace Blog.Models;

public class ApplicationRole : IdentityRole
{
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}
