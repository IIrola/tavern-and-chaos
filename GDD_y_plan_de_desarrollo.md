# Tavern Gamblers & Chaos — diseño y plan de desarrollo

**Estado:** propuesta de trabajo, 4 de noviembre de 2026. **Fuente:** documento inicial facilitado por el equipo. Las cifras y decisiones que siguen son hipótesis de diseño para probar, no resultados medidos ni compromisos definitivos. El proyecto Unity y la primera demo local se encuentran ahora en `TavernChaos/`; el alcance implementado y la validación se documentan en [README.md](README.md). El resto de este documento conserva el diseño objetivo y sus decisiones pendientes.

## 1. Visión y límites

Una taberna social en la que un grupo de amigos alterna entre juegos de mesa reconocibles, cócteles absurdos y accidentes físicos breves. El humor surge de acciones legibles y consecuencias compartidas; nadie debe quedar atrapado en una partida o incapacitado durante largos periodos.

**Público y plataforma propuestos:** grupos de amigos, partidas casuales de 20–40 minutos, macOS y Windows con juego cruzado entre ambas plataformas, teclado/ratón y mando; salas privadas alojadas localmente por un jugador. Cada sala admite un máximo de 6 personas. Idioma inicial español, interfaz preparada para inglés. Estética 3D low poly, cómica y sin violencia gráfica, en una taberna rockera. El público objetivo es de 16 años o más; la clasificación oficial depende de la evaluación de cada territorio. El modelo comercial y el nombre definitivo están pendientes.

**Pilares:** interacción social inmediata; caos físico comprensible; reglas de mesa justas; entrada y salida sin fricción; accesibilidad de controles y efectos. Las fichas son ficticias, pertenecen a la sesión y circulan entre sus participantes; si alguien sale, sus fichas se pierden. Los precios y apuestas iniciales se definen más abajo como valores de partida ajustables. No se plantea dinero real, compra de fichas ni premios canjeables.

### Bucle principal
1. Crear una sala o entrar mediante código; configurar nombre, apariencia y controles.
2. Aparecer en la taberna, explorar y reunirse con otros jugadores.
3. Elegir entre preparar una bebida, interactuar con objetos o sentarse a una mesa.
4. Resolver una actividad corta, recibir una reacción visible y volver al espacio común.
5. Repetir, cambiar de actividad o salir. La progresión persistente es opcional y cosmética; una sesión es divertida desde el inicio.

### Alcance por entregas
- **Primer prototipo:** una pregunta: ¿es divertido fallar o conectar un único golpe físico? Una sala gris, dos personajes, golpe y caída; sin red ni arte final. Prueba desechable con tres a cinco personas. Mantener la idea si la mayoría entiende el golpe sin explicación y quiere repetirlo; ajustar o replantear si no distinguen acierto de fallo.
- **Corte vertical:** sala privada para 2–4 jugadores, movimiento, caída recuperable, un objeto lanzable, una bebida, una mesa de billar con físicas y salida segura. Arte y audio representativos pero limitados. El corte demuestra la experiencia completa y los riesgos de red.
- **Lanzamiento propuesto:** soportar hasta seis jugadores, coctelería y seis actividades: billar, blackjack, póker, dominó, Ve a Pescar y pulseadas. NPCs, recetas y variedad visual se amplían solo tras validar rendimiento y diversión. El billar físico multijugador es prioritario y un riesgo técnico que se valida desde el corte vertical; blackjack se desarrolla después.

## 2. Experiencia y espacio

### Taberna
Una sala principal compacta con entrada, barra, zona social y mesas separadas para evitar que los objetos bloqueen las rutas. Bloqueo inicial: rectángulo de aproximadamente 24 × 18 m; pasillos de al menos 2 m y espacio libre alrededor de cada mesa; son objetivos para probar, no medidas finales. Cada actividad tiene silueta y señal visual propias. Las mesas ocupadas muestran asientos libres; las interacciones relevantes tienen indicación de acción y distancia.

El jugador puede caminar, correr, mirar, interactuar, agarrar/soltar, cargar/lanzar, golpear y abrir menú. El apuntado selecciona un único objetivo elegible, resaltado y limitado por distancia y línea de visión. Orden de desempate: objeto más cercano al centro de la mira, después distancia e identificador estable. Solo el servidor confirma la interacción. Los controles exactos se configuran en ajustes y se presentan en un tutorial breve contextual.

