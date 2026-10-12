namespace Proyecto_Arquitectura_Micromercado.Application.Comprobantes;

public interface IGeneradorComprobante
{
    string GenerarHtml(ComprobanteDto comprobante);
}
