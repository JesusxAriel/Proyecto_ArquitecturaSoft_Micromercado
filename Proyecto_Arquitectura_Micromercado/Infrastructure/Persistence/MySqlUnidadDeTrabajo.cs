using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

// Adaptador MySQL del puerto IUnidadDeTrabajo.
//
// Cumple dos papeles:
//   1. Hacia Application, implementa el puerto: iniciar, confirmar, revertir.
//   2. Hacia los adaptadores MySQL, reparte la conexion con la que deben trabajar.
//      Ese reparto es interno de Infrastructure, asi que ningun detalle de ADO.NET
//      vuelve a asomarse por el borde del hexagono.
//
// Se registra con ciclo de vida Scoped: una unidad de trabajo por peticion HTTP. Por
// eso basta con que los repositorios la reciban por constructor para quedar enlistados
// en la misma transaccion, sin pasarsela por parametro.
public sealed class MySqlUnidadDeTrabajo(DatabaseConnection conexion) : IUnidadDeTrabajo, IAsyncDisposable
{
    private MySqlConnection? conexionDeTransaccion;
    private MySqlTransaction? transaccion;

    public bool HayTransaccionActiva => transaccion is not null;

    public async Task IniciarAsync(CancellationToken cancellationToken = default)
    {
        if (transaccion is not null)
        {
            throw new InvalidOperationException(
                "Ya hay una transaccion en curso en esta unidad de trabajo.");
        }

        conexionDeTransaccion = conexion.CreateConnection();
        await conexionDeTransaccion.OpenAsync(cancellationToken);
        transaccion = await conexionDeTransaccion.BeginTransactionAsync(cancellationToken);
    }

    public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        if (transaccion is null)
        {
            return;
        }

        await transaccion.CommitAsync(cancellationToken);
        await CerrarAsync();
    }

    public async Task RevertirAsync(CancellationToken cancellationToken = default)
    {
        if (transaccion is null)
        {
            return;
        }

        await transaccion.RollbackAsync(cancellationToken);
        await CerrarAsync();
    }

    // Entrega la conexion que corresponde usar en este momento.
    //
    // Con una transaccion abierta devuelve la conexion compartida y marca el alquiler
    // como ajeno, para que el repositorio que lo libere NO cierre la conexion de la
    // transaccion. Sin transaccion devuelve una conexion propia y de vida corta, igual
    // que antes de existir esta clase: el comportamiento de las lecturas no cambia.
    internal async Task<AlquilerDeConexion> AlquilarAsync(CancellationToken cancellationToken)
    {
        if (transaccion is not null && conexionDeTransaccion is not null)
        {
            return new AlquilerDeConexion(conexionDeTransaccion, transaccion, esPropia: false);
        }

        var conexionNueva = conexion.CreateConnection();
        await conexionNueva.OpenAsync(cancellationToken);

        return new AlquilerDeConexion(conexionNueva, null, esPropia: true);
    }

    // Si alguien olvida confirmar o revertir, el contenedor de dependencias libera la
    // unidad al cerrar el scope y la transaccion se descarta en vez de quedar colgada.
    public async ValueTask DisposeAsync()
    {
        if (transaccion is not null)
        {
            await transaccion.RollbackAsync();
            await CerrarAsync();
        }
    }

    private async Task CerrarAsync()
    {
        if (transaccion is not null)
        {
            await transaccion.DisposeAsync();
            transaccion = null;
        }

        if (conexionDeTransaccion is not null)
        {
            await conexionDeTransaccion.DisposeAsync();
            conexionDeTransaccion = null;
        }
    }
}

// Conexion prestada a un repositorio por la unidad de trabajo, junto con la transaccion
// en curso si la hay.
//
// Liberarlo solo cierra la conexion cuando el alquiler es propio. Asi los repositorios
// pueden seguir escribiendo "await using var alquiler = ..." sin riesgo de cerrarle la
// conexion a una transaccion que no les pertenece.
internal sealed class AlquilerDeConexion(
    MySqlConnection conexion,
    MySqlTransaction? transaccion,
    bool esPropia) : IAsyncDisposable
{
    public MySqlConnection Conexion { get; } = conexion;

    public MySqlTransaction? Transaccion { get; } = transaccion;

    public ValueTask DisposeAsync() =>
        esPropia ? Conexion.DisposeAsync() : ValueTask.CompletedTask;
}
