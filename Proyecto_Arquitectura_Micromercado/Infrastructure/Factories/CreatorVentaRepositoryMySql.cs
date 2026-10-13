using Proyecto_Arquitectura_Micromercado.Application.Sales;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence.Errores;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories;

public sealed class CreatorVentaRepositoryMySql(MySqlUnidadDeTrabajo unidadDeTrabajo) 
    : CreatorVentaRepository
{
    protected override IVentaRepository CrearRepositorio()
    {
        return new VentaRepositoryConErroresTraducidos(new MySqlVentaRepository(unidadDeTrabajo));
    }
}