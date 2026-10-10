namespace Proyecto_Arquitectura_Micromercado.Application.Common;

// Fallo al hablar con el almacenamiento de datos, expresado sin nombrar ninguna
// tecnologia.
//
// POR QUE EXISTE
// Las paginas capturaban MySqlException directamente, es decir el adaptador de entrada
// dependia del driver del adaptador de salida: los dos extremos del hexagono acoplados
// entre si, saltandose el nucleo. Ademas el manejo de errores quedaba casado con un
// motor: con el adaptador en memoria esos catch nunca se disparaban, porque ese motor
// jamas lanza MySqlException.
//
// Ahora cada adaptador de persistencia traduce los errores de su tecnologia a esta
// excepcion, y la UI captura una sola cosa que entiende sin saber que hay detras.
//
// Se reserva para fallos de infraestructura (la base no responde, la consulta no es
// valida). Las violaciones de reglas de negocio siguen viajando como ArgumentException
// y sus derivadas, por ejemplo DuplicateProductException, para que las paginas las
// muestren como errores de validacion junto al campo que corresponde.
public sealed class ErrorDePersistenciaException : Exception
{
    private const string MensajePorDefecto =
        "Fallo la comunicacion con el almacenamiento de datos.";

    public ErrorDePersistenciaException(Exception innerException)
        : base(MensajePorDefecto, innerException)
    {
    }

    public ErrorDePersistenciaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
