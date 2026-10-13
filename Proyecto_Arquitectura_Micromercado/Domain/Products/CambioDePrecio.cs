namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

public static class CambioDePrecio
{
    public const string MotivoPorDefecto = "Actualización de precio";

    public const string MotivoInvalidoMessage =
        "Ingresa una justificación de al menos 3 caracteres. Evita etiquetas HTML, comillas y caracteres de control.";

    private const int LargoMinimoDelMotivo = 3;

    private static readonly char[] CaracteresProhibidos =
        ['<', '>', '"', '\'', '`', '\\', ';'];

    public static bool Hubo(
        decimal precioVentaAnterior,
        decimal precioCostoAnterior,
        decimal precioVentaNuevo,
        decimal precioCostoNuevo) =>
        precioVentaAnterior != precioVentaNuevo ||
        precioCostoAnterior != precioCostoNuevo;

    public static string NormalizarMotivo(string? valor)
    {
        var normalizado = string.Join(
            " ",
            (valor ?? string.Empty).TrimStart().Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrEmpty(normalizado))
        {
            return string.Empty;
        }

        var indiceDeLaPrimeraLetra = normalizado
            .Select((caracter, indice) => (caracter, indice))
            .FirstOrDefault(item => char.IsLetter(item.caracter))
            .indice;

        if (indiceDeLaPrimeraLetra == 0 && char.IsLetter(normalizado[0]))
        {
            return char.ToUpperInvariant(normalizado[0]) + normalizado[1..];
        }

        if (indiceDeLaPrimeraLetra > 0)
        {
            return normalizado[..indiceDeLaPrimeraLetra] +
                char.ToUpperInvariant(normalizado[indiceDeLaPrimeraLetra]) +
                normalizado[(indiceDeLaPrimeraLetra + 1)..];
        }

        return normalizado;
    }

    public static bool EsMotivoValido(string? motivo) =>
        motivo is not null &&
        motivo.Length >= LargoMinimoDelMotivo &&
        !motivo.Any(caracter =>
            char.IsControl(caracter) ||
            CaracteresProhibidos.Contains(caracter));

    public static string? ResolverMotivo(bool huboCambio, string? motivoIngresado) =>
        huboCambio ? NormalizarMotivo(motivoIngresado) : null;
}
