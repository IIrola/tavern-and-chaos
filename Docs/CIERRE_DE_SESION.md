# Cierre de sesión

Política definida antes de implementar la iteración 0.6, conforme al GDD completo.

- Duraciones admitidas: 20, 30, 45 y 60 minutos. El límite se evalúa antes de aceptar acciones del siguiente paso de servidor, también en aprobación y confirmación de nuevas conexiones.
- Al alcanzar el límite se bloquean movimiento voluntario, nuevos golpes/lanzamientos, bebidas, asientos e inicios. Se cancela la carga no soltada. Un golpe o botella ya soltados y el tiro de billar en marcha terminan con sus límites existentes. No se acepta un segundo tiro durante el cierre.
- Después del tiro, un resultado de partida ya alcanzado se paga normalmente, una sola vez. Una partida inconclusa se cancela y devuelve sus apuestas; la apertura ya consumida permanece en el bar. No inventa una victoria o empate. Se liberan todos los asientos.
- Insolvencia: ninguna persona conectada puede pagar ni el precio más barato del catálogo del GDD (aperturas y bebida), sin partidas activas, apuestas, reservas, pagos o acciones físicas pendientes. Un saldo individual cero no termina la sala. Se evalúa después de liquidar para no expulsar a quien va a recuperar fichas. Las cinco actividades aún pendientes conservan sus precios, sin declararse implementadas.
- Si tiempo e insolvencia coinciden, prevalece tiempo. La cinemática de expulsión corresponde a insolvencia: seis segundos compartidos por tiempo de servidor, cámara fija y personal/participantes provisionales sin nueva simulación física. Movimiento reducido evita giros y rebotes. No hay control para adelantar el reloj de los demás.
- Al declararse el final se congela el registro de estadísticas, incluidos quienes salieron y el saldo antes de borrarlo. Después se eliminan fondos/botes; se comprueba el libro. Resultados consultables por páginas, entrada de juego bloqueada, opción de menú o crear otra sala con concesiones y estado nuevos.
- Los clientes reciben páginas finales bajo demanda y las conservan; descargan las restantes en segundo plano. Si el anfitrión se va después de recibir resultados, se mantienen las páginas recibidas y se informa de la desconexión. No se inventan páginas ausentes. La pérdida de anfitrión antes de un resultado verificable sigue cerrando la sala e informando; no hay migración.

El transporte puede permanecer abierto solo para distribuir/consultar resultados: no admite nuevas personas ni juego. El registro personal se limpia al salir explícitamente o crear otra sala. Las pruebas con reloj acelerado no equivalen a esperar 20–60 minutos reales ni validan una red entre equipos.

En 0.7 el contrato consulta billar y pulseadas. El límite de tiempo se comunica al módulo de pulseadas antes de avanzar el paso siguiente: solo resuelve el impulso ya admitido, cancela el duelo inconcluso y devuelve sus apuestas. La apertura consumida queda en el bar. Una partida de pulseadas activa o su bote pendiente impiden expulsar por insolvencia.
