namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

// Reglas de negocio del cambio de precio de un producto, en un solo lugar.
//
// POR QUE EXISTE
// Estas reglas estaban repartidas y duplicadas en tres capas a la vez:
//
//   - "hubo cambio de precio" se calculaba en el PageModel de Productos para exigir
//     una justificacion, y otra vez en cada repositorio para decidir si registrar el
//     historial: tres copias de la misma comparacion.
//   - normalizar y validar el motivo vivian como metodos privados del PageModel, asi
//     que el servicio NO las aplicaba: cualquier otra entrada al sistema (una API, o
//     el VentaFacade) se las saltaba y guardaba un motivo sin validar.
//   - el texto del motivo por defecto estaba escrito a mano en los dos repositorios.
//
// Son dos decisiones distintas que comparten una comparacion: exigir justificacion es
// una regla de aplicacion, y registrar el historial es una consecuencia en persistencia.
// Lo que no puede estar duplicado es la comparacion, y ahora tiene una sola definicion.
public static class CambioDePrecio
{
    // Motivo que se guarda cuando el precio cambia y nadie escribio una justificacion
    // propia (por ejemplo, una actualizacion hecha desde un proceso automatico).
    public const string MotivoPorDefecto = "Actualización de precio";

    public const string MotivoInvalidoMessage =
        "Ingresa una justificación de al menos 3 caracteres. Evita etiquetas HTML, comillas y caracteres de control.";

    private const int LargoMinimoDelMotivo = 3;

    // Caracteres que podrian terminar en una inyeccion al mostrarse o al consultarse.
    private static readonly char[] CaracteresProhibidos =
        ['<', '>', '"', '\'', '`', '\\', ';'];

    // Unica definicion de "hubo cambio de precio" del sistema.
    public static bool Hubo(
        decimal precioVentaAnterior,
        decimal precioCostoAnterior,
        decimal precioVentaNuevo,
        decimal precioCostoNuevo) =>
        precioVentaAnterior != precioVentaNuevo ||
        precioCostoAnterior != precioCostoNuevo;

    // Colapsa los espacios internos y pone en mayuscula la primera letra, dejando
    // intactos los signos que la precedan (por ejemplo: "  (ajuste) pil" -> "(Ajuste) Pil"
    // conserva el parentesis inicial).
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

    // Si el precio cambio hay que justificarlo; si no cambio, el motivo no corresponde.
    public static string? ResolverMotivo(bool huboCambio, string? motivoIngresado) =>
        huboCambio ? NormalizarMotivo(motivoIngresado) : null;
}