### Personaje y cámara
Cámara en primera persona, con mirada libre hacia los lados, arriba y abajo, sensibilidad e inversión ajustables; evita atravesar paredes. El cuerpo sigue la orientación horizontal de la vista y permite desplazamiento lateral. Las actividades de mesa pueden utilizar una cámara dedicada, como la vista cenital del billar. El personaje conserva una cápsula o representación de movimiento verificable; el modelo físico sigue un esqueleto animado. Leer intención de entrada por fotograma y aplicar física en paso fijo. El ragdoll es una representación física cuyos detalles y coste deben validarse con un spike: no se presupone que predecir todos los huesos en red sea práctico.

Estados exclusivos: **Libre**, **Agarrando**, **Cargando golpe**, **Caído**, **Recuperándose**, **Sentado**. Ebriedad y efectos temporales son modificadores independientes. El servidor autoriza cambios. Al caer se suelta el objeto, se cancela la carga y se inicia recuperación tras 3 s de base; al recuperarse, se busca un punto libre y se restablecen colisiones, controles y cámara. El golpe no se activa mientras se está sentado o caído. Una caída termina el duelo, no expulsa al jugador de la sala. Evitar posiciones imposibles y asegurar salida incluso si se interrumpe una animación.

## 3. Combate, objetos y NPC

### Golpe único
- Inicio: jugador libre, sin objeto, dentro de alcance válido de otro jugador o NPC; carga máxima propuesta 1 s. Cancelación si cae, se sienta o pierde conexión.
- Resolución: al soltar, el servidor abre una ventana de impacto de 0,25 s y comprueba alcance, orientación, obstáculo y colisión; solo se registra un impacto. Alcance y fuerza se ajustan en pruebas.
- Acierto: el objetivo cae y termina el duelo. Fallo: al terminar la ventana sin impacto válido, cae quien golpea. Una colisión contra un objeto no equivale automáticamente a acertar al rival. Si ambos golpes coinciden en el mismo paso, ambos caen y hay empate.
- No se requiere invitación ni consentimiento para golpear a otra persona de la misma sala; cualquier caída termina la pelea. No hay daño acumulado ni muerte. Los jugadores sentados también pueden recibir golpes y caer.

### Agarre y lanzamiento
Solo un jugador puede poseer un objeto; el servidor decide reclamaciones simultáneas. Al agarrar comprueba alcance y disponibilidad; al soltar aplica el impulso una sola vez, limitado por carga, dirección y velocidad máximas. La botella y silla tienen masa y colisiones distintas, con límites que impiden proyectiles extremos. Impacto elegible: velocidad relativa superior al umbral provisional de 5 m/s, objetivo válido y sin impacto ya procesado durante esa trayectoria. El servidor decide el derribo; efectos audiovisuales son cosméticos. Probar detección continua para objetos rápidos. Roturas y reposición de botellas necesitan límite de objetos y limpieza; no crear fragmentos físicos ilimitados.

### NPCs
En la versión completa, un jugador puede golpear a un NPC disponible. Estados: ocioso, invitado, preparándose, ataque, caída y recuperación. Tiempo de reacción y puntería configurables; el servidor ejecuta su lógica. El comportamiento del NPC hacia las mesas y otros jugadores queda pendiente de diseño. El corte vertical puede prescindir de NPCs.

## 4. Barra y bebidas

### Preparación
Interacción con estación disponible → recipiente → añadir ingredientes en proporciones configurables → agitar → servir → consumir, ofrecer o lanzar. Se permiten hasta cuatro ingredientes; cuatro licores fuertes producen la mezcla fallida. El catálogo de bebidas no está cerrado; por ahora solo se recogen las combinaciones originales.

| Combinación original | Nombre | Efecto original |
|---|---|---|
| Vodka + curazao azul + hielo | Laguna Azul | Fricción de pies a cero y luz azul en el jugador. |
| Tequila + salsa picante + Tabasco | El Volcánico | Fuego en la boca y onda expansiva sobre NPC cercanos. |
| Absenta + bebida energética + cerveza | Mezcla Inestable | Velocidad al 150 % durante 5 s y caída impulsada hacia atrás. |
| Ginebra + bebida carbonatada misterio | Cóctel de Helio | Gravedad reducida al 20 % durante 15 s. |
| Cuatro licores fuertes | Veneno / K.O. | Colapso inmediato, humo y caída durante 10 s. |

El servidor valida preparación, posesión, impacto y efectos. Todas las bebidas activan su efecto tanto al consumirse como al impactar a otro jugador al ser lanzadas, sin consentimiento adicional. El servidor aplica el efecto una sola vez al objetivo impactado. Esto también afecta a jugadores sentados: se aplica el efecto y, si provoca caída, el personaje cae. Las proporciones, el alcance y las duraciones no indicadas en el original quedan por diseñar.

En Unity 3D no debe asumirse que `Rigidbody` tiene `gravityScale`: reducir gravedad requiere una implementación física adecuada y restaurar el estado anterior al terminar.

