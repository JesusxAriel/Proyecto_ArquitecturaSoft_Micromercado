# Cliente tipado de consultas de Ventas

IVentasApiClient/VentasApiClient consume exclusivamente GET de productos, precio, stock y clientes en http://localhost:5002/. Los handlers Razor validan filtros y traducen errores HTTP a mensajes seguros.

El JWT se obtiene de la sesión individual del servidor y se agrega por petición; nunca se entrega a HTML/JavaScript. La sesión local solo se habilita en Development. No hay referencias a Domain, Application, Infrastructure, Api ni al monolito.

Registro de ventas, clientes y anulación siguen bloqueados. Detalles y comandos: [informe de consultas reales](../../../docs/FRONTEND_CONSULTAS_REALES.md).
