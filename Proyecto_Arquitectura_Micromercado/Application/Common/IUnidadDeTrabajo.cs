namespace Proyecto_Arquitectura_Micromercado.Application.Common;

// Puerto que expresa un limite transaccional sin nombrar ninguna tecnologia de base
// de datos.
//
// POR QUE EXISTE
// Antes, el unico caso del sistema que necesitaba atomicidad (actualizar un producto y
// registrar su cambio de precio) lo resolvia pasando IDbConnection e IDbTransaction a
// traves del puerto IPriceHistoryRepository. Eso metia ADO.NET en el borde del
// hexagono: el contrato de Application hablaba de conexiones de base de datos, y el
// adaptador en memoria tenia que aceptar una conexion que no usaba.
//
// Con este puerto, quien orquesta declara el limite (iniciar, confirmar, revertir) y
// cada adaptador decide que significa eso en su tecnologia: en MySQL una transaccion
// real, en memoria una operacion vacia.
//
// COMO SE ENLISTAN LOS REPOSITORIOS
// No reciben la transaccion por parametro. Los adaptadores de una misma tecnologia
// comparten esta unidad de trabajo por inyeccion (es Scoped, una por peticion HTTP), y
// mientras haya una transaccion abierta todos usan su conexion. Por eso los metodos de
// los repositorios conservan firmas limpias.
//
// Lo necesita la Parte 3 del entregable: guardar una venta son 1 INSERT de cabecera,
// N INSERT de detalle y N UPDATE de stock, todo o nada.
public interface IUnidadDeTrabajo
{
    // Abre el limite transaccional. Falla si ya hay uno en curso: las transacciones
    // anidadas no estan soportadas y silenciarlo esconderia un error de orquestacion.
    Task IniciarAsync(CancellationToken cancellationToken = default);

    // Confirma todo lo hecho desde IniciarAsync. Sin transaccion en curso no hace nada.
    Task ConfirmarAsync(CancellationToken cancellationToken = default);

    // Descarta todo lo hecho desde IniciarAsync. Sin transaccion en curso no hace nada,
    // de modo que sea seguro llamarlo desde un catch.
    Task RevertirAsync(CancellationToken cancellationToken = default);
}
