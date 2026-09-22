namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public abstract class CreatorRepositorio<TRepositorio> where TRepositorio : class
    {
        public abstract TRepositorio CrearRepositorio();
    }
}
