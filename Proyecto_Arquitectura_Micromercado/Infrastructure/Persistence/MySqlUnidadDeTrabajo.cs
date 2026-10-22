using MySql.Data.MySqlClient;
using Proyecto_Arquitectura_Micromercado.Application.Common;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

public sealed class MySqlUnidadDeTrabajo(DatabaseConnection conexion) : IUnidadDeTrabajo, IAsyncDisposable
{
    private MySqlConnection? conexionDeTransaccion;
    private MySqlTransaction? transaccion;

    public bool HayTransaccionActiva => transaccion is not null;

    public Task IniciarAsync(CancellationToken cancellationToken = default)
    {
        if (transaccion is not null)
        {
            throw new InvalidOperationException(
                "Ya hay una transacción en curso en esta unidad de trabajo.");
        }

        return ErroresDePersistencia.TraducirAsync(async () =>
        {
            conexionDeTransaccion = conexion.CreateConnection();
            await conexionDeTransaccion.OpenAsync(cancellationToken);
            transaccion = await conexionDeTransaccion.BeginTransactionAsync(cancellationToken);
        });
    }

    public Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        if (transaccion is null)
        {
            return Task.CompletedTask;
        }

        return ErroresDePersistencia.TraducirAsync(async () =>
        {
            await transaccion.CommitAsync(cancellationToken);
            await CerrarAsync();
        });
    }

    public Task RevertirAsync(CancellationToken cancellationToken = default)
    {
        if (transaccion is null)
        {
            return Task.CompletedTask;
        }

        return ErroresDePersistencia.TraducirAsync(async () =>
        {
            await transaccion.RollbackAsync(cancellationToken);
            await CerrarAsync();
        });
    }

    internal Task<AlquilerDeConexion> AlquilarAsync(CancellationToken cancellationToken)
    {
        if (transaccion is not null && conexionDeTransaccion is not null)
        {
            return Task.FromResult(
                new AlquilerDeConexion(conexionDeTransaccion, transaccion, esPropia: false));
        }

        return ErroresDePersistencia.TraducirAsync(async () =>
        {
            var conexionNueva = conexion.CreateConnection();
            await conexionNueva.OpenAsync(cancellationToken);

            return new AlquilerDeConexion(conexionNueva, null, esPropia: true);
        });
    }

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
