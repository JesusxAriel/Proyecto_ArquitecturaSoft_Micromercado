using System.Globalization;
using System.Text;
using Proyecto_Arquitectura_Micromercado.Domain.Common;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

internal static class TextoEnMemoria
{
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

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

    public static bool Contiene(string? campo, string? busqueda) =>
        Normalizar(campo).Contains(Normalizar(busqueda), StringComparison.Ordinal);
}

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
