# Hitos y plan de entrega — Tavern & Chaos

Referencia de alcance: [GDD y plan de desarrollo](../GDD_y_plan_de_desarrollo.md), confirmado íntegro por el usuario. Este plan conserva sus hitos 0–6 y concreta entregas y puertas de aceptación. El registro detallado contiene [85 criterios](REQUISITOS.md) y [evidencia por criterio](REQUISITOS.json).

La prioridad sigue siendo jugabilidad, UX y física del billar. Los modelos definitivos se sustituyen después. El arte diferido no elimina las seis actividades, la coctelería, la economía, la accesibilidad ni la validación multijugador.

## Entrega actual

**Versión 0.9.0: barra jugable en sala LAN.** La estación de coctelería está conectada a mundo, interfaz y red: E junto a la barra abre el panel (recipiente, hasta cuatro ingredientes con proporciones y atajos de receta, agitar, servir por 20 fichas). El vaso aparece en el mundo y se puede beber (E), ofrecer (F, el receptor acepta con E sin coste), dejar (Q), recoger (E) y lanzar (mantener y soltar clic) contra otra persona o contra Bruno. Las cinco recetas aplican su efecto a quien beba o reciba el impacto. Mantiene bola 8, pulseadas, apuestas y cierre de sesión. El protocolo de sala pasa a 9: todas las copias deben ser 0.9.0.

**Límite de esta entrega:** la prueba automatizada usa dos procesos Mac en loopback (servir, cobro único, ofrecer/aceptar, beber, estación exclusiva, cancelar sin cobro, lanzamiento con K.O.). No acredita LAN entre equipos, seis personas, mando físico ni lectura de la interfaz por personas nuevas. Los vasos son cilindros provisionales y el lanzamiento se resuelve por puntería (cono de 20°) en el anfitrión. Evidencia en [Evidence/bar/flow](../Evidence/bar/flow/summary.json).

No se declara terminado ningún hito amplio por haber compilado o pasado un recorrido programado. “Parcial” indica que existe implementación o evidencia de parte del alcance; “Pendiente” indica trabajo sin esa integración.

## Hitos originales y puertas de aceptación

| Hito del GDD | Estado | Entrega existente | Falta para aceptarlo |
|---|---|---|---|
| **0. Definición** | Parcial | GDD, alcance, seis actividades, precios, fuentes/variantes de billar y pulseadas, Unity fijado y scripts de construcción. | Confirmar público y hardware de referencia; decisión documentada LTS/URP; validar comprensión y repetición del golpe con personas. |
| **1. Red y movimiento** | Parcial | Sala privada de anfitrión, nombre/código/IP, capacidad seis, primera persona, réplica de cuerpos, caída/recuperación y salida. | Sesiones entre equipos, seis conectados, IP compartida/cambiante, desconexiones y latencia/jitter/pérdida medidos. |
| **2. Interacción** | Parcial | Botellas exclusivas, golpe/lanzamiento, impactos únicos; estación de barra, vasos físicos, ofrecer/aceptar/dejar/recoger/lanzar y efectos de recetas en red (0.9.0). | Objetivos por mira con visibilidad real (hoy cono en el anfitrión); caídas, ofertas y efectos sobre sentados/caídos durante juego real con personas. |
| **3. Billar físico** | Parcial | Bola 8 compartida, asientos/cámara, anuncios, turnos, faltas, troneras, apuestas, pagos y tiro durante cierre; partida física programada completa. | Personas completan una partida sin guía entre equipos; ausencia real de un minuto; red adversa; calibración de cortes/bandas/saque; Windows y juego cruzado cuando haya acceso. |
| **4. Corte vertical** | Parcial | Exploración, combate, billar, pulseadas, fichas, consentimiento, cierre y resultados compartidos con controles básicos. | Recorrido íntegro entrar → preparar bebida → completar billar → consultar resultados → salir con 2–4 personas nuevas; accesibilidad mínima y cero bloqueos. |
| **5. Ampliación** | Parcial | Pulseadas integradas, reglas de las cinco recetas, contrato y libro reutilizables; NPC y arte provisionales. | Coctelería completa, blackjack, Hold’em, dominó, Ve a Pescar, NPC ampliados y sustitución posterior de arte. Cada módulo debe superar reglas, integración, dinero y usabilidad. |
| **6. Preparación** | Pendiente | Builds y paquetes de desarrollo con informes; releases históricas y documentación. | Medir CPU/GPU/física/red en cuatro y seis personas, localización, ajustes completos, compatibilidad, distribución final y defectos aceptados. |

Los hitos funcionales pueden avanzar en paralelo con validación y accesibilidad. Una puerta pendiente se mantiene visible aunque comience contenido de otro hito.

## Orden de las siguientes entregas