### Ebriedad y efectos
Valor entero 0–5, con incremento definido por receta, limitado al rango. El incremento por bebida y la velocidad de recuperación están pendientes de diseño. El nivel se muestra sin depender del color. Niveles 1–2 añaden balanceo leve; 3–4 aumentan el balanceo y reducen estabilidad; 5 añade tropiezos con intervalo mínimo de 15 s. Las distorsiones visuales, sacudidas, desenfoque y aberración tienen intensidad ajustable hasta cero, sin cambiar las reglas de juego. El servidor controla duración y resultados; la cámara y partículas se reproducen localmente.

Cada efecto tiene identificador, origen, inicio, duración y política de acumulación. Al repetir el mismo efecto se renueva duración, no se multiplican valores. Efectos distintos pueden coexistir; se aplican en orden fijo y con límites máximos de velocidad/fuerza. Caída, salida, desconexión y fin de efecto restauran solo los parámetros que corresponda. Mientras permanece sentado, las alteraciones de movimiento no desplazan al personaje; los efectos que provocan caída sí lo derriban; los modificadores específicos de cada juego se indican antes de comenzar.

## 5. Sistema común de mesas

Existe una única mesa por cada juego en cada sala; una mesa tiene identificador, capacidad, asientos, estado, reglas y partida. Flujo: **Disponible → Preparación → En curso → Resultado → Disponible**. Sentarse requiere asiento libre, proximidad, estado Libre y confirmación del servidor. Se guarda el punto de retorno; se suelta el objeto y se desactiva el movimiento voluntario mientras permanece sentado. Los golpes y bebidas siguen afectando al jugador. La cámara cambia solo en el cliente implicado. Una persona puede entrar en la sala en cualquier momento si no está llena. Solo puede incorporarse a una partida de mesa al comenzar; si ya está en curso, puede apuntarse a la espera para la siguiente. Quien ya participa puede levantarse en cualquier momento y volver a sentarse en su propia partida si hay asiento libre. Levantarse libera el asiento, pero no equivale por sí mismo a abandonar la partida: puede volver mientras siga siendo participante. Si es su turno y permanece fuera de la mesa durante más de un minuto, se expulsa de la partida de esa mesa, no de la sala. No hay límite de tiempo por inactividad mientras esté sentado. Cómo se reincorpora a una ronda en curso y cómo se resuelven las apuestas al ser expulsado quedan pendientes según cada juego. Al finalizar se muestran resultados y opción de revancha o salir.

**Interrupción por caída:** el jugador puede ser derribado aun estando sentado; pierde el asiento, pero puede volver a sentarse si sigue disponible. La caída por sí sola no lo expulsa de la partida. Si es su turno, el tiempo fuera de la mesa cuenta para el límite de un minuto. El servidor resuelve un tiro ya iniciado antes de procesar nuevas acciones.

El primer jugador sentado organiza la mesa; si se levanta, se transfiere este papel al siguiente asiento ocupado. El servidor inicia un temporizador de **60 segundos** cuando coinciden el turno del jugador y su ausencia de la mesa: si ya estaba fuera, empieza al llegar su turno; si se levanta durante su turno, empieza al levantarse. Al volver a sentarse antes de superar el minuto, el temporizador se cancela; si vuelve a levantarse comienza de nuevo. No se expulsa automáticamente por tardar en jugar mientras permanezca sentado. Cualquier persona de la sala puede iniciar una votación para expulsar a otra persona de la sala. La persona propuesta no puede votar. En cuanto los votos a favor superen la mitad del total de personas de la sala, incluida la persona propuesta, se ejecuta la expulsión sin esperar los votos restantes. Con tres personas hacen falta dos votos a favor; con dos es imposible reunir la mayoría. La persona expulsada puede volver a entrar. La votación dura 15 segundos y termina sin expulsión si no se alcanza la mayoría. Solo se toman en cuenta las personas presentes al iniciarla: quien entre después no vota ni cuenta para calcular la mayoría de esa votación. Si una persona sale de la sala, se invalida su voto si ya había votado y se recalculan el total de participantes originales que permanecen en la sala y la mayoría; si con el nuevo total ya hay mayoría, la expulsión se ejecuta inmediatamente. Si sale la persona propuesta, la votación se cierra sin ejecutar una expulsión adicional. Si la persona expulsada iba a ser ganadora, se adjudica la victoria a la siguiente en la clasificación; los desempates y casos sin siguiente participante quedan pendientes. El servidor valida fase, turno, asiento, parámetros y secuencia; solicitudes repetidas o tardías no producen acciones duplicadas. Un espectador ve información pública, nunca manos ajenas.

