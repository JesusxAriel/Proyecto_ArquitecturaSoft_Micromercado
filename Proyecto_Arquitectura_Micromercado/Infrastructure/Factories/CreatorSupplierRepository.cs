using Proyecto_Arquitectura_Micromercado.Application.Suppliers;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorSupplierRepository : CreatorRepositorio<ISupplierRepository>
    {
        private readonly IConfiguration _configuration;

        public CreatorSupplierRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override ISupplierRepository CrearRepositorio()
        {
            return new MySqlSupplierRepository(_configuration);
        }
    }
}
