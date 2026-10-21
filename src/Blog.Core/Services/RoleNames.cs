namespace Blog.Services;

public static class RoleNames
{
    public const string Admin = "Администратор";
    public const string Moderator = "Модератор";
    public const string User = "Пользователь";
}

public record ActorContext(string Id, bool IsAdmin, bool IsModerator);
