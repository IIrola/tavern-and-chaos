# Revisión de iteraciones con criterio de juego cooperativo

## 0.3.0 — sala autoritativa

**Decisión de alcance:** implementar la base que todos los juegos compartirán, conservando los 85 criterios completos. La demo 0.2 no se reclasifica como proyecto terminado. Se usaron router, Unity C#, físicas, UX y pipeline de compilación para esta entrega; no hay una skill instalada con el nombre exacto `game creator`.

**Criterio de aceptación de esta iteración:** seis participantes pueden entrar, verse, mover sus personajes, cargar/golpear, caer, recuperarse, agarrar un objeto y salir/volver; el séptimo recibe un rechazo legible y la pérdida del anfitrión devuelve un menú utilizable. La pausa de una persona no debe congelar a todas.

**Resultado observado:** 67 comprobaciones de sala/red aprobadas en procesos separados y 63 de regresión local; cero errores en el recorrido. Se inspeccionaron las capturas reales del lobby, seis participantes y menú de votación. La compilación pasó sin advertencias.

**Problemas encontrados y corregidos:**

- Compatibilidad de paquetes: NGO 2.7 utilizaba conversiones ya rechazadas por el editor; se fijó 2.13.3 y se compiló.
- Crédito al volver: el endpoint incluía puerto efímero, permitiendo otra concesión; ahora se identifica por bytes de IP.
- Pausa: la simulación compartida mantiene su reloj aunque la entrada de una persona esté detenida.
- Legibilidad: se replica la carga del golpe antes del impacto y se animan las réplicas al desplazarse.
- Justicia: abrir un menú o parar el movimiento no cancela la consecuencia de un golpe ya soltado; agarrar no interrumpe una carga para convertirla en otra acción.
- UX: la lista de votación se abre con cursor libre, no como botones inaccesibles sobre un juego que captura el ratón; se separaron filas del sexto jugador y el temporizador.
- Puntería local: se evita reescribir el estado del cursor cuando no ha cambiado, y la prueba verifica que la posición no se desplace sola.

**Juicio de la iteración:** fundación útil y comprobada para continuar, no corte vertical completo ni experiencia cooperativa final. Los actores aparecen próximos y la distribución inicial debe ganar más separación y selección de puntos libres; la respuesta local todavía depende del viaje al host, sin predicción. Falta medir latencia real, jitter y pérdida, completar el billar reglamentario en red y el contrato de asientos, probar economía/votos en acciones de varios clientes, sesiones humanas y compatibilidad Windows. No hay evidencia suficiente para afirmar diversión, rendimiento estable o ausencia general de bloqueos.

**Siguiente revisión prioritaria:** contrato común de mesas y billar bola 8, con reglas elegidas y diferencias del GDD visibles. Los demás cinco juegos, coctelería completa, economía y accesibilidad siguen en el registro de requisitos; no se eliminan para declarar éxito.
