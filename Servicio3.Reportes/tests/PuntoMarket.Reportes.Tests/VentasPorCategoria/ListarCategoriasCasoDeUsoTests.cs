using PuntoMarket.Reportes.Application.Ports.Consultas;
using PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.ListarCategorias;

namespace PuntoMarket.Reportes.Tests.VentasPorCategoria;

public class ListarCategoriasCasoDeUsoTests
{
    private sealed class ConsultaFalsa(IReadOnlyList<CategoriaDisponible> categorias) : IConsultaCategorias
    {
        public Task<IReadOnlyList<CategoriaDisponible>> ListarAsync(CancellationToken cancelacion) => Task.FromResult(categorias);
    }

    [Fact]
    public async Task Devuelve_las_categorias_que_entrega_la_consulta()
    {
        var esperadas = new[] { new CategoriaDisponible(1, "Bebidas"), new CategoriaDisponible(2, "Lácteos") };
        var casoDeUso = new ListarCategoriasCasoDeUso(new ConsultaFalsa(esperadas));

        var categorias = await casoDeUso.EjecutarAsync(CancellationToken.None);

        Assert.Equal(esperadas, categorias);
    }
}
