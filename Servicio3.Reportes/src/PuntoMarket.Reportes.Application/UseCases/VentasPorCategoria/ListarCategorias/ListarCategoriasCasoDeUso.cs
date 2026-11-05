using PuntoMarket.Reportes.Application.Ports.Consultas;

namespace PuntoMarket.Reportes.Application.UseCases.VentasPorCategoria.ListarCategorias;

public class ListarCategoriasCasoDeUso
{
    private readonly IConsultaCategorias _consulta;

    public ListarCategoriasCasoDeUso(IConsultaCategorias consulta)
    {
        _consulta = consulta;
    }

    public Task<IReadOnlyList<CategoriaDisponible>> EjecutarAsync(CancellationToken cancelacion) =>
        _consulta.ListarAsync(cancelacion);
}
