namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public abstract class CreatorRepositorio<TRepositorio> where TRepositorio : class
    {
        private TRepositorio? repositorio;

        protected abstract TRepositorio CrearRepositorio();

        public TRepositorio ObtenerRepositorio()
        {
            return repositorio ??= CrearRepositorio();
        }
    }
}
