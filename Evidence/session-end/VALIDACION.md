# Validación 0.6.0 — cierre de sesión

Unity 6000.6.8f1, Mono, macOS Universal. El GDD completo sigue en desarrollo. Las evidencias de 0.4 y 0.5 se conservan en sus carpetas históricas.

## Resultados finales

| Verificación | Resultado |
|---|---|
| Reglas de cierre | 20 aprobadas |
| Libro de cuentas | 38 aprobadas; 1.000 rondas en 14 salas |
| Bola 8/asientos | 48 aprobadas |
| Cierre por tiempo inconcluso | 57 aprobadas |
| Cierre con 8 ganadora | 39 aprobadas |
| Insolvencia/expulsión | 44 aprobadas |
| Partida de billar completa | 80 aprobadas |
| Regresión local | 86 aprobadas |
| Construcción | Succeeded; cero errores de resumen/pasos, una advertencia Intel |
| Referencias/GUID | 83 archivos, cero errores/advertencias |

Todos los procesos finales terminaron con salida 0, sin fallos ni errores de ejecución. Los cierres suman 140 comprobaciones en tres salas separadas.

## Casos de cierre

Cada caso usa tres procesos conectados y un cuarto que intenta entrar después del cierre, con UDP real por loopback. No son cuatro participantes admitidos ni pruebas entre equipos. El límite de tiempo se acelera mediante el mismo reloj autoritativo: no se esperaron veinte minutos reales.

| Informe | Alcance |
|---|---|
| time-cancel/summary.json | Tiempo durante tiro y botella ya lanzada; devolución de apuestas, apertura conservada, ninguna victoria inventada, ocho conexiones históricas en dos páginas, pausa/ajustes, navegación de teclado y tamaños de pantalla. |
| time-win/summary.json | 8 legal ya lanzada al agotarse el tiempo: premio único y saldos finales exactos; crear sala nueva desde la interfaz de teclado, reiniciando cuentas/reloj/historial. |
| insolvency/summary.json | Bote pendiente protege saldo cero; pago físico de 100 devuelve capacidad y mantiene sala abierta; al gastarlo se inicia expulsión compartida de seis segundos; movimiento reducido y presentación sin colliders. |

Los tres casos comprueban el rechazo de comandos crudos durante el cierre, admisión tardía denegada sin concesión de fichas, cierre contable sin botes pendientes, descarga de páginas finales por los clientes, conservación después de salir el host y Escape desde resultados desconectados.

Para aislar la 8 final, dos casos preparan el historial previo de reglas con eventos controlados. No se afirma que esos siete emboques anteriores hayan sido físicos: el tiro final, los contactos, tronera, victoria, pago y cierre sí usan la física/reglas reales. Las pruebas de partida completa se conservan por separado en pool-processes-side1.

## Reglas y regresión

- lifecycle.json: 20 comprobaciones de duraciones, borde exacto de admisión, resolución pendiente, prioridad tiempo/insolvencia, reloj compartido, premio que repone saldo, devolución y borrado de fondos.
- ledger.json: 38 comprobaciones del libro, incluidas 1.000 rondas deterministas en 14 salas.
- pool-rules.json: 48 comprobaciones de reglas/asientos.
- pool-processes-side1/summary.json: partida completa y revancha con tres procesos, apuestas, caída/retorno, espera, resultado físico y desconexión; no usa el historial de 8 preparado por los casos de cierre.
- regression/player-tests.json y pool-physics.json: regresión local de cámara, entrada, golpes, objetos, bebida de prueba, recuperación y billar.
- build-macos.json y build-macos-steps.json: construcción y mensajes de pasos. El script regenera metadatos de tipos para evitar las copias duplicadas diagnosticadas en 0.5, conserva copia fuera del proyecto y rechaza errores.

## Revisión visual

Se revisaron el aviso de resolución, la página inicial y la segunda de historial, la expulsión, el modo reducido, los resultados y el regreso a sala nueva. Las capturas del resultado cubren 1280×720, 1440×900 y 1920×900. No presentan controles cortados; falta texto ampliable, prueba de TV y accesibilidad completa.

Se corrigió un encabezado que pasaba a tres líneas y se limitó el encuadre de la cinemática para que no apareciese otro escenario detrás. La cámara de expulsión es fija; los personajes son originales provisionales, sin colliders ni resultado físico propio. Reducir movimiento elimina giros y saltos, conservando el mismo reloj compartido.

La captura de una página espera un fotograma dibujado y a que termine de guardarse el archivo antes de pedir la siguiente. La prueba anterior pedía dos al mismo final de fotograma y podía guardar una sola con contenido anterior; se corrigió el ensayo.

## Límites

Solo está integrada la mesa de billar. El catálogo del GDD conserva aperturas de 5; esas actividades siguen pendientes y, entre 5–9 fichas, esta demo todavía solo ofrece interacción libre. Se mantiene el alcance completo y se prioriza pulseadas para cerrar ese hueco.

Faltan otras cinco actividades, cócteles completos, integración del cierre de cada módulo nuevo, redes entre equipos/Windows actual, seis participantes simultáneos, latencia/jitter/pérdida, pérdida de host durante la cinemática, duración real, mando físico y sesiones humanas sin guía. No se ha medido el objetivo de 60 FPS. El humor/ritmo de la cinemática y el confort del billar requieren evaluación humana.

## Repetir

1. Cerrar el editor del proyecto y ejecutar ./build-demo.sh.
2. python3 run-session-end-checks.py time-cancel --visual-host
3. python3 run-session-end-checks.py time-win --visual-host
4. python3 run-session-end-checks.py insolvency --visual-host
5. python3 run-pool-room-checks.py --first-side-1
6. Player con -demo-smoke-tests -evidencePath <carpeta> -logFile <registro>.
7. Verificador de GUID de unity-csharp-scripting sobre TavernChaos.
