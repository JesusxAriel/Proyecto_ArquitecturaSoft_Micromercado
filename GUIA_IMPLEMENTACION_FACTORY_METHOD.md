# Guía de implementación: Factory Method en las 4 tablas

Este documento es la guía operativa para que **cada grupo** (Categoría, Producto,
Proveedor, Historial de Precios) implemente el Factory Method en su propia tabla
sin dudas ni variantes distintas entre grupos. Complementa a
`FACTORY_METHOD_ESTRUCTURA_FINAL.md` (que explica el "qué" y el "por qué") con el
"cómo, paso a paso".

**Regla de oro para todo el equipo**: la conexión de la inyección de dependencias
se hace **siempre en `Program.cs`**, nunca dentro de un `Service`. Ningún `Service`
debe recibir un Creador por constructor. Esto es igual para las 4 tablas, sin
excepción — ver sección 4 para la justificación completa si alguien tiene dudas.

---

## 1. Estructura final (por tabla)

```
Application/
├── Common/
│   ├── IRepositorioBase.cs              (sin cambios — ya existe)
│   └── IServicioCRUD.cs                 (sin cambios — ya existe)
├── Categories/                          (sin cambios)
├── Products/
│   ├── IPriceHistoryRepository.cs       ← NUEVO (grupo Historial)
│   ├── IProductRepository.cs            ← MODIFICADO (grupo Producto)
│   ├── IProductService.cs               ← MODIFICADO (grupo Producto)
│   ├── IConCatalogo.cs                  (sin cambios)
│   ├── IConHistorialPrecios.cs          (sin cambios)
│   ├── IConListado.cs                   (sin cambios)
│   ├── IConRegistroHistorial.cs         (sin cambios)
│   └── ProductService.cs                ← MODIFICADO (grupo Producto)
└── Suppliers/                           (sin cambios)

Infrastructure/
├── Persistence/
│   ├── MySqlCategoryRepository.cs       (sin cambios)
│   ├── MySqlProductRepository.cs        ← MODIFICADO (grupo Producto)
│   ├── MySqlSupplierRepository.cs       (sin cambios)
│   └── MySqlPriceHistoryRepository.cs   ← NUEVO (grupo Historial)
├── Factories/                           ← NUEVA carpeta (todos los grupos escriben acá)
│   ├── CreatorRepositorio.cs            ← NUEVO — se crea UNA sola vez, es compartido
│   ├── CreatorCategoryRepository.cs     ← NUEVO (grupo Categoría)
│   ├── CreatorProductRepository.cs      ← NUEVO (grupo Producto)
│   ├── CreatorSupplierRepository.cs     ← NUEVO (grupo Proveedor)
│   └── CreatorPriceHistoryRepository.cs ← NUEVO (grupo Historial)
└── Web/
    └── DecimalModelBinder.cs            (sin cambios)

Program.cs                               ← MODIFICADO por los 4 grupos (cada uno agrega su bloque)
Pages/, Domain/, Validations/, wwwroot/  (sin cambios — ningún grupo los toca)
```

**Importante**: `CreatorRepositorio.cs` (el Creador Abstracto genérico) es
**compartido por los 4 grupos** — no lo cree cada grupo por separado, porque si
dos personas lo crean a la vez van a tener conflicto de merge. Que lo cree una
sola persona (por ejemplo, quien coordina el equipo) y lo suba primero; el resto
hace `pull` antes de empezar su Creador Concreto.

---

## 2. El Creador Abstracto (una sola vez, compartido)

`Infrastructure/Factories/CreatorRepositorio.cs`:

```csharp
namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public abstract class CreatorRepositorio<TRepositorio> where TRepositorio : class
    {
        private TRepositorio? repositorio;

        // Método fábrica (hook): lo implementa cada Creador Concreto.
        protected abstract TRepositorio CrearRepositorio();

        // Operación del Creator que usa el producto del método fábrica.
        public TRepositorio ObtenerRepositorio() => repositorio ??= CrearRepositorio();
    }
}
```

