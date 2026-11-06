# Estado y siguiente trabajo

Rama de trabajo: main. La base recibida fue b89a6d1 (0.3.1); se conservaron primera persona, reinicio personal, puntos libres, cuerpos replicados y efectos. La 0.4 añadió bola 8 compartida, la 0.5 apuestas y libro, y la 0.6 cierre de sesión y resultados. Las evidencias anteriores conservan su alcance histórico.

## Iteración 0.10.0: blackjack jugable y HUD aligerado (entrega C)

Mesa de blackjack en la sala (5 asientos, banca al este del salón): E junto a un asiento; el organizador fija la apuesta (26 mínimo, par: 26/50/100/250), todos confirman y reparte (apertura 10, solo la paga quien organiza). Se juega con ratón o teclado (Enter/A pedir, Backspace/Y plantarse, Tab/RB apuesta, E/B levantarse). Quien llega con la mano en curso espera sin pagar. Cada mano se liquida contra la banca (1:1, 3:2, empate) mediante `SettleHouse`; ausencia >60 s, caída, salida o cierre de sala plantan la mano y se liquida con normalidad. Protocolo de sala 10 (todas las copias 0.10.0). Prueba: `python3 run-bj-room-checks.py` (host + 2 procesos Mac; el tercer proceso comparte IP loopback, por eso no recibe fichas y prueba la cola) y 28 comprobaciones de reglas.

Revisión de UI/UX: el HUD libre ya no ocupa el campo de visión: una pastilla de fichas/tiempo arriba a la izquierda, ebriedad y efectos arriba a la derecha, avisos como toast de cinco segundos, un único aviso contextual abajo y la ayuda de controles solo los primeros 25 s o al mantener **Tab** (que también muestra código de sala, jugadores y actividades). Los paneles laterales fijos de sala desaparecen; las mesas conservan su panel solo durante la preparación. En blackjack la cámara se coloca sobre el asiento mirando la mesa y las cartas se dibujan en la parte inferior/superior sin cubrir el centro. No se ha medido con personas; solo hay capturas en Evidence/blackjack/flow.

Límites: sin sesiones humanas, sin mando físico probado, cartas dibujadas en el HUD (no en la mesa), arte provisional. Hold'em, dominó y Ve a Pescar siguen pendientes.

## (histórico) blackjack: reglas puras

`BlackjackRound` (reglas puras, sin Unity) implementa 2–5 jugadores, baraja de 52 mezclada por el servidor, naturales, as 1/11, pedir/plantarse, crupier que pide hasta 16 y planta en cualquier 17, y resultado por jugador (`HouseOutcome`) para `SessionLedger.SettleHouse` (1:1, 3:2, empate). La vista por jugador nunca incluye el identificador de la carta oculta antes de la liquidación. Decisiones: una mano con natural se planta sola; con todas las manos pasadas el crupier no roba; ausencia, expulsión o desconexión planta la mano comprometida y se liquida con normalidad. 28 comprobaciones (`Tavern → Validate blackjack rules`, también en cada compilación; informe en Evidence/blackjack). Falta la mesa física, comandos de red, UI, turno de 60 s y la apuesta mínima de 26 en la sala.

## Iteración 0.9.0: barra jugable

La barra deja de ser un fixture. `BarRoomController` (anfitrión) valida cercanía, aptitud (de pie, no sentado) y puntería, y delega dinero y posesión en `BarService`. Intenciones nuevas: `BarContainer/Adjust/Recipe/Shake/Serve/Cancel/PutDown/Grab/Offer/Accept`; E sobre la barra abre la estación, E con vaso bebe, E con oferta acepta, E junto a un vaso apoyado lo recoge, F ofrece al más alineado a menos de 3 m, Q deja, clic mantenido lanza (cono de 20°, hasta 9 m, incluye a Bruno). El snapshot incluye `BarView` (estación y vasos con posición); todas las copias dibujan los vasos. Caer, salir, resetear o alejarse cancelan la preparación sin cobro; un vaso en mano al caer se apoya. La bebida de prueba antigua en la sala se retira: las comprobaciones que la usaban ahora sirven un Laguna Azul real (`BarFlowChecks`).

Validación: `python3 run-bar-room-checks.py` (14 comprobaciones del anfitrión y 5 del cliente, dos procesos Mac en loopback), más `run-arm-room-checks.py drunk` y `run-cocktail-effects-checks.py` en verde tras el cambio. `-lan-check` (LanProcessChecks, Hito 1) se adaptó pero no se repitió en esta iteración. Pendiente: sesiones humanas entre equipos, seis personas, mando, calibración del lanzamiento y arte definitivo de vasos.

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

1. Cócteles: hecho en 0.9.0 salvo validación humana entre equipos y calibración. Seguir con la entrega B (recorrido completo con personas nuevas).
2. Blackjack, Hold'em con botes laterales, dominó y Ve a Pescar; fijar fuente/variante antes de implementar y conservar manos privadas.
3. Sesiones humanas de billar y pulseadas entre 2–4 equipos; medir comprensión, diversión, latencia/jitter/pérdida, esfuerzo/descanso y resultado. Validar seis participantes, Windows actual y juego cruzado; caída/expulsión durante pulseadas y pérdida del anfitrión durante cierre.
4. Accesibilidad y configuración completas: texto ampliable, contraste, reasignación, inversión, intensidad de ebriedad, audio por canales y ajustes versionados. Medir cuatro/seis personas y decidir LTS/URP.

El alcance íntegro permanece en los [85 criterios](REQUISITOS.md) y [sus evidencias](REQUISITOS.json). El arte final sigue diferido y el GDD no está terminado.
