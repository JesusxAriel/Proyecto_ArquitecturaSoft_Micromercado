namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

public sealed class MotivoDeCambioInvalidoException : ArgumentException
{
    public MotivoDeCambioInvalidoException()
        : base(CambioDePrecio.MotivoInvalidoMessage)
    {
    }
}
