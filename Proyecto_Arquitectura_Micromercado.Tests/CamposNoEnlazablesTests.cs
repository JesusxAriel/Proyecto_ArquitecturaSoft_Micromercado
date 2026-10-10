using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Fija la politica de over-posting: que campos NO puede imponer el navegador y, igual de
// importante, que campos si deben seguir enlazandose. Lo segundo es lo que evita que una
// proteccion de mas rompa un formulario que hoy funciona.
public class CamposNoEnlazablesTests
{
    [Theory]
    // Baja logica: la maneja SoftDeleteAsync, nunca un formulario.
    [InlineData(typeof(Category), nameof(Category.IsActive))]
    [InlineData(typeof(Supplier), nameof(Supplier.EstaActivo))]
    [InlineData(typeof(Product), nameof(Product.EstaActivo))]
    // Auditoria de categoria: la define el servidor.
    [InlineData(typeof(Category), nameof(Category.AdminUserId))]
    [InlineData(typeof(Category), nameof(Category.CreatedAt))]
    [InlineData(typeof(Category), nameof(Category.UpdatedAt))]
    // El codigo lo genera el servicio a partir del nombre.
    [InlineData(typeof(Category), nameof(Category.Code))]
    public void Los_campos_de_estado_y_auditoria_no_se_enlazan(Type contenedor, string propiedad)
    {
        Assert.True(CamposNoEnlazables.EsProhibido(contenedor, propiedad));
    }

    [Theory]
    // Identificadores: sin ellos el formulario de edicion no sabria a quien actualizar.
    [InlineData(typeof(Category), nameof(Category.Id))]
    [InlineData(typeof(Supplier), nameof(Supplier.Id))]
    [InlineData(typeof(Product), nameof(Product.Id))]
    // Datos que el usuario escribe de verdad.
    [InlineData(typeof(Category), nameof(Category.Name))]
    [InlineData(typeof(Category), nameof(Category.Description))]
    [InlineData(typeof(Category), nameof(Category.AisleLocation))]
    [InlineData(typeof(Supplier), nameof(Supplier.NombreEmpresa))]
    [InlineData(typeof(Supplier), nameof(Supplier.NumeroEmpresa))]
    [InlineData(typeof(Supplier), nameof(Supplier.CorreoReferencia))]
    [InlineData(typeof(Supplier), nameof(Supplier.EsAutogestionado))]
    [InlineData(typeof(Product), nameof(Product.Nombre))]
    [InlineData(typeof(Product), nameof(Product.IdEmpaque))]
    [InlineData(typeof(Product), nameof(Product.PrecioVenta))]
    [InlineData(typeof(Product), nameof(Product.PrecioCosto))]
    [InlineData(typeof(Product), nameof(Product.StockMinimo))]
    [InlineData(typeof(Product), nameof(Product.IdCategoria))]
    [InlineData(typeof(Product), nameof(Product.IdProveedor))]
    [InlineData(typeof(Product), nameof(Product.MotivoCambio))]
    public void Los_campos_que_el_usuario_llena_siguen_enlazandose(Type contenedor, string propiedad)
    {
        Assert.False(CamposNoEnlazables.EsProhibido(contenedor, propiedad));
    }

    [Fact]
    public void La_politica_solo_nombra_propiedades_que_existen()
    {
        foreach (var (contenedor, propiedad) in CamposNoEnlazables.Todos)
        {
            Assert.NotNull(contenedor.GetProperty(propiedad));
        }
    }

    // EsAutogestionado es un booleano que el formulario de proveedores si ofrece, asi que
    // no debe confundirse con un campo de estado interno.
    [Fact]
    public void No_se_confunde_un_booleano_del_formulario_con_uno_de_estado()
    {
        Assert.False(CamposNoEnlazables.EsProhibido(typeof(Supplier), nameof(Supplier.EsAutogestionado)));
        Assert.True(CamposNoEnlazables.EsProhibido(typeof(Supplier), nameof(Supplier.EstaActivo)));
    }
}
