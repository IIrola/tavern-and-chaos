# Economía de la sesión

Se aplica la economía aprobada del GDD: fichas ficticias, 1.000 una vez por IP/sala, apertura separada del bote, bebidas al servir, sin recarga. El servidor conserva saldos, botes y estadísticas en memoria. La IP no se publica en las vistas ni se considera identidad fiable de una persona.

## Decisiones de esta iteración

En billar, el organizador propone una apuesta igual por lado, incluido cero. Ambos confirman el desglose de apertura/apuesta antes de iniciar. Una revisión identifica la propuesta; cambiar importe, asientos u organizador invalida confirmaciones anteriores. Estar en espera no cobra nada. El organizador inicia solo con dos personas sentadas, confirmadas y con saldo suficiente.

La apertura y las apuestas se reservan en una única operación. Si falta saldo o participantes, no cambia ningún saldo. Si falla el inicio, se devuelve lo reservado. Al activarse la partida, la apertura pasa al bar y las apuestas quedan en el bote. Repetir el saque o reponer por bloqueo no cobra otra apertura.

El ganador de billar recibe el bote completo. La salida de la sala o la ausencia de más de 60 s durante el turno se resuelven como pérdida conforme al reglamento de mesa. Se termina primero el tiro ya iniciado. Si no queda ganador elegible, se devuelven apuestas a quienes sigan presentes y se destruyen las de quienes salieron. En un empate se devuelve cada apuesta; la apertura permanece en el bar. Cancelar una partida ya iniciada devuelve apuestas, pero no apertura.

El resultado se fija al liquidarse: una salida posterior destruye el saldo disponible, incluido el premio, y no reparte el mismo bote otra vez. Una expulsión conocida antes de liquidar excluye al jugador de la adjudicación. No hay transferencia retrospectiva del premio ya liquidado.

## Contabilidad y estadísticas

Invariante: concesiones iniciales + beneficios emitidos por banca = saldos disponibles + reservas de apertura + apuestas en custodia + ingresos del bar + apuestas perdidas ante la banca + fichas destruidas. Los importes son enteros; se rechazan desbordamientos y no se admiten saldos negativos. Las operaciones y resultados aplicados tienen identificadores para impedir pagos o cobros repetidos.

La estadística de ganancia/pérdida de apuestas usa el beneficio neto del resultado: recuperar la apuesta propia no es ganancia. Los costes de apertura/bebida se muestran aparte. Una partida ganada sin apuesta cuenta como victoria, con beneficio cero. Se conserva el historial de conexiones que abandonaron, sin fusionar personas distintas por compartir IP.

La contabilidad contempla la banca infinita para blackjack: devolución de apuesta propia más beneficio 1:1 o 3:2, sin descontar ese beneficio de un saldo limitado del bar. Esto no implementa todavía el juego ni su reglamento. Los casos de cierre por tiempo/insolvencia, la presentación final y las demás actividades se verifican aparte; esta base no se declara economía completa del GDD por sí sola.