**Normas de juego:** las reglas planteadas han sido aprobadas y se aplicarán las reglas oficiales de cada juego. Cuando existan distintas federaciones o variantes oficiales, deberá fijarse y citarse una fuente concreta antes de implementar; los resúmenes siguientes son provisionales y deben revisarse contra esa fuente. La regla de levantarse y la expulsión por ausencia durante el turno son reglas particulares de la taberna. Las reglas de bola 8 descritas están aprobadas; se contrastarán con el reglamento oficial elegido y se señalarán posibles diferencias antes de implementar.

### Blackjack
2–5 jugadores contra crupier automático; baraja estándar de 52 cartas, sin comodines, mezclada por servidor antes de cada ronda. Todos reciben dos cartas; una carta del crupier queda oculta. As vale 1 u 11; figuras 10. Turno: pedir o plantarse; pasarse de 21 pierde. Crupier pide con 16 o menos y se planta en 17, incluido 17 blando. Blackjack natural vence a 21 de tres o más cartas; naturales simultáneos empatan. Sin dividir, doblar, seguro ni rendición en la primera versión. El bar actúa como banca: las apuestas perdidas pasan al bar y los pagos por manos ganadoras salen de sus fondos. El pago propuesto es 1:1 para victoria normal y 3:2 para blackjack natural; el empate devuelve la apuesta. La expulsión se produce tras más de 60 segundos fuera de la mesa durante su turno; la liquidación de la mano y las desconexiones quedan por definir conforme a las reglas seleccionadas.

### Póker Texas Hold'em
2–6 jugadores, baraja de 52; dos cartas privadas, cinco comunitarias, rondas preflop/flop/turn/river. Cada jugador forma su mejor mano de cinco cartas; jerarquía estándar de carta alta a escalera real, con desempates por rangos. Fichas ficticias de sesión que se transfieren entre participantes; ciegas y reglas de incorporación y saldo cero están pendientes de definir. Acciones: retirarse, pasar, igualar, apostar, subir o ir all-in cuando sean legales. Subida mínima según la última subida completa; calcular botes principales y secundarios por contribución, repartir empates y asignar ficha indivisible desde el primer asiento activo a la izquierda del botón. Más de 60 segundos fuera de la mesa durante el turno expulsan al jugador de la partida; el tratamiento de la mano y contribuciones debe concretarse. El póker requiere especificación de pruebas exhaustivas antes de producción.

### Dominó
2–4 jugadores, doble seis de 28 fichas, 7 por jugador. Dos jugadores dejan el resto como pozo; tres o cuatro dejan las fichas restantes sin usar. Comienza el doble más alto repartido; si no hay doble, la ficha con suma más alta. En cada turno se coloca una ficha coincidente en un extremo; si no puede, roba hasta poder jugar o vaciar el pozo y entonces pasa. Gana quien vacía la mano; si todos pasan consecutivamente con pozo vacío, gana la menor suma de puntos en mano; empate compartido. Más de 60 segundos fuera de la mesa durante el turno expulsan al jugador de la partida; debe definirse cómo continúa la ronda. La disposición visual no determina legalidad.

### Ve a Pescar
2–5 jugadores, baraja de 52; siete cartas por jugador con 2–3 participantes y cinco con 4–5. En su turno se pide a un rival un rango que se tenga en mano; el rival entrega todas las cartas de ese rango. Si recibe alguna, repite turno; si no, roba una del mazo y termina turno, salvo que robe el rango pedido, en cuyo caso repite. Cuatro cartas del mismo rango forman un libro y se colocan visibles. Mano vacía: robar una al inicio del turno si quedan cartas; si no, queda fuera. La partida termina cuando no quedan cartas en manos ni mazo; gana quien tenga más libros, empate compartido. Más de 60 segundos fuera de la mesa durante el turno expulsan al jugador de la partida; debe definirse cómo continúa la ronda.

### Billar
**Prioridad principal para el corte vertical.** Primera modalidad propuesta: bola 8, para dos jugadores o dos equipos. Las reglas de bola 8 propuestas han sido aceptadas. Otras modalidades quedan fuera del primer corte hasta que se seleccionen.

