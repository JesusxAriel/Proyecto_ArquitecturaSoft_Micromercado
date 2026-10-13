using Proyecto_Arquitectura_Micromercado.Application.Stock;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;

public sealed class CreatorLoteRepositoryMySql(MySqlUnidadDeTrabajo unidadDeTrabajo)
    : CreatorLoteRepository
{
    protected override ILoteRepository CrearRepositorio()
    {
        return new LoteRepositoryConErroresTraducidos(new MySqlLoteRepository(unidadDeTrabajo));
    }
}
