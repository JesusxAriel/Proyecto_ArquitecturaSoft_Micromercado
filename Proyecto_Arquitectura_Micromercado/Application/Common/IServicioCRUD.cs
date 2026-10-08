namespace Proyecto_Arquitectura_Micromercado.Application.Common;

// Contrato CRUD comun a los servicios de aplicacion.
//
// Antes declaraba cuatro parametros genericos: <TEntidad, TId, TCrearDto, TActualizarDto>.
// Los tres servicios lo cerraban con la misma entidad en los tres lugares
// (por ejemplo IServicioCRUD<Product, int, Product, Product>), asi que la interfaz
// prometia una separacion entre entidad y DTO que no existia: los dos ultimos
// parametros no aportaban nada y solo hacian ruido al leer.
//
// Quedan los dos que si varian. Si mas adelante se introducen DTOs de entrada reales
// (ver la deuda documentada sobre el binding directo de entidades), se vuelven a
// agregar los parametros, pero esa vez con tipos distintos de verdad.
public interface IServicioCRUD<TEntidad, TId>
{
    Task<TEntidad?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default);

    Task<TId> CreateAsync(
        TEntidad entity,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        TEntidad entity,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        TId id,
        CancellationToken cancellationToken = default);
}
