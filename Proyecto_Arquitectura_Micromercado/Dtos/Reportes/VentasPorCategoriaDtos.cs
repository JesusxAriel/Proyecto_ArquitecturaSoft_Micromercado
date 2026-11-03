namespace Proyecto_Arquitectura_Micromercado.Dtos.Reportes;

public sealed record FiltrosVentasPorCategoriaDto(DateOnly? Desde, DateOnly? Hasta, int? IdCategoria, int? Top);

public sealed record FilaVentasPorCategoriaDto(
    int IdCategoria,
    string Categoria,
    long UnidadesVendidas,
    decimal MontoVendido);

public sealed record TotalesVentasPorCategoriaDto(long TotalUnidades, decimal TotalMonto, string? CategoriaLider);

public sealed record RespuestaVentasPorCategoriaDto(
    IReadOnlyList<FilaVentasPorCategoriaDto> Filas,
    TotalesVentasPorCategoriaDto Totales,
    DateTime GeneradoEn,
    string GeneradoPor);

public sealed record ArchivoDescargadoDto(byte[] Contenido, string TipoDeContenido, string NombreDeArchivo);

public enum FormatoDeDescarga
{
    Pdf,
    Xlsx
}