- **Equipo y objetivo:** 15 bolas numeradas, blanca y seis troneras. Cada lado debe embocar su grupo (lisas 1–7 o rayadas 9–15) y después la bola 8 legalmente.
- **Preparación y apertura:** bola 8 al centro del triángulo, una lisa y una rayada en las esquinas posteriores; el resto se distribuye. Se decide el primer turno al azar. Un saque es válido si emboca una bola de color o al menos cuatro bolas de color tocan banda. Si no es válido, el rival decide aceptar la mesa o pedir un nuevo saque. Una blanca embocada o fuera es falta y concede bola en mano detrás de la línea de salida. Si la 8 entra en el saque, se repone y continúa la mesa abierta; si además hay falta, el rival recibe bola en mano detrás de la línea. Estas son reglas de proyecto propuestas y deben mostrarse en la mesa.
- **Mesa abierta y grupos:** tras el saque, la mesa sigue abierta; el primer embocamiento legal anunciado asigna grupos. Mientras está abierta no se usa la 8 como primera bola de contacto. En tiros normales el jugador anuncia bola y tronera; una bola propia en otra tronera no conserva el turno, aunque permanece embocada.
- **Tiro legal:** la blanca debe tocar primero una bola válida del propio grupo, o la 8 cuando se ha terminado el grupo; después del primer contacto debe entrar alguna bola o cualquier bola tocar una banda. Embocar legalmente la bola anunciada conserva el turno; en otro caso cambia, salvo pérdida por 8.
- **Faltas:** blanca embocada o fuera, primera bola incorrecta, no tocar bola legal, ausencia de bola embocada o banda tras contacto, o tiro fuera de turno. El rival recibe bola en mano en cualquier punto legal de la mesa, salvo la excepción del saque. Las bolas de color embocadas permanecen fuera; una bola de color saltada se repone en el punto de salida o posición libre más cercana.
- **Bola 8 y victoria:** tras limpiar su grupo, el jugador anuncia tronera y gana al embocar legalmente la 8 allí. Pierde si emboca la 8 antes de tiempo, en otra tronera, con falta, o si la 8 sale de la mesa. Empates y jugadas simultáneas no se resuelven por orden visual: el servidor registra y evalúa el conjunto de eventos del tiro.
- **Física y red:** mesa 3D con bolas, bandas y troneras; servidor simula y resuelve tiros. El cliente envía dirección, potencia y posición válida de blanca cuando corresponda, nunca posiciones finales. El tiro termina cuando todas las bolas permanecen bajo umbrales de movimiento durante un intervalo estable; un límite de seguridad resuelve estados atascados sin dejar la mesa bloqueada. Ajustar fricción, restitución, masas y detección continua mediante pruebas, sin prometer simulación realista antes de medirla.
- **Interrupciones:** tras más de 60 segundos fuera de la mesa durante su turno se expulsa al jugador de la partida; el resultado de la partida ante expulsión o desconexión queda por definir. La ebriedad superior a 2 puede añadir oscilación visual configurable, sin alterar de forma oculta la dirección confirmada.

### Pulseadas
Dos jugadores aceptan un duelo de 15 s. Marcador de posición central entre −100 y +100; gana quien alcance su extremo o tenga ventaja al terminar; cero es empate. Entrada alternativa de mantener pulsado con estímulos de ritmo, sin exigir pulsaciones repetitivas. Cada acierto de ritmo aplica fuerza; fallar consume resistencia, que se recupera lentamente sin entrada. Ebriedad duplica el consumo de resistencia en esta actividad y se comunica antes de aceptar. El servidor comprueba tiempos y límites de entrada. La consecuencia de desconexión o expulsión debe concretarse; levantarse por sí solo no supone abandonar.

## Economía de fichas — reglas aprobadas y detalles pendientes

Las fichas son ficticias y duran solo la sala. El servidor llevará un libro de saldos y botes: cada participante recibe 1.000 fichas al entrar; las apuestas se trasladan de su saldo al bote y los premios pasan del bote a los ganadores. Los premios entre jugadores se pagan con las fichas del bote; la banca infinita de blackjack puede introducir fichas nuevas en los saldos de los jugadores. Si un jugador abandona la sala, desaparece su saldo disponible; una apuesta ya comprometida permanece en el bote y se liquida según las reglas de esa partida. Al cerrar la sala se eliminan todos los saldos y botes. La asignación inicial de 1.000 se concede una vez por dirección IP por sala: si alguien sale, pierde su saldo y al volver desde la misma IP no recibe otra asignación. Se debe probar el comportamiento con direcciones compartidas o cambiantes: una IP no identifica de manera fiable a una persona.

La sala no ofrece una recarga automática: si nadie puede pagar una nueva apertura o bebida y ya no quedan partidas en curso ni pagos pendientes, el bar los echa mediante una cinemática y termina la sesión. Para evaluar esta condición, el servidor espera a resolver las apuestas y pagos pendientes; si alguna persona recupera fichas, la sesión continúa. Los precios iniciales se indican a continuación y se ajustarán tras las pruebas.

