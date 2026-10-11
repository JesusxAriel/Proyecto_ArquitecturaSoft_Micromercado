namespace Proyecto_Arquitectura_Micromercado.Application.Security;

// Puerto que responde "quien esta usando el sistema ahora mismo".
//
// Es la unica fuente de identidad para la auditoria: los repositorios y los casos de uso
// lo piden por constructor en vez de recibir un identificador por parametro o de leer
// una constante. Asi ningun dato de auditoria puede venir del navegador.
//
// Si no hay un usuario autenticado, cada propiedad lanza InvalidOperationException: es
// preferible fallar a escribir un registro de auditoria sin autor.
public interface IUsuarioActual
{
    int Id { get; }

    string NombreUsuario { get; }

    string Rol { get; }
}
