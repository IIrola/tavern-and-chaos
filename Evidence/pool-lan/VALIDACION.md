# Validación de la iteración 0.4.0

Base: `main` 0.3.1 (`b89a6d1`). Unity 6000.6.8f1, Mono. Comprobaciones ejecutadas el 5 de noviembre de 2026 en una Mac Apple Silicon.

## Resultados

| Evidencia | Resultado y alcance |
|---|---|
| `rules.json` | 48 comprobaciones puras aprobadas, incluidos 1.000 triángulos, turnos, anuncios, faltas, ocho, bloqueo y asientos/ausencia. |
| `processes/summary.json` | 67 comprobaciones aprobadas: anfitrión visible y dos aplicaciones sin ventana, tres participantes por UDP loopback. |
| `processes-side1/summary.json` | 65 comprobaciones aprobadas: tres procesos por UDP loopback con el otro lado inicial; cambia quién ejecuta los tiros del recorrido. |
| `regression/player-tests.json` | 86 comprobaciones aprobadas de jugabilidad, física, entrada, cámara y recuperación; cero fallos y cero errores de ejecución. |
| `regression/pool-physics.json` | Mediciones de contactos, energía, bandas, freno, troneras, colocación y consistencia a distintas tasas de render. |
| `build-macos.json` | `Succeeded`; proceso de compilación con salida 0, aplicación ejecutada. El binario declara `x86_64 arm64`, comprobado con `lipo`. |

Los seis procesos de los dos recorridos salieron con código 0 y no registraron errores de ejecución. Son dos salas de tres participantes, no una prueba de seis personas en una misma sala.

## Qué cubren los recorridos de red

Asiento cercano y exclusivo, precio de apertura cobrado una vez, rechazo fuera de turno/zona, saque insuficiente y aceptación, anuncios, ocho tiros físicos anunciados hasta ganar, caída durante tiro sin perder participación, recuperación/retorno, cambio de organizador, siguiente persona en espera, nueva ronda y desconexión. Se verifican réplicas cinemáticas, orientación del cuerpo propio sentado y eventos de audio.

Las posiciones controladas del servidor aíslan los tiros de emboque; **los impactos, troneras y resultados se resuelven mediante la física y las reglas reales**, sin inyectar ganadores ni resultados de tiros. La segunda ronda usa el triángulo normal: se movieron las quince bolas de color, el pico observado fue aproximadamente 4,60 m/s y el tiro terminó dentro del límite. El primer lado se fija solo en el ensayo para cubrir ambos recorridos; el juego normal lo decide al azar.

Las capturas `processes/01-table.png`, `02-result.png` y `03-rules.png` proceden de la aplicación visible. Se revisaron mesa, resultado y texto de reglas. Abrir reglas bloquea los tiros locales y mantiene el reloj compartido; cerrar restaura el mapa de entrada.

## Correcciones del proceso de prueba

Los primeros intentos leían una fase anterior a la última instantánea. El coordinador de pruebas ahora espera el estado que ve cada participante antes de actuar. El arranque espera al primer fotograma del anfitrión antes de conectar invitados. Los procesos sin gráficos no solicitan bloqueo del cursor; las pruebas con dispositivos virtuales ignoran temporalmente el foco y restauran esa configuración al terminar. El juego normal conserva su pausa local al perder foco.

## Límites y anomalía de compilación

El contador `errors` de `BuildReport` devuelve **745** aunque el resultado es `Succeeded`, el registro termina en éxito y el player pasa sus comprobaciones. Se conserva ese dato sin cambiarlo a cero; su causa queda pendiente. Hay una advertencia por el soporte Intel futuro. El verificador de referencias/GUID revisó 60 archivos con cero errores y cero advertencias; eso es una comprobación distinta del contador de Unity.

No se han probado aún esta versión en Windows, juego cruzado, redes entre máquinas, latencia/jitter/pérdida, mando físico, un minuto real de ausencia ni sesiones humanas sin guía. Tampoco se han medido 60 FPS a cuatro/seis personas. Las apuestas, economía completa, otras cinco actividades, recetas y accesibilidad completa siguen pendientes. Estos resultados no acreditan el GDD completo ni una simulación profesional calibrada.

## Repetir

1. `./build-demo.sh` ejecuta primero las reglas puras y después genera la aplicación.
2. `python3 run-pool-room-checks.py --visual-host` ejecuta el primer recorrido.
3. `python3 run-pool-room-checks.py --first-side-1` ejecuta el otro lado inicial.
4. Ejecuta el player con `-demo-smoke-tests -evidencePath <carpeta> -logFile <registro>` para la regresión. `-pool-input-only` aísla la entrada.

El archivo local para compartir esta compilación es `Builds/Tavern-and-Chaos-0.4.0-macOS-Universal.zip`; su tamaño y hash se registran en `package.json`. Los ejecutables no forman parte del historial de código.
