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
│   ├── Persistence/EnMemoria/                 ← segundo motor (Productos Concretos)
│   │   ├── TextoEnMemoria.cs                  (normalización + paginación)
│   │   ├── InMemoryCategoryRepository.cs
│   │   ├── InMemoryProductRepository.cs
│   │   ├── InMemorySupplierRepository.cs
│   │   └── InMemoryPriceHistoryRepository.cs
│   ├── Factories/                             ← acá vive el patrón
│   │   ├── CreatorRepositorio.cs              (Creador Abstracto genérico)
│   │   ├── CreatorCategoryRepository.cs       (raíz de jerarquía, abstracta)
│   │   ├── CreatorProductRepository.cs        (raíz de jerarquía, abstracta)
│   │   ├── CreatorSupplierRepository.cs       (raíz de jerarquía, abstracta)
│   │   ├── CreatorPriceHistoryRepository.cs   (raíz de jerarquía, abstracta)
│   │   ├── Creator*RepositoryMySql.cs         (4 Creadores Concretos)
│   │   └── Creator*RepositoryEnMemoria.cs     (4 Creadores Concretos)
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
| **Creador** (declara el método fábrica y lo consume en una operación propia) | `CreatorCRUD<T>` | `CreatorRepositorio<TRepositorio>`, más una raíz abstracta por contrato: `CreatorCategoryRepository`, `CreatorProductRepository`, `CreatorSupplierRepository`, `CreatorPriceHistoryRepository` |
| **Creador Concreto** (decide qué clase concreta instanciar) | `CreatorCliente` | `Creator*RepositoryMySql` y `Creator*RepositoryEnMemoria` (dos por jerarquía) |

Cada Creador Concreto sobrescribe `CrearRepositorio()` devolviendo la
implementación de su motor, igual que `CreatorCliente.CrearRepositorio()`
devuelve `new ClienteRepositorio()` en el ejemplo del docente.

Dos detalles del Creador Abstracto que son los que lo vuelven un Factory Method
y no una fábrica simple:

1. **`CrearRepositorio()` es `protected`, no `public`.** Es un *hook* interno del
   patrón: los clientes no lo llaman.
2. **El Creador tiene una operación propia, `ObtenerRepositorio()`**, que consume
   ese hook y memoiza el resultado. En el GoF el Creador siempre tiene una
   operación que usa el producto del método fábrica; sin ella la clase abstracta
   sería una interfaz de fábrica y nada más. La memoización, además, es lo que
   hace que el ciclo de vida `Scoped` se respete: un Creador por petición HTTP
   significa un repositorio por petición HTTP, incluso cuando otro Creador
   reutiliza ese repositorio como dependencia.

### Por qué hay una raíz abstracta por contrato

`CreatorRepositorio<ICategoryRepository>` y `CreatorRepositorio<IProductRepository>`
son, en C#, dos tipos **sin relación de herencia**: su único ancestro común es
`object`. Apoyando la jerarquía sólo en el genérico, ninguna variable del programa
podía apuntar a dos Creadores distintos, y sin eso no hay polimorfismo — que es
toda la razón de existir del patrón. Las raíces no genéricas
(`CreatorCategoryRepository` y sus hermanas) resuelven exactamente eso: son el
tipo que `Program.cs` y las pruebas declaran, sin saber qué motor hay detrás.

### Por qué el eje de variación es el motor y no la entidad

El patrón sirve para intercambiar **implementaciones alternativas del mismo
producto**. `ICategoryRepository` e `IProductRepository` no son alternativas entre
sí: son productos distintos que nunca van a sustituirse. Lo que sí varía en una
capa de persistencia es la tecnología, así que cada jerarquía varía por motor
(MySQL / en memoria) y no por tabla.

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

