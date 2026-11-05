# Tavern & Chaos · Primera ronda

Demo local 0.2.0 para **macOS Apple Silicon**, creada con **Unity 6000.6.8f1** (la versión instalada). Proyecto: `TavernChaos/`. Ejecutable: `Builds/macOS/Tavern and Chaos.app`. Copia comprimida: `Builds/Tavern-and-Chaos-Demo-0.2.0-macOS-arm64.zip`.

## Descargar la demo

El repositorio contiene el código, la escena y la configuración de Unity. La aplicación compilada para Mac se distribuye en [GitHub Releases](https://github.com/IIrola/tavern-and-chaos/releases); las cachés y los ejecutables no forman parte del historial de código.

Para jugar desde una copia descargada del repositorio, compila el proyecto o descomprime el ZIP de la release dentro de `Builds/macOS/`, de modo que exista `Builds/macOS/Tavern and Chaos.app`.

La rama `main` corresponde a la demo local 0.2.0. El diseño completo del juego está en [GDD_y_plan_de_desarrollo.md](GDD_y_plan_de_desarrollo.md); esta demo todavía no cumple todo ese alcance. Los experimentos iniciales de multijugador se conservan en `work/lan-foundation` y no se consideran una versión jugable validada.

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

El **billar libre** empieza con un emboque recto de una sola bola. Apunta con el cursor sobre el paño, ajusta la potencia con la rueda o ↑ ↓ y **haz un clic para tirar**. No hay que cargar el taco manteniendo pulsado. La guía muestra el primer contacto y la dirección inicial de la bola golpeada o el rebote en una banda; es una ayuda geométrica, no una promesa del recorrido completo.

Hay cuatro ejercicios: **emboque recto, corte, bandas y triángulo de seis**. Se muestra el número de tiros, las bolas embocadas y un resultado al completar el ejercicio. Mientras ruedan las bolas no se puede volver a tirar. Si entra la blanca, espera a que pare la mesa, busca un punto libre y haz clic para colocarla; el contorno indica si la posición es válida. Salir de los límites nunca suma un emboque.

E te levanta; Esc pausa sin abandonar la mesa. R solicita reiniciar el ejercicio y una segunda pulsación lo confirma durante tres segundos. Tab o los botones laterales cambian de ejercicio cuando las bolas están paradas. Los golpes y lanzamientos siguen la dirección de la cámara, para que coincidan con lo que estás mirando.

Esc abre pausa y ajustes: volumen de efectos, sensibilidad y desactivación de sacudidas. Los menús tienen foco visible, navegación con ↑ ↓, Enter para aceptar y Esc para volver; en ajustes, ← → modifica el control seleccionado. Los ajustes se guardan en el equipo. La simulación se pausa al perder el foco. Los bindings de mando están incluidos, pero no se ha validado un mando físico.

### Modelo físico del billar

Simulación plana de práctica: PhysX resuelve contactos entre esferas de igual masa y bandas; el paño aplica una resistencia constante de rodadura. Las bolas giran de acuerdo con su desplazamiento, y las bandas tienen restitución propia. La simulación corre a 100 Hz con interpolación y detección continua. Las bolas están aisladas de los colisionadores del mueble y los personajes. Las bocas biseladas y el detector de entrada en tronera comparten geometría; hay una breve caída visual al embocar. Un tiro termina después de un intervalo de reposo estable, con un límite de recuperación ante bloqueos.

No hay efectos laterales, saltos, massé ni reglas competitivas de bola 8. Esta aproximación debe seguir afinándose con sesiones de juego; no representa una mesa profesional calibrada.

## Abrir y compilar con Unity

1. En Unity Hub, añade la carpeta `TavernChaos` y usa **6000.6.8f1**.
2. Abre `Assets/Scenes/Tavern.unity` y pulsa Play. El entorno se compone al iniciar a partir de módulos originales en C#.
3. Usa **Tavern → Build macOS demo**, o ejecuta `./build-demo.sh` desde esta carpeta.

La compilación fija la escena inicial, salida macOS ARM64, Mono, stripping desactivado y opciones de release. El script inspecciona `BuildReport` y falla si el resultado no es `Succeeded`. Conserva los shaders usados por los materiales creados en ejecución. Se puede seleccionar otro editor mediante la variable `UNITY_EDITOR`; no se garantiza compatibilidad con otras versiones.

## Validación del ejecutable

`Evidence/build-report.json` contiene el resultado de compilación actual. Las pruebas nuevas están en `Evidence/ux-physics/`: resultados del player, mediciones de física y capturas en 16:10, 16:9 y pantalla ancha. `Evidence/VALIDACION.md` resume la entrega vigente; los archivos anteriores en la raíz de Evidence conservan la validación inicial 0.1.0. Para ejecutar las pruebas de integración en el player real:

```sh
"Builds/macOS/Tavern and Chaos.app/Contents/MacOS/Tavern and Chaos" \
  -demo-smoke-tests -evidencePath "$PWD/Evidence/ux-physics" \
  -logFile "$PWD/Evidence/ux-physics/player-tests.log"
```

Las pruebas optativas recorren golpe válido, golpe fallido, recuperación, agarre/lanzamiento, impacto único, bebida y caducidad, pausa, tiro y salida del billar, límites de objetos y reinicio. También prueban transferencia de velocidad, conservación de energía, rebotes, reposo, troneras, apertura máxima, consistencia a 30/120 FPS de render y entrada a través de dispositivos virtuales de ratón/teclado. Escriben `player-tests.json`, `pool-physics.json`, capturas y código de salida distinto de cero ante fallos. No sustituyen una sesión con personas ni pruebas en otro hardware.

## Alcance y decisiones

Esta entrega concreta el primer prototipo del GDD en una demo local ambientada: un jugador, un rival de práctica, caídas articuladas, botellas, una bebida y billar libre. Usa el renderizador integrado de Unity para reducir dependencias y acelerar la primera entrega; URP y la arquitectura de red se evaluarán en el siguiente hito. Paquete de entrada fijado en 1.20.1. Arte geométrico y efectos de sonido sintetizados originales; sin canciones, descargas de assets ni servicios externos.

Quedan para los siguientes hitos: LAN y autoridad del anfitrión, partidas de billar con reglas oficiales, seis participantes, otros juegos, recetas completas, animación y arte finales, validación de menús con mando físico, reasignación de controles y validación de Windows/juego cruzado. La instalación disponible es Unity 6.6; no se afirma que sea la 6.3 LTS indicada como referencia en la skill. La demo no está firmada ni notarizada para distribución pública.

## Código principal

- `DemoGame`: sesión, objetivos y resolución de acciones en paso fijo.
- `TavernActor`: movimiento, caída articulada, restauración y efecto temporal.
- `ThrowableBottle`: posesión, física, barrido de impacto y reposición acotada.
- `PoolPractice`: estados de tiro, ejercicios, troneras y ayudas de apuntado.
- `PoolPhysics`: parámetros del paño, cuerpos, materiales y restitución.
- `PoolPhysicsChecks`, `PoolInputChecks`: regresiones físicas y de controles en el player.
- `TavernCamera`: órbita, colisión con entorno y sacudida opcional.
- `DemoInput`, `DemoHUD`, `DemoAudio`: controles, presentación y sonido.
- `Assets/Editor/DemoBuild`: escena inicial y compilación verificable.
