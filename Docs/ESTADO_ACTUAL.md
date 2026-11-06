# Estado y siguiente trabajo

Rama de trabajo: main. La base recibida fue b89a6d1 (0.3.1); se conservaron primera persona, reinicio personal, puntos libres, cuerpos replicados y efectos. La 0.4 añadió bola 8 compartida, la 0.5 apuestas y libro, y la 0.6 cierre de sesión y resultados. Las evidencias anteriores conservan su alcance histórico.

## Iteración 0.8.0: efectos físicos y planificación

El dominio de coctelería conserva 76 comprobaciones y las 38 del libro: recetas/proporciones, estación exclusiva, catálogo congelado, vasos con uso único y servicio/pago separados del consumo. Esta entrega conecta los efectos al personaje y a la réplica: fricción cero con inercia, velocidad 150 %, caída de inestabilidad a los 5 s, gravedad 20 % sobre cápsula y ragdoll, onda sobre NPC y K.O. de 10 s. Efectos distintos coexisten y se restauran selectivamente; la salida y el cierre eliminan todos. Luces, fuego/humo provisionales, información de duración e intensidad visual hasta cero forman parte de la presentación.

La validación nativa mide movimiento y aceleración, interrumpe un asiento real, transmite los cinco efectos y comprueba limpieza/resultados entre dos procesos. Las recetas se aplican por fixture: todavía no existe la estación jugable, el vaso físico ni la UI/red de preparar/servir/ofrecer/lanzar. La bebida de prueba sigue disponible. Evidencia y límites en Evidence/effects/VALIDACION.md.

El [documento de hitos](HITOS_Y_PLAN_DE_ENTREGA.md) conserva los siete hitos originales del GDD, estado, dependencias, aceptación y orden siguiente. La prioridad inmediata es conectar BarService con mundo, UI y transporte para cerrar la barra; después completar el corte vertical con personas y continuar los cuatro juegos pendientes. La demo 0.8 es una entrega de desarrollo; no completa el GDD.

## Iteración 0.7.0: segunda actividad compartida

Pulseadas ofrece dos asientos, apertura de 5 y apuesta igual opcional, ambas confirmadas antes del cobro. Tras 3 s de cuenta atrás, el duelo dura 15 s con marcador −100..100: extremo o ventaja gana y cero empata. Mantener y ritmo manual comparten fuerza, coste y recuperación; tres esfuerzos permiten elegir consumo y empuje. El servidor admite un impulso por ventana y resuelve ambos lados juntos.

La ebriedad 0–5 se separa del efecto azul temporal. La bebida de prueba añade un nivel y se recupera uno cada 30 s. Su coste doble de resistencia se comunica antes de confirmar, se replica y continúa tras expirar Laguna Azul. Cambio de coste invalida el consentimiento. La base incluye balanceo visual reducible e intervalo mínimo de 15 s para tropiezos al moverse en nivel 5; faltan las recetas y su validación física completa.

Levantarse conserva la participación y el reloj, impide jugar en otra mesa y permite volver. La desconexión concede victoria al rival elegible antes de liquidar; un resultado pagado permanece fijo. La espera no cobra. El cierre consulta billar y pulseadas, resuelve el impulso admitido y devuelve apuestas del duelo inconcluso conservando la apertura. Las reglas y decisiones están en [REGLAS_PULSEADAS.md](REGLAS_PULSEADAS.md).

La cámara, resistencia, señal de ritmo, feedback, pausa local, reglas y resultado económico están integrados. Movimiento reducido elimina sacudida y pop del agarre sin alterar la simulación. Los brazos y muebles son provisionales.

La apertura de 5 ya permite jugar con saldos entre 5 y 9. Cuatro actividades del catálogo siguen pendientes. La evidencia actual está en Evidence/armwrestling; son pruebas programadas con procesos Mac en loopback, no aceptación humana o LAN entre equipos.

## Lo que sigue, en orden

1. Cócteles: estación exclusiva, hasta cuatro ingredientes y proporciones, servir/consumir/ofrecer/lanzar; cinco recetas y coexistencia/restauración de efectos, incluidos sentados. Mantener las pruebas de cierre y pago único.
2. Blackjack, Hold'em con botes laterales, dominó y Ve a Pescar; fijar fuente/variante antes de implementar y conservar manos privadas.
3. Sesiones humanas de billar y pulseadas entre 2–4 equipos; medir comprensión, diversión, latencia/jitter/pérdida, esfuerzo/descanso y resultado. Validar seis participantes, Windows actual y juego cruzado; caída/expulsión durante pulseadas y pérdida del anfitrión durante cierre.
4. Accesibilidad y configuración completas: texto ampliable, contraste, reasignación, inversión, intensidad de ebriedad, audio por canales y ajustes versionados. Medir cuatro/seis personas y decidir LTS/URP.

El alcance íntegro permanece en los [85 criterios](REQUISITOS.md) y [sus evidencias](REQUISITOS.json). El arte final sigue diferido y el GDD no está terminado.
