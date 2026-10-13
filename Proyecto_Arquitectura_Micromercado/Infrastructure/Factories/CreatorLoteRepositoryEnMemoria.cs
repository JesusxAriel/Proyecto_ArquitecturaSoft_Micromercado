using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.EnMemoria;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;

public sealed class CreatorLoteRepositoryEnMemoria : CreatorLoteRepository
{
    protected override ILoteRepository CrearRepositorio()
    {
        return new InMemoryLoteRepository();
    }
}
