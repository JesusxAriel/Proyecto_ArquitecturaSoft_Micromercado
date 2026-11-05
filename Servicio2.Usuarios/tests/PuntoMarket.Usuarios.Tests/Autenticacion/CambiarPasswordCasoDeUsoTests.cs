using PuntoMarket.Usuarios.Application.Ports.Autenticacion;
using PuntoMarket.Usuarios.Application.Ports.Comunes;
using PuntoMarket.Usuarios.Application.UseCases.Autenticacion.CambiarPassword;
using PuntoMarket.Usuarios.Domain.Enums;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Autenticacion;

public sealed class CambiarPasswordCasoDeUsoTests
{
    private const string PasswordActual = "Actual1!";
    private const string HashActual = "hash-actual";
    private const string PasswordNueva = "NuevaSegura2!";

    [Fact]
    public async Task Cambio_exitoso_actualiza_hash_y_desactiva_cambio_obligatorio()
    {
        var repositorio = new RepositorioStub(UsuarioValido());
        var hasheador = new HasheadorStub();
        var caso = CrearCaso(repositorio, hasheador);

        var salida = await caso.EjecutarAsync(new(PasswordActual, PasswordNueva));

        Assert.True(salida.Cambiada);
        Assert.Equal(42, repositorio.UsuarioIdActualizado);
        Assert.Equal(42, repositorio.IdUsuarioActualizacion);
        Assert.Equal("hash-de-NuevaSegura2!", repositorio.HashActualizado);
        Assert.Equal(HashActual, repositorio.HashAnterior);
        Assert.False(repositorio.Usuario!.DebeCambiarPassword);
        Assert.Contains((PasswordActual, HashActual), hasheador.Verificaciones);
    }

    [Fact]
    public async Task Rechaza_contrasena_actual_incorrecta_sin_actualizar_hash()
    {
        var repositorio = new RepositorioStub(UsuarioValido());
        var hasheador = new HasheadorStub { VerificacionCorrecta = false };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => CrearCaso(repositorio, hasheador)
                .EjecutarAsync(new(PasswordActual, PasswordNueva)));

