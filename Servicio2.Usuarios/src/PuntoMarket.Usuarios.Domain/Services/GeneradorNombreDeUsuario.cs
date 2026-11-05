using System.Text;

namespace PuntoMarket.Usuarios.Domain.Services;

public static class GeneradorNombreDeUsuario
{
    public static string GenerarBase(string? nombre, string? apellido)
    {
        var nombreNormalizado = NormalizarEspacios(nombre);
        var apellidoNormalizado = NormalizarEspacios(apellido);
        if (nombreNormalizado.Length == 0 || apellidoNormalizado.Length == 0)
            throw new ArgumentException("Nombre y apellido son obligatorios.");

        var letrasNombre = Letras(nombreNormalizado);
        var primerApellido = Letras(apellidoNormalizado.Split(' ')[0]);
        if (letrasNombre.Length == 0 || primerApellido.Length == 0)
            throw new ArgumentException("Nombre y primer apellido deben contener letras latinas para generar el nombre de usuario.");

        return letrasNombre[..1] + primerApellido;
    }

    private static string NormalizarEspacios(string? valor) =>
        string.Join(" ", (valor ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Letras(string valor) => new(valor.Normalize(NormalizationForm.FormD)
        .Where(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
        .Select(char.ToLowerInvariant).ToArray());
}