En blackjack, el bar es la banca infinita: recibe las apuestas perdidas y paga las ganadoras sin límite de saldo. Los pagos de la banca pueden aumentar las fichas en circulación; registrar entradas y salidas para comprobar la contabilidad. En póker los botes pasan a ganadores; en billar y otras mesas las apuestas y el tratamiento de empates, expulsiones y equipos deben ajustarse a las reglas de juego y a las decisiones del proyecto.

### Precios y apuestas iniciales

Valores de diseño propuestos en fichas, ajustables después de probarlos. La persona que inicia una nueva partida paga al bar el coste de apertura una sola vez, además de su propia apuesta inicial cuando corresponda. Las demás personas admitidas al inicio pagan solo su apuesta; apuntarse a la lista de espera es gratis. Antes de comenzar se muestra el desglose y se valida que cada participante tenga saldo suficiente. Si no se alcanza el mínimo de jugadores, se cancela sin cobrar; si la apertura falla, se devuelven los importes reservados. El coste de apertura no es una apuesta y no entra en el bote.

| Juego | Apertura pagada por quien inicia | Apuesta mínima por participante | Destino |
|---|---:|---:|---|
| Billar | 10 | 0 | Si hay apuesta, bote para el ganador. |
| Blackjack | 10 | 26 | Contra la banca infinita; cada mano requiere apuesta. |
| Póker | 10 | 0 | Si se juega con apuesta, se emplean fichas de sesión para el bote. |
| Dominó | 5 | 0 | Si hay apuesta, bote para quien gane. |
| Ve a Pescar | 5 | 0 | Si hay apuesta, bote para quien gane. |
| Pulseadas | 5 | 0 | Si hay apuesta, bote para quien gane. |

La apuesta es opcional en todos los juegos salvo blackjack: cero significa jugar sin intercambiar fichas por el resultado. Para póker sin apuesta se necesitan fichas de juego sin valor económico para conservar las ciegas y la dinámica de apuestas; no se pueden convertir en fichas de sesión. Con apuesta real de sesión, las ciegas y el mínimo de entrada se acordarán antes de iniciar. El coste de apertura siempre se cobra por separado, incluso si la apuesta es cero.

Preparar una bebida cuesta 20 fichas por vaso servido, independientemente de los ingredientes; se cobra al servir, no al cancelar. Agarrar objetos, moverse y mirar las mesas es gratis.

Propuesta para blackjack: victoria normal paga 1:1, natural 3:2 y empate devuelve la apuesta. Para permitir el pago 3:2 sin fracciones, exigir apuestas pares; la apuesta mínima pasa a **26 fichas**. Las cantidades de la tabla son una base económica, no una modificación de las reglas oficiales de cada juego. En caso de empate se devuelve la apuesta a cada participante; el coste de apertura permanece en el bar. El tratamiento de la expulsión y salida durante una mano se concretará para cada juego.

### Fin de sala y estadísticas
Al crear la sala, el anfitrión define su duración; propuesta inicial seleccionable de 20, 30, 45 o 60 minutos. La sesión termina al agotarse ese tiempo o cuando nadie pueda pagar nada más, una vez resueltas las apuestas y pagos pendientes. Al agotarse el tiempo, el servidor cierra nuevas entradas y acciones de inicio, liquida partidas y apuestas en curso según una política por definir y muestra los resultados; la cinemática de expulsión se aplica al final por falta de dinero. Las estadísticas muestran mesas ganadas, apuestas ganadas y fichas ganadas/perdidas, desmayos y bebidas consumidas; guardar también empates y aclarar que cada partida de mesa cuenta una vez. Los resultados se calculan en el servidor y se muestran al final, incluidos los de personas que salieron o fueron expulsadas antes del cierre. La sala conserva su registro de estadísticas hasta terminar; el estado personal se reinicia al cambiar de sala. La identificación por IP se usa para asociar una posible vuelta a la misma sala, con las limitaciones indicadas.

### Cinemática de expulsión
Cuando ninguna persona puede pagar una nueva apertura de mesa o bebida, y no quedan partidas en curso ni pagos pendientes, el servidor declara el final de la sesión. Se bloquean nuevas acciones y comienza para todos una cinemática cómica en la que el personal del bar los echa; después se muestra una pantalla de fin con opción de volver al menú o iniciar una sala nueva. La puesta en escena, el personal que aparece y la duración quedan por diseñar. Un jugador individual sin fichas no activa por sí solo la expulsión.

## 6. Multijugador y arquitectura técnica

