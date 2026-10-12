using System.ComponentModel.DataAnnotations;
using Proyecto_Arquitectura_Micromercado.Application.Customers;
using Proyecto_Arquitectura_Micromercado.Application.Security;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class ClienteCreacionServiceTests
{
    [Fact]
    public async Task Normaliza_y_usa_el_usuario_autenticado_para_auditoria()
    {
        var repositorio = new Repositorio();
        var servicio = new ClienteCreacionService(repositorio, new Usuario());
        Assert.Equal(42, await servicio.CrearAsync(new() { NitCi = " 123 ", RazonSocial = " Ana Pérez " }));
        Assert.Equal(("123", "Ana Pérez", 7), repositorio.Datos);
    }

    [Theory]
    [InlineData("", "Ana")]
    [InlineData("   ", "Ana")]
    [InlineData("123", "   ")]
    [InlineData("123", "")]
    public async Task Rechaza_campos_vacios_sin_escribir(string nitCi, string razonSocial)
    {
        var repositorio = new Repositorio();
        var servicio = new ClienteCreacionService(repositorio, new Usuario());
        await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearAsync(new() { NitCi = nitCi, RazonSocial = razonSocial }));
        Assert.Null(repositorio.Datos);
    }

    [Theory]
    [InlineData(31, 10)]
    [InlineData(10, 151)]
    public async Task Rechaza_longitudes_superiores_a_la_tabla(int longitudNit, int longitudRazon)
    {
        var repositorio = new Repositorio();
        var servicio = new ClienteCreacionService(repositorio, new Usuario());
        await Assert.ThrowsAsync<ValidationException>(() => servicio.CrearAsync(new()
        { NitCi = new string('1', longitudNit), RazonSocial = new string('a', longitudRazon) }));
        Assert.Null(repositorio.Datos);
    }

    [Fact]
    public async Task Propaga_el_duplicado_para_que_la_UI_pueda_informarlo()
    {
        var servicio = new ClienteCreacionService(new Repositorio { Duplicado = true }, new Usuario());
        await Assert.ThrowsAsync<ClienteDuplicadoException>(() => servicio.CrearAsync(new() { NitCi = "123", RazonSocial = "Ana" }));
    }

    private sealed class Repositorio : IClienteCreacionRepository
    {
        public (string, string, int)? Datos { get; private set; }
        public bool Duplicado { get; init; }
        public Task<int> CrearAsync(string nitCi, string razonSocial, int idUsuarioCreacion,
            CancellationToken cancellationToken = default)
        {
            if (Duplicado) throw new ClienteDuplicadoException();
            Datos = (nitCi, razonSocial, idUsuarioCreacion);
            return Task.FromResult(42);
        }
    }

    private sealed class Usuario : IUsuarioActual
    {
        public int Id => 7;
        public string NombreUsuario => "cajero";
        public string Rol => Roles.Cajero;
    }
}
