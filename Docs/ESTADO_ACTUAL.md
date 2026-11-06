# Estado y siguiente trabajo

Rama de trabajo: main. La base recibida fue b89a6d1 (0.3.1); se preservaron primera persona, reinicio personal, puntos libres, cuerpos replicados y efectos. La 0.4 añadió bola 8 compartida y la 0.5 añadió apuestas y el libro. Sus evidencias permanecen por versión, sin convertir pruebas locales en aceptación entre equipos.

## Iteración 0.6.0: terminar una sesión sin perder resultados

La sala admite duraciones de 20/30/45/60 minutos. Al llegar al límite rechaza nuevas entradas y acciones, termina el tiro/golpe/botella ya soltados y después paga las partidas terminadas o devuelve apuestas inconclusas. La apertura consumida permanece en el bar. Un resultado cancelado no inventa victoria o empate.

La insolvencia se evalúa después de resolver: una apuesta pendiente y una partida sin apuesta en curso impiden la expulsión; un premio que devuelve saldo mantiene la sala abierta. Cuando nadie puede pagar, empieza una cinemática compartida de seis segundos, con personal y participantes provisionales, cámara fija y movimiento reducido.

Los resultados son inmutables y tienen páginas que incluyen salidos/retornos. Conservan el saldo previo al borrado; después el libro elimina fondos y botes. Los clientes descargan las páginas y conservan las recibidas si sale el anfitrión. Crear otra sala reinicia cuentas, reloj, historial, asientos y entrada. Escape sale de resultados sin dejar el juego bloqueado. La política está en [CIERRE_DE_SESION.md](CIERRE_DE_SESION.md).

La evidencia actual está en Evidence/session-end. Se usa Mac, tres participantes conectados y un cuarto intento rechazado, UDP loopback y reloj acelerado. El ensayo de 8 ganadora prepara su historial de reglas de forma controlada; el tiro final, tronera, pago y cierre son reales. No se presenta como partida humana ni red entre equipos.

El umbral económico usa los precios completos del GDD, cuyo mínimo es 5. Las actividades de 5 fichas aún faltan; con saldos entre 5 y 9 la demo conserva interacción libre, pero no ofrece todavía esas mesas. Esto es una carencia de contenido pendiente, no una economía terminada.

## Lo que sigue, en orden

1. Incorporar una actividad de apertura 5: pulseadas con aceptación, ritmo/resistencia, alternativa de mantener, duración 15 s, reloj/entradas en servidor e integración de cierre y apuestas. Cierra además el hueco del catálogo entre 5 y 9 fichas, manteniendo el alcance de las demás actividades.
2. Cócteles: estación, hasta cuatro ingredientes, oferta/lanzamiento, las cinco recetas, ebriedad y restauración de efectos, incluidos sentados.
3. Blackjack, Hold'em con botes laterales, dominó y Ve a Pescar; fijar fuente/variante antes de implementar y conservar manos privadas.
4. Sesiones humanas de billar y actividades entre 2–4 equipos; ausencia real de un minuto, latencia/jitter/pérdida, sala de seis y desconexiones durante cierre. Validar Windows actual y juego cruzado.
5. Accesibilidad y configuración completas: texto ampliable, contraste, reasignación, inversión, audio por canales y ajustes versionados. Medir cuatro/seis personas y decidir LTS/URP.

El alcance íntegro permanece en los [85 criterios](REQUISITOS.md) y [sus evidencias](REQUISITOS.json). El arte final sigue diferido y el GDD no está terminado.
