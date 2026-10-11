using System.ComponentModel.DataAnnotations;
using Proyecto_Arquitectura_Micromercado.Domain.Suppliers;
using Proyecto_Arquitectura_Micromercado.Pages.Suppliers;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Fija que el formulario de proveedores siga mostrando los mismos mensajes que mostraba
// cuando los atributos vivian en la entidad de Domain.
public class SupplierFormModelTests
{
    private static SupplierFormModel Valido() => new()
    {
        NombreEmpresa = "Distribuidora Andina",
        NumeroEmpresa = "71234567",
        CorreoReferencia = "ventas@andina.com.bo",
        EsAutogestionado = false
    };

    private static List<ValidationResult> Validar(SupplierFormModel modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
        return resultados;
    }

    private static string[] MensajesDe(List<ValidationResult> resultados, string campo) =>
        resultados
            .Where(r => r.MemberNames.Contains(campo))
            .Select(r => r.ErrorMessage!)
            .ToArray();

    [Fact]
    public void Un_formulario_valido_no_tiene_errores()
    {
        Assert.Empty(Validar(Valido()));
    }

    [Fact]
    public void Nombre_vacio_muestra_campo_obligatorio()
    {
        var modelo = Valido();
        modelo.NombreEmpresa = "";

        Assert.Equal(["Campo obligatorio."], MensajesDe(Validar(modelo), nameof(modelo.NombreEmpresa)));
    }

    [Fact]
    public void Nombre_de_mas_de_150_caracteres_muestra_su_mensaje()
    {
        var modelo = Valido();
        modelo.NombreEmpresa = new string('a', 151);

        Assert.Equal(
            ["El nombre no puede exceder los 150 caracteres."],
            MensajesDe(Validar(modelo), nameof(modelo.NombreEmpresa)));
    }

    [Fact]
    public void Nombre_con_caracteres_de_control_muestra_el_mensaje_del_campo()
    {
        var modelo = Valido();
        modelo.NombreEmpresa = "Distribuidora\tAndina";

        Assert.Equal(
            ["El campo Nombre de la empresa debe tener palabras separadas por un solo espacio."],
            MensajesDe(Validar(modelo), nameof(modelo.NombreEmpresa)));
    }

    [Fact]
    public void Telefono_vacio_muestra_campo_obligatorio()
    {
        var modelo = Valido();
        modelo.NumeroEmpresa = "";

        Assert.Equal(["Campo obligatorio."], MensajesDe(Validar(modelo), nameof(modelo.NumeroEmpresa)));
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("5123456")]
    [InlineData("+592 71234567")]
    public void Telefono_invalido_muestra_el_mensaje_de_celular_boliviano(string telefono)
    {
        var modelo = Valido();
        modelo.NumeroEmpresa = telefono;

        Assert.Equal(
            [SupplierValidation.TelefonoMessage],
            MensajesDe(Validar(modelo), nameof(modelo.NumeroEmpresa)));
    }

    [Theory]
    [InlineData("71234567")]
    [InlineData("+591 71234567")]
    public void Telefono_valido_con_o_sin_prefijo_no_tiene_errores(string telefono)
    {
        var modelo = Valido();
        modelo.NumeroEmpresa = telefono;

        Assert.Empty(MensajesDe(Validar(modelo), nameof(modelo.NumeroEmpresa)));
    }

    [Fact]
    public void Correo_vacio_muestra_el_correo_es_obligatorio()
    {
        var modelo = Valido();
        modelo.CorreoReferencia = "";

        Assert.Equal(
            ["El correo es obligatorio."],
            MensajesDe(Validar(modelo), nameof(modelo.CorreoReferencia)));
    }

    [Fact]
    public void Correo_con_formato_invalido_muestra_su_mensaje()
    {
        var modelo = Valido();
        modelo.CorreoReferencia = "no-es-un-correo";

        Assert.Equal(
            [SupplierValidation.CorreoMessage],
            MensajesDe(Validar(modelo), nameof(modelo.CorreoReferencia)));
    }

    [Fact]
    public void Correo_de_mas_de_150_caracteres_muestra_su_mensaje()
    {
        var modelo = Valido();
        modelo.CorreoReferencia = new string('a', 140) + "@empresa.com";

        Assert.Equal(
            ["El correo no puede exceder los 150 caracteres."],
            MensajesDe(Validar(modelo), nameof(modelo.CorreoReferencia)));
    }

    [Fact]
    public void La_regla_entre_campos_exige_correo_a_un_proveedor_autogestionado()
    {
        var modelo = Valido();
        modelo.EsAutogestionado = true;
        modelo.CorreoReferencia = "  ";

        var resultados = modelo.Validate(new ValidationContext(modelo)).ToList();

        var error = Assert.Single(resultados);
        Assert.Equal(SupplierValidation.CorreoRequeridoParaAutogestionadoMessage, error.ErrorMessage);
        Assert.Contains(nameof(modelo.CorreoReferencia), error.MemberNames);
    }

    [Fact]
    public void La_regla_entre_campos_no_aplica_si_el_proveedor_no_es_autogestionado()
    {
        var modelo = Valido();
        modelo.CorreoReferencia = null;

        Assert.Empty(modelo.Validate(new ValidationContext(modelo)));
    }

    [Fact]
    public void ToSupplier_copia_los_campos_del_formulario()
    {
        var modelo = Valido();
        modelo.Id = 7;
        modelo.EsAutogestionado = true;

        var proveedor = modelo.ToSupplier();

        Assert.Equal(7, proveedor.Id);
        Assert.Equal("Distribuidora Andina", proveedor.NombreEmpresa);
        Assert.Equal("71234567", proveedor.NumeroEmpresa);
        Assert.Equal("ventas@andina.com.bo", proveedor.CorreoReferencia);
        Assert.True(proveedor.EsAutogestionado);
    }

    [Fact]
    public void El_formulario_no_expone_el_estado_activo_asi_el_navegador_no_puede_imponerlo()
    {
        Assert.Null(typeof(SupplierFormModel).GetProperty(nameof(Supplier.EstaActivo)));
    }
}
