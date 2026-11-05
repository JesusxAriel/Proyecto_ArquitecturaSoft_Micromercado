namespace PuntoMarket.Ventas.Application.Comprobantes;

public interface IGeneradorComprobante
{
    string GenerarHtml(ComprobanteDto comprobante);
}