Dos cosas que **no** hay que cambiar al copiar esto:

- `CrearRepositorio()` es **`protected`**. Es un hook interno; nadie lo llama de
  afuera. Los clientes usan `ObtenerRepositorio()`.
- `ObtenerRepositorio()` **memoiza**. Esto no es una optimización: es lo que hace
  que el ciclo de vida `Scoped` se cumpla. Si un Creador llamara a
  `CrearRepositorio()` para resolver una dependencia, crearía una instancia
  paralela a la que el contenedor DI ya entregó al resto de la petición.

Esto reemplaza al `CreatorCRUD<T>` del ejemplo del docente, pero parametrizado por
el **tipo de repositorio** (`TRepositorio`) en vez de por la entidad — porque acá
cada repositorio ya es una interfaz distinta (`ICategoryRepository`,
`IProductRepository`, etc.), no un genérico único como `ICRUD<T>`.

### 2.1. Además, una raíz abstracta por cada tabla

El genérico solo no alcanza: en C#, `CreatorRepositorio<ICategoryRepository>` y
`CreatorRepositorio<IProductRepository>` son tipos **sin relación de herencia**, así
que ninguna variable puede contener a dos Creadores distintos y el polimorfismo del
patrón queda inalcanzable. Por eso cada tabla aporta una raíz no genérica:

```csharp
public abstract class Creator<Entidad>Repository : CreatorRepositorio<I<Entidad>Repository>
{
}
```

Ese es el tipo que se registra y se declara en todos lados. Las subclases son los
Creadores Concretos, y **lo que varía entre ellas es el motor de persistencia**, no
la entidad.

---

## 3. Paso a paso genérico (cada grupo lo aplica a su tabla)

Reemplazá `<Entidad>` por `Category`, `Product`, `Supplier` o `PriceHistory` según
tu tabla.

### Paso 1 — Verificá tu interfaz de repositorio (no la toques, salvo Producto)

`I<Entidad>Repository.cs` ya existe y ya hereda de `IRepositorioBase<...>` más sus
interfaces segregadas. **No hay que crear nada nuevo acá**, salvo en el grupo de
Producto (ver sección 5) y el de Historial (que sí crea `IPriceHistoryRepository`
desde cero, ver sección 6).

### Paso 2 — Verificá tu implementación MySQL (no la toques, salvo Producto)

`MySql<Entidad>Repository.cs` ya implementa esa interfaz con SQL parametrizado.
Tampoco se toca, salvo en Producto e Historial.

### Paso 3 — Creá tu raíz abstracta y tu Creador Concreto

Son **dos** archivos. Primero la raíz, en
`Infrastructure/Factories/Creator<Entidad>Repository.cs`:

```csharp
using Proyecto_Arquitectura_Micromercado.Application.<Modulo>;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public abstract class Creator<Entidad>Repository : CreatorRepositorio<I<Entidad>Repository>
    {
    }
}
```

Después el Creador Concreto de MySQL, en
`Infrastructure/Factories/Creator<Entidad>RepositoryMySql.cs`:

```csharp
using Proyecto_Arquitectura_Micromercado.Application.<Modulo>;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Database;
using Proyecto_Arquitectura_Micromercado.Infrastructure.Persistence;

namespace Proyecto_Arquitectura_Micromercado.Infrastructure.Factories
{
    public sealed class Creator<Entidad>RepositoryMySql(DatabaseConnection conexion)
        : Creator<Entidad>Repository
    {
        protected override I<Entidad>Repository CrearRepositorio()
        {
            return new MySql<Entidad>Repository(conexion);
        }
    }
}
```

**Antes de copiar esto**: abrí tu `MySql<Entidad>Repository.cs` y fijate qué recibe
su constructor. Todos reciben `DatabaseConnection`, y el de Producto recibe además
`IPriceHistoryRepository`. El Creador debe recibir **exactamente lo mismo** que
recibe el constructor de tu repositorio y pasárselo tal cual al `new`.

