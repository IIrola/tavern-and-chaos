# Tavern & Chaos · Primera ronda

Demo de trabajo **0.3.1, local + LAN**, con **Unity 6000.6.8f1**. Proyecto: `TavernChaos/`. La compilación Windows actual queda en `Builds/Windows/Tavern and Chaos.exe`. La nueva compilación universal para Mac está en `Builds/macOS/Tavern-and-Chaos-0.3.1-macOS-Universal.zip`; consulta [cómo probarla](Docs/PROBAR_MAC.md). La release pública anterior 0.2.0 de macOS es una demo local; no incluye esta integración.

El historial de funciones y correcciones está en [CHANGELOG.md](CHANGELOG.md), incluida la primera persona en LAN, los tropiezos con Laguna Azul y las correcciones de sincronización de la 0.3.1. Son cambios locales todavía sin publicar en GitHub.

## Descargar la demo

El repositorio contiene el código, la escena y la configuración de Unity. Las cachés y los ejecutables no forman parte del historial de código. El ZIP de Mac 0.3.1 se comparte por separado; todavía no se ha subido a [GitHub Releases](https://github.com/IIrola/tavern-and-chaos/releases), donde permanece la release anterior.

Para jugar desde una copia descargada del repositorio, compila el proyecto o descomprime el ZIP de la release dentro de `Builds/macOS/`, de modo que exista `Builds/macOS/Tavern and Chaos.app`.

Esta copia integra la fundación de `work/lan-foundation` con la primera persona, el reinicio y las caídas por Laguna Azul. El diseño completo está en [GDD_y_plan_de_desarrollo.md](GDD_y_plan_de_desarrollo.md); el alcance y recorrido de prueba actuales están en [Docs/LAN_HITO1.md](Docs/LAN_HITO1.md). El usuario confirmó que las correcciones de la 0.3.1 funcionan en su prueba con dos copias en un equipo; las pruebas entre equipos siguen pendientes. No se han iniciado las mesas compartidas ni el Hito 2.

## Probar LAN en una sola máquina Windows

Fuera de Play, usa **Tavern → Build Windows LAN demo** en Unity. Después abre **`Probar-LAN.cmd`**: inicia dos copias con registros separados. Crea una sala en una, y en la otra entra mediante IP **127.0.0.1**, puerto **7777**. Usa nombres distintos y el mismo puerto en ambas. No necesitas abrir dos editores ni cambiar de rama. Las comprobaciones automáticas de componentes se pueden repetir con `./run-lan-checks.ps1`; no sustituyen la aceptación manual del Hito 1.

La sala integra movimiento, golpe, botella y Laguna Azul en primera persona. **R recoloca solo a quien lo solicita** si está de pie y no está resolviendo un golpe; no reinicia la sala. La pausa es local. El billar sigue como práctica local separada. Para dos equipos de la misma red usa el código del anfitrión; las pruebas entre máquinas siguen pendientes.

En 0.3.1, el invitado recibe las posturas de las seis partes del cuerpo y las caídas simuladas por el anfitrión. La interpolación de red controla esos cuerpos cinemáticos sin una segunda interpolación de PhysX. Los impactos de puños y botellas tienen eventos de partículas y sonido, identificados para reproducirse una sola vez. Cierra las dos copias antiguas y abre de nuevo `Probar-LAN.cmd`; ambas deben mostrar 0.3.1 en el menú.

## Jugar

Abre `Jugar.command` o el ejecutable y pulsa **Abrir la taberna**, o elige **Practicar billar** para entrar directamente a la mesa. Completa los cuatro objetivos de la izquierda: conectar un golpe, fallarlo, beber en la barra y derribar a Bruno con una botella. Puedes seguir explorando después de completarlos. Bruno se recupera automáticamente.

| Acción | Teclado y ratón | Mando (bindings incluidos) |
|---|---|---|
| Moverse | WASD | Stick izquierdo |
| Mirar | Ratón | Stick derecho |
| Correr | Shift | Pulsar stick izquierdo |
| Cargar y soltar golpe / botella | Mantener y soltar clic izquierdo | Gatillo derecho |
| Billar: apuntar | Cursor del ratón / ← → ajuste fino | Stick derecho |
| Billar: potencia | Rueda / ↑ ↓ / deslizador | Cruceta arriba / abajo |
| Billar: tirar / colocar blanca | Un clic izquierdo | Gatillo derecho |
| Billar: cambiar ejercicio | Tab / botones | RB |
| Interactuar en la taberna | E | Botón sur / A |
| Levantarse del billar | E / botón de volver | Botón este / B |
| Soltar botella | Q | Botón este / B |
| Reiniciar demo / solicitar reposición en billar | R (dos veces en billar) | Select |
| Pausa (también dentro del billar) | Esc | Start |
| Menús: elegir / aceptar / volver | ↑ ↓ / Enter / Esc | Stick o cruceta / A / B |

Para acertar, mira hacia Bruno a menos de unos dos metros. Un fallo te derriba durante tres segundos. Las botellas están en el expositor junto a la alfombra; vuelven solas y su número está limitado a cinco. La Laguna Azul, en la barra izquierda, reduce la fricción de movimiento durante ocho segundos. Repetirla renueva la duración. Caer elimina su efecto.

La exploración se juega en **primera persona**, con mirada libre hacia los lados, arriba y abajo. El cuerpo sigue la orientación de la vista; las botellas se lanzan en la dirección de la mirada. R reinicia posición, orientación e inercia aunque mantengas movimiento. Con Laguna Azul, un choque frontal de al menos **3 m/s** provoca caída; el suelo, los roces laterales y empujar lentamente una pared no la activan. El billar conserva su cámara cenital.

Para comprobar estos cambios en el editor, abre `Tavern.unity` y usa **Tavern → Run first-person checks** fuera de Play. Ejecuta las pruebas con entrada virtual y vuelve al modo de edición; el informe se guarda en `TavernChaos/Temp/FirstPersonChecks/player-tests.json`, sin regenerar la escena.

El **billar libre** empieza con un emboque recto de una sola bola. Apunta con el cursor sobre el paño, ajusta la potencia con la rueda o ↑ ↓ y **haz un clic para tirar**. No hay que cargar el taco manteniendo pulsado. La guía muestra el primer contacto y la dirección inicial de la bola golpeada o el rebote en una banda; es una ayuda geométrica, no una promesa del recorrido completo.

Hay cuatro ejercicios: **emboque recto, corte, bandas y triángulo de seis**. Se muestra el número de tiros, las bolas embocadas y un resultado al completar el ejercicio. Mientras ruedan las bolas no se puede volver a tirar. Si entra la blanca, espera a que pare la mesa, busca un punto libre y haz clic para colocarla; el contorno indica si la posición es válida. Salir de los límites nunca suma un emboque.

E te levanta; Esc pausa sin abandonar la mesa. R solicita reiniciar el ejercicio y una segunda pulsación lo confirma durante tres segundos. Tab o los botones laterales cambian de ejercicio cuando las bolas están paradas. Los golpes y lanzamientos siguen la dirección de la cámara, para que coincidan con lo que estás mirando.

Esc abre pausa y ajustes: volumen de efectos, sensibilidad y desactivación de sacudidas. Los menús tienen foco visible, navegación con ↑ ↓, Enter para aceptar y Esc para volver; en ajustes, ← → modifica el control seleccionado. Los ajustes se guardan en el equipo. Al perder foco, la práctica local pausa la simulación; en LAN solo se pausa la entrada de esa copia y la sala sigue activa. Los bindings de mando están incluidos, pero no se ha validado un mando físico.

### Modelo físico del billar

Simulación plana de práctica: PhysX resuelve contactos entre esferas de igual masa y bandas; el paño aplica una resistencia constante de rodadura. Las bolas giran de acuerdo con su desplazamiento, y las bandas tienen restitución propia. La simulación corre a 100 Hz con interpolación y detección continua. Las bolas están aisladas de los colisionadores del mueble y los personajes. Las bocas biseladas y el detector de entrada en tronera comparten geometría; hay una breve caída visual al embocar. Un tiro termina después de un intervalo de reposo estable, con un límite de recuperación ante bloqueos.

No hay efectos laterales, saltos, massé ni reglas competitivas de bola 8. Esta aproximación debe seguir afinándose con sesiones de juego; no representa una mesa profesional calibrada.

## Abrir y compilar con Unity

1. En Unity Hub, añade la carpeta `TavernChaos` y usa **6000.6.8f1**.
2. Abre `Assets/Scenes/Tavern.unity` y pulsa Play. El entorno se compone al iniciar a partir de módulos originales en C#.
3. Para compilar Mac desde el editor, selecciona macOS como plataforma activa en **File → Build Profiles** y usa **Tavern → Build macOS demo**. También puedes ejecutar `./build-demo.sh` desde esta carpeta en un Mac; el script selecciona la plataforma automáticamente. El editor necesita el módulo Mac Build Support correspondiente a su versión.

La compilación usa la escena guardada, Mono, stripping desactivado y opciones de release. No regenera ni guarda la escena; rechaza cambios de escena sin guardar. El menú Windows genera x64 y el de macOS prepara un binario universal para Intel y Apple Silicon. El script inspecciona `BuildReport` y falla si el resultado no es `Succeeded`. Conserva los shaders usados por los materiales creados en ejecución. Se puede seleccionar otro editor mediante la variable `UNITY_EDITOR`; no se garantiza compatibilidad con otras versiones.

## Validación del ejecutable

El [informe de validación Windows](Evidence/pre-hito1/VALIDACION.md) reúne la compilación 0.3.1, 23 comprobaciones locales, 54 de integración y 115 de cuerpos/caídas/efectos, con capturas del invitado. Su alcance se limita a componentes con colocaciones controladas del servidor, no a una certificación del Hito 1. `Evidence/build-report.json`, `Evidence/ux-physics/` y `Evidence/VALIDACION.md` conservan la evidencia histórica de la demo macOS 0.2.0. El ejecutable macOS puede ejecutar su recorrido local mediante:

```sh
"Builds/macOS/Tavern and Chaos.app/Contents/MacOS/Tavern and Chaos" \
  -demo-smoke-tests -evidencePath "$PWD/Evidence/ux-physics" \
  -logFile "$PWD/Evidence/ux-physics/player-tests.log"
```

Las pruebas optativas recorren golpe válido, golpe fallido, recuperación, agarre/lanzamiento, impacto único, bebida y caducidad, pausa, tiro y salida del billar, límites de objetos y reinicio. También prueban transferencia de velocidad, conservación de energía, rebotes, reposo, troneras, apertura máxima, consistencia a 30/120 FPS de render y entrada a través de dispositivos virtuales de ratón/teclado. Escriben `player-tests.json`, `pool-physics.json`, capturas y código de salida distinto de cero ante fallos. No sustituyen una sesión con personas ni pruebas en otro hardware.

## Alcance y decisiones

Esta entrega integra la práctica local con una sala LAN de autoridad en el anfitrión, caídas articuladas, botellas y una bebida. Se conserva el controlador de cápsula por decisión del usuario para este hito. Usa Built-in; una migración a URP y el active ragdoll de locomoción requieren decisiones independientes. Paquete de entrada fijado en 1.20.1 y NGO en 2.13.3. Arte geométrico y sonidos sintetizados originales; sin canciones ni servicios de emparejamiento.

Quedan pendientes LAN entre máquinas y condiciones de latencia/pérdida, partidas compartidas de billar con reglas oficiales, validación actual con seis personas, los otros juegos, coctelería completa, arte final, mando físico, reasignación y juego cruzado. La instalación disponible es Unity 6.6; no se afirma que sea la 6.3 LTS indicada como referencia en la skill. No es una distribución pública firmada.

## Código principal

- `DemoGame`: sesión, objetivos y resolución de acciones en paso fijo.
- `TavernActor`: movimiento, caída articulada, restauración y efecto temporal.
- `ThrowableBottle`: posesión, física, barrido de impacto y reposición acotada.
- `PoolPractice`: estados de tiro, ejercicios, troneras y ayudas de apuntado.
- `PoolPhysics`: parámetros del paño, cuerpos, materiales y restitución.
- `PoolPhysicsChecks`, `PoolInputChecks`: regresiones físicas y de controles en el player.
- `TavernCamera`: primera persona, vista cenital de billar y sacudida opcional.
- `InteractionRules`: validaciones y límites compartidos por práctica local y autoridad LAN.
- `Networking/LanSession`, `LanProtocol`, `RoomRoster`: transporte, intenciones, réplicas, posesión y sesión.
- `DemoInput`, `DemoHUD`, `DemoAudio`: controles, presentación y sonido.
- `Assets/Editor/DemoBuild`: escena inicial y compilación verificable.
