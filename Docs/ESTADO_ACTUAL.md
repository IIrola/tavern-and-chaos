# Estado y siguiente trabajo

Base descargada: `main` en `b89a6d1` (0.3.1). Se conservaron primera persona, reinicio individual, búsqueda de puntos libres, posturas físicas replicadas y eventos de impacto. El trabajo local anterior quedó guardado en `work/lan-foundation`, commit `c79d8cb`; se recuperaron únicamente los modelos iniciales y preparación de billar compatibles con la nueva base.

## Iteración 0.4.0: bola 8 compartida

- Dos asientos confirmados por el anfitrión, organizador, apertura de 10 fichas una vez, cola para siguiente ronda y preparación de revancha.
- Anuncio de bola/tronera, grupos, faltas, bola en mano, decisiones de saque y resultado por la 8. [Reglamento y variantes](REGLAS_BOLA8.md), también visibles con H/X en la mesa.
- Quince bolas numeradas y blanca; física solo en anfitrión, réplicas cinemáticas interpoladas. El cliente transmite intenciones, nunca resultados físicos.
- Caer o levantarse libera asiento y conserva participación. El tiro iniciado termina antes de aplicar salida/desconexión. El reloj de ausencia solo cuenta durante el turno; sentarse lo cancela.
- Cámara de exploración en primera persona y cámara cenital solo para quien se sienta. Pausa/reglas bloquean entrada local mientras continúa la sala.
- Sonidos de tiro, contacto y tronera se envían como eventos del servidor.

La evidencia de esta versión queda en `Evidence/pool-lan/`. Las pruebas de partida usan procesos reales y posiciones controladas para aislar tiros; no equivalen a una partida humana sin guía. La versión nueva se ha ejecutado en Mac; la evidencia Windows de 0.3.1 se conserva por separado. No se considera terminado el GDD.

## Lo que sigue, en orden

1. Cerrar el corte vertical con juego libre de billar entre 2–4 personas/equipos: latencia, pérdida, reinicios, tiros variados, ausencia de un minuto real, experiencia sin instrucciones externas y comodidad de mandos. Revisar fricción/potencia y el saque de quince bolas con datos de partidas, no solo ejercicios preparados.
2. Extender economía a un libro de saldos/botes, apuestas opcionales y liquidación; cierre por tiempo/insolvencia y estadísticas. La apertura de billar y la bebida de prueba son solo una parte.
3. Preparación de cócteles con ingredientes, oferta/lanzamiento, las cinco recetas, ebriedad 0–5 y acumulación/restauración de efectos, incluidos sentados.
4. Blackjack después del billar; después Hold’em con botes laterales, dominó, Ve a Pescar y pulseadas. Fijar fuente oficial/variante antes de cada implementación. Reutilizar el contrato común de mesas sin sustituir las reglas de cada juego.
5. Completar configuración y accesibilidad: inversión, reasignación, tamaño de texto, navegación completa, canales de audio, interfaz responsive y ajustes versionados. Medir cuatro/seis personas en Mac/Windows y decidir LTS/URP sin perder la base jugable.

El desglose completo de 85 criterios y sus límites permanece en [REQUISITOS.md](REQUISITOS.md) y [REQUISITOS.json](REQUISITOS.json). El arte final sigue diferido; los modelos provisionales no reducen el alcance funcional.
