# Fundación LAN 0.3.0 — estado comprobado

La rama `work/lan-foundation` contiene una primera sala integrada; `main` conserva la demo local estable 0.2.0. El objetivo sigue siendo todo el GDD, no esta entrega parcial.

## Usar la sala

En el menú elige «Crear / unirse a sala LAN». Escribe tu nombre, crea una sala y comparte el código con personas de la misma red. Quien entra puede usar el código o la IPv4 y puerto. El puerto predeterminado es UDP 7777; el anfitrión mantiene la aplicación abierta. Esc pausa solo la entrada local, sin congelar a los demás. En pausa, «Jugadores y votación» abre la lista con cursor libre. Salir del anfitrión cierra la sala de todos.

En esta fundación funcionan movimiento, carga de golpe visible, acierto/fallo, caída y recuperación, botellas y una bebida de prueba. La bebida cuesta 20 fichas en la sala; el crédito inicial solo se concede una vez por IP dentro de esa sala. Una IP compartida limita el crédito de varias personas, y cambiar de IP puede eludir la identificación: se respeta la regla del GDD sin afirmar que sea una identidad segura.

Las mesas reglamentarias todavía no están integradas en LAN; «Practicar billar» sigue siendo un modo local separado. No se considera logrado el corte vertical.

## Arquitectura y versiones

- Unity 6000.6.8f1; NGO 2.13.3, Unity Transport 6.6.0, Collections 6.6.0, fijados en el bloqueo de paquetes.
- Custom messages de NGO: intenciones con secuencia y límites de frecuencia; el cliente no envía posiciones finales ni decide impactos.
- El host simula actores y objetos. Réplicas interpoladas en cliente y aproximación visual de caída sin predecir todos los huesos.
- Snapshots públicos provisionales a 20 Hz y movimiento a 25 Hz. Estas tasas no son el presupuesto definitivo: faltan medidas de latencia/jitter/pérdida.
- La economía de sala guarda IP sin puerto; las direcciones no forman parte de los snapshots públicos.

## Evidencia

`./build-demo.sh` compila y `./run-lan-checks.py --visual-host` levanta siete procesos independientes del player: host, cinco clientes aceptados y séptimo rechazado. Usa UDP real de UTP por loopback, no un transporte simulado dentro de un solo proceso.

La última ejecución aprobó **67 comprobaciones de red/sala** en los siete procesos, sin errores de ejecución. La regresión local aprobó **63 comprobaciones**. Compilación con cero errores y cero advertencias. Resultados en `Evidence/lan-foundation/summary.json`, los informes por proceso, `Evidence/regression-0.3/player-tests.json` y las capturas del lobby y la lista de seis.

Esto no demuestra una LAN entre equipos ni Windows ni una sesión con seis personas reales. Tampoco prueba aún la votación completa por varios clientes: se han validado sus reglas puras y el panel gráfico.
