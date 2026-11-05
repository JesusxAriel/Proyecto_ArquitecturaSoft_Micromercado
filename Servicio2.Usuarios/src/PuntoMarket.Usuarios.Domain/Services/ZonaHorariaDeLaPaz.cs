namespace PuntoMarket.Usuarios.Domain.Services;

public static class ZonaHorariaDeLaPaz
{
    private static readonly TimeSpan Desfase = TimeSpan.FromHours(-4);

    public static DateTime ALocal(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Unspecified).Add(Desfase);

    public static DateTime AUtc(DateTime local) =>
        DateTime.SpecifyKind(local.Subtract(Desfase), DateTimeKind.Utc);
}
