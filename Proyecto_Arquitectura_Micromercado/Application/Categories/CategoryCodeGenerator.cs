using System.Globalization;
using System.Text;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

public static class CategoryCodeGenerator
{
    public const string Prefix = "CAT-";
    private const int LetterCount = 3;
    private const string Vowels = "AEIOU";

    public static bool HasEnoughLetters(string name) =>
        string.Concat(ExtractWords(name)).Length >= LetterCount;

    public static string? Generate(string name, IReadOnlySet<string> existingCodes)
    {
        var words = ExtractWords(name);
        var letters = string.Concat(words);

        if (letters.Length < LetterCount)
        {
            return null;
        }

        foreach (var candidate in Candidates(words, letters))
        {
            var code = Prefix + candidate;

            if (!existingCodes.Contains(code))
            {
                return code;
            }
        }

        return null;
    }

    private static IEnumerable<string> Candidates(IReadOnlyList<string> words, string letters)
    {
        var seen = new HashSet<string>();

        var baseCode = BuildBase(words);

        if (seen.Add(baseCode))
        {
            yield return baseCode;
        }

        var consonants = letters[1..].Where(letter => !Vowels.Contains(letter)).ToArray();

        for (var a = 0; a < consonants.Length; a++)
        {
            for (var b = a + 1; b < consonants.Length; b++)
            {
                var code = $"{letters[0]}{consonants[a]}{consonants[b]}";

                if (seen.Add(code))
                {
                    yield return code;
                }
            }
        }

        for (var i = 0; i < letters.Length - 2; i++)
        {
            for (var j = i + 1; j < letters.Length - 1; j++)
            {
                for (var k = j + 1; k < letters.Length; k++)
                {
                    var code = $"{letters[i]}{letters[j]}{letters[k]}";

                    if (seen.Add(code))
                    {
                        yield return code;
                    }
                }
            }
        }
    }

    private static string BuildBase(IReadOnlyList<string> words)
    {
        var code = new StringBuilder();

        foreach (var word in words.Take(LetterCount))
        {
            code.Append(word[0]);
        }

        var filler = words
            .Select((word, index) => index < LetterCount ? word[1..] : word)
            .SelectMany(rest => rest);

        foreach (var letter in filler)
        {
            if (code.Length == LetterCount)
            {
                break;
            }

            code.Append(letter);
        }

        return code.ToString();
    }

    private static List<string> ExtractWords(string name)
    {
        var words = new List<string>();
        var current = new StringBuilder();

        foreach (var character in (name ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            {
                current.Append(char.ToUpperInvariant(character));
            }
            else if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }
}
