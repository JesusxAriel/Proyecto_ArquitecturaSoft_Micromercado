namespace Proyecto_Arquitectura_Micromercado.Application.Security;

// Puerto para proteger y comprobar contrasenas.
//
// Application solo conoce estas dos operaciones; el algoritmo y su configuracion viven
// en el adaptador. Asi se puede cambiar de algoritmo sin tocar el caso de uso del login
// y el hash nunca aparece en Domain.
public interface IPasswordHasher
{
    // Devuelve el hash de la contrasena. Dos llamadas con el mismo texto devuelven
    // valores distintos porque cada hash lleva su propia sal.
    string Hash(string contrasena);

    // Indica si la contrasena corresponde al hash. Devuelve false, sin lanzar, cuando el
    // hash esta vacio o no tiene un formato valido.
    bool Verify(string contrasena, string hash);
}
