namespace PuntoMarket.Reportes.Domain.ValueObjects;

public sealed record ParteDelTotal(string Nombre, decimal Monto)
{
    public static IReadOnlyList<ParteDelTotal> TopConOtros(
        IReadOnlyList<ParteDelTotal> ordenadasDeMayorAMenor,
        int maximo)
    {
        if (ordenadasDeMayorAMenor.Count <= maximo)
            return ordenadasDeMayorAMenor;

        var principales = ordenadasDeMayorAMenor.Take(maximo).ToList();
        var resto = ordenadasDeMayorAMenor.Skip(maximo).ToList();
        principales.Add(new ParteDelTotal($"Otros ({resto.Count})", resto.Sum(p => p.Monto)));
        return principales;
    }
}
