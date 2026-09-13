namespace LinkShortner.Services;

public static class ShortCodeGenerator
{
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string Generate(int length = 6)
    {
        var random = Random.Shared;
        return new String(Enumerable.Range(0, length).Select(_ => Chars[random.Next(Chars.Length)]).ToArray());
    }
}