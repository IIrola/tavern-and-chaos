# Preparación de LAN — trabajo pendiente

Esta rama conserva el inicio de la integración de Netcode for GameObjects. No es una versión validada para jugar: la demo local funcional está en `main`, versión 0.2.0.

Se añadió Netcode for GameObjects 2.7.0, pero su código depende de conversiones de `EntityId` que Unity 6000.6.8f1 ya rechaza al compilar. Antes de implementar la sala se debe seleccionar una versión oficial compatible con el editor, fijar sus dependencias y comprobar una compilación limpia. El archivo de bloqueo conserva la resolución de la demo local; Unity deberá resolver la dependencia experimental al abrir esta rama.

No se han implementado todavía autoridad del anfitrión, sincronización ni flujo de conexión. El alcance íntegro sigue siendo el del `GDD_y_plan_de_desarrollo.md`: seis jugadores, sala privada LAN, acciones validadas por el servidor, reglas y economía de mesas, y los seis juegos. Una importación de paquetes no demuestra que esos requisitos estén terminados.
