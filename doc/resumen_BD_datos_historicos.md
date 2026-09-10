# Resumen Ejecutivo y Estructurado: Seguimiento de Cambios de Datos y Caso Práctico en Modelo Entidad-Relación (MER)

---

## 1. NIVEL CRÍTICO: Conceptos Imprescindibles y Patrones de Diseño MER

### 1.1. Principio Fundamental del Seguimiento de Datos Históricos
* **Pérdida de Información por Actualización**: Cada vez que se actualiza un atributo o se transfiere/modifica una relación en un modelo de datos convencional, los valores anteriores se sobrescriben, provocando la pérdida irreversible del estado previo.
* **Solución Estructural**: Para mantener trazabilidad y auditoría, el Modelo Entidad-Relación (MER) debe adaptar su esquema mediante entidades adicionales (de intersección o débiles) y atributos temporales (fechas de inicio/fin, marcas de tiempo).

---

### 1.2. Patrones de Modelado para el Control de Cambios

#### A. Cambio de Estado de Entidades
* **Mecanismo**: Se crean entidades débiles adicionales conectadas a la entidad principal.
* **Aplicación**: Permite almacenar el historial detallado de estados por los que ha pasado una entidad a lo largo del tiempo.
* **Ejemplo del Documento**:
  * `Contrato` (#id, *descripcion) 1 ---- (1,n) `Estado` (#id, *valorActual, *activo, *fechaActualizacion).

#### B. Cambio del Valor de un Atributo
* **Mecanismo**: Adición de entidades especificas con atributos de control para registrar modificaciones en valores numéricos o normativos.
* **Ejemplo del Documento**:
  * Control del monto de multas: `Multa` (#id, *fecha, *monto, *estado) conectada a `ModificacionMonto` (#id, *montoPrevio, *fechaModificacion, *estado, *modificadoPor).

#### C. Cambio de Relaciones
1. **Relaciones que No se Repiten (Historial Directo)**:
   * **Mecanismo**: Se utilizan entidades de intersección para registrar el cambio de una relación simple a lo largo del tiempo.
   * **Ejemplo**: Transición de cargos de un empleado. `Empleado` ---- `HistorialCargo` (*fechaInicio, o fechaFin, *estado) ---- `Cargo`.
2. **Relaciones que Pueden Repetirse**:
   * **Mecanismo**: Se crea una entidad explícita (p. ej. `Alquiler`) para modelar transacciones repetitivas entre dos entidades a lo largo del tiempo.
   * **Ejemplo**: `Persona` ---- `Alquiler` (#id, *fechaInicio, o fechaFin, *estado) ---- `Departamento`.

---

### 1.3. Reglas de Negocio Críticas del Caso Práctico (Ejercicio 9: Biblioteca Central)

1. **Gestión de Ejemplares y Estados**:
   * Cada libro posee ejemplares físicos identificados por un código único codificado por el sistema.
   * Los estados de preservación son: *Nuevo*, *Usado*, *Desgastado*, *Para restaurar*, *En Desuso*.
   * **Obligatorio**: Controlar el historial de cambios de estado de cada ejemplar.

2. **Gestión de Préstamos**:
   * Modos: Domicilio o Sala (un préstamo de Sala puede cambiar a Domicilio, lo cual debe ser registrado).
   * Límite: Máximo 3 libros por préstamo.
   * Devolución: Registro exacto de fecha y hora. La devolución completa de todos los libros es condición previa para dar por finalizado el préstamo.

3. **Políticas de Multas y Devoluciones Retrasadas**:
   * Multa automática por retraso en préstamos a Domicilio: **Bs. 10 por día de retraso**.
   * La multa se calcula y mantiene hasta la devolución total de los libros.
   * Multas adicionales: Daño al libro, reincidencia, etc.
   * **Historial de Tarifas**: Los costos base de las multas cambian con el tiempo; debe mantenerse un registro histórico de dichos ajustes.

4. **Facturación e Ingresos por Caja**:
   * Las multas se saldan en Caja mediante la emisión de una Factura.
   * Atributos obligatorios de la factura: Número, Fecha, Número de Autorización, Código de Control y Total.
   * Una factura puede agrupar el pago de múltiples multas.

5. **Rotación de Roles del Personal**:
   * Roles del personal: Bibliotecario, Cajero, Administrador.
   * Un empleado solo puede ejercer **un rol a la vez**, pero puede rotar entre roles.
   * **Obligatorio**: Control temporal explícito de la rotación de roles.

---

## 2. NIVEL IMPORTANTE: Arquitectura del Sistema y Requisitos Operativos

### 2.1. Gestión de Roles y Permisos de Usuarios
* **Lector**: Realiza búsquedas y solicita préstamos. Atributos: CI, Nombre, Apellidos, Edad, Dirección, Teléfono(s), Usuario, Contraseña.
* **Bibliotecario**: Registra préstamos, procesa devoluciones y gestiona altas/bajas de Lectores.
* **Cajero**: Procesa el cobro de multas y emite las facturas correspondientes.
* **Administrador**: Encargado de registrar a los Bibliotecarios y Cajeros dentro del sistema.

### 2.2. Gestión Catalográfica de Libros
* Atributos del Libro: Título, Autor(es) (Nombre y Nacionalidad), Edición, Año de publicación, Editorial (Nombre y País), ISBN, Cantidad de páginas, Resumen general.
* **Control de Ediciones**: Cada nueva edición de un libro se registra de forma independiente conservando la misma estructura de datos catalográficos.

---

## 3. NIVEL DE IMPORTANCIA MEDIA: Metodología de Evaluación y Preguntas Clave

Antes de diseñar mecanismos de seguimiento histórico en un MER, el diseñador debe validar los requisitos de negocio mediante cuatro preguntas fundamentales:

1. **¿Es necesaria una pista de auditoría?** (Verificar si el negocio requiere rastrear quién y cuándo modificó los registros).
2. **¿Pueden cambiar los valores de los atributos a lo largo del tiempo?** (Determinar qué campos específicos requieren guardar valores antiguos, ej. montos o salarios).
3. **¿Pueden cambiar las relaciones a lo largo del tiempo?** (Evaluar si el vínculo entre entidades es estático o evolutivo, ej. empleado-cargo o cliente-departamento).
4. **¿Es necesario consultar datos antiguos?** (Asegurar que la acumulación histórica aporte valor analítico o normativo).

> **Advertencia de Costo**: Almacenar datos históricos innecesarios incrementa significativamente el tamaño de la base de datos, la complejidad de las consultas y los costos de infraestructura.

---

## 4. BUENAS PRÁCTICAS Y OPTIMIZACIONES DE DISEÑO

### 4.1. Sobrescritura Controlada e Inclusión de Auditoría Básica
Para entidades de referencia o aquellas cuya información pasada no requiera trazabilidad estricta (donde los datos se sobrescriben), se recomienda como buena práctica incorporar atributos estándar de auditoría y borrado lógico:

* `estado / eliminado`: Indica la disponibilidad lógica del registro sin borrarlo físicamente.
* `fechaCreacion`: Timestamp de registro inicial.
* `ultimaModificacion`: Timestamp de la última actualización.
* `creadoModificadoPor`: Identificador del usuario que ejecutó la operación.

```
+------------------------------------+
| Persona                            |
+------------------------------------+
| # id                               |
| * ci                               |
| * nombres                          |
| * primerApellido                   |
| o segundoApellido                  |
| * estado/eliminado                 |
| * fechaCreacion                    |
| o ultimaModificacion               |
| * creadoModificadoPor              |
+------------------------------------+
```

### 4.2. Análisis de Tendencias e Innovación
* La conservación adecuada de datos históricos no solo cumple con auditorías legales, sino que sirve como base para el **análisis de tendencias, detección de patrones y mejora continua** de los procesos organizacionales.