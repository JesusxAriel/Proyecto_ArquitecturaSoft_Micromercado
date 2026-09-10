*   **El módulo Copy:** Un módulo `Copy()` lee directamente del teclado con `ReadKeyboard()` y escribe a la impresora con `WritePrinter()`. No puede reusarse para leer/escribir en archivos.  
    * *Solución:* Hacer que `Copy` dependa de las abstracciones `Reader` y `Writer`.
*   **Problema del Botón y la Lámpara (`Button` / `Lamp`):** Un botón depende directamente de una clase concreta `Lamp` mediante invocaciones en `Poll()`.  
    * *Solución:* Interfaz `ButtonServer` o `ISwitchableDevice` implementada por `Lamp`.

---

## NIVEL 3: IMPORTANCIA MEDIA (Detalles teóricos, arquitectura y patrones)

### 1. Patrones de Diseño Asociados
*   **Template Method (GoF):** Alternativa clásica para lograr OCP mediante clases base que definen el esqueleto del algoritmo y subclases que extienden pasos específicos.
*   **Adapter (GoF):** Herramienta clave para ISP al mediar entre interfaces no compatibles sin contaminar la abstracción base.
*   **Inyección de Dependencias:** Mecanismo indispensable para DIP donde las abstracciones son suministradas externamente (p. ej., a través de constructores).

### 2. Concepto de "Diseño por Contrato"
*   Definido por Bertrand Meyer. Los métodos establecen **pre-condiciones** y **post-condiciones**.
*   *Regla de Subtipado:* Al redefinir un método en una clase derivada, **solo se puede debilitar la pre-condición y fortalecer la post-condición**.

### 3. Layering (Arquitectura en Capas) e Inversión de Dependencias
*   *Enfoque tradicional:* La capa de Políticas (*Policy Layer*) depende de la capa de Mecanismos (*Mechanism Layer*), y esta de Utilidades (*Utility Layer*).
*   *Enfoque con DIP:* La capa de Políticas define sus propias interfaces. Las capas inferiores (*Mechanism* y *Utility*) implementan dichas interfaces, invirtiendo la dirección habitual de las dependencias.

---

## NIVEL 4: BUENAS PRÁCTICAS Y CONSEJOS ÚTILES

Recomendaciones prácticas de implementación que facilitan el cumplimiento diario de estos principios:

1.  **Regla de Oro de SOLID:** Los principios SOLID son **guías**, no reglas inamovibles. Aplica el sentido común y pregúntate constantemente: *"¿Por qué tomo esta decisión arquitectónica?"*.
2.  **No aplicar sin síntomas:** No es prudente forzar un principio SOLID si no existe un problema o síntoma claro en el software. Aplicarlos prematuramente genera complejidad innecesaria.
3.  **Comentarios descriptivos previas:** Un excelente método para validar SRP es escribir el resumen en XML/Docstring de la clase antes de programar. Si la descripción usa "Y" o abarca múltiples tareas (`"Obtiene, guarda y envía órdenes"`), la clase necesita dividirse.
4.  **Métodos pequeños y expresivos:** Diseña métodos estructurados con verbos y sustantivos claros. Un método debe realizar una sola tarea.
5.  **Evitar Transaction Scripts Generalistas:** En lugar de clases monolíticas con múltiples operaciones sobre una entidad, crea servicios con un propósito único (ej. `GetOrderService`, `SaveOrderService`, `SubmitOrderService`).
6.  **Uso de Expresiones Lambda para Extensibilidad:** Emplea expresiones lambda/delegados para modificar comportamientos en tiempo de ejecución sin alterar el código interno (ejemplo de Fluent NHibernate mediante `WithConvention`).
7.  **Evitar el "Pegado" Directo:** No soldes o acoples componentes de alto nivel a detalles hardware o de infraestructura.