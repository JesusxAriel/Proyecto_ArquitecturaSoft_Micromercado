namespace PuntoMarket.Ventas.Application.Products;

public sealed record DescripcionDeProducto(string Nombre, string Presentacion)
{
    public override string ToString() => $"{Nombre} ({Presentacion})";
}
