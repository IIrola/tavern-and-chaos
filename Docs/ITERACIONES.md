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

## 0.5.0 — consentimiento, apuestas y estadísticas

**Problema:** una apertura de mesa debitaba un saldo aislado; faltaban reservas, bote, liquidación y una explicación del resultado económico. Iniciar por decisión de una sola persona también habría permitido comprometer fichas ajenas sin confirmar.

**Cambio:** libro puro del servidor, configuración estática en ScriptableObject y propuesta de apuesta con revisión. Ambos lados confirman su coste antes de iniciar; cambiar importe/asientos borra el consentimiento. La reserva es atómica y se revierte si falla el inicio. El premio y las estadísticas distinguen devolución de apuesta propia, beneficio, pérdidas y servicios. El historial conserva a quienes abandonaron, y las páginas bajo demanda no transportan IP.

**Criterio de aceptación:** ningún cobro al confirmar o ante saldo insuficiente; fallo de apertura devuelve ambos importes; cambios invalidan confirmaciones; una partida física paga el bote una vez; revancha sin apuesta no emite premio; salida conserva historia y no redistribuye un resultado ya pagado. La integración usa Enter mediante el sistema de entrada real, mensajes de red y física del anfitrión.

**Evaluación senior:** la decisión económica ya es comprensible antes de jugar y el premio es trazable después. Esto reduce disputas en una experiencia social donde levantarse, caer o perder conexión son parte del diseño. Las pruebas programadas son una barrera contra duplicaciones y fugas, pero no acreditan que personas nuevas entiendan la propuesta ni que los importes estén equilibrados. Los paneles se hicieron opacos y muestran saldo para evitar que la decoración oculte información útil. El cierre de sesión, la paginación con historial largo, interrupciones monetarias entre equipos, las demás mesas y el servicio completo de bebidas siguen pendientes.

**Prioridad siguiente:** cerrar tiempo/insolvencia con resultados compartidos antes de ampliar el número de actividades que pueden mantener botes pendientes. Después, probar billar sin guía y continuar cócteles y blackjack. La revisión mantiene todo el GDD en alcance y los modelos provisionales.

**Hallazgos de validación:** la inspección de pasos reveló errores TypeDB reales aunque Unity anunciaba éxito. Tras una limpieza inicial reaparecieron; se localizaron copias numeradas y byte a byte idénticas de los metadatos en `Library/BuildPlayerData`. Regenerar solo esa carpeta produjo cero errores sin tocar Assets ni el resto de la caché. El script de construcción ahora conserva esos metadatos fuera del proyecto y los regenera; el menú rechaza duplicados detectados y cualquier error de pasos/resumen. No se atribuye el origen de las copias a un servicio sin evidencia.

El ensayo virtual también detectó selección del cursor físico; se restringen sus mapas a los dispositivos virtuales y se comprueba la acción resultante. En la revancha, el coordinador esperaba ahora la revisión exacta de la propuesta antes de enviar la apuesta cero. Estas correcciones conservan las protecciones del juego, en vez de permitir clics sobre UI o confirmaciones antiguas.

**Resultado final:** 38 comprobaciones del libro, 48 de reglas/asientos, 162 entre procesos (dos salas de tres), 86 de regresión y 14 de entrada aislada aprobadas; cero errores de ejecución. Construcción y pasos con cero errores, una advertencia de soporte Intel futuro. Referencias/GUID: 72 archivos sin incidencias. Cobertura y límites en `Evidence/economy/VALIDACION.md`.

## 0.6.0 — cierre y resultados compartidos

**Problema:** al agotarse el tiempo se abandonaba al menú y no había cierre colectivo por falta de fichas. Esto podía ocultar resultados y dejar sin explicación el destino de una apuesta o de un tiro ya lanzado.

**Cambio:** reloj puro con fases abiertas, resolución, expulsión y resultados. El servidor corta acciones antes de procesarlas al límite, espera los objetos/tiros soltados, paga resultados alcanzados y cancela con devolución las partidas inconclusas. La insolvencia espera los premios. El registro final se congela antes de borrar fondos; las páginas se conservan en cliente, incluidos salidos/retornos, y una nueva sala reinicia estado. La presentación tiene seis segundos por reloj de servidor y alternativa de movimiento reducido.

**Criterio de aceptación:** un cierre con bote pendiente termina sin fuga ni premio inventado; una 8 legal durante el cierre paga una vez; saldo recuperado evita la expulsión; solo después de agotarse vuelve a empezar la cinemática. Nuevos intentos de entrada y comandos crudos quedan rechazados. Pausa/ajustes no congelan el reloj ni absorben la navegación final. Historial de ocho conexiones se comparte en dos páginas y sigue accesible al salir host. Crear sala y Escape funcionan mediante entrada real cuando hay interfaz.

**Evaluación senior:** el final deja de romper la experiencia social: cada persona puede leer lo ocurrido y conservar el registro recibido. Separar el cierre de la pausa evita que el anfitrión congele o pierda un pago por estar en un menú. La cámara fija y la opción reducida permiten usar el mismo reloj sin imponer giros de cámara a todos. El humor y la duración todavía requieren sesiones humanas; una prueba programada no demuestra que la espera de seis segundos resulte agradable.

La revisión visual corrigió encabezados partidos y el encuadre que mostraba otro escenario detrás. El ensayo de capturas ahora espera un fotograma dibujado y el archivo antes de pedir otra imagen; así se conserva cada página que se está verificando. La navegación final no modifica ajustes que quedaron abiertos detrás.

**Límites relevantes:** solo el billar ofrece todavía una partida de mesa integrada. El umbral del GDD contempla aperturas de 5 que aún no tienen actividad, por lo que falta cerrar ese hueco funcional con pulseadas y las otras mesas. Las pruebas de cierre usan reloj acelerado y, para la 8 ganadora, un historial de reglas preparado antes del tiro físico final. Faltan redes entre equipos, seis personas, host perdido durante la cinemática, controles físicos y aceptación humana. No se declara completo el fin económico de todas las actividades ni el GDD.

**Prioridad siguiente:** pulseadas con apertura 5 y el mismo contrato de sesión; después cócteles y los restantes juegos. Mantener las pruebas de cierre al añadir cada módulo para que ninguna mesa nueva quede fuera de la espera de pagos.

**Resultado final:** 20 comprobaciones de cierre, 38 del libro, 48 de reglas/asientos, 140 en recorridos de cierre, 80 de partida completa y 86 de regresión local aprobadas. Construcción con cero errores en resumen/pasos; referencia/GUID sin incidencias en 83 archivos. Evidencia en Evidence/session-end/VALIDACION.md.