**Si tu repositorio necesita otro repositorio** (el caso de Producto), el Creador
recibe el **Creador abstracto** de esa dependencia y la obtiene con
`ObtenerRepositorio()` — nunca con un `new` directo ni con `CrearRepositorio()`:

```csharp
public sealed class CreatorProductRepositoryMySql(
    DatabaseConnection conexion,
    CreatorPriceHistoryRepository creatorHistorialPrecios) : CreatorProductRepository
{
    protected override IProductRepository CrearRepositorio() =>
        new MySqlProductRepository(conexion, creatorHistorialPrecios.ObtenerRepositorio());
}
```

### Paso 3.1 (opcional) — Tu Creador Concreto en memoria

Si querés poder probar tu servicio sin base de datos, agregá un segundo Creador
Concreto, `Creator<Entidad>RepositoryEnMemoria`, que devuelva un
`InMemory<Entidad>Repository`. Ya existen los cuatro en
`Infrastructure/Persistence/EnMemoria/`, así que lo más probable es que el tuyo ya
esté hecho. Si escribís uno nuevo, tiene que respetar el **mismo comportamiento
observable** que el de MySQL (baja lógica, las mismas excepciones de duplicado y
comparaciones que ignoren mayúsculas, tildes y espacios repetidos), o estarás
rompiendo el LSP y las pruebas lo van a detectar.

### Paso 4 — Conectá en `Program.cs` (Opción A, la única válida)

Buscá el bloque donde hoy está registrado tu repositorio (algo como
`builder.Services.AddScoped<I<Entidad>Repository, MySql<Entidad>Repository>();`)
y reemplazalo por:

```csharp
builder.Services.AddScoped<Creator<Entidad>Repository, Creator<Entidad>RepositoryMySql>();
builder.Services.AddScoped<I<Entidad>Repository>(sp =>
    sp.GetRequiredService<Creator<Entidad>Repository>().ObtenerRepositorio());
```

Fijate en las dos cosas que importan acá:

- Lo que se **registra** es el tipo abstracto; lo que se **elige** es el concreto.
  Cambiar de motor = cambiar sólo el segundo tipo de la primera línea.
- Se llama a `ObtenerRepositorio()`, nunca a `CrearRepositorio()` (que además es
  `protected` y no compilaría).

No toques el registro de tu `I<Entidad>Service` — ese sigue exactamente igual que
antes.

### Paso 5 — Confirmá que tu Service no cambió

Abrí `<Entidad>Service.cs` y confirmá que su constructor sigue recibiendo
`I<Entidad>Repository` (la interfaz), no el Creador. Si no tocaste ese archivo,
no debería haber cambiado nada — eso es correcto y esperado.

### Paso 6 — Compilá y probá tu módulo

1. `dotnet build` — debe compilar en 0 errores.
2. Probá crear/editar/listar/eliminar desde la UI de tu tabla, para confirmar que
   sigue funcionando exactamente igual que antes (el comportamiento no cambia, solo
   cambia quién construye el objeto).

---

## 4. Por qué la conexión va siempre en `Program.cs` (resumen para el que tenga dudas)

- El `Service` solo debe conocer contratos de `Application` (interfaces), nunca
  clases concretas de `Infrastructure` como el Creador.
- Si el `Service` recibiera el Creador, tendría que llamar `CrearRepositorio()` él
  mismo — mezclando "construir su dependencia" con "hacer su trabajo de negocio".
- `Program.cs` es el único lugar del proyecto que ya tiene permitido conocer clases
  concretas (es el *composition root* de toda la app) — por eso es el único lugar
  correcto para usar el Creador.
- Si cada grupo conecta su tabla distinto, el proyecto deja de tener una
  arquitectura consistente y nadie puede revisar el código de otro grupo sin
  reaprender su variante.

---

## 5. Caso especial — Grupo de Producto

Producto tiene un paso extra porque hoy `IProductRepository` incluye operaciones
de historial de precios que se están moviendo a su propio repositorio (ver
sección 6). Antes de hacer los pasos 1 a 6 de la sección 3, el grupo de Producto
debe:

