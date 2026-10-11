namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

// Puerto de salida que convierte un comprobante en un documento.
//
// Hoy el adaptador produce HTML imprimible. Como la pagina solo conoce este puerto,
// generar un PDF mas adelante consiste en escribir otro adaptador y cambiar el registro
// en Program.cs, sin tocar el servicio ni la pagina.
public interface IGeneradorComprobante
{
    string GenerarHtml(ComprobanteDto comprobante);
}
