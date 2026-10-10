namespace Proyecto_Arquitectura_Micromercado.Domain.Products;

// Se lanza cuando el precio de un producto cambia y la justificacion no cumple la regla
// de CambioDePrecio.EsMotivoValido.
//
// Deriva de ArgumentException, igual que DuplicateProductException, para que las paginas
// lo muestren como error de validacion con el manejo que ya tienen. Es un tipo propio y
// no una ArgumentException generica para que la pagina pueda asociar el mensaje al campo
// de la justificacion en vez de al resumen del formulario.
public sealed class MotivoDeCambioInvalidoException : ArgumentException
{
    public MotivoDeCambioInvalidoException()
        : base(CambioDePrecio.MotivoInvalidoMessage)
    {
    }
}
