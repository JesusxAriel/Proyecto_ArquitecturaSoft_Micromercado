using System.Globalization;
using System.Text;

namespace Proyecto_Arquitectura_Micromercado.Application.Categories;

/// <summary>
/// Genera el código de categoría "CAT-" + 3 letras mayúsculas (sin tildes, espacios ni números)
/// a partir del nombre. Es determinista: el mismo nombre y los mismos códigos ocupados dan
/// siempre el mismo resultado. Nunca agrega números ni más caracteres; ante una colisión prueba
/// otras combinaciones de 3 letras tomadas del propio nombre, en este orden:
///   1. Base: iniciales de las palabras (hasta 3) completadas con las siguientes letras de la
///      primera palabra. "Lácteos" -> LAC, "Jugos de Frutas" -> JDF, "Cuidado Personal" -> CPU.
///   2. Primera letra del nombre + dos consonantes siguientes, en orden. "Lacticinios" -> LCT.
///   3. Cualquier combinación de 3 letras del nombre conservando su orden (posiciones i&lt;j&lt;k).
/// Si ninguna combinación está libre devuelve null y el llamador debe pedir otro nombre.
/// </summary>
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

        // Fase 1: base.
        var baseCode = BuildBase(words);

        if (seen.Add(baseCode))
        {
            yield return baseCode;
        }

        // Fase 2: primera letra + dos consonantes posteriores.
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

        // Fase 3: todas las combinaciones de 3 posiciones en orden creciente.
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

        // Completa con las letras restantes empezando por la primera palabra.
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

    // Palabras en mayúsculas A-Z sin tildes; cualquier otro carácter separa palabras.
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