**Decisión propuesta:** Unity con URP y Netcode for GameObjects, servidor autoritativo y anfitrión-jugador para la primera prueba; salas privadas locales entre amigos, sin servicio público de emparejamiento previsto. La primera versión se configura exclusivamente para LAN; conexión desde casas distintas queda como posible mejora futura, fuera del alcance inicial. No existe versión instalada o fijada en este repositorio: seleccionar una versión LTS y versiones compatibles de paquetes al crear el proyecto; registrar números exactos y licencia. Evaluar servidor dedicado solo si host y conectividad no satisfacen requisitos. El servidor anfitrión no es una autoridad de confianza frente a trampas del propio anfitrión.

- El cliente envía intenciones; servidor valida propiedad, estado, distancia, turno y frecuencia; aplica resultado una vez y publica estado. No enviar cartas privadas a clientes no autorizados, incluso en instantáneas o registros.
- Personajes: probar control local con corrección e interpolación remota; replicar raíz, estado y eventos esenciales. En caída, evaluar un conjunto reducido de huesos o una aproximación visual local, comparando legibilidad y ancho de banda. La predicción completa del ragdoll queda abierta.
- Objetos y billar: simular en servidor y enviar instantáneas con interpolación. Física no determinista: nunca confiar en simulaciones independientes idénticas. Medir latencia y discrepancia con pérdidas simuladas; no fijar frecuencia final antes de pruebas.
- Tablas: estado de partida en servidor, vistas filtradas por destinatario y números de revisión. Mensajes de acción identificados, idempotentes y limitados por frecuencia. La semilla de cartas permanece en servidor hasta que ya no revele información oculta.
- La salida de la sala destruye el saldo disponible; el retorno se identifica por IP y no vuelve a conceder el saldo inicial. Una persona usa una sola máquina y no hay pantalla dividida. Al cambiar de sala se reinician saldo, estadísticas y estado de sesión; el control por IP corresponde a cada sala. La consecuencia de una desconexión accidental durante una partida queda pendiente; el tratamiento del tiempo fuera de la mesa durante una desconexión queda pendiente. Si el host se desconecta, cerrar sala e informar; migración de host queda fuera de la primera versión.

### Separación de módulos
`Core` (sesión, estados, tiempo y servicios); `Player` (movimiento, estados y cámara); `PhysicsInteractions` (agarre, golpe, objetos); `Bar` (recetas y efectos); `Tables/Common` (asientos, contrato, temporizadores); módulos independientes `Blackjack`, `Poker`, `Domino`, `GoFish`, `Pool`, `ArmWrestling`; `Presentation` (UI, audio, animaciones); `Content` (definiciones). Cada minijuego ofrece crear partida, validar/aplicar acción, avance de tiempo, vista por jugador y finalizar. Reglas puras cuando sea posible, separadas de Unity y transporte. ScriptableObjects contienen configuración estática; el estado mutable vive en la partida del servidor. No crear clases ni paquetes como si ya existieran.

### Datos y persistencia
Local: ajustes de accesibilidad, audio, idioma y controles; versión de esquema y valores predeterminados ante archivo inválido. Persistencia de cosméticos y progreso se decide después de validar el bucle; no guardar información secreta de partidas ajenas. La partida y las fichas viven en memoria y terminan al cerrar sala. No recopilar datos personales ni analíticas por defecto; si se agregan servicios, documentar datos, consentimiento y costes según corresponda.

## 7. Dirección visual, sonido y accesibilidad

Arte 3D low poly caricaturesco, ambientado en una taberna rockera. La [imagen de referencia](referencias/taberna-rockera.png) muestra un local íntimo con escenario al fondo, batería, pantallas laterales, mesas de madera, sillas oscuras, ladrillo, techo negro y luz roja, violeta y azul. Adaptar estos rasgos al estilo low poly y conservar iluminación legible para jugar; siluetas distintas para personajes y objetos peligrosos. Iluminación legible sobre oscuridad ambiental, interfaz de contraste alto, símbolos además de colores. La producción empieza con bloqueos y una escena de referencia; lista inicial de activos: personaje y rig, NPC reutilizable, botella, silla, vaso/coctelera, barra modular, seis mesas, cartas/fichas/bolas, iconos y efectos.

Audio por buses maestro, música, ambiente, efectos e interfaz; controles independientes. Variantes de golpe, cristal y fichas evitan repetición; señales de turno también visuales. Las canciones las incorporará manualmente la persona desarrolladora; el formato, carga y posible sincronización entre jugadores se decidirán al implementar. La música no debe ocultar voz externa. Subtítulos para información relevante, sin depender de audio direccional.

La UI usa anclajes y contenedores, navegación completa por teclado y mando, foco visible, texto ampliable y confirmación de acciones destructivas. Ajustes: reasignación de controles, sensibilidad, reducción de movimiento y efectos de cámara, volumen, contraste y tamaño de texto. Interacciones con tiempo deben admitir márgenes razonables; pulseadas ofrecen mantener y no requieren pulsación rápida. Diseño inicial 16:9 con prueba en 16:10 y ultrawide; comprobar que las cartas y botones sigan legibles.

