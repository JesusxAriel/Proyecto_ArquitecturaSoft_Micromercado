using Proyecto_Arquitectura_Micromercado.Application.Categories;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public class CreatorCategoryRepository : CreatorRepositorio<ICategoryRepository>
    {
        private readonly IConfiguration _configuration;

        public CreatorCategoryRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override ICategoryRepository CrearRepositorio()
        {
            return new MySqlCategoryRepository(_configuration);
        }
    }
}