        Assert.Equal("ContrasenaActual", exception.ParamName);
        Assert.Null(repositorio.HashActualizado);
    }

    [Fact]
    public async Task Rechaza_reutilizar_contrasena_actual_antes_de_generar_nuevo_hash()
    {
        var repositorio = new RepositorioStub(UsuarioValido());
        var hasheador = new HasheadorStub { NuevaCoincideConActual = true };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => CrearCaso(repositorio, hasheador)
                .EjecutarAsync(new(PasswordActual, PasswordNueva)));

        Assert.Equal("NuevaContrasenaIgualActual", exception.ParamName);
        Assert.StartsWith(
            "La nueva contraseña no puede ser igual a la contraseña actual.",
            exception.Message);
        Assert.Equal(0, hasheador.CantidadHashGenerados);
        Assert.Null(repositorio.HashActualizado);
    }

    [Theory]
    [InlineData("corta")]
    [InlineData("minusculas1!")]
    [InlineData("MAYUSCULAS1!")]
    [InlineData("SinNumero!")]
    [InlineData("SinEspecial2")]
    public async Task Rechaza_nueva_contrasena_que_no_cumple_la_politica(string passwordNueva)
    {
        var repositorio = new RepositorioStub(UsuarioValido());
        var hasheador = new HasheadorStub();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => CrearCaso(repositorio, hasheador)
                .EjecutarAsync(new(PasswordActual, passwordNueva)));

        Assert.Equal("NuevaContrasena", exception.ParamName);
        Assert.Null(repositorio.UsuarioIdConsultado);
        Assert.Null(repositorio.HashActualizado);
    }

    [Fact]
    public async Task Rechaza_usuario_inexistente()
    {
        var repositorio = new RepositorioStub(null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearCaso(repositorio, new HasheadorStub())
                .EjecutarAsync(new(PasswordActual, PasswordNueva)));

        Assert.Null(repositorio.HashActualizado);
    }

    [Fact]
    public async Task Rechaza_usuario_pendiente_sin_actualizar_repositorio_ni_generar_hash()
    {
        var repositorio = new RepositorioStub(UsuarioValido() with
        {
            Estado = EstadoUsuario.PENDIENTE
        });
        var hasheador = new HasheadorStub();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearCaso(repositorio, hasheador)
                .EjecutarAsync(new(PasswordActual, PasswordNueva)));

        Assert.Null(repositorio.UsuarioIdActualizado);
        Assert.Null(repositorio.HashActualizado);
        Assert.Equal(0, hasheador.CantidadHashGenerados);
    }

    [Theory]
    [InlineData(EstadoUsuario.INACTIVO, true)]
    [InlineData(EstadoUsuario.ACTIVO, false)]
    public async Task Rechaza_usuario_inactivo_o_con_rol_inactivo(
        EstadoUsuario estado,
        bool rolActivo)
    {
        var repositorio = new RepositorioStub(UsuarioValido() with
        {
            Estado = estado,
            RolActivo = rolActivo
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearCaso(repositorio, new HasheadorStub())
                .EjecutarAsync(new(PasswordActual, PasswordNueva)));

        Assert.Null(repositorio.HashActualizado);
    }

    [Fact]
    public async Task Usa_el_id_autenticado_y_no_recibe_id_del_cliente()
    {
        var repositorio = new RepositorioStub(UsuarioValido() with { Id = 99 });
        var caso = CrearCaso(repositorio, new HasheadorStub(), usuarioId: 99);

        await caso.EjecutarAsync(new(PasswordActual, PasswordNueva));

        Assert.Equal(99, repositorio.UsuarioIdConsultado);
        Assert.Equal(99, repositorio.UsuarioIdActualizado);
    }

    [Fact]
    public async Task Rechaza_usuario_actual_no_autenticado()
    {
        var usuarioActual = new UsuarioActualStub { LanzarAlLeerId = true };
        var caso = new CambiarPasswordCasoDeUso(
            new RepositorioStub(UsuarioValido()),
            new HasheadorStub(),
            usuarioActual);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => caso.EjecutarAsync(new(PasswordActual, PasswordNueva)));
    }

    private static CambiarPasswordCasoDeUso CrearCaso(
        RepositorioStub repositorio,
        HasheadorStub hasheador,
        int usuarioId = 42) =>
        new(repositorio, hasheador, new UsuarioActualStub { UsuarioId = usuarioId });

    private static UsuarioAutenticacionDto UsuarioValido() =>
        new(
            42,
            "ana",
            HashActual,
            "Ana Pérez",
            EstadoUsuario.ACTIVO,
            true,
            "Administrador",
            true);

    private sealed class RepositorioStub : IUsuarioAutenticacionRepositorio
    {
        public RepositorioStub(UsuarioAutenticacionDto? usuario)
        {
            Usuario = usuario;
        }

        public UsuarioAutenticacionDto? Usuario { get; private set; }
        public int? UsuarioIdConsultado { get; private set; }
        public int? UsuarioIdActualizado { get; private set; }
        public int? IdUsuarioActualizacion { get; private set; }
        public string? HashAnterior { get; private set; }
        public string? HashActualizado { get; private set; }

        public Task<UsuarioAutenticacionDto?> ObtenerPorCorreoAsync(
            string correo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UsuarioAutenticacionDto?>(null);

        public Task<UsuarioAutenticacionDto?> ObtenerPorIdAsync(
            int usuarioId,
            CancellationToken cancellationToken = default)
        {
            UsuarioIdConsultado = usuarioId;
            return Task.FromResult(Usuario?.Id == usuarioId ? Usuario : null);
        }

        public Task<bool> ActualizarPasswordAsync(
            int usuarioId,
            int idUsuarioActualizacion,
            string passwordHashActual,
            string passwordHash,
            CancellationToken cancellationToken = default)
        {
            UsuarioIdActualizado = usuarioId;
            IdUsuarioActualizacion = idUsuarioActualizacion;
            HashAnterior = passwordHashActual;
            HashActualizado = passwordHash;
            if (Usuario is null ||
                Usuario.Id != usuarioId ||
                Usuario.PasswordHash != passwordHashActual)
                return Task.FromResult(false);

            Usuario = Usuario with { PasswordHash = passwordHash, DebeCambiarPassword = false };
            return Task.FromResult(true);
        }
    }

    private sealed class HasheadorStub : IHasheadorPassword
    {
        public bool VerificacionCorrecta { get; init; } = true;
        public bool NuevaCoincideConActual { get; init; }
        public int CantidadHashGenerados { get; private set; }
        public List<(string Contrasena, string Hash)> Verificaciones { get; } = [];

        public string Hash(string contrasena)
        {
            CantidadHashGenerados++;
            return $"hash-de-{contrasena}";
        }

        public bool Verify(string contrasena, string hash)
        {
            Verificaciones.Add((contrasena, hash));
            if (contrasena == PasswordActual && hash == HashActual)
                return VerificacionCorrecta;
            return NuevaCoincideConActual &&
                contrasena == PasswordNueva &&
                hash == HashActual;
        }
    }

    private sealed class UsuarioActualStub : IUsuarioActual
    {
        public int UsuarioId { get; init; } = 42;
        public bool LanzarAlLeerId { get; init; }
        public int Id => LanzarAlLeerId
            ? throw new InvalidOperationException("No hay usuario autenticado.")
            : UsuarioId;
        public string Rol => "Administrador";
    }
}
