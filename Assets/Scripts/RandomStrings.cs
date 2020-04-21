using System;
using System.Linq;

public static class RandomStrings
{
    private static Random random = new Random();
    private static string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string Generate(int length)
    {
        return new string(Enumerable.Repeat(charset, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