1. Quitar de `IProductRepository.cs` la herencia de `IConHistorialPrecios` e
   `IConRegistroHistorial`. `IProductRepository` queda heredando solo de
   `IRepositorioBase<Product, ProductListItem, int>`, `IConCatalogo` e
   `IConListado`.
2. En `MySqlProductRepository.cs`: sacar la implementación de esos dos métodos
   (el SQL se muda a `MySqlPriceHistoryRepository`, no se reescribe). Agregar al
   constructor un parámetro `IPriceHistoryRepository`, y usarlo donde antes se
   llamaba al método privado de historial (por ejemplo, al actualizar el precio
   de un producto).
3. En `ProductService.cs`: agregar `IPriceHistoryRepository` al constructor (además
   de `IProductRepository`, que ya tenía). El método que hoy expone el historial a
   `Pages/Products/History.cshtml.cs` debe seguir llamándose igual, pero por dentro
   ahora delega en `IPriceHistoryRepository` en vez de en `IProductRepository`.
4. Recién ahí, seguir los pasos 3 a 6 de la sección 3 para conectar
   `CreatorProductRepository` en `Program.cs`.

`Pages/Products/History.cshtml.cs` **no se toca** — sigue llamando al mismo método
del mismo `IProductService` de siempre.

---

## 6. Caso especial — Grupo de Historial de Precios

Este grupo crea el repositorio desde cero, no modifica uno existente:

1. Crear `Application/Products/IPriceHistoryRepository.cs`:
   ```csharp
   namespace Proyecto_Arquitectura_Micromercado.Application.Products
   {
       public interface IPriceHistoryRepository : IConHistorialPrecios, IConRegistroHistorial
       {
       }
   }
   ```
   (Ajustá los nombres exactos de los métodos según lo que ya tengan definido
   `IConHistorialPrecios` e `IConRegistroHistorial` hoy — no se agregan métodos
   nuevos, solo se agrupan los que ya existen.)

2. Crear `Infrastructure/Persistence/MySqlPriceHistoryRepository.cs`, moviendo ahí
   el SQL que hoy está dentro de `MySqlProductRepository` para insertar y consultar
   el historial. **No se reescribe la lógica SQL**, solo cambia de archivo.

3. Coordinar con el grupo de Producto el orden: Producto necesita que
   `IPriceHistoryRepository` y `MySqlPriceHistoryRepository` ya existan antes de
   poder inyectarlos en su propio constructor (paso 2 de la sección 5).

4. Seguir los pasos 3 a 6 de la sección 3 para `CreatorPriceHistoryRepository`.

---

## 7. Checklist final por grupo

- [ ] Mi interfaz de repositorio no cambió de forma inesperada (o cambió
      exactamente como dice mi sección especial, si soy Producto o Historial).
- [ ] Mi `MySql<Entidad>Repository` sigue con el mismo SQL, solo reorganizado si
      corresponde.
- [ ] Creé `Creator<Entidad>Repository.cs` (raíz abstracta) y
      `Creator<Entidad>RepositoryMySql.cs` (Creador Concreto) en
      `Infrastructure/Factories/`.
- [ ] Mi método fábrica es `protected override`, no `public override`.
- [ ] Si mi repositorio depende de otro repositorio, mi Creador recibe el Creador
      **abstracto** de esa dependencia y la obtiene con `ObtenerRepositorio()`.
- [ ] `Program.cs` registra el Creador abstracto apuntando al concreto y resuelve mi
      interfaz con `ObtenerRepositorio()` (dos líneas, sin tocar el registro de mi
      Service).
- [ ] Mi `<Entidad>Service.cs` NO recibe el Creador por constructor.
- [ ] `dotnet build` da 0 errores.
- [ ] Probé crear/editar/listar/eliminar desde la UI de mi tabla y funciona igual
      que antes.
- [ ] `dotnet test` pasa en verde.
- [ ] No toqué ningún archivo de `Pages/`, `Domain/`, `Validations/` ni `wwwroot/`.
