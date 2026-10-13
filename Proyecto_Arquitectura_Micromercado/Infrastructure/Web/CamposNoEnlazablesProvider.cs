using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Web;

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
