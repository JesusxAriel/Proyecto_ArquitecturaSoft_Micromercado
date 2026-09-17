# Estructura final del proyecto aplicando Factory Method

Este documento define la estructura objetivo del proyecto para el sprint de
Factory Method, y explica **qué archivo cambia, por qué, y cómo la estructura
cumple con el patrón** descrito en `Factory_Method_Y_Principios_SOLID.md`.

---

## 1. Estructura final

```
Proyecto_Arquitectura_Micromercado/
├── Application/
│   ├── Common/
│   │   ├── IRepositorioBase.cs
│   │   └── IServicioCRUD.cs
│   ├── Categories/                     (igual que ahora)
│   ├── Products/
│   │   ├── IPriceHistoryRepository.cs  ← NUEVO: contrato propio de la tabla histórica
│   │   ├── IProductRepository.cs
│   │   ├── IProductService.cs
│   │   ├── IConCatalogo.cs
│   │   ├── IConHistorialPrecios.cs
│   │   ├── IConListado.cs
│   │   ├── IConRegistroHistorial.cs
│   │   └── ProductService.cs
│   └── Suppliers/                      (igual que ahora)
├── Domain/                             (sin cambios)
├── Infrastructure/
│   ├── Persistence/
│   │   ├── MySqlCategoryRepository.cs
│   │   ├── MySqlProductRepository.cs
│   │   ├── MySqlSupplierRepository.cs
│   │   └── MySqlPriceHistoryRepository.cs   ← NUEVO: se extrae de Product
│   ├── Factories/                            ← NUEVA carpeta (acá vive el patrón)
│   │   ├── CreatorRepositorio.cs             (Creador Abstracto genérico)
│   │   ├── CreatorCategoryRepository.cs      (Creador Concreto)
│   │   ├── CreatorProductRepository.cs       (Creador Concreto)
│   │   ├── CreatorSupplierRepository.cs      (Creador Concreto)
│   │   └── CreatorPriceHistoryRepository.cs  (Creador Concreto)
│   └── Web/
│       └── DecimalModelBinder.cs
├── Pages/                               (sin cambios)
├── Validations/                         (sin cambios)
└── Program.cs                           (cambia solo el bloque de registro DI)
```

---

## 2. Qué archivo cambia y por qué

**Aclaración importante primero**: en este sprint **no se elimina ningún archivo**.
Todo lo que hoy existe sigue existiendo; algunos archivos se *modifican* (les
cambia el contenido, no el propósito) y otros son *nuevos*. Nada se borra porque
nada deja de ser necesario — solo se reorganiza una responsabilidad (el historial
de precios) que hoy vive mezclada dentro de Producto.

### 2.1. Archivos NUEVOS

| Archivo | Por qué es necesario |
|---|---|
| `Application/Products/IPriceHistoryRepository.cs` | Contrato propio para la tabla `HISTORIAL_PRECIO`. Agrupa `IConHistorialPrecios` (leer) + `IConRegistroHistorial` (escribir) en un único contrato, porque el historial pasa a ser un repositorio independiente, no un apéndice de Producto. |
| `Infrastructure/Persistence/MySqlPriceHistoryRepository.cs` | Implementación MySQL de `IPriceHistoryRepository`. Contiene el SQL que hoy vive dentro de `MySqlProductRepository` para insertar y consultar el historial. |
| `Infrastructure/Factories/CreatorRepositorio.cs` | El **Creador Abstracto** genérico del patrón (equivalente a `CreatorCRUD<T>` del ejemplo del docente, pero parametrizado por el tipo de repositorio en vez de por la entidad). |
| `Infrastructure/Factories/CreatorCategoryRepository.cs` | **Creador Concreto** para Categoría. |
| `Infrastructure/Factories/CreatorProductRepository.cs` | **Creador Concreto** para Producto. |
| `Infrastructure/Factories/CreatorSupplierRepository.cs` | **Creador Concreto** para Proveedor. |
| `Infrastructure/Factories/CreatorPriceHistoryRepository.cs` | **Creador Concreto** para el Historial de Precios. |

### 2.2. Archivos MODIFICADOS (no eliminados)

| Archivo | Qué cambia | Por qué |
|---|---|---|
| `IProductRepository.cs` | Deja de heredar directamente de `IConHistorialPrecios` e `IConRegistroHistorial`. | Esas operaciones ahora pertenecen al contrato `IPriceHistoryRepository`, no al de Producto. Producto conserva `IRepositorioBase`, `IConCatalogo` e `IConListado`. |
| `MySqlProductRepository.cs` | Se elimina de **adentro de esta clase** el código SQL de historial (no el archivo, el contenido). Donde antes llamaba a su propio método privado para registrar el historial al actualizar un producto, ahora recibe `IPriceHistoryRepository` por constructor y lo invoca. | Antes esta clase tenía dos responsabilidades (SRP violado): gestionar productos Y gestionar su historial. Ahora solo gestiona productos, y delega el historial al repositorio correspondiente. |
| `ProductService.cs` | Recibe también `IPriceHistoryRepository` por constructor (además de `IProductRepository`). El método que expone el historial a `Pages/Products/History.cshtml.cs` sigue existiendo con el mismo nombre, pero por dentro ahora delega en `IPriceHistoryRepository` en vez de en `IProductRepository`. | Así `Pages/` no se entera del cambio: sigue llamando al mismo método de `IProductService` que ya usaba. |
| `Program.cs` | El bloque de registro de dependencias cambia de instanciar las clases MySQL directamente a resolverlas a través de su Creador correspondiente (ver sección 3). | Es el único lugar donde se "activa" el patrón: acá es donde se decide qué Creador Concreto se usa. |

