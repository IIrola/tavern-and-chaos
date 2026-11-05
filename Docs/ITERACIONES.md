# Revisión de iteraciones

## 0.3.1 recibida de main

Se conserva la validación previa y la aceptación del usuario de dos copias en un equipo, documentadas en `LAN_HITO1.md` y `Evidence/pre-hito1/VALIDACION.md`. No se transforma esa aceptación en prueba entre equipos ni en aceptación de la nueva mesa.

## 0.4.0 — asientos y bola 8

**Problema:** la sala permitía interacción física, pero el billar seguía siendo un ejercicio local sin rival, turnos o consecuencias reglamentarias.

**Cambio:** coordinador autoritativo de mesa, reglas puras contrastadas con WPA/variante GDD, simulador compartido de 16 bolas y controles para anunciar/colocar/tirar. La participación se conserva al caer; el asiento y la cámara cambian por separado. Organizador, cobro y cola se resuelven en servidor.

**Criterio de aceptación:** dos lados completan una partida con tiros físicos, una tercera persona espera y entra a la siguiente; una caída durante tiro no pierde el resultado ni la participación; se rechazan colocación fuera de turno y fuera de zona; se cobran 10 una sola vez; salir/desconectarse deja una resolución utilizable. Las comprobaciones y sus límites se publican en `Evidence/pool-lan/VALIDACION.md`.

**Hallazgos y correcciones:** el ensayo inicial leía al ganador antes de la última instantánea; ahora espera el resultado replicado. La línea de salida se hace visible para evitar colocaciones por tanteo. Se muestra el reloj al estar fuera durante el turno. Las variantes no quedan solo en documentación: H/X abre reglas sin congelar a los demás. Se añadieron eventos de audio porque los clientes cinemáticos no reciben colisiones físicas locales. La reposición busca puntos próximos al punto previsto en lugar del primer punto de una cuadrícula arbitraria.

**Evaluación senior:** avance funcional hacia el corte vertical. Los resultados de reglas, procesos reales y física permiten continuar; no bastan para afirmar diversión, tolerancia a redes reales o calibración profesional. La partida automática usa tiros preparados para aislar decisiones; también se ensaya un saque con el triángulo normal de quince. Falta jugar sin guía, medir entre equipos y bajo latencia/pérdida, cubrir ausencia real e interrupciones variadas, apuestas y equipos si se incorporan. Las otras cinco actividades, recetas y economía completa siguen en el alcance, con su estado explícito en el registro de requisitos.

**Resultado final de la iteración:** 48 comprobaciones de reglas, 67 + 65 entre procesos y 86 de regresión local aprobadas. Se corrigió también la orientación de la réplica propia al sentarse, y se añadió reposición sin recobro si una mesa abierta queda sin bolas de color ni anuncio legal posible. Los ensayos virtuales se aislaron del foco/cursor y esperan el estado replicado antes de actuar. La compilación terminó en `Succeeded`; el contador incoherente de errores de Unity queda registrado como anomalía, no se sustituye por cero. Los límites de hardware, red y aceptación humana permanecen abiertos.
