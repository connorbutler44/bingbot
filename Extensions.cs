using System;
using System.Collections.Generic;
using System.Linq;

public static class Extensions
{
    public static IEnumerable<string> Chunk(this string str, int n)
    {
        if (String.IsNullOrEmpty(str) || n < 1)
        {
            throw new ArgumentException();
        }

        var chunkCount = (str.Length / n) + 1;
        return Enumerable
            .Range(0, chunkCount)
            .Select(i => (i == chunkCount - 1) ? str.Substring(i * n) : str.Substring(i * n, n));
    }
}