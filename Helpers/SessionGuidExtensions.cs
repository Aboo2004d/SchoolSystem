public static class SessionGuidExtensions
{
    public static void SetGuid(this ISession session, string key, Guid value) =>
        session.SetString(key, value.ToString("D"));

    public static Guid? GetGuid(this ISession session, string key) =>
        Guid.TryParse(session.GetString(key), out var value) ? value : null;
}
