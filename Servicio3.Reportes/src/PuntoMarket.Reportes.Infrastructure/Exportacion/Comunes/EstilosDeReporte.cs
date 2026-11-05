namespace PuntoMarket.Reportes.Infrastructure.Exportacion.Comunes;

public static class EstilosDeReporte
{
    public const string AzulOscuro = "#1E293B";
    public const string AzulGrisaceo = "#334155";
    public const string AzulClaro = "#64748B";
    public const string Ambar = "#F59E0B";
    public const string AmbarOscuro = "#B45309";
    public const string AmbarClaro = "#FCD34D";
    public const string Blanco = "#FFFFFF";
    public const string FondoSuave = "#F1F5F9";
    public const string Borde = "#CBD5E1";
    public const string TextoAtenuado = "#64748B";

    public static readonly IReadOnlyList<string> SerieDeGraficos =
    [
        AzulOscuro,
        Ambar,
        AzulClaro,
        AmbarOscuro,
        AmbarClaro,
        Borde
    ];

    public static string ColorDeSerie(int indice) => SerieDeGraficos[indice % SerieDeGraficos.Count];
}
