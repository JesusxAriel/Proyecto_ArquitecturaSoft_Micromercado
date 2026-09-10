# LISTA DE COMPROBACIÓN Y GUÍA DE REQUISITOS DEL PROYECTO
## Aplicación de Clean Code, Principios SOLID y Buenas Prácticas en Razor Pages (.NET / C#)

---

## 1. Estructura General y Repositorio de Código

- [ ] **Repositorio en Control de Versiones (GitHub o GitLab):**
  - [ ] Repositorio creado y público o privado con acceso otorgado al docente.
  - [ ] **Organización de Ramas:**
    - [ ] `main` / `master`: Código estable y listo para producción.
    - [ ] `develop`: Rama de integración para desarrollo continuo.
    - [ ] `feature/*`: Ramas específicas por funcionalidad (ej. `feature/crud-cliente`, `feature/historico`).
  - [ ] **Historial de Commits:**
    - [ ] Mensajes de commit claros, descriptivos y bajo convenciones (ej. Conventional Commits: `feat:`, `fix:`, `docs:`).
    - [ ] Frecuencia constante de aportes de los integrantes del equipo.
  - [ ] **Documentación en Repositorio:**
    - [ ] Archivo `README.md` explicativo con instrucciones de configuración, ejecución del proyecto y estructura del repositorio.

---

## 2. Requisitos Interfaz de Usuario y Frontend (Razor Pages)

- [ ] **Personalización Visual y Branding:**
  - [ ] **No usar la plantilla por defecto de Razor Pages** (bootstrap básico / layout por defecto de Visual Studio).
  - [ ] Implementación de una línea gráfica personalizada y paleta de colores coherente con la temática del proyecto.
  - [ ] Incorporación del **Logo oficial** del sistema/proyecto en la barra de navegación o encabezado.
- [ ] **Ventana Home y Menú de Opciones (Puntuación: 6 pts):**
  - [ ] Página de bienvenida (Home) estructurada y funcional.
  - [ ] Menú de navegación principal con múltiples opciones coherentes con las características de la lógica del negocio.

---

## 3. Base de Datos y Modelo de Datos

- [ ] **Tablas Principales (3 Tablas Solicitadas):**
  - [ ] **Tabla 1:** Definida con al menos 4 atributos independientes (sin contar PK, FKs ni campos de auditoría).
  - [ ] **Tabla 2:** Definida con al menos 4 atributos independientes (sin contar PK, FKs ni campos de auditoría).
  - [ ] **Tabla 3:** Definida con al menos 4 atributos independientes (sin contar PK, FKs ni campos de auditoría).
  - *Nota sobre Atributos Validados:*
    - Excluidos del conteo: Primary Keys (PK), Foreign Keys (FK), atributos de auditoría (`FechaCreacion`, `UsuarioCreacion`, `FechaModificacion`, `EstadoLogico`).
- [ ] **Gestión de Datos Históricos (Puntuación: 7 pts):**
  - [ ] Implementación del control de datos históricos en **al menos 1 de las 3 tablas**.
  - [ ] Modelo de base de datos diseñado para escalar y almacenar cambios futuros (ej. tabla de auditoría/historial con `ID`, `ID_Entidad`, `ValorAnterior`, `ValorNuevo`, `FechaCambio`, `TipoOperacion`, `Usuario`).
  - [ ] **Vista en Interfaz de Usuario:** Creación de una pantalla/vista en Razor Pages que muestre el historial completo de cambios registrados.

---

## 4. Requisitos de Lógica y Operaciones CRUD completos (36 pts - 12 pts por tabla)

Para cada una de las 3 tablas asignadas:

- [ ] **Operación SELECT (3 pts por tabla):**
  - [ ] Listado de registros ordenado lógicamente.
  - [ ] Refresco automático/correcto de la información al realizar cambios.
- [ ] **Operación INSERT (4 pts por tabla):**
  - [ ] Formulario de creación funcional alineado a la lógica del negocio.
- [ ] **Operación UPDATE (4 pts por tabla):**
  - [ ] Formulario de edición con precarga correcta de datos.
- [ ] **Operación DELETE (1 pt por tabla):**
  - [ ] Eliminación física o borrado lógico según reglas de negocio.

---

