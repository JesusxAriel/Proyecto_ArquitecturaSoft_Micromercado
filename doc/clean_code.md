# RESUMEN COMPLETO Y DETALLADO DE "CLEAN CODE" (ROBERT C. MARTIN)

Este documento organiza exhaustivamente los principios, patrones, prácticas, técnicas de refactorización y heurísticas del libro **Clean Code: A Handbook of Agile Software Craftsmanship** priorizados explícitamente en cuatro niveles de importancia:

1. **NIVEL CRÍTICO (SÍ O SÍ / IMPRESCINDIBLE)**: Reglas de oro, principios fundamentales de arquitectura de código y prácticas cuya violación arruina el software.
2. **NIVEL IMPORTANTE (ALTA PRIORIDAD)**: Directrices para diseño de funciones, clases, pruebas y manejo de errores que elevan drásticamente la calidad y mantenibilidad.
3. **NIVEL MEDIO (BUENAS PRÁCTICAS Y DETALLES)**: Convenciones de formato, reglas de nombres específicas, detalles de concurrencia y heurísticas complementarias.
4. **NIVEL COMPLEMENTARIO Y AVANZADO (ÚTIL Y DE NICHO)**: Guías de refactorización paso a paso, integración de librerías de terceros, arquitectura de sistemas, patrones de prueba y catálogo extendido de smells (olores de código).

---

# PARTE 1: NIVEL CRÍTICO (SÍ O SÍ / IMPRESCINDIBLE)

## 1. Reglas de Oro Fundamentales
- **Ley de LeBlanc**: *"Later equals never"* (Luego es nunca). Dejar código sucio para "limpiarlo después" es una trampa mortal; la deuda técnica acumulada destruye la productividad del equipo gradualmente hasta llevarla a cero.
- **La Regla del Boy Scout**: *"Deja el campamento más limpio de como lo encontraste"*. Cada vez que hagas un *check-in* o toques un archivo, realiza una pequeña mejora (renombra una variable, extrae una función pequeña, elimina código duplicado). Con el tiempo, la base de código mejora sola.
- **Responsabilidad Profesional**: Es obligación del programador defender la calidad del código frente a presiones de tiempo no realistas. Así como un médico no omite el lavado de manos prequirúrgico por prisa del paciente, un desarrollador no debe entregar código sucio por presión de fechas límite.

## 2. Principios SOLID en el Código
- **SRP (Single Responsibility Principle - Principio de Responsabilidad Única)**:
  - Una clase o módulo debe tener **una y solo una razón para cambiar**.
  - Separa responsabilidades técnicas de negocio (ej. lógica de cálculo separada del renderizado/formato de salida).
- **OCP (Open/Closed Principle - Principio Abierto/Cerrado)**:
  - El software debe estar **abierto para la extensión, pero cerrado para la modificación**.
  - Usa polimorfismo e interfaces para agregar nuevas funcionalidades sin modificar código existente probado.
- **DIP (Dependency Inversion Principle - Principio de Inversión de Dependencias)**:
  - Módulos de alto nivel no deben depender de módulos de bajo nivel; ambos deben depender de abstracciones (interfaces).

