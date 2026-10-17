namespace Proyecto_Arquitectura_Micromercado.Application.Products;

public sealed record DescripcionDeProducto(string Nombre, string Presentacion)
{
    public override string ToString() => $"{Nombre} ({Presentacion})";
}
