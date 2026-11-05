# Demo 0.2.0 · Jugabilidad y UX

Actualización del 4 de noviembre de 2026. Unity 6000.6.8f1, macOS Apple Silicon / Apple M5, Mono, release.

## Resultado de la entrega

- Compilación: **Succeeded**, cero errores y cero advertencias.
- Player real: **62 comprobaciones aprobadas**, cero fallos y cero errores o excepciones durante el recorrido; salida del proceso 0.
- Proyecto: cero errores y cero advertencias en referencias de scripts y metadatos.
- ZIP verificado, versión interna 0.2.0, SHA-256 `76a818b55f0151caaf74b29d1ff5863b54628273145c9059773f0b30eff2efa4`.

## Corrección del billar

La versión inicial bloqueaba toda rotación, usaba amortiguación exponencial, daba por terminado el movimiento demasiado pronto y contabilizaba salir del tablero como un emboque. La actualización separa colisiones de bolas/bandas y del mueble, aplica resistencia constante del paño, usa bocas biseladas y exige reposo sostenido antes del siguiente tiro. La blanca embocada requiere colocación válida; una bola de color fuera del tablero se repone sin puntuar.

La simulación es **plana**, con contactos resueltos por PhysX y giro ligado al desplazamiento. No implementa efectos laterales, saltos ni un modelo profesional de deslizamiento/transición a rodadura. Se fijaron 100 pasos físicos por segundo, interpolación y detección continua.

Mediciones del ejecutable (`ux-physics/pool-physics.json`):

| Escenario | Resultado observado |
|---|---|
| Bolas en reposo | 0.000 m de deriva en el ensayo |
| Choque frontal de masas iguales | La bola objetivo sale a 1.282 m/s; sin velocidad lateral inventada |
| Energía del choque frontal | La suma de velocidades al cuadrado baja de 2.250 a 1.643 |
| Rebote de banda | Razón de velocidades 0.758, frente al parámetro 0.760 |
| Rebote suave | La componente normal invierte su dirección; −0.145 m/s en la muestra |
| Resistencia del paño | Pérdida de 0.190 m/s en 0.5 s, constante |
| Apertura máxima | Pico medido 4.596 m/s; bolas acotadas, sin solapamiento al terminar |
| Ensayo de rodadura a 30 / 120 FPS de render | 0.000 m de diferencia tras el mismo segundo físico |

También se comprobaron corte, captura lateral y diagonal, roce de bocana sin falso emboque, reposición por salida, huecos inválidos de blanca, rechazo de doble tiro, reposo estable y ejercicio introductorio completado en un tiro. La coincidencia entre tasas de render corresponde a ese ensayo; no implica determinismo general de PhysX ni valida red.

## UX y controles

Acceso directo «Practicar billar»; cursor libre sobre el paño; rueda, flechas y deslizador de potencia; un clic para tirar; guía del primer contacto y dirección inicial; cuatro ejercicios, contador y resultado; pausa sin abandonar la mesa; salida a caminar sin golpe accidental; confirmación de reinicio y navegación de menús por teclado.

Las pruebas de entrada usaron **dispositivos virtuales del Input System** dentro de la aplicación compilada, atravesando los mapas de acciones y handlers de Update que usa el jugador. Se verificaron apuntado, rueda, clic de tiro, bloqueo de clics sobre paneles, pulsación mantenida sin repetición, Enter, Escape, E y acceso directo al billar. No se declara una sesión manual completa ni una validación de mando físico.

Se revisaron las capturas del player a 1440×900, 1280×720 y 1920×900; la mesa y los paneles permanecen separados, sin recortes de controles. `05-pool-aim.png`, `06-pool-result.png`, `07-pool-1280x720.png` y `08-pool-wide.png` están en `ux-physics/`.

## Archivos y límites

Aplicación: `Builds/macOS/Tavern and Chaos.app`. Acceso: `Jugar.command`. Copia: `Tavern-and-Chaos-Demo-0.2.0-macOS-arm64.zip`. Código editable: `TavernChaos/`. Cambios: `CHANGELOG.md`.

Continúa siendo práctica local sin LAN ni reglamento competitivo de bola 8. Arte provisional. Sin prueba en Windows, mando físico o sesiones con varias personas; sin promesa de FPS estables en otro hardware. Aplicación sin firma/notarización de distribución pública. La validación inicial 0.1.0 se conserva en `VALIDACION-0.1.0.md`; los resultados vigentes de esta actualización son los de `ux-physics/`.
