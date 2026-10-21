namespace Blog.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string? CreatorId { get; set; }
    public ApplicationUser? Creator { get; set; }

    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}
