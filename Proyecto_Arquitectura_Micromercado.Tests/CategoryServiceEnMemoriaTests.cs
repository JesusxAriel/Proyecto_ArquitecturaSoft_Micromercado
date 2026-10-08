using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Domain.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;

namespace Proyecto_Arquitectura_Micromercado.Tests;

// Estas pruebas son el beneficio concreto del patron corregido: CategoryService se ejercita
// completo, sin base de datos, cambiando unicamente el Creador Concreto que se elige. El
// servicio no se modifico ni se entera del cambio de motor (OCP + DIP), y el repositorio en
// memoria cumple el mismo contrato que el de MySQL (LSP).
public class CategoryServiceEnMemoriaTests
{
    private static ICategoryService CrearServicio()
    {
        CreatorCategoryRepository creador = new CreatorCategoryRepositoryEnMemoria();

        return new CategoryService(creador.ObtenerRepositorio());
    }

    private static Category NuevaCategoria(string nombre) => new()
    {
        Name = nombre,
        AisleLocation = "Pasillo 1",
        Description = "Productos de prueba"
    };

    [Fact]
    public async Task Crea_la_categoria_y_le_genera_el_codigo()
    {
        var servicio = CrearServicio();

        int id = await servicio.CreateAsync(NuevaCategoria("Lácteos"));

        Category? guardada = await servicio.GetByIdAsync(id);

        Assert.NotNull(guardada);
        Assert.Equal("Lácteos", guardada.Name);
        Assert.Equal("CAT-LAC", guardada.Code);
        Assert.True(guardada.IsActive);
    }

    [Fact]
    public async Task Rechaza_un_nombre_duplicado_ignorando_tildes_y_mayusculas()
    {
        var servicio = CrearServicio();
        await servicio.CreateAsync(NuevaCategoria("Lácteos"));

        await Assert.ThrowsAsync<DuplicateCategoryNameException>(
            () => servicio.CreateAsync(NuevaCategoria("LACTEOS")));
    }

    [Fact]
    public async Task Asigna_codigos_distintos_cuando_la_base_choca()
    {
        var servicio = CrearServicio();

        await servicio.CreateAsync(NuevaCategoria("Lácteos"));
        int segundo = await servicio.CreateAsync(NuevaCategoria("Lacticinios"));

        Category? guardada = await servicio.GetByIdAsync(segundo);

        Assert.NotNull(guardada);
        Assert.Equal("CAT-LCT", guardada.Code);
    }

    [Fact]
    public async Task La_baja_logica_saca_la_categoria_del_listado()
    {
        var servicio = CrearServicio();
        int id = await servicio.CreateAsync(NuevaCategoria("Lácteos"));

        Assert.True(await servicio.SoftDeleteAsync(id));

        Assert.Empty(await servicio.GetAllAsync());
        Assert.Null(await servicio.GetByIdAsync(id));

        // Una segunda baja ya no encuentra nada activo que dar de baja.
        Assert.False(await servicio.SoftDeleteAsync(id));
    }

    [Fact]
    public async Task El_codigo_de_una_categoria_dada_de_baja_sigue_ocupado()
    {
        var servicio = CrearServicio();
        int id = await servicio.CreateAsync(NuevaCategoria("Lácteos"));
        await servicio.SoftDeleteAsync(id);

        // Mismo nombre otra vez: el nombre quedo libre, pero CAT-LAC no, porque el indice
        // UNIQUE del codigo tambien cuenta las filas inactivas.
        int nuevo = await servicio.CreateAsync(NuevaCategoria("Lácteos"));

        Category? guardada = await servicio.GetByIdAsync(nuevo);

        Assert.NotNull(guardada);
        Assert.NotEqual("CAT-LAC", guardada.Code);
    }

    [Fact]
    public async Task El_listado_paginado_filtra_por_busqueda()
    {
        var servicio = CrearServicio();
        await servicio.CreateAsync(NuevaCategoria("Lácteos"));
        await servicio.CreateAsync(NuevaCategoria("Bebidas"));
        await servicio.CreateAsync(NuevaCategoria("Limpieza"));

        var pagina = await servicio.GetPagedAsync(1, 10, "lacteos");

        Assert.Equal(1, pagina.TotalCount);
        Assert.Equal("Lácteos", Assert.Single(pagina.Items).Name);
    }

    [Fact]
    public async Task Actualiza_el_nombre_y_conserva_el_codigo()
    {
        var servicio = CrearServicio();
        int id = await servicio.CreateAsync(NuevaCategoria("Lácteos"));

        var cambio = NuevaCategoria("Lácteos y Quesos");
        cambio.Id = id;

        Assert.True(await servicio.UpdateAsync(cambio));

        Category? guardada = await servicio.GetByIdAsync(id);

        Assert.NotNull(guardada);
        Assert.Equal("Lácteos Y Quesos", guardada.Name);
        Assert.Equal("CAT-LAC", guardada.Code);
    }
}
