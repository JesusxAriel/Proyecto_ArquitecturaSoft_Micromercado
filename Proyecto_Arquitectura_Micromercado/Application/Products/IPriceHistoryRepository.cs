namespace Proyecto_Arquitectura_Micromercado.Application.Products;

// Puerto del repositorio de historial de precios.
//
// Antes declaraba una sobrecarga adicional que recibia IDbConnection e IDbTransaction,
// para que el repositorio de producto pudiera delegar la insercion dentro de su propia
// transaccion. Eso ponia ADO.NET en el borde del hexagono: el contrato de Application
// hablaba de conexiones de base de datos, el adaptador en memoria tenia que aceptar una
// conexion que no usaba, y el de MySQL fallaba si no recibia justo un MySqlConnection.
//
// La atomicidad ahora se expresa con el puerto IUnidadDeTrabajo, que no nombra ninguna
// tecnologia. Los adaptadores de un mismo motor comparten esa unidad por inyeccion, asi
// que se enlistan solos en la transaccion abierta y este contrato queda limpio.
public interface IPriceHistoryRepository : IConHistorialPrecios, IConRegistroHistorial
{
}
