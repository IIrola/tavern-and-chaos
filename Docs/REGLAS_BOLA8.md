# Bola 8 de la taberna

Fuente fijada antes de implementar: [World Pool-Billiard Association, Rules of Play, vigencia 15-09-2025](https://wpapool.com/wp-content/uploads/2025/09/2025.09.15-WPA-Rules.pdf), sección 4 y reglas generales de contacto, anuncio y faltas. Consulta: 04-11-2026. El contrato aprobado en `GDD_y_plan_de_desarrollo.md` prevalece donde difiere.

## Diferencias deliberadas

- Primer saque aleatorio en lugar de arrime.
- Saque insuficiente: el rival acepta o pide repetir al sacador original. Se omite la tercera opción WPA de sacar él mismo.
- La 8 embocada en saque siempre se repone. No se ofrece repetir por ese motivo. Si además hay falta, blanca en mano tras la línea.
- Una bola de color saltada se repone; WPA normalmente la mantiene fuera.
- En mesa abierta nunca se permite primer contacto con 8, incluso si un grupo ya desapareció. Se omite la excepción WPA de reclamación temporal.
- Sin reloj mientras se está sentado. La ausencia durante el turno solo expulsa al superar 60 segundos, regla particular de la taberna.
- Los comandos de personas sin turno se rechazan sin mover bolas. La falta física de jugar fuera de turno no es posible mediante los controles disponibles.

## Decisiones para casos no cerrados en el GDD

Primera entrega reglamentaria: dos personas, un lado por persona. Equipos pendientes. Sin apuestas opcionales aún; abrir cuesta 10 fichas al organizador una vez confirmadas dos personas sentadas. Antes de iniciar se informa de precio y reglas.

La línea de salida está a un cuarto del largo desde el extremo inicial. Blanca en mano de saque: centro estrictamente detrás de ella. Tras una falta de saque, el primer objeto debe estar más allá de la línea; se permite alcanzarlo mediante bandas. El saque inicial también permite colocar blanca detrás de la línea. El tiro normal exige anunciar una bola propia elegible y una tronera; para jugar defensivo puede anunciarse una bola sin embocarla, conservando la obligación de contacto y banda.

Saque insuficiente con falta: el rival decide aceptar con blanca en mano tras línea o repetir el saque original. La 8 fuera durante el saque se repone como excepción de saque; en tiro normal pierde. Tras un saque legal sin emboque cambia el turno. Ningún saque asigna grupos.

Las bolas de color embocadas con falta permanecen fuera. La elegibilidad de tirar a la 8 se evalúa **antes** del tiro: embocar la última propia y la 8 juntas pierde. Ante contacto simultáneo legal e ilegal en el mismo paso físico, se concede contacto legal; los eventos no se resuelven por el orden de los mensajes de colisión. Una recuperación forzada por el límite de 18 segundos se declara falta, visible para ambos.

Si todas las bolas de color desaparecen sin que se haya asignado un grupo, la prohibición aprobada de contactar primero la 8 dejaría la mesa sin tiro legal. Se declara mesa bloqueada y se repite el saque original sin volver a cobrar apertura; se conservan participantes y asientos. Así se mantiene la prohibición del GDD y una salida equivalente a la reposición por bloqueo de WPA 4.11.

Salir de la sala o superar la ausencia concede la partida al rival que siga en la sala. Si hay un tiro en marcha se resuelve primero; un resultado legal ya conseguido conserva prioridad si su ganador continúa conectado. Si nadie continúa, resultado sin ganador. La caída libera el asiento, conserva la participación y no interrumpe el tiro. Al finalizar, el organizador puede abrir revancha por el mismo precio; quienes esperan ocupan asientos libres en orden de solicitud al cerrar la mesa.

Las reglas puras, la simulación y el transporte se validan por separado. Este documento no es prueba de implementación ni de validación completa del GDD.