## 5. Validaciones de Entrada de Datos (Puntuación: 18 pts - 6 pts por tabla)

- [ ] **Validación de Datos por Tabla:**
  - [ ] Validaciones en capa cliente (HTML5 / jQuery Unobtrusive) y capa servidor (C# Data Annotations / Custom Logic).
  - [ ] Basadas estrictamente en la **Lógica del Negocio** (ej. rangos numéricos, formato de emails, RUT/CI válidos, fechas coherentes, duplicados).
  - [ ] Mensajes de error claros, amigables y visibles para el usuario final en la interfaz.

---

## 6. Arquitectura, Clean Code y Principios SOLID

- [ ] **Acceso a Datos (ADO.NET):**
  - [ ] Uso exclusivo de ADO.NET (`SqlConnection`, `SqlCommand`, `SqlDataReader`, etc.) sin frameworks ORM pesados (Entity Framework) para el acceso a datos según el requerimiento.
  - [ ] Manejo correcto de parámetros SQL (`SqlParameter`) para prevenir inyecciones SQL.
- [ ] **Clean Code (Código Limpio):**
  - [ ] **Nombres Significativos:** Clases, métodos y variables expresivos que revelan su intención.
  - [ ] **Funciones Pequeñas:** Métodos con una sola responsabilidad clara y pocas líneas de código.
  - [ ] **Formato Consistente:** Uso correcto de identación y convenciones de nombres en C# (PascalCase, camelCase).
  - [ ] **Manejo de Excepciones:** Control limpio de errores sin bloques try-catch vacíos o código duplicado.
- [ ] **Principios SOLID Aplicados:**
  - [ ] **S - Single Responsibility Principle (SRP):** Separación clara de clases (Controllers/PageModels, Services, Repositories, Models).
  - [ ] **O - Open/Closed Principle (OCP):** Extensiones mediante interfaces o clases abstractas sin modificar código existente.
  - [ ] **L - Liskov Substitution Principle (LSP):** Uso adecuado de herencia e interfaces intercambiables.
  - [ ] **I - Interface Segregation Principle (ISP):** Interfaces pequeñas y específicas en lugar de interfaces monolíticas.
  - [ ] **D - Dependency Inversion Principle (DIP):** Inyección de dependencias en las Razor Pages/Services para desacoplar el acceso a datos.

---

## 7. Entregables: Informe en Formato PDF (Puntuación: 10 pts)

El informe debe ser entregado en formato PDF de manera individual por cada estudiante (mismo documento elaborado en grupo) en el LMS - UCB conteniendo:

- [ ] **Carátula:** Datos de la materia, universidad (UCB), integrantes y fecha.
- [ ] **1. Introducción:**
  - [ ] Breve explicación de los principios SOLID.
  - [ ] Objetivo del trabajo práctico.
- [ ] **2. Descripción del Proyecto:**
  - [ ] Contexto y problemática a resolver.
  - [ ] Requisitos funcionales mínimos detallados.
- [ ] **3. Diseño e Implementación:**
  - [ ] Diagramas de Clases del sistema.
  - [ ] Justificación de las decisiones de diseño tomadas.
  - [ ] Ejemplos concretos de código donde se aplique **cada uno de los 5 principios SOLID** con su respectiva justificación.
- [ ] **4. Repositorio de Control de Versiones:**
  - [ ] URL directa al repositorio de GitHub/GitLab.
  - [ ] Explicación de la organización del repositorio (estructura de ramas, flujo de commits).
- [ ] **5. Conclusiones:**
  - [ ] Aprendizajes del grupo sobre el uso de SOLID y Clean Code.
  - [ ] Dificultades encontradas durante el desarrollo.
  - [ ] Recomendaciones para futuros trabajos.
- [ ] **6. Bibliografía / Fuentes Consultadas:**
  - [ ] Referencias bibliográficas en formato estándar.

---

## 8. Exposición y Fechas de Evaluación

- [ ] **Fecha de Entrega e Exposición Grupal:** `01/09/2026` (Impostergable).
- [ ] **Defensa Individual (18 pts):** Respuestas claras sobre el código implementado y los conceptos teóricos/prácticos de SOLID y Clean Code.
- [ ] **Presentación de Feedback (5 pts):** Exposición al curso realizando una retroalimentación de la evaluación del proyecto.