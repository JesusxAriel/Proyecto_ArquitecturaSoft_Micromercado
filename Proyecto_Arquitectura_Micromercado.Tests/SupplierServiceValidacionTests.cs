using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class SupplierServiceValidacionTests
{
    private readonly SupplierService servicio = new(
        new InMemorySupplierRepository(),
        new InMemoryProductRepository(new InMemoryPriceHistoryRepository()));

    private static Supplier Proveedor(
        string nombre = "Distribuidora Andina",
        string telefono = "71234567",
        string? correo = "ventas@andina.com.bo",
        bool autogestionado = false) => new()
        {
            NombreEmpresa = nombre,
            NumeroEmpresa = telefono,
            CorreoReferencia = correo,
            EsAutogestionado = autogestionado
        };

    [Fact]
    public async Task Un_proveedor_valido_se_guarda_normalizado()
    {
        var id = await servicio.CreateAsync(
            Proveedor(
                nombre: "  distribuidora   ANDINA ",
                telefono: "+591 71234567",
                correo: "  Ventas@Andina.COM.bo "));

        var guardado = await servicio.GetByIdAsync(id);

        Assert.NotNull(guardado);
        Assert.Equal("Distribuidora Andina", guardado.NombreEmpresa);
        Assert.Equal("71234567", guardado.NumeroEmpresa);
        Assert.Equal("ventas@andina.com.bo", guardado.CorreoReferencia);
    }

    [Theory]
    [InlineData("", "El nombre de la empresa es obligatorio.")]
    [InlineData("   ", "El nombre de la empresa es obligatorio.")]
    public async Task El_nombre_es_obligatorio(string nombre, string mensaje)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(nombre: nombre)));

        Assert.Equal(mensaje, ex.Message);
    }

    [Fact]
    public async Task El_nombre_no_puede_superar_150_caracteres()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(nombre: new string('a', 151))));

        Assert.Equal("El nombre no puede exceder los 150 caracteres.", ex.Message);
    }

    [Fact]
    public async Task El_nombre_con_exactamente_150_caracteres_es_valido()
    {
        var id = await servicio.CreateAsync(Proveedor(nombre: new string('a', 150)));

        Assert.True(id > 0);
    }

    [Fact]
    public async Task El_nombre_no_puede_llevar_caracteres_de_control()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(nombre: "Distribuidora\tAndina")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task El_telefono_es_obligatorio(string telefono)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(telefono: telefono)));

        Assert.Equal("El teléfono de contacto es obligatorio.", ex.Message);
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("5123456")]
    [InlineData("712345678")]
    [InlineData("7123456a")]
    [InlineData("+592 71234567")]
    public async Task El_telefono_debe_ser_un_celular_boliviano(string telefono)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(telefono: telefono)));

        Assert.Equal(SupplierValidation.TelefonoMessage, ex.Message);
    }

    [Theory]
    [InlineData("71234567")]
    [InlineData("61234567")]
    [InlineData("+591 71234567")]
    [InlineData("+59171234567")]
    public async Task Acepta_celulares_bolivianos_con_o_sin_prefijo(string telefono)
    {
        Assert.True(await servicio.CreateAsync(Proveedor(telefono: telefono)) > 0);
    }

    [Theory]
    [InlineData("no-es-un-correo")]
    [InlineData("sin@dominio")]
    [InlineData("con espacio@empresa.com")]
    public async Task El_correo_debe_tener_formato_valido(string correo)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(correo: correo)));

        Assert.Equal(SupplierValidation.CorreoMessage, ex.Message);
    }

    [Fact]
    public async Task El_correo_no_puede_superar_150_caracteres()
    {
        var correo = new string('a', 140) + "@empresa.com";

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(correo: correo)));

        Assert.Equal("El correo no puede exceder los 150 caracteres.", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_proveedor_autogestionado_necesita_correo(string? correo)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.CreateAsync(Proveedor(correo: correo, autogestionado: true)));

        Assert.Equal("Un proveedor autogestionado requiere correo.", ex.Message);
    }

    [Fact]
    public async Task Un_proveedor_que_no_es_autogestionado_puede_guardarse_sin_correo()
    {
        var id = await servicio.CreateAsync(Proveedor(correo: null, autogestionado: false));

        var guardado = await servicio.GetByIdAsync(id);

        Assert.NotNull(guardado);
        Assert.Null(guardado.CorreoReferencia);
    }

    [Fact]
    public async Task No_se_permiten_dos_proveedores_con_el_mismo_nombre()
    {
        await servicio.CreateAsync(Proveedor(nombre: "Distribuidora Andina"));

        await Assert.ThrowsAsync<DuplicateSupplierException>(
            () => servicio.CreateAsync(Proveedor(nombre: "  DISTRIBUIDORA   andina ")));
    }

    [Fact]
    public async Task Editar_un_proveedor_con_el_nombre_de_otro_es_rechazado()
    {
        await servicio.CreateAsync(Proveedor(nombre: "Distribuidora Andina"));
        var idOtro = await servicio.CreateAsync(Proveedor(nombre: "Embotelladora del Sur"));

        var edicion = Proveedor(nombre: "Distribuidora Andina");
        edicion.Id = idOtro;

        await Assert.ThrowsAsync<DuplicateSupplierException>(() => servicio.UpdateAsync(edicion));
    }

    [Fact]
    public async Task Editar_un_proveedor_conservando_su_propio_nombre_es_valido()
    {
        var id = await servicio.CreateAsync(Proveedor(nombre: "Distribuidora Andina"));

        var edicion = Proveedor(nombre: "Distribuidora Andina", telefono: "61234567");
        edicion.Id = id;

        Assert.True(await servicio.UpdateAsync(edicion));
        Assert.Equal("61234567", (await servicio.GetByIdAsync(id))!.NumeroEmpresa);
    }

    [Fact]
    public async Task Editar_con_datos_invalidos_tambien_es_rechazado_por_el_servicio()
    {
        var id = await servicio.CreateAsync(Proveedor());

        var edicion = Proveedor(telefono: "12345678");
        edicion.Id = id;

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.UpdateAsync(edicion));
    }
}
