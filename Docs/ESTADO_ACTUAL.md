# Estado y siguiente trabajo

Rama de trabajo: `main`. Base recibida `b89a6d1` (0.3.1), conservando primera persona, reinicio individual, puntos libres, posturas físicas replicadas y eventos de impacto. El trabajo anterior a esa base permanece en `work/lan-foundation`, commit `c79d8cb`.

## Iteración 0.5.0: apuestas y cuentas de sala

La bola 8 de 0.4 mantiene asientos, cola, reglas, física autoritativa y participación al caer. Ahora el organizador propone una apuesta igual por lado, incluido cero. Ambos confirman un desglose; cambiar propuesta o asientos cancela esas confirmaciones. Solo se cobra al empezar correctamente: apertura de 10 al organizador y apuesta a cada lado. Una apertura fallida devuelve todo lo reservado.

El libro del servidor separa saldos, reservas, botes, gastos de servicios, beneficios de banca y fichas destruidas. No acepta saldo negativo ni pagos repetidos. Una salida destruye disponible y mantiene la apuesta comprometida hasta resolver. El resultado muestra bote, pago y beneficio neto. Las estadísticas bajo demanda incluyen conexiones que salieron, con seis filas por página; las IP no se publican.

`Assets/Resources/EconomySettings.asset` permite editar los precios previstos del GDD sin guardar en él el estado de partida. La banca de blackjack se ha probado como contabilidad pura; el juego sigue pendiente. Las reglas y decisiones económicas se describen en [ECONOMIA_DE_SESION.md](ECONOMIA_DE_SESION.md).

Las evidencias actuales están en `Evidence/economy/`; las de 0.4 se conservan en `Evidence/pool-lan/`. Las partidas automáticas usan tres procesos Mac, UDP loopback y posiciones preparadas que aíslan tiros físicos reales. No acreditan una sesión humana, una sala de seis ni una red entre equipos. Windows de 0.3.1 conserva evidencia histórica; esta entrega requiere nueva validación allí.

## Lo que sigue, en orden

1. Cierre por tiempo e insolvencia: bloquear nuevas entradas/inicios, resolver tiros/pagos pendientes, mostrar estadísticas finales y cinemática compartida. Hoy el cierre por tiempo todavía vuelve al menú; no se declara fin económico completo.
2. Sesiones libres de billar entre 2–4 personas/equipos: comodidad de apuntado, descubrimiento de controles, latencia/pérdida, ausencia real de un minuto, continuidad de revancha y equilibrio de potencia/fricción. Probar también una sala real de seis y paginación con historial largo.
3. Preparación de cócteles con ingredientes, oferta/lanzamiento, cinco recetas, ebriedad 0–5 y acumulación/restauración de efectos, incluidos sentados.
4. Blackjack, Hold’em con botes laterales, dominó, Ve a Pescar y pulseadas. Fijar fuente oficial/variante antes de cada implementación. Reutilizar asientos y libro, preservando reglas y vistas privadas propias.
5. Accesibilidad y configuración: inversión, reasignación, tamaño de texto, navegación completa, canales de audio, interfaz responsive y ajustes versionados. Medir Mac/Windows con cuatro/seis personas y decidir LTS/URP con evidencia.

El alcance íntegro permanece en los [85 criterios](REQUISITOS.md) y [su registro de evidencias](REQUISITOS.json). El arte final sigue diferido; el GDD no está terminado.
