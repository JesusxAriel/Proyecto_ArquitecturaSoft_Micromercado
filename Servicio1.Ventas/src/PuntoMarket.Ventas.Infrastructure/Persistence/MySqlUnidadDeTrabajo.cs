using MySql.Data.MySqlClient;
using PuntoMarket.Ventas.Application.Common;
using PuntoMarket.Ventas.Infrastructure.Database;

namespace PuntoMarket.Ventas.Infrastructure.Persistence;

/// <summary>Una instancia por operación; no admite uso paralelo de una misma sesión.</summary>
public sealed class MySqlUnidadDeTrabajo(DatabaseConnection conexion) : IUnidadDeTrabajo, IAsyncDisposable
{
    private MySqlConnection? conexionDeTransaccion;
    private MySqlTransaction? transaccion;
    private bool disposed;

    public bool HayTransaccionActiva => transaccion is not null;

    public async Task IniciarAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (HayTransaccionActiva)
            throw new InvalidOperationException("Ya hay una transacción en curso en esta unidad de trabajo.");

        var nueva = conexion.CreateConnection();
        try
        {
            await nueva.OpenAsync(cancellationToken);
            var nuevaTransaccion = await nueva.BeginTransactionAsync(cancellationToken);
            conexionDeTransaccion = nueva;
            transaccion = nuevaTransaccion;
        }
        catch
        {
            await nueva.DisposeAsync();
            throw;
        }
    }

    public async Task ConfirmarAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (transaccion is null) return;

        // Si falla el commit, conserva la transacción para el rollback solicitado por Application.
        await transaccion.CommitAsync(cancellationToken);
        await CerrarAsync();
    }

    public async Task RevertirAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        try
        {
            if (transaccion is not null)
                await transaccion.RollbackAsync(cancellationToken);
        }
        finally
        {
            await CerrarAsync();
        }
    }

    internal async Task<AlquilerDeConexion> AlquilarAsync(
        CancellationToken cancellationToken, bool requiereTransaccion = false)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (transaccion is not null && conexionDeTransaccion is not null)
            return new AlquilerDeConexion(conexionDeTransaccion, transaccion, esPropia: false);

        if (requiereTransaccion)
            throw new InvalidOperationException("La operación requiere una transacción activa en la unidad de trabajo.");

        var nueva = conexion.CreateConnection();
        try
        {
            await nueva.OpenAsync(cancellationToken);
            return new AlquilerDeConexion(nueva, null, esPropia: true);
        }
        catch
        {
            await nueva.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        try
        {
            if (transaccion is not null)
                await transaccion.RollbackAsync(CancellationToken.None);
        }
        finally
        {
            disposed = true;
            await CerrarAsync();
        }
    }

    private async Task CerrarAsync()
    {
        var anteriorTransaccion = transaccion;
        var anteriorConexion = conexionDeTransaccion;
        transaccion = null;
        conexionDeTransaccion = null;
        try
        {
            if (anteriorTransaccion is not null) await anteriorTransaccion.DisposeAsync();
        }
        finally
        {
            if (anteriorConexion is not null) await anteriorConexion.DisposeAsync();
        }
    }
}

internal sealed class AlquilerDeConexion(
    MySqlConnection conexion, MySqlTransaction? transaccion, bool esPropia) : IAsyncDisposable
{
    public MySqlConnection Conexion { get; } = conexion;
    public MySqlTransaction? Transaccion { get; } = transaccion;

    public ValueTask DisposeAsync() => esPropia ? Conexion.DisposeAsync() : ValueTask.CompletedTask;
}
