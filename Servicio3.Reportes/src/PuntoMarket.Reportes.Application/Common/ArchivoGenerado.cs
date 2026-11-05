namespace PuntoMarket.Reportes.Application.Common;

public sealed record ArchivoGenerado(byte[] Contenido, string TipoDeContenido, string NombreDeArchivo);