## 3. Duplicación y Diseño Emergente (Las 4 Reglas del Diseño Simple de Kent Beck)
1. **Ejecuta todos los tests**: El código sin pruebas no es limpio y no se puede refactorizar con seguridad.
2. **Sin duplicación (DRY - Don't Repeat Yourself)**: La duplicación es el enemigo #1 de un sistema mantenible. Ocurre cuando un mismo concepto o algoritmo se repite en múltiples lugares.
3. **Expresa la intención del desarrollador**: El código debe leerse como prosa clara y directa; nombres descriptivos y funciones enfocadas.
4. **Minimiza clases y métodos**: Mantener el sistema pequeño y ligero sin sobre-diseñar abstracciones innecesarias.

## 4. Pruebas Unitarias Limpias (TDD y F.I.R.S.T.)
- **Las 3 Leyes del TDD (Test-Driven Development)**:
  1. No escribirás código de producción sin antes escribir un test unitario que falle.
  2. No escribirás más de un test unitario que sea suficiente para fallar (y los errores de compilación son fallos).
  3. No escribirás más código de producción que el necesario para hacer pasar el test que falla.
- **Criterios F.I.R.S.T. para Tests**:
  - **F (Fast - Rápidos)**: Se deben ejecutar velozmente para correr miles de tests en segundos.
  - **I (Independent - Independientes)**: Un test no debe depender del resultado de otro.
  - **R (Repeatable - Repetibles)**: Deben ejecutarse en cualquier entorno (desarrollo, CI/CD, sin red).
  - **S (Self-Validating - Autovalidables)**: Deben retornar un booleano (pasa o falla), sin requerir inspección manual de logs.
  - **T (Timely - Oportunos)**: Se escriben justo antes del código de producción.

---

# PARTE 2: NIVEL IMPORTANTE (ALTA PRIORIDAD)

## 1. Funciones
- **Pequeñas (Small!)**: Las funciones deben ser ridículamente pequeñas (idealmente de 2 a 10 líneas, raras veces más de 20).
- **Hacen una sola cosa (Do One Thing)**: Una función debe hacer una sola cosa, hacerlo bien y hacerlo únicamente. Si puedes extraer otra función independiente de ella, la función original hacía más de una cosa.
- **Un solo nivel de abstracción por función**: Todas las sentencias dentro de una función deben estar en el mismo nivel de abstracción.
- **La Regla del Escalón (The Stepdown Rule)**: El código debe leerse de arriba a abajo como una narrativa; cada función debe estar seguida por las funciones del siguiente nivel de abstracción.
- **Argumentos de Funciones**:
  - **Ideal**: 0 argumentos (niládica).
  - **Aceptable**: 1 argumento (monádica) o 2 (diádica).
  - **Evitar en lo posible**: 3 argumentos (triádica). Requiere justificación muy fuerte.
  - **Prohibido**: Más de 3 argumentos (poliádica). Agrupa los argumentos en un objeto con nombre propio.
  - **Argumentos Flag (Booleanos)**: Evitarlos por completo. Pasar un booleano a una función indica explícitamente que la función hace dos cosas diferentes (si es true hace X, si es false hace Y).
- **Sin efectos secundarios (No Side Effects)**: La función no debe alterar variables globales ni estados ocultos de la clase que no formen parte de su nombre.
- **Separación de Comando y Consulta (Command Query Separation - CQS)**:
  - Una función debe **hacer algo** (comando) O **responder algo** (consulta), pero **NUNCA ambas cosas**.

## 2. Manejo de Errores
- **Preferir Excepciones sobre Códigos de Error**: Los códigos de retorno obligan al llamador a procesar errores inmediatamente con estructuras `if/else` anidadas que ensucian la lógica principal.
- **Usar Excepciones No Chequeadas (Unchecked Exceptions)**: Las excepciones chequeadas (`checked exceptions` en Java) violan el principio Abierto/Cerrado, ya que modificar una firma de método fuerza a cambiar todos los métodos superiores en la cadena de llamadas.
- **Extraer bloques Try/Catch**: El manejo de errores es "una sola cosa". Separa la lógica de `try/catch` del algoritmo principal extrayéndola en una función dedicada.
- **No retornar Null**: Retornar `null` fuerza a llenar el código de validaciones `if (item == null)` repetitivas. Retorna colecciones vacías o utiliza el patrón *Special Case Object* / *Null Object*.
- **No pasar Null**: Pasar `null` como argumento a métodos es una mala práctica por defecto que genera fallos inesperados (`NullPointerException`).

## 3. Clases y Estructuras de Datos
- **Antisimetría de Objetos y Estructuras de Datos**:
  - **Objetos**: Ocultan sus datos detrás de abstracciones y exponen funciones que operan sobre ellos. Facilita agregar nuevas clases sin cambiar funciones existentes.
  - **Estructuras de Datos / DTOs**: Exponen sus datos (campos) y no poseen comportamiento significativo. Facilita agregar nuevas funciones sin cambiar las estructuras de datos.
- **La Ley de Demeter**: Un módulo no debe conocer los entresijos de los objetos que manipula.
  - *"Habla solo con tus amigos cercanos, no con extraños"*.
  - Evita "choques de trenes" (*Train Wrecks*): `a.getB().getC().getD().doSomething()`.

## 4. Comentarios
- **No compenses código sucio con comentarios**: Expresa la intención directamente en el código refinando nombres y estructura.
- **Comentarios Buenos (Justificados)**:
  - Comentarios legales o de derechos de autor.
  - Explicación de intención tras una decisión de diseño no evidente.
  - Advertencia de consecuencias graves (ej. `// Test que tarda 2 horas en ejecutarse`).
  - Notas `// TODO` para tareas pendientes explícitas.
- **Comentarios Malos (Eliminar)**:
  - Comentarios redundantes que repiten lo que hace la línea de código.
  - Código comentado (elimínalo; para guardar historial existe Git/SVN).
  - Marcadores de posición o separadores de sección en archivos.
  - Comentarios de diario o historial de modificaciones en el encabezado.

---

# PARTE 3: NIVEL MEDIO (BUENAS PRÁCTICAS Y DETALLES)

## 1. Nombres Significativos
- **Nombres que revelen la intención**: El nombre debe responder por qué existe, qué hace y cómo se usa.
  - *Mal*: `int d;`
  - *Bien*: `int elapsedTimeInDays;`
- **Evitar Desinformación**: No usar nombres engañosos como `accountList` si el contenedor no es una lista real.
- **Hacer distinciones significativas**: Evitar nombres como `data1`, `data2`, `ProductInfo` vs `ProductData`.
- **Usar nombres pronunciables y buscables**: Evitar abreviaturas crípticas (`genymdhms`) e indicar constantes globales con nombres largos para ser buscados (`WORK_DAYS_PER_WEEK` en vez de `5`).
- **Evitar codificaciones**: No usar notación húngara ni prefijos de miembros de clase como `m_`.
- **Nombres de Clases y Métodos**:
  - Clases: Sustantivos o frases sustantivas (`Customer`, `WikiPage`, `AccountParser`). Evitar `Manager`, `Processor`, `Data`.
  - Métodos: Verbos o frases verbales (`postPayment`, `deletePage`, `save`).

## 2. Formato del Código
- **Metáfora del Periódico**: El archivo fuente debe leerse como un artículo de periódico: arriba el título y resumen de alto nivel, bajando gradualmente a los detalles de bajo nivel.
- **Apertura y Densidad Vertical**:
  - Separar conceptos (métodos, imports) con líneas en blanco.
  - Mantener juntas las líneas de código estrechamente asociadas.
- **Distancia Vertical**: Las variables locales deben declararse cerca de su primer uso. Las variables de instancia deben declararse arriba en la clase.
- **Líneas Cortas**: Mantener el ancho de línea dentro de límites razonables (máximo 80-120 caracteres).

## 3. Concurrencia (Principios Generales)
- La concurrencia es una estrategia de desacoplamiento (desacopla qué se hace de cuándo se hace).
- **Principios de defensa de concurrencia**:
  - Aplicar SRP: Mantener el código concurrente separado del código de producción general.
  - Limitar el alcance de datos compartidos (`encapsulación`) o preferir copias de datos inmutables.
  - Hacer los hilos tan independientes como sea posible (que no compartan estado).

---

# PARTE 4: NIVEL COMPLEMENTARIO Y AVANZADO (ÚTIL Y DE NICHO)

## 1. Límites y Código de Terceros (Capítulo 8)
- **Aislamiento de API Externas**: No propagues los tipos u objetos de librerías de terceros por todo tu código. Envuélvelos (*Adapter Pattern*) para proteger tu código si la API cambia o es reemplazada.
- **Learning Tests (Pruebas de Aprendizaje)**: Escribe pruebas unitarias pequeñas para explorar y entender cómo funciona una librería de terceros antes de integrarla en producción. Esto verifica que la librería funcione como esperas y detecta roturas cuando se actualiza la versión de la librería.
- **Diseño para Interfaces Inexistentes**: Si necesitas integrar una funcionalidad de un equipo o proveedor que aún no la ha desarrollado, define tu propia interfaz ideal. Cuando ellos terminen, usa un adaptador para conectar su código con tu interfaz.

## 2. Arquitectura de Sistemas y Escalabilidad (Capítulo 11)
- **Separar la Construcción del Uso**:
  - Un sistema debe separar el proceso de inicio/construcción (creación de objetos y cableado de dependencias) de la lógica de ejecución en tiempo de corrida.
  - Usar la función `main` únicamente para construir el grafo de objetos del sistema o delegar esto a contenedores de **Inyección de Dependencias (DI / IoC)**.
- **Crecimiento Orgánico (Scaling Up)**:
  - Es un mito que los sistemas deben diseñarse "bien desde el primer día" con abstracciones masivas e hiper-complejas. Las arquitecturas de software deben crecer incrementalmente mediante la separación limpia de incumbencias (*Cross-Cutting Concerns* usando AOP/Aspectos o Proxies).
- **Domain-Specific Languages (DSL)**: El uso de lenguajes específicos de dominio o APIs fluidas reduce la brecha entre el concepto del negocio y el código implementado.

## 3. Tácticas Avanzadas de Concurrencia (Capítulo 13 y Apéndice A)
- **Modelos de Ejecución Clásicos**:
  - **Productor-Consumidor**: Una o más tareas crean trabajo y lo colocan en una cola; una o más tareas consumen el trabajo de la cola. Requiere señalización para evitar esperas activas.
  - **Lectores-Escritores**: Optimizar el rendimiento permitiendo lecturas concurrentes sin bloqueo, pero bloqueando el acceso exclusivo durante escrituras.
  - **Cena de los Filósofos**: Problema clásico de inanición (*starvation*) y interbloqueo (*deadlock*) cuando múltiples hilos compiten por recursos finitos.
- **Condiciones para un Interbloqueo (Deadlock)**: Para que ocurra un deadlock se deben cumplir 4 condiciones simultáneamente. Romper cualquiera de ellas evita el interbloqueo:
  1. *Exclusión Mutua*: Múltiples hilos necesitan acceso exclusivo al mismo recurso.
  2. *Bloqueo y Espera*: Un hilo sostiene un recurso mientras espera otro.
  3. *Sin Apropiación (No Preemption)*: Un recurso no puede ser quitado a la fuerza a un hilo.
  4. *Espera Circular*: El hilo A espera al hilo B, que espera al hilo A.
- **Estrategias de Pruebas Concurrentes**:
  - Haz tu código concurrente ejecutable con número configurable de hilos.
  - Ejecuta las pruebas en plataformas diferentes y con más hilos que procesadores reales.
  - Introduce código de instrumentación artificial (`Thread.sleep()`, `Thread.yield()`) de manera automatizada para forzar condiciones de carrera (*race conditions*).

## 4. El Proceso de Refactorización Incremental (Capítulos 14, 15 y 16)
- **Primer Principio de la Refactorización**: Para refactorizar sin romper nada, **debes tener un conjunto completo de pruebas unitarias pasando en verde**.
- **Avance a Pasos de Bebé (Incrementalism)**:
  - Nunca intentes reescribir un módulo entero de un solo golpe.
  - Haz cambios minúsculos: extrae una variable, ejecuta los tests; extrae una función, ejecuta los tests; cambia un nombre, ejecuta los tests.
  - Si los tests fallan, revierte inmediatamente al último estado verde conocido en lugar de pasar horas depurando la refactorización fallida.
- **Limpieza en Cascada**: Refactorizar una función pequeña frecuentemente revela la oportunidad de simplificar la clase que la contiene, lo cual a su vez permite dividir el paquete o módulo.

## 5. Catálogo Extendido de Smells (Olores de Código) e Heurísticas (Capítulo 17)

### Entorno y Build
- **E1: El build requiere más de un paso**: Compilar e implementar un proyecto debe lograrse con un solo comando en la terminal (`mvn clean install`, `npm run build`).
- **E2: Los tests requieren más de un paso**: Toda la suite de pruebas unitarias debe ejecutarse con un solo comando sencillo.

### Funciones
- **F1: Demasiados argumentos**: Más de tres argumentos es una señal clara de rediseño.
- **F2: Argumentos de salida**: Los argumentos deben ser entradas, no salidas. Si una función va a cambiar el estado de algo, debe cambiar el estado de su objeto dueño (`report.appendFooter()` en vez de `appendFooter(report)`).
- **F3: Argumentos de bandera (Flag Arguments)**: Indican que la función hace más de una cosa. Deben eliminarse dividiendo la función en dos.
- **F4: Función muerta**: Métodos que nunca son invocados por nadie deben borrarse sin piedad.

### General
- **G1: Múltiples lenguajes en un solo archivo**: Evitar mezclar HTML, JavaScript, SQL y CSS dentro del mismo archivo de código fuente.
- **G2: Comportamiento obvio no implementado**: Si un desarrollador espera intuitivamente que un método haga X según su nombre y convención, debe hacerlo (*Principio de la Menor Sorpresa*).
- **G3: Comportamiento incorrecto en los límites**: Nunca asumas que tu código funciona sin probar los casos de esquina (0, null, cadenas vacías, valores límite).
- **G4: Medidas de seguridad anuladas**: Desactivar advertencias del compilador o tests que fallan es una receta para el desastre.
- **G6: Código en el nivel incorrecto de abstracción**: No mezcles conceptos de alto nivel con detalles de bajo nivel en el mismo módulo.
- **G14: Envidia de Características (Feature Envy)**: Ocurre cuando un método de una clase usa intensivamente los métodos y datos de otra clase. El método probablemente pertenece a la otra clase.
- **G18: Estáticos Inapropiados**: Prefeir métodos de instancia. Si un método usa polimorfismo o pretende ser sobreescrito, jamás debe ser `static`.
- **G25: Reemplazar números mágicos por constantes nombradas**: Evita literales crudos en el código (`86400` -> `SECONDS_PER_DAY`).
- **G28: Encapsular condicionales**: Extrae expresiones booleanas complejas a métodos con nombres descriptivos (`if (shouldBeDeleted(timer))` en lugar de `if (timer.hasExpired() && !timer.isRecurrent())`).
- **G29: Evitar condicionales negativas**: `if (buffer.isFull())` es mucho más fácil de entender mentalmente que `if (!buffer.isNotFull())`.

### Pruebas Unitarias
- **T1: Tests insuficientes**: Una suite de tests que no prueba todas las condiciones límite o rutas de código es una falsa sensación de seguridad.
- **T2: Usar herramientas de cobertura**: Monitorea el *code coverage*, pero enfócate en cubrir la lógica crítica y no solo líneas de código triviales.
- **T3: No omitir tests triviales**: Su valor de documentación es alto y son sencillos de escribir.
- **T6: Probar exhaustivamente cerca de los errores**: Los errores suelen agruparse. Si encuentras un bug en una función, escribe múltiples tests alrededor de esa área porque suele haber más.