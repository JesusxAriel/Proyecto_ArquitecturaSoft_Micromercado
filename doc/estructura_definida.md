# Estructura y arquitectura del CRUD de Productos, Proveedores y Categorías

Este documento describe la arquitectura unificada para los módulos de Productos, Proveedores y Categorías del proyecto `Proyecto_Arquitectura_Micromercado`. La solución utiliza ASP.NET Core Razor Pages, ADO.NET con MySQL y una separación estricta de responsabilidades entre Dominio, Aplicación, Infraestructura y Presentación, manteniendo el código en inglés y respetando los principios SOLID y Clean Code.

---

## 1. Estructura general de archivos

```text
Proyecto_Arquitectura_Micromercado/
│
├── Program.cs                                    # Punto de entrada de la aplicación: configura inyección de dependencias, cultura local y rutas.
│
├── Domain/                                       # CAPA DE DOMINIO: Contiene las entidades puras, DTOs y reglas de negocio del sistema.
│   │
│   ├── Products/                                 # Módulo de Dominio para Productos
│   │   ├── Product.cs                            # Entidad principal de Producto, DataAnnotations y proyecciones (ProductListItem, LookupOption).
│   │   └── ProductDtos.cs                        # Atributos de validación personalizados, expresiones regulares y constantes de mensajes.
│   │
│   ├── Suppliers/                                # Módulo de Dominio para Proveedores
│   │   ├── Supplier.cs                           # Entidad principal de Proveedor (Id, Nombre, Teléfono, Dirección) y sus DataAnnotations.
│   │   └── SupplierDtos.cs                       # Validadores de atributos personalizados (NIT, teléfono) y mensajes de error del módulo.
│   │
│   └── Categories/                               # Módulo de Dominio para Categorías
│       ├── Category.cs                           # Entidad principal de Categoría (Id, Nombre, Descripción, Estado) con DataAnnotations.
│       └── CategoryDtos.cs                       # Reglas de validación avanzadas para categorías y mensajes de error estandarizados.
│
├── Application/                                  # CAPA DE APLICACIÓN: Contiene las interfaces e implementaciones de los casos de uso.
│   │
│   ├── Products/                                 # Casos de uso de Productos
│   │   ├── IProductRepository.cs                 # Interfaz que define el contrato de persistencia para productos.
│   │   ├── IProductService.cs                    # Interfaz de los casos de uso consumidos por Razor Pages.
│   │   └── ProductService.cs                     # Implementación de reglas de aplicación: Title Case, redondeo y llamadas al repositorio.
│   │
│   ├── Suppliers/                                # Casos de uso de Proveedores
│   │   ├── ISupplierRepository.cs                # Interfaz que define las operaciones de datos para proveedores (CRUD).
│   │   ├── ISupplierService.cs                   # Interfaz que expone las acciones de negocio para la interfaz gráfica.
│   │   └── SupplierService.cs                    # Lógica de negocio de proveedores: limpieza de texto, validación de duplicados y ordenamiento.
│   │
│   └── Categories/                               # Casos de uso de Categorías
│       ├── ICategoryRepository.cs                # Interfaz del contrato de datos para categorías en la base de datos.
│       ├── ICategoryService.cs                   # Interfaz de casos de uso para la gestión de categorías.
│       └── CategoryService.cs                    # Lógica de negocio para categorías: normalización de nombres e integridad antes de guardar.
│
├── Infrastructure/                               # CAPA DE INFRAESTRUCTURA: Implementaciones de acceso a base de datos y utilidades HTTP.
│   │
│   ├── Persistence/                              # Implementación del acceso a datos con ADO.NET
│   │   ├── MySqlProductRepository.cs             # Implementación ADO.NET (MySqlConnection/MySqlCommand) para la tabla PRODUCTO y vistas.
│   │   ├── MySqlSupplierRepository.cs            # Consulta, inserción, edición y borrado lógico (estaActivo = 0) para PROVEEDOR.
│   │   └── MySqlCategoryRepository.cs            # Ejecución de SQL parametrizado para la tabla CATEGORIA.
│   │
│   └── Web/                                      # Componentes y middleware de la capa web
│       └── DecimalModelBinder.cs                 # Binder personalizado para aceptar decimales con coma (,) o punto (.).
│
└── Pages/                                        # CAPA DE PRESENTACIÓN: Interfaz de usuario estructurada con Razor Pages.
    │
    ├── Products/                                 # Interfaz gráfica para el CRUD de Productos
    │   ├── Index.cshtml                          # Vista en tabla HTML con el listado de productos y control de stock.
    │   ├── Index.cshtml.cs                       # Code-behind que llama a IProductService.GetAllAsync() para poblar la vista.
    │   ├── Create.cshtml                         # Vista contenedora del formulario de registro de productos.
    │   ├── Create.cshtml.cs                      # Code-behind que recibe y procesa el POST de creación de productos.
    │   ├── Edit.cshtml                           # Vista contenedora para modificar un producto existente.
    │   ├── Edit.cshtml.cs                        # Code-behind que obtiene los datos previos y procesa la actualización.
    │   ├── Delete.cshtml                         # Vista de confirmación para eliminar un producto.
    │   ├── Delete.cshtml.cs                      # Code-behind que invoca la eliminación lógica (Soft Delete) del producto.
    │   ├── ProductFormModel.cs                   # PageModel base que centraliza la conversión y parseo de entradas monetarias.
    │   └── _ProductForm.cshtml                   # Formulario HTML parcial reutilizado en Create y Edit (Aplica DRY).
    │
    ├── Suppliers/                                # Interfaz gráfica para el CRUD de Proveedores
    │   ├── Index.cshtml                          # Vista de listado de proveedores registrados y activos.
    │   ├── Index.cshtml.cs                       # Code-behind que solicita la lista de proveedores a ISupplierService.
    │   ├── Create.cshtml                         # Vista de registro de nuevo proveedor.
    │   ├── Create.cshtml.cs                      # Code-behind que procesa los datos del nuevo proveedor enviado por el cliente.
    │   ├── Edit.cshtml                           # Vista de edición de información del proveedor.
    │   ├── Edit.cshtml.cs                        # Code-behind para cargar datos de un proveedor y enviar sus cambios.
    │   ├── Delete.cshtml                         # Confirmación visual para dar de baja a un proveedor.
    │   ├── Delete.cshtml.cs                      # Code-behind que actualiza el estado del proveedor a inactivo.
    │   ├── SupplierFormModel.cs                  # PageModel base para precargar controles e inputs comunes de proveedores.
    │   └── _SupplierForm.cshtml                  # Formulario HTML parcial compartido entre la creación y edición de proveedores.
    │
    ├── Categories/                               # Interfaz gráfica para el CRUD de Categorías
    │   ├── Index.cshtml                          # Tabla HTML para mostrar todas las categorías registradas.
    │   ├── Index.cshtml.cs                       # Code-behind que obtiene el listado mediante ICategoryService.
    │   ├── Create.cshtml                         # Vista para el registro de una nueva categoría.
    │   ├── Create.cshtml.cs                      # Code-behind que captura la información enviada para crear una categoría.
    │   ├── Edit.cshtml                           # Vista de edición de una categoría existente.
    │   ├── Edit.cshtml.cs                        # Code-behind para actualizar la categoría seleccionada.
    │   ├── Delete.cshtml                         # Confirmación visual para dar de baja una categoría.
    │   ├── Delete.cshtml.cs                      # Code-behind que ejecuta la baja lógica de la categoría.
    │   ├── CategoryFormModel.cs                  # PageModel base con propiedades y métodos compartidos para categorías.
    │   └── _CategoryForm.cshtml                  # Formulario HTML parcial para creación y actualización de categorías.
    │
    └── Shared/                                   # Componentes globales de la interfaz
        ├── _Layout.cshtml                        # Maqueta principal HTML con la barra de navegación hacia Productos, Proveedores y Categorías.
        └── _ValidationScriptsPartial.cshtml      # Carga jQuery Validate con reglas adaptadas a la cultura local para separadores decimales.