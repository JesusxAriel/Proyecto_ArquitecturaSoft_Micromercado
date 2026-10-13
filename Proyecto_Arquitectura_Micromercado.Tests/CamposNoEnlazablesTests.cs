using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Products;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

namespace Proyecto_Arquitectura_Micromercado.Tests;

public class CamposNoEnlazablesTests
{
    [Theory]
    [InlineData(typeof(Category), nameof(Category.IsActive))]
    [InlineData(typeof(Supplier), nameof(Supplier.EstaActivo))]
    [InlineData(typeof(Product), nameof(Product.EstaActivo))]
    [InlineData(typeof(Category), nameof(Category.AdminUserId))]
    [InlineData(typeof(Category), nameof(Category.CreatedAt))]
    [InlineData(typeof(Category), nameof(Category.UpdatedAt))]
    [InlineData(typeof(Category), nameof(Category.Code))]
    public void Los_campos_de_estado_y_auditoria_no_se_enlazan(Type contenedor, string propiedad)
    {
        Assert.True(CamposNoEnlazables.EsProhibido(contenedor, propiedad));
    }

    [Theory]
    [InlineData(typeof(Category), nameof(Category.Id))]
    [InlineData(typeof(Supplier), nameof(Supplier.Id))]
    [InlineData(typeof(Product), nameof(Product.Id))]
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

    [Fact]
    public void No_se_confunde_un_booleano_del_formulario_con_uno_de_estado()
    {
        Assert.False(CamposNoEnlazables.EsProhibido(typeof(Supplier), nameof(Supplier.EsAutogestionado)));
        Assert.True(CamposNoEnlazables.EsProhibido(typeof(Supplier), nameof(Supplier.EstaActivo)));
    }
}
