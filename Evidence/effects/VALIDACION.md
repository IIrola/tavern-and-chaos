# Validación 0.8.0 — base física de cócteles

Unity 6000.6.8f1, macOS universal, Mono. Resumen y pasos de construcción: cero errores; una advertencia sobre futura retirada de Intel. Se ejecuta la aplicación construida en Apple Silicon. El paquete y su huella se registran en package.json.

## Resultados y alcance

| Recorrido | Comprobaciones aprobadas | Informe |
|---|---:|---|
| Dominio de recetas, preparación, vasos y servicio | 76 | ../bar/rules.json |
| Libro, incluidos casos de conservación y banca | 38 | ledger.json |
| Reloj/cierre puro | 20 | lifecycle.json |
| Reglas de bola 8 y asientos | 48 | pool-rules.json |
| Pulseadas y ebriedad puras | 30 | rules.json |
| Física de efectos y réplica entre dos procesos | 36 | actors/summary.json |
| Pulseadas con bebida, resistencia y pago | 34 | drunk/summary.json |
| Billar: partida física completa y siguiente ronda | 80 | pool-processes/summary.json |
| Cierre con tiro y botella pendientes | 52 | time-cancel/summary.json |
| Regresión local con ventana, física y controles | 86 | regression/player-tests.json |

La regresión del libro en ../bar/ledger-regression.json repite esos mismos 38 casos y no se cuenta como cobertura distinta. Los recorridos finales indicados terminaron con salida 0 y sin errores de ejecución. Referencias/GUID: 127 archivos, cero errores o avisos.

## Física medida

Las recetas se aplican mediante una fixture explícita del anfitrión y las posiciones/cámara se preparan. El movimiento, gravedad, colisiones, recuperación, asiento y transporte UDP se ejecutan realmente. Esta prueba no recorre una estación de preparar/servir ni un vaso físico.

La cápsula recorre aproximadamente 3,30 m por segundo de prueba normal y 4,95 m con inestabilidad. Laguna Azul conserva el impulso horizontal al soltar movimiento y correr; después de quitarla vuelve la frenada. Helio da un pequeño impulso inicial y menor aceleración; no desplaza al actor sentado. Dos ragdolls en caída libre miden cerca de −9,81 y −1,962 m/s², sin contactos externos y sin modificar Physics.gravity global.

La onda de Volcánico derriba al NPC próximo y deja de pie al jugador próximo. La inestabilidad vence a los 5 s, libera un asiento real de pulseadas y conserva participación. K.O. sobre un actor ya caído retrasa recuperación hasta 10 s. El invitado recibe los cinco efectos con tipo, origen, inicio, duración y tiempo restante; sus cuerpos siguen cinemáticos. Luz/fuego/humo se activan localmente y pueden ocultarse a intensidad cero sin cancelar reglas.

El cierre de sala elimina los modificadores antes de congelar resultados. El invitado recibe el informe y la instantánea sin efectos, conserva resultados tras salir el host y vuelve al menú al salir explícitamente. Se revisan capturas de presentación combinada, intensidad cero y ajustes. La regresión local incluye su recorrido de billar en varias resoluciones.

## Hallazgos y correcciones

La primera medición de Helio dio aproximadamente −2,96 m/s². Se añadió registro de contactos: la sonda alta tocaba una viga del techo. Una sonda inferior sin contactos mide −1,962, manteniendo la fuerza y tolerancia previstas. La sonda alta se conserva como diagnóstico en el informe; gravity-investigation.json preserva el resultado inicial y su explicación verificada.

Al instrumentar ese diagnóstico se corrigió una llamada a una API no disponible en Physics 3D; el pipeline rechazó la compilación y la siguiente compiló con el registro de colisiones adecuado. No se distribuye la construcción fallida.

Se corrigió una frenada oculta al soltar correr bajo Laguna Azul: el límite de velocidad de marcha no debe borrar inercia existente. También se completó la limpieza al finalizar sala, para que los temporizadores no queden congelados con modificadores activos detrás de resultados. La revisión visual corrigió un salto de línea literal en el aviso de pulseadas.

## Límites y siguiente entrega

Falta la estación jugable y conectar el dominio BarService con UI, mundo, vasos físicos, oferta/aceptación y lanzamiento por red. En la demo permanece la bebida de prueba; las otras recetas físicas se ejercitan por aplicación controlada de pruebas. Las comprobaciones puras de servicio/posesión no prueban ese flujo físico.

Faltan expiración natural física de 15 s de Helio y más combinaciones/interrupciones, pruebas con personas nuevas, hardware de mando, seis participantes, dos mesas simultáneas, LAN entre máquinas, latencia/jitter/pérdida y Windows/juego cruzado actuales. No se afirma diversión, calibración definitiva o rendimiento de lanzamiento. Los cuatro juegos restantes y el resto del GDD continúan en el [plan de hitos](../../Docs/HITOS_Y_PLAN_DE_ENTREGA.md) y los [85 criterios](../../Docs/REQUISITOS.json).
