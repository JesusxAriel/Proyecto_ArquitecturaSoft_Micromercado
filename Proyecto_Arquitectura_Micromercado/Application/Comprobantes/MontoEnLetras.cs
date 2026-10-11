namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

// Escribe un monto en bolivianos con letras, como en el pie del comprobante:
//
//     31,00  ->  "Son Treinta y Un 00/100 Bolivianos"
//
// No usa librerias externas. Cada palabra va con inicial mayuscula, salvo la "y". El
// numero se apocopa ("Un", "Veintiún") porque siempre le sigue un sustantivo: "Un Mil" no
// se usa, pero "Veintiún Mil", "Un Millón" y "Un 00/100 Bolivianos" si. Los centavos van
// en dos cifras sobre 100, y la moneda queda siempre en plural, como en los documentos
// contables.
//
// Cubre de 0 a 999.999.999,99, que sobra para un DECIMAL(10,2) de la base de datos.
public static class MontoEnLetras
{
    public const decimal MontoMaximo = 999_999_999.99m;

    private static readonly string[] Unidades =
    [
        "", "un", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve",
        "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete",
        "dieciocho", "diecinueve", "veinte", "veintiún", "veintidós", "veintitrés",
        "veinticuatro", "veinticinco", "veintiséis", "veintisiete", "veintiocho", "veintinueve"
    ];

    private static readonly string[] Decenas =
        ["", "", "", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa"];

    private static readonly string[] Centenas =
    [
        "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos",
        "seiscientos", "setecientos", "ochocientos", "novecientos"
    ];

    public static string Convertir(decimal monto)
    {
        if (monto < 0 || monto > MontoMaximo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monto),
                $"El monto debe estar entre 0 y {MontoMaximo:N2}.");
        }

        var redondeado = decimal.Round(monto, 2, MidpointRounding.AwayFromZero);
        var entero = (int)decimal.Truncate(redondeado);
        var centavos = (int)((redondeado - entero) * 100);

        return $"Son {EnPalabras(entero)} {centavos:00}/100 Bolivianos";
    }

    private static string EnPalabras(int numero)
    {
        if (numero == 0)
        {
            return "Cero";
        }

        var partes = new List<string>();
        var millones = numero / 1_000_000;
        var miles = numero / 1_000 % 1_000;
        var resto = numero % 1_000;

        if (millones > 0)
        {
            partes.Add(millones == 1 ? "un millón" : $"{MenorQueMil(millones)} millones");
        }

        if (miles > 0)
        {
            partes.Add(miles == 1 ? "mil" : $"{MenorQueMil(miles)} mil");
        }

        if (resto > 0)
        {
            partes.Add(MenorQueMil(resto));
        }

        return ConInicialesMayusculas(string.Join(' ', partes));
    }

    // De 1 a 999, ya apocopado ("un", "veintiún").
    private static string MenorQueMil(int numero)
    {
        if (numero == 100)
        {
            return "cien";
        }

        var palabras = new List<string>();
        var centenas = numero / 100;
        var resto = numero % 100;

        if (centenas > 0)
        {
            palabras.Add(Centenas[centenas]);
        }

        if (resto > 0 && resto < 30)
        {
            palabras.Add(Unidades[resto]);
        }
        else if (resto >= 30)
        {
            var decena = Decenas[resto / 10];
            var unidad = resto % 10;

            palabras.Add(unidad == 0 ? decena : $"{decena} y {Unidades[unidad]}");
        }

        return string.Join(' ', palabras);
    }

    private static string ConInicialesMayusculas(string texto) =>
        string.Join(
            ' ',
            texto.Split(' ').Select(palabra =>
                palabra == "y" ? palabra : char.ToUpperInvariant(palabra[0]) + palabra[1..]));
}