| Orden | Entrega concreta | Dependencias | Criterio verificable de salida |
|---|---|---|---|
| **A. Barra jugable** (0.9.0: implementada, falta sesión humana y prueba entre equipos) | Estación exclusiva, recipiente, hasta cuatro ingredientes/proporciones, agitar, servir, consumir, ofrecer/aceptar, dejar/recoger y lanzar. | Dominio BarService y catálogo; física/replicación de efectos. | Dos jugadores preparan y transfieren vasos; se cobra 20 solo al servir; saldo insuficiente/cancelación no cobran; posesión y efecto únicos; lanzamiento y expiración restauran estado, incluidos sentados. |
| **B. Cierre del corte vertical** | Integrar barra, billar, pulseadas, UX y estadísticas en un recorrido continuo. | A y mesas actuales. | 2–4 personas nuevas completan el recorrido sin ayuda ni pérdida de control; dos partidas distintas pueden coexistir; cierre por tiempo/insolvencia no pierde pagos ni efectos admitidos. |
| **C. Blackjack** | 2–5 contra crupier; baraja 52, as 1/11, natural, pedir/plantarse y banca infinita. | Contrato común, vistas privadas y libro. | Apuesta par mínima 26; pagos 1:1/3:2 y empate exactos; carta oculta nunca enviada a ajenos; ausencia, desconexión y cierre liquidados sin fuga. |
| **D. Texas Hold’em** | 2–6, manos privadas, fases, ciegas, apuestas legales, all-in, evaluación y botes laterales. | Privacidad y pagos de C. | Conservación de 52 cartas y de cada bote; subidas mínimas correctas; empates y ficha indivisible definidos; no se filtran manos ni se duplican premios con desconexiones. |
| **E. Dominó** | 2–4, doble seis, reparto/inicio, extremos, pozo a dos, bloqueo y puntuación. | Contrato común y reglamento fuente. | Fichas conservadas, acciones legales, final por mano vacía/bloqueo, empates y ausentes resueltos; partida en red y UI sin guía. |
| **F. Ve a Pescar** | 2–5, pedir rango propio, transferencias, robo, repetición, libros y final. | Vistas privadas y contrato. | Conservación de cartas, reparto según participantes, libros públicos y manos privadas, mano vacía/mazo agotado/empates/desconexiones cubiertos. |
| **G. Experiencia y contenido final** | NPC, modelos/animaciones posteriores, audio por canales, localización, contraste, texto, reasignación y ajustes versionados. | Seis actividades funcionales; revisión continua desde A. | Teclado y mando físicos, foco visible, texto legible en 16:9/16:10/ancho, intensidad cero, información sin depender solo de color o sonido, configuraciones válidas tras reiniciar. |
| **H. Candidata de distribución** | Optimización medida, compatibilidad y paquete final. | B–G y matriz de pruebas cerrada. | Presupuestos CPU/GPU/física/tráfico registrados; objetivo provisional 60 FPS evaluado sobre hardware acordado; cuatro/seis participantes, Windows/macOS/juego cruzado, regresión y defectos de lanzamiento revisados. |

## Verificación transversal en cada entrega

1. **Reglas:** casos límite, turnos, empates, ausencia, desconexión y final de sesión. Cartas/fichas sin pérdidas o duplicados; economía conservada, incluida banca infinita y destrucción al salir.
2. **Autoridad:** el cliente envía intenciones; el servidor valida dueño, estado, fase, parámetros, frecuencia y secuencia. Los observadores y rivales solo reciben información permitida.
3. **Interrupciones:** caer, levantarse, pausa local, expulsión, salida del anfitrión y pérdida de conexión dejan un resultado y controles utilizables. Una partida pagada queda fija.
4. **UX y accesibilidad:** tareas con personas nuevas, teclado/mando, estados visibles, precio/efecto antes de aceptar, movimiento reducido e intensidad cero. No confundir un binding con un mando físico probado.
5. **Física y red:** condiciones controladas para aislar errores y después juego libre; latencia/jitter/pérdida y hardware registrados. Una prueba en loopback no acredita LAN entre máquinas.
6. **Entrega:** versión coherente, build y pasos sin errores, aplicación ejecutada, evidencia con límites, revisión senior y paquete cuya huella coincida con el probado.

## Revisión senior y riesgos que dirigen el trabajo

- **Billar y física en red:** el código y los recorridos actuales permiten continuar. Falta calibración con personas y condiciones de red reales; proteger esto al incorporar vasos y efectos.
- **Experiencia cooperativa/social:** caer o levantarse conserva la participación cuando corresponde. Mostrar coste, dueño, turno y liquidación reduce disputas; comprobar que esas explicaciones se entienden sin ayuda.
- **Pulseadas:** mantener evita machacar, pero el atractivo del ritmo manual y la elección fuerza/descanso requieren sesiones humanas. La equidad por tiempo de recepción bajo latencia no está acreditada.
- **Coctelería:** las reglas de dominio evitan cobros y aplicaciones duplicados. Los efectos físicos no equivalen a una estación completa; la entrega A debe probar el flujo entero y su lectura mientras ocurren otras partidas.
- **Cartas privadas:** prioridad de diseño antes de añadir blackjack/póker/pesca. La ocultación visual de una carta no basta si sus datos viajaron a otro cliente.
- **Amplitud:** conservar el alcance y cerrar módulos verificables. Evitar atribuir a los seis juegos la evidencia de dos mesas.
- **Arte y rendimiento:** conservar modelos provisionales hasta validar jugabilidad. Perfilar antes de optimizar o sustituir el rig; no declarar rendimiento por intuición.

## Dependencias externas y control del plan

El GDD indica una persona, Mac M5 y presupuesto cero. No hay acceso actual acreditado a Windows ni un grupo de equipos físicos de prueba. Estas dependencias se mantienen abiertas; compilar Windows no demuestra ejecutarlo ni jugar cruzado.

No hay fechas de lanzamiento aprobadas. El orden anterior expresa dependencias y criterios de salida; las estimaciones se revisarán con sesiones humanas y mediciones. El trabajo pendiente se consulta en [ESTADO_ACTUAL.md](ESTADO_ACTUAL.md) y el detalle de cada iteración en [ITERACIONES.md](ITERACIONES.md).

Un hito solo se marca completo cuando su puerta de aceptación tiene evidencia del mismo alcance. Una release de desarrollo puede publicarse con hitos parciales si enumera claramente qué incluye y qué falta.
