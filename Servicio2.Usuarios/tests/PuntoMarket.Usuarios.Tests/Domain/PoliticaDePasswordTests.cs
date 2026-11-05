using PuntoMarket.Usuarios.Domain.Services;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class PoliticaDePasswordTests
{
    [Theory]
    [InlineData("Segura1!")]
    [InlineData("MiClave2026@")]
    [InlineData("Prueba#123A")]
    public void Acepta_contrasenas_que_cumplen_todos_los_requisitos(string contrasena)
    {
        Assert.True(PoliticaDePassword.CumpleRequisitos(contrasena));
    }

    [Theory]
    [InlineData("Aa1!")]
    [InlineData("segura1!")]
    [InlineData("SEGURA1!")]
    [InlineData("Seguraaa!")]
    [InlineData("Segura12")]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechaza_contrasenas_que_incumplen_algun_requisito(string contrasena)
    {
        Assert.False(PoliticaDePassword.CumpleRequisitos(contrasena));
    }

    [Fact]
    public void Rechaza_contrasena_nula()
    {
        Assert.False(PoliticaDePassword.CumpleRequisitos(null));
    }
}