### 2.3. Archivos SIN CAMBIOS

`IConHistorialPrecios.cs` e `IConRegistroHistorial.cs` no cambian — solo cambia
*quién* las implementa (pasan de `MySqlProductRepository` a
`MySqlPriceHistoryRepository`, a través de `IPriceHistoryRepository`). Todo
`Pages/`, `Domain/`, `Validations/` y `wwwroot/` quedan intactos, igual que en el
sprint anterior.

---

## 3. Por qué esta estructura cumple con el Factory Method

Usando la terminología de la guía del docente (sección 2.3, los 4 componentes del
patrón):

| Componente del patrón | En el ejemplo del docente | En esta estructura |
|---|---|---|
| **Producto** (interfaz que se crea) | `ICRUD<T>` | `IRepositorioBase<TEntidad, TListItem, TId>` y las interfaces específicas (`ICategoryRepository`, `IProductRepository`, `ISupplierRepository`, `IPriceHistoryRepository`) |
| **Producto Concreto** | `ClienteRepositorio`, `ProductoRepositorio` | `MySqlCategoryRepository`, `MySqlProductRepository`, `MySqlSupplierRepository`, `MySqlPriceHistoryRepository` |
| **Creador** (declara el método fábrica) | `CreatorCRUD<T>` | `CreatorRepositorio<TRepositorio>` |
| **Creador Concreto** (decide qué clase concreta instanciar) | `CreatorCliente` | `CreatorCategoryRepository`, `CreatorProductRepository`, `CreatorSupplierRepository`, `CreatorPriceHistoryRepository` |

Cada Creador Concreto sobrescribe `CrearRepositorio()` devolviendo su
implementación MySQL, exactamente igual que `CreatorCliente.CrearRepositorio()`
devuelve `new ClienteRepositorio()` en el ejemplo del docente.

### Por qué se necesitaban 4 Creadores y no 3

Pediste que el patrón cubra tus 3 tablas CRUD **y** la tabla histórica. Como hoy
el historial vive mezclado dentro de Producto, no existía como una "tabla" con
repositorio propio — por eso el paso 2.1 lo extrae primero a
`IPriceHistoryRepository` / `MySqlPriceHistoryRepository`. Una vez que existe como
repositorio independiente, puede tener su propio Creador Concreto, igual que
cualquier otra tabla. Sin esa extracción, el patrón solo tendría 3 Creadores (uno
por entidad CRUD) y el historial quedaría "escondido" dentro del de Producto, sin
demostrar el patrón sobre él.

### Relación con los principios SOLID (según la guía del docente, sección 4)

| Principio | Cómo se cumple acá |
|---|---|
| **SRP** | Se separa la responsabilidad de *crear* el repositorio (el Creador) de la responsabilidad de *usarlo* (el Servicio). También se separa la responsabilidad de "gestionar productos" de "gestionar su historial". |
| **OCP** | Si mañana se agrega una quinta tabla, se crea un quinto Creador Concreto sin tocar ninguno de los 4 existentes ni `Program.cs` más que para agregar una línea. |
| **LSP** | Los 4 repositorios concretos cumplen el contrato de su interfaz sin lanzar excepciones inesperadas ni romper el comportamiento esperado — son intercambiables desde el punto de vista del Servicio. |
| **ISP** | Cada entidad implementa solo las interfaces segregadas que realmente necesita (`IConCatalogo`, `IConListado`, etc.), en vez de una interfaz única con todos los métodos de todas las entidades. |
| **DIP** | `ProductService` y `Program.cs` dependen de las abstracciones (`IProductRepository`, `IPriceHistoryRepository`), nunca de `MySqlProductRepository` o `MySqlPriceHistoryRepository` directamente. |

---

## 4. Qué NO cambia (para tranquilidad del equipo)

- Ninguna consulta SQL existente cambia su lógica, solo su ubicación de archivo.
- Ninguna página (`Pages/`) cambia — siguen llamando a los mismos métodos de los
  mismos servicios.
- El ciclo de vida `Scoped` de las dependencias se mantiene igual.
- No se implementa nada más que Factory Method: no se agregan otros patrones
  creacionales en este sprint.