## 8. Calidad y criterios de aceptación

**Funcionales:** dos clientes pueden entrar, moverse, preparar una bebida, completar una partida de billar físico y salir sin perder control; no pueden ocupar el mismo asiento ni tomar el mismo objeto; un turno rechazado no modifica estado; ningún cliente recibe cartas privadas ajenas; la desconexión no bloquea mesa ni deja objetos poseídos. Los efectos expiran y restauran valores aun al caer o abandonar.

**Pruebas:** reglas puras con casos límite y propiedades (mazo sin duplicados, conservación de cartas y contabilidad de fichas, incluidas emisiones de la banca infinita y pérdidas al salir, orden y legalidad de turno, desempates y botes); pruebas de integración de conexión, asientos y desconexión; pruebas físicas manuales de colisiones, recuperación y objetos rápidos; pruebas de red con latencia, jitter y pérdida simuladas; sesiones sin guía con jugadores nuevos y pruebas de accesibilidad. Registrar versión, plataforma, configuración, observación y resultado; no declarar estabilidad sin medición.

**Rendimiento inicial a validar:** 60 FPS como objetivo provisional en macOS y Windows, con hardware de referencia por definir, cuatro jugadores y presupuesto de red medido; validar también el máximo de seis participantes. Registrar tiempo de CPU/GPU, coste de articulaciones, número de cuerpos activos, tráfico y correcciones. Perfilar antes de optimizar; reducir huesos replicados, objetos activos y efectos según resultados.

## 9. Plan por hitos y riesgos

| Hito | Entrega | Criterio para continuar |
|---|---|---|
| 0. Definición | Decisiones de público, plataforma, equipo y versión Unity; prototipo de golpe desechable | Jugadores comprenden y repiten el golpe; riesgo y coste de red estimados. |
| 1. Red y movimiento | Sala privada, 2 clientes, movimiento, caída y recuperación | No hay bloqueo tras desconexión; movimiento y caída legibles en condiciones de red probadas. |
| 2. Interacción | Agarre, lanzamiento, una bebida y efectos con limpieza | Una sola posesión e impacto validado; estado restaurado correctamente. |
| 3. Billar físico | Asientos, cámara, turnos, bolas, colisiones y troneras sincronizadas | Partida completa y coherente en LAN sobre macOS; comprobar Windows y juego cruzado cuando se disponga de acceso. |
| 4. Corte vertical | Integración, interfaz, audio y pruebas con 2–4 personas | Bucle completo jugable y defectos bloqueantes resueltos. |
| 5. Ampliación | Resto de juegos, NPC, arte y contenido por prioridad | Cada módulo supera reglas, integración y pruebas de usabilidad. |
| 6. Preparación | Optimización, localización, compatibilidad y distribución | Presupuestos medidos y lista de defectos de lanzamiento aceptada. |

Riesgos principales: **física en red** (spike temprano y degradación visual); **amplitud de minijuegos** (primero billar físico y contrato común); **filtración de información oculta** (vistas por destinatario); **host desconectado** (cierre explícito); **mareo y accesibilidad** (efectos opcionales); **desacuerdos de reglas** (variantes visibles antes de jugar). El proyecto lo desarrolla una sola persona en una Mac con M5; con presupuesto cero y dedicación al 100 %. No se estiman fechas fiables sin una planificación y pruebas del alcance. Las pruebas de Windows y juego cruzado requieren acceso a un equipo Windows.

## 10. Decisiones pendientes con el equipo

1. Seleccionar y contrastar la fuente oficial concreta para cada reglamento aprobado, y señalar cualquier diferencia con las reglas de bola 8 ya acordadas.
2. Definir el tratamiento de empates o ausencia de siguiente clasificado cuando se expulse a quien iba a ganar, así como la liquidación ante desconexiones y fin del tiempo de sala.
3. Verificar los casos de votación: entradas posteriores, salidas, invalidación de votos y cierre al salir la persona propuesta.
4. Probar identificación por IP en LAN y el reinicio al cambiar de sala; una IP puede cambiar o compartirse y no garantiza identidad.
5. Ajustar precios, apuestas y estadísticas mediante pruebas.
6. Hay una sola persona, dedicada al 100 %, con presupuesto cero y Mac M5. No hay acceso a Windows: compilar desde Mac no demuestra que la versión Windows funcione ni que el juego cruzado haya sido probado.

Las decisiones confirmadas prevalecen sobre las hipótesis de equilibrio. Los valores pendientes se concretarán con evidencia de pruebas.
