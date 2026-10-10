using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

// Aplica la politica de CamposNoEnlazables al enlace de modelo de ASP.NET.
//
// Es el equivalente de poner [BindNever] en cada propiedad, pero declarado desde el
// adaptador web, para que las entidades de Domain no tengan que conocer ASP.NET.
//
// ASP.NET consulta este proveedor una vez por propiedad al construir los metadatos del
// modelo, asi que la politica se aplica a todas las paginas y a todos los handlers sin
// repetir nada. Al marcar IsBindingAllowed en false, el valor que llegue en el POST se
// descarta en silencio y la propiedad conserva lo que tenga el objeto.
public sealed class CamposNoEnlazablesProvider : IBindingMetadataProvider
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Key.ContainerType is { } contenedor &&
            context.Key.Name is { } propiedad &&
            CamposNoEnlazables.EsProhibido(contenedor, propiedad))
        {
            context.BindingMetadata.IsBindingAllowed = false;
        }
    }
}
