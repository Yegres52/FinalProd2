using Blog.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) :
    IdentityDbContext<ApplicationUser, ApplicationRole, string,
        Microsoft.AspNetCore.Identity.IdentityUserClaim<string>, ApplicationUserRole,
        Microsoft.AspNetCore.Identity.IdentityUserLogin<string>,
        Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>,
        Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostTag> PostTags => Set<PostTag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUserRole>(entity =>
        {
            entity.HasOne(userRole => userRole.User)
                .WithMany(user => user.UserRoles)
                .HasForeignKey(userRole => userRole.UserId);
            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.UserRoles)
                .HasForeignKey(userRole => userRole.RoleId);
        });

        builder.Entity<Post>(entity =>
        {
            entity.Property(post => post.Title).IsRequired().HasMaxLength(200);
            entity.Property(post => post.Content).IsRequired();
            entity.HasOne(post => post.Author)
                .WithMany(user => user.Posts)
                .HasForeignKey(post => post.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.Property(tag => tag.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(tag => tag.Name).IsUnique();
            entity.HasOne(tag => tag.Creator)
                .WithMany(user => user.Tags)
                .HasForeignKey(tag => tag.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Comment>(entity =>
        {
            entity.Property(comment => comment.Content).IsRequired().HasMaxLength(2000);
            entity.HasOne(comment => comment.Author)
                .WithMany(user => user.Comments)
                .HasForeignKey(comment => comment.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(comment => comment.Post)
                .WithMany(post => post.Comments)
                .HasForeignKey(comment => comment.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PostTag>(entity =>
        {
            entity.HasKey(postTag => new { postTag.PostId, postTag.TagId });
            entity.HasOne(postTag => postTag.Post)
                .WithMany(post => post.PostTags)
                .HasForeignKey(postTag => postTag.PostId);
            entity.HasOne(postTag => postTag.Tag)
                .WithMany(tag => tag.PostTags)
                .HasForeignKey(postTag => postTag.TagId);
        });
    }
}
