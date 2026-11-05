# Demo LAN integrada: preparación del Hito 1

Documento histórico de la versión 0.3.1 recibida de `main`. El desarrollo posterior de mesas y bola 8 se describe en [ESTADO_ACTUAL.md](ESTADO_ACTUAL.md).

La integración se mantiene en el alcance del Hito 1. En la primera prueba, el usuario dejó parciales la representación de jugadores, las botellas y el combate por los desfases del invitado y la ausencia de efectos de golpe. Tras las correcciones de la 0.3.1, confirmó que ya funcionan en su prueba con dos copias en un equipo. Las pruebas entre equipos siguen pendientes. No se han iniciado el contrato de mesas, las reglas de bola 8 ni el Hito 2. Los cambios por versión se registran en [CHANGELOG.md](../CHANGELOG.md).

## Base y decisiones

- Se incorporó la fundación de `origin/work/lan-foundation` (`351cb65`) al checkout actual, conservando los cambios locales. No se cambió de rama ni se hicieron commits.
- Unity 6000.6.8f1, Built-in, Input System 1.20.1, NGO 2.13.3 y dependencias fijadas en `packages-lock.json`.
- El usuario confirmó mantener el controlador con cápsula y caída física para este hito. El active ragdoll durante locomoción queda para una prueba independiente.
- La primera persona, el reinicio y las caídas por Laguna Azul pertenecen al código compartido de la demo local y LAN. Desde 0.3.1, las réplicas representan las posiciones y rotaciones de los seis cuerpos del anfitrión, tanto de pie como caídos. Solo el anfitrión simula las colisiones; el invitado interpola las posturas sin simulación física independiente.
- Protocolo de sala 3: todas las instancias deben usar la compilación 0.3.1. Cierra las dos ventanas antiguas antes de abrir otra vez el lanzador.

## Orden del plan

| Tarea | Integración |
|---|---|
| 0. Alcance/base | Controlador actual confirmado; integración limitada al pre-Hito 1. |
| 1. Reproducir base | Comprobaciones locales y compilación Windows; segunda máquina no disponible. |
| 2. Compilación segura | Compilar no recrea ni guarda la escena. La preparación explícita solo crea una escena ausente. |
| 3. Red | Dos procesos de prueba por loopback; conexión entre equipos y latencia/pérdida reales siguen pendientes. |
| 4. Personaje | Se conserva cápsula + ragdoll pasivo por decisión del usuario. |
| 5. Acciones | Reglas compartidas de alcance, orientación, obstáculos, selección y fuerza; un golpe soltado no se cancela pausando. |
| 6. Objetos/efectos/recuperación | Posesión exclusiva, liberación al caer/salir, bebida autoritativa y búsqueda de posiciones libres. |
| Hito 1 | Correcciones 0.3.1 confirmadas por el usuario con dos copias en un equipo; LAN entre equipos pendiente. |

## Compilar y abrir dos copias en una máquina Windows

1. En Unity, fuera de Play, abre `TavernChaos/Assets/Scenes/Tavern.unity` y guarda cualquier cambio propio pendiente.
2. Elige **Tavern → Build Windows LAN demo**. El ejecutable queda en `Builds/Windows/Tavern and Chaos.exe`. No uses una aplicación antigua de la release de macOS para esta prueba.
3. Ejecuta `Probar-LAN.cmd` desde la raíz del repositorio. Abre dos copias con registros separados; también puedes abrir el ejecutable dos veces manualmente.
4. En la primera copia: **Crear / unirse a sala LAN**, nombre `Anfitrión`, puerto `7777`, **Crear sala**.
5. En la segunda: mismo menú, nombre `Cliente`, IP **127.0.0.1**, puerto **7777**, **Entrar en sala**.
6. Comprueba que ambas muestran dos participantes. Alterna con Alt+Tab. Al perder foco se pausa la entrada local; vuelve con Esc o «Volver a la ronda». La simulación compartida continúa.

No necesitas cambiar de rama, abrir dos editores, configurar el router ni exponer puertos en Internet para esta prueba local. Para probar en dos equipos de la misma red, usa la misma compilación en ambos y el código que muestra el anfitrión; `127.0.0.1` solo sirve dentro de un mismo equipo.

## Recorrido manual de aceptación: Hito 1