| Principio | Cómo se cumple acá | Cómo se verifica |
|---|---|---|
| **SRP** | Se separa la responsabilidad de *crear* el repositorio (el Creador) de la de *usarlo* (el Servicio). También se separa "gestionar productos" de "gestionar su historial": el SQL del historial vive en su propio repositorio y el de producto se lo delega. | Lectura de código: `MySqlProductRepository` ya no contiene SQL de `HISTORIAL_PRECIO`. |
| **OCP** | Para cambiar de motor de persistencia se cambia **un tipo por par de líneas en `Program.cs`** y nada más: ni los Creadores, ni los repositorios, ni los servicios, ni las páginas. Agregar un tercer motor es agregar archivos nuevos y una subclase por jerarquía, sin modificar las existentes. | `CategoryServiceEnMemoriaTests` ejercita `CategoryService` entero sobre otro motor sin que el servicio cambie. |
| **LSP** | El repositorio en memoria respeta el mismo contrato observable que el de MySQL: baja lógica, los mismos índices UNIQUE traducidos a validaciones, las mismas excepciones de duplicado, y comparaciones que ignoran mayúsculas, tildes y espacios repetidos (equivalente a la collation `utf8mb4_unicode_ci`). | `CategoryServiceEnMemoriaTests` comprueba ese comportamiento esperado caso por caso. |
| **ISP** | Cada entidad implementa sólo las interfaces segregadas que necesita (`IConCatalogo`, `IConListado`, `IConBusqueda`, `IConHistorialPrecios`, `IConRegistroHistorial`), en vez de una interfaz única con todos los métodos de todas las entidades. | Lectura de código: ningún repositorio implementa métodos que no usa. |
| **DIP** | Los servicios dependen sólo de abstracciones de `Application/`. Los repositorios reciben la conexión por constructor en vez de leerla del estático `DatabaseConnection.Instance`, así que ya no tienen dependencias ocultas. `Program.cs` conoce los tipos concretos, y eso es correcto: es el *composition root*, el único lugar que tiene permitido conocerlos. | `FactoryMethodTests` construye los Creadores de los dos motores sin abrir ninguna conexión. |

**Advertencia honesta sobre OCP.** "Agregar una quinta tabla sin tocar las otras
cuatro" **no es** un beneficio del patrón: agregar una entidad siempre fue agregar
archivos nuevos, con fábrica o sin ella. El beneficio real y medible es el otro:
cambiar la implementación de un repositorio existente sin modificar a sus
clientes. Conviene no acreditarle al patrón lo que ya era gratis.

---

## 4. Qué NO cambia (para tranquilidad del equipo)

- Ninguna consulta SQL existente cambia su lógica, solo su ubicación de archivo.
- Ninguna página (`Pages/`) cambia — siguen llamando a los mismos métodos de los
  mismos servicios.
- El ciclo de vida `Scoped` de las dependencias se mantiene igual.
- No se implementa nada más que Factory Method: no se agregan otros patrones
  creacionales en este sprint.

---

## 5. Correcciones aplicadas sobre la primera versión

La primera implementación de este sprint no era un Factory Method del GoF, sino
cuatro fábricas simples (una por entidad) unificadas sólo en apariencia por la
clase base genérica. Se corrigieron estos puntos, en este orden:

1. **Defecto de ciclo de vida.** `CreatorProductRepository` llamaba directamente a
   `CrearRepositorio()` del Creador de historial, salteando el contenedor DI, con
   lo que había **dos instancias** de `IPriceHistoryRepository` por petición HTTP:
   una la recibía `ProductService` y otra quedaba dentro de
   `MySqlProductRepository`. El `AddScoped` declarado en `Program.cs` era una
   promesa incumplida. Era inofensivo sólo porque los repositorios no guardan
   estado, pero rompía en cuanto el historial compartiera una transacción.
   Corregido con la operación memoizada `ObtenerRepositorio()`.

2. **Creador sin operación propia.** La clase abstracta sólo declaraba el método
   fábrica. Se le agregó `ObtenerRepositorio()` y `CrearRepositorio()` pasó a ser
   `protected`.

3. **Dependencia oculta en los repositorios.** Los cuatro repositorios MySQL
   resolvían su conexión con `DatabaseConnection.Instance` dentro de
   `OpenConnectionAsync`. Ahora la reciben por constructor. Esto es también lo que
   le dio trabajo real al método fábrica: antes hacía un `new` sin argumentos, o
   sea devolvía un tipo fijo sin ninguna decisión ni dato que entregar.

4. **Eje de variación equivocado.** El patrón variaba por entidad. Ahora varía por
   motor, con una raíz abstracta no genérica por contrato.

5. **Punto de extensión sin ejercitar.** Cada jerarquía tenía una sola subclase y
   cada contrato una sola implementación, así que OCP y LSP se afirmaban sin poder
   verificarse. Se agregó el motor en memoria y las pruebas que lo demuestran.

El comportamiento de la aplicación no cambió: `Program.cs` sigue registrando el
motor MySQL y las páginas siguen llamando a los mismos métodos de los mismos
servicios.
