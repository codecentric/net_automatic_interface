using System.Collections.Generic;

namespace AutomaticInterface
{
    public static class StringExtensions
    {
        public static IEnumerable<string> SplitToLines(this string? input)
        {
            if (input == null)
            {
                yield break;
            }

            using var reader = new System.IO.StringReader(input);
            while (reader.ReadLine() is { } line)
            {
                yield return line;
            }
        }
    }
}