Dedica al menos quince minutos y alterna las acciones entre anfitrión y cliente. Todavía no hay billar multijugador.

- [ ] Entrar y verse en posiciones separadas. Caminar/correr y mirar arriba, abajo y a los lados; cada cámara pertenece a su jugador.
- [ ] Mantener movimiento y pulsar **R**. Se recoloca solo ese jugador, con cámara e inercia reiniciadas. No se reinicia la sala. R no cancela una caída ni un golpe ya soltado.
- [ ] Acercarse, mirar una botella y pulsar **E**; cargar/soltar clic para lanzar y **Q** para soltar. No debe haber dos poseedores de la misma botella.
- [ ] Lanzar mirando hacia arriba/abajo y comprobar que la otra copia ve la trayectoria y la reposición.
- [ ] Golpear a Bruno y al otro jugador; comprobar acierto, fallo y recuperación a los tres segundos.
- [ ] Acercarse a la barra y pulsar **E**. Laguna Azul cuesta **20 fichas**, dura ocho segundos y repetir renueva duración.
- [ ] Con Laguna Azul, tomar impulso y chocar de frente contra pared/mueble: caer, soltar lo agarrado, perder el efecto y recuperarse. El suelo, los roces y empujar lentamente una pared no deben derribar.
- [ ] Abrir pausa en una copia y comprobar que la otra puede seguir moviéndose. Un golpe ya soltado mantiene su resultado.
- [ ] Salir con una botella agarrada: el otro jugador no debe verla permanentemente poseída.
- [ ] Volver a entrar: no deben reaparecer objetos poseídos ni efectos antiguos. La regla vigente concede las 1.000 fichas una sola vez por IP y sala; al volver se reciben cero. Crear una sala nueva reinicia ese registro.
- [ ] Salir del anfitrión: el cliente vuelve a un menú utilizable y puede crear/entrar en una nueva sala.

Anota la acción, qué copia controlabas y qué observaste si algo falla. Los registros manuales quedan junto al ejecutable: `anfitrion-manual.log` y `cliente-manual.log`. No es necesario aprobar el hito si aparece un bloqueo; se corrige antes de avanzar.

## Repetir la comprobación de los puntos corregidos

El usuario confirmó que las correcciones funcionan. Este recorrido queda como referencia para repetirlas en otra copia o con otro equipo.

Comprueba el número **0.3.1** en los dos menús. Desde el invitado, observa al anfitrión caminar, girar, detenerse y volver: torso y pies deben permanecer juntos, sin alejarse de su posición. Mira también tu cuerpo hacia abajo al moverte lateralmente y girar. Repite varios golpes a Bruno, primero con los puños y después con una botella, esperando su recuperación entre impactos; debe caer con su postura física y volver a quedar de pie. Comprueba una caída del anfitrión y otra del invitado. Escucha el golpe y mira las partículas en ambas copias: cada impacto debe producir una sola reproducción. El volumen sigue respetando tus ajustes locales.

## Límites de las comprobaciones automáticas

La compilación Windows 0.3.1 terminó sin errores ni advertencias. Pasaron 23 comprobaciones locales en el ejecutable, 54 verificaciones de integración por loopback y 115 comprobaciones de cuerpos, caídas, cámara y efectos en anfitrión/invitado. Se incluyeron golpes a Bruno iniciados desde ambas copias y se inspeccionaron capturas de las caídas desde la cámara del invitado. La reproducción del audio se comprobó automáticamente mediante sus llamadas y eventos; después el usuario confirmó de forma general que las correcciones funcionan. El detalle y los informes vigentes están en [Evidence/pre-hito1/VALIDACION.md](../Evidence/pre-hito1/VALIDACION.md).

Las pruebas previas usan procesos separados y UDP real por loopback, con colocaciones controladas del servidor para aislar choques, objetos y reinicio. No sustituyen este recorrido manual ni demuestran LAN entre equipos, soporte de Internet, diversión, rendimiento estable o tolerancia a latencia/jitter/pérdida. Las tasas provisionales siguen siendo 25 Hz para movimiento y 20 Hz para instantáneas.

La identificación por IP tiene limitaciones: varias copias cliente en la misma máquina comparten dirección y no reciben todas otra concesión inicial. Para la prueba de dos copias, el anfitrión y el primer cliente disponen de sus saldos; las reincorporaciones conservan la regla de no volver a conceder crédito.
