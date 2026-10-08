using System.Globalization;
using System.Text;
using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

// Reproduce en memoria el comportamiento de la collation utf8mb4_unicode_ci de MySQL:
// las comparaciones ignoran mayúsculas, tildes y espacios repetidos. Sin esto el motor
// en memoria no respetaría el mismo contrato que el motor MySQL y se rompería el LSP.
internal static class TextoEnMemoria
{
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        // Colapsa los espacios internos, igual que REGEXP_REPLACE(.., '[[:space:]]+', ' ').
        var sinEspaciosExtra = string.Join(
            ' ',
            valor.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        var descompuesto = sinEspaciosExtra.Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder(descompuesto.Length);

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                sinTildes.Append(caracter);
            }
        }

        return sinTildes
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    public static bool SonIguales(string? izquierda, string? derecha) =>
        string.Equals(Normalizar(izquierda), Normalizar(derecha), StringComparison.Ordinal);

    // Equivalente a "campo LIKE '%busqueda%'" con la collation insensible.
    public static bool Contiene(string? campo, string? busqueda) =>
        Normalizar(campo).Contains(Normalizar(busqueda), StringComparison.Ordinal);
}

// Equivalente en memoria de PagedSqlRunner: cuenta el total y devuelve una página.
internal static class PaginadorEnMemoria
{
    public static PagedResult<T> Paginar<T>(IEnumerable<T> origen, int page, int pageSize)
    {
        var coincidencias = origen.ToList();

        return new PagedResult<T>
        {
            Items = coincidencias
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = coincidencias.Count
        };
    }
}
