# Validación 0.7.0 — pulseadas y dos mesas

Compilación macOS universal con Unity 6000.6.8f1 y Mono. Resumen y pasos: cero errores; una advertencia de futura retirada de Intel. Los ensayos se ejecutan contra el player construido, no una simulación del transporte. El paquete y su hash están en package.json.

## Resultados

| Recorrido | Comprobaciones aprobadas | Evidencia |
|---|---:|---|
| Reglas de pulseadas y ebriedad | 30 | rules.json |
| Libro, incluidos 1000 repartos y 14 salas puras | 38 | ledger.json |
| Reloj/cierre puro | 20 | lifecycle.json |
| Bola 8 y asientos puros | 48 | pool-rules.json |
| Pulseadas: mantener contra ritmo, empate y UI | 42 | draw/summary.json |
| Pulseadas: bebida real, resistencia y ganador | 34 | drunk/summary.json |
| Pulseadas: cierre con impulso y apuestas pendientes | 30 | closing/summary.json |
| Pulseadas: desconexión y relevo desde la cola | 28 | disconnect/summary.json |
| Billar: partida física completa y siguiente ronda | 80 | pool-processes/summary.json |
| Cierre por tiempo con tiro/botella pendientes | 52 | time-cancel/summary.json |
| Ocho física ganadora durante cierre | 36 | time-win/summary.json |
| Insolvencia después de resolver premio | 41 | insolvency/summary.json |
| Regresión local con ventana, física y entrada real | 86 | regression/player-tests.json |

Todos los recorridos finales indicados terminaron con salida 0 y sin errores de ejecución. Referencias/GUID: 101 archivos, cero errores o avisos. Los números cuentan comprobaciones, no sesiones humanas ni criterios completos del GDD.

## Qué se probó

Tres procesos independientes en cada escenario de pulseadas: anfitrión, rival y espectador. Apertura 5 y apuesta 25 confirmadas por ambos, reservas exactas y bote 50. El anfitrión usa mapas de entrada reales con teclado virtual para confirmar, cambiar modo/esfuerzo y mantener Espacio. El rival envía intenciones de red, sin marcas de tiempo elegidas por él. Veinte impulsos por lado producen empate, una devolución 25 a cada uno, saldos 995/1000 y un empate registrado por persona. Los datos exactos están en draw/result-state.json.

La bebida se compra por interacción real en la barra y cuesta 20. Su nivel replica el coste doble y continúa tras expirar los ocho segundos azules; la victoria paga 50 con beneficio neto 25 y pérdida 25. En desconexión se paga una vez, el resultado se cierra y el espectador entra desde el asiento libre próximo. Levantarse no abandona ni permite entrar a billar mientras participa; regresar conserva el duelo.

Para cierre se agotan carteras mediante una operación declarada del libro y se acelera el límite de sala. El bote/duelo activo impiden insolvencia prematura. Se admite un impulso, se cierra, se devuelve la apuesta inconclusa y no se cuenta victoria ni empate. Los dos invitados reciben el informe final inmutable. Las posiciones iniciales de jugadores son fixtures, no recorridos de navegación humana.

La regresión de billar prepara posiciones para aislar tiros físicos, troneras, grupo, 8 y pagos; no inventa sus resultados. En el caso de 8 ganadora durante cierre se prepara además el historial reglamentario anterior al tiro final. Los otros cierres mantienen tres conectados y rechazan una cuarta entrada tardía. Los relojes son acelerados: no equivalen a esperar una sesión completa de 20–60 minutos.

Se inspeccionaron capturas de preparación, reglas, duelo, movimiento reducido y resultado en 1440×900, 1280×720 y 1920×900. Los avisos se movieron para no tapar el marcador. El movimiento reducido restaura la escala del agarre y suprime la sacudida sin detener la autoridad.

## Incidencias y límites

El primer arranque gráfico no avanzó hasta el plazo del runner; no se identificó una causa. Otro empate visible no alcanzó la comprobación conjunta prevista; se separó el diagnóstico de pulsos y saldos y los recorridos posteriores verificaron los valores exactos. La regresión completa ejecutada inicialmente sin gráficos aprobó 78 comprobaciones y falló ocho dependientes de menús/ratón: ese entorno no dibuja los botones que despachan las acciones. Se repitió con ventana y pasó las 86; el intento está descrito en input-investigation.json. No se modificaron los criterios para ocultar estos intentos.

Faltan personas nuevas, mando físico, prueba entre equipos, Windows actual, seis participantes, dos partidas distintas simultáneas y latencia/jitter/pérdida. No está acreditada la equidad de la ventana manual bajo latencia ni el atractivo de fuerza/descanso. Caída y expulsión durante pulseadas y pérdida del anfitrión durante cinemática siguen pendientes. La ebriedad añade solo la base; recetas, intensidad continua y efectos combinados faltan. Cuatro juegos y otros criterios del GDD permanecen en Docs/REQUISITOS.json.
