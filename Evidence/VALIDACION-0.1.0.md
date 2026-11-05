# Primera demo · validación

4 de noviembre de 2026. Versión 0.1.0.

## Resultado

- Unity 6000.6.8f1, macOS Apple Silicon (Apple M5), Mono, release, stripping desactivado.
- Compilación: **Succeeded**, 0 errores, 0 advertencias. `build-report.json` conserva el resumen real de BuildPipeline.
- Player macOS: **22 comprobaciones aprobadas**, cero fallos, cero errores/excepciones registrados durante el recorrido. Proceso finalizó con código 0.
- Archivos del proyecto: cero errores y cero advertencias en la revisión de GUID, `.meta` y referencias de scripts.

Las pruebas se ejecutaron en el **ejecutable compilado**, con física real y entradas de acción inyectadas por el runner optativo. No equivalen a una sesión manual completa ni a una prueba física de mando.

## Cobertura

Golpe en alcance e impacto único; fallo y caída del atacante; recuperación automática de ambos personajes; bebida, renovación y expiración; agarre, lanzamiento y derribo con botella; cuatro objetivos completables; liberación del objeto y limpieza del efecto al caer; pausa; entrada en billar con personaje al costado; tiro físico, reposición y salida; cinco botellas acotadas; reinicio completo.

Capturas generadas por el player y revisadas visualmente:

- `01-menu.png`: entrada, controles y alcance local.
- `02-impact.png`: resultado de un golpe y progreso de objetivos.
- `03-pool.png`: mesa completa, cámara cenital sin viga ni personaje sobre las bolas, controles y guía de tiro.
- `04-gameplay.png`: taberna, personaje, rival, objetivos y regreso al estado inicial.

Se abrió también la aplicación final a través de la interfaz de macOS. El sistema de ventanas la mostró en su vista lateral; la verificación visual completa se basa en las capturas del player. No se afirma que se haya completado una partida manual sin guía.

## Entrega

- Proyecto: `TavernChaos/`.
- Aplicación: `Builds/macOS/Tavern and Chaos.app`.
- Acceso: `Jugar.command`.
- ZIP: `Tavern-and-Chaos-Demo-0.1.0-macOS-arm64.zip` (26,326,483 bytes).
- SHA-256: `132734d66f4e3138aa1d55b5e84bc65a8481153decafa1981e71f6fd0ec12f02`.

## Límites

Demo local de un jugador y un rival de práctica. El billar es práctica física, sin reglamento de bola 8 ni turnos competitivos. Sin LAN, Windows, juego cruzado, canciones, otros minijuegos ni arte final. Los bindings de mando están implementados, pero el hardware no se probó y los menús se operan con ratón. No se midió rendimiento para prometer 60 FPS estables. No se realizó aún la sesión con tres a cinco personas que pide el GDD para decidir si el golpe resulta divertido. El ejecutable no está firmado ni notarizado para distribución pública.
