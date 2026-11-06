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

## 0.7.0 — pulseadas y segunda mesa

**Problema:** solo billar ofrecía una actividad compartida; los saldos de 5–9 no tenían una apertura jugable. Ampliar mesas también podía romper un cierre que solo consultara billar.

**Cambio:** reglas puras de pulseadas, dos asientos con consentimiento revisado, apuestas y apertura 5; cuenta atrás 3 s y duelo 15 s. Mantener y ritmo tienen igual fuerza/coste/recuperación y tres esfuerzos. La autoridad valida la recepción, admite un intento por ventana y resuelve ambos lados simultáneamente. Ebriedad independiente del efecto azul, coste doble previo al consentimiento y réplica. El contrato de cierre consulta todas las mesas.

**Criterio de aceptación:** ambos aceptan el total; mantener y ritmo completan veinte ventanas y empatan con devolución exacta. Una bebida real altera resistencia tras expirar el efecto azul y el ganador recibe el bote una vez. Levantarse conserva reloj/participación, bloquea otra mesa y permite regresar; desconexión paga al rival y la persona en espera entra después desde un asiento próximo. Cerrar con saldo disponible agotado y bote pendiente espera la resolución, devuelve apuestas y entrega informes sin victoria inventada.

**Hallazgos:** una petición tardía podía cambiar modo/esfuerzo si era la primera llamada que hacía avanzar el reloj más allá del final; se volvió a comprobar la fase después de avanzar y se añadieron dos casos. El cierre usa el límite de sala al drenar, evitando convertir el pequeño retraso del paso siguiente en victoria por tiempo. El resultado distingue devolución de beneficio y el aviso no tapa el marcador. Una prueba de cola situaba a la persona junto al asiento ocupado: se corrigió el ensayo, manteniendo la proximidad exigida en juego.

**Evaluación senior:** hay un segundo bucle social completo y una actividad de bajo coste; el consentimiento y las cuentas siguen siendo comprensibles. Mantener evita machacar y no impone una desventaja de fuerza frente al ritmo. Eso no demuestra que la elección de fuerza y descanso sea divertida ni que el ritmo manual resulte atractivo: requiere personas nuevas y medición. La ventana usa tiempo de recepción del servidor; la equidad bajo latencia no está acreditada. Los modelos siguen provisionales y las cuatro actividades restantes y recetas no se recortan.

El primer arranque visible no avanzó y tuvo que terminar por plazo; otro recorrido visible empató, pero no alcanzó la comprobación conjunta de veinte impulsos y cuentas. Se separaron datos de pulsos/saldo para diagnosticar el ensayo; el siguiente recorrido verificó veinte impulsos por lado y saldos 995/1000. No se atribuye un origen al bloqueo gráfico ni se presenta esta repetición como prueba de tolerancia a redes reales. Las comprobaciones finales y sus límites se detallan en Evidence/armwrestling/VALIDACION.md.

**Resultado final:** 136 comprobaciones puras, 134 de pulseadas entre procesos, 80 de billar, 129 de cierre y 86 de regresión local aprobadas; cero errores de ejecución. Construcción y pasos con cero errores, una advertencia Intel futura y 101 archivos verificados sin incidencias de referencias. La revisión visual cubre 1440×900, 1280×720 y 1920×900. La siguiente iteración aborda coctelería manteniendo el contrato de dos mesas.

## 0.8 en desarrollo — dominio de coctelería

**Problema:** la barra solo ofrecía compra directa de Laguna Azul. Añadir cinco efectos y vasos compartidos sin separar preparación, cobro, posesión y consumo permitiría cargos sin objeto, efectos repetidos o cierre antes de una aplicación pendiente.

**Cambio de esta fase:** reglas puras de mezclas con cuatro ingredientes y proporciones, catálogo ScriptableObject con copia inmutable por sala, preparación con revisión/tiempo y un único dueño, vasos identificados y servicio ligado al libro. Servir cobra una vez sin registrar todavía consumo; quien acepta y bebe recibe ese contador. Consumir e impactar son excluyentes. El servicio conserva vuelos al cerrar y aplicaciones pendientes antes de finalizar. La caída cancela solo los efectos previstos en el modelo, con expiración de inestabilidad entregada una vez.

**Evaluación senior:** esta fase reduce riesgos de autoridad y disputas económicas antes de conectar la interacción física. Los controles y el ritmo de preparar siguen sin validación: 76 comprobaciones puras y 38 del libro prueban reglas y cuentas, no una barra divertida o utilizable. Se preservan las cinco combinaciones y las duraciones explícitas del GDD. Las cuatro bebidas fuertes producen K.O. a cualquier proporción no nula; no se introduce un mínimo que contradiga esa condición. Los porcentajes en el asset admiten el borde exacto pese al redondeo de float. La vista no podrá cobrar sin mostrar la receta/efecto; queda implementarla y probarla con personas.

**Pendiente inmediato:** integrar el servicio con red, mundo, interfaz y físicas; conectar velocidad, fricción, gravedad, K.O., fuego y humo a personajes sentados/caídos; comprobar lanzamiento/consumo y coexistencia en players reales. No hay nueva release ni aceptación de coctelería completa en esta fase. La compilación de reglas terminó con salida 0; los reintentos por plazo de revisión de permisos no iniciaron Unity y no se contabilizan como pruebas ejecutadas.

## 0.8.0 — efectos conectados y documento de hitos

**Problema:** la fase anterior verificaba modelos de datos, pero el personaje solo conocía el temporizador azul anterior. Faltaban fuerzas, limpieza por efecto y réplica de las cinco clases, además de una planificación de entregas revisable.

**Cambio final:** efectos enlazados a cápsula y ragdoll, identidad/origen/tiempos en snapshots y presentación local. Azul conserva inercia sin fricción, inestabilidad aumenta velocidad y cae al vencer, Helio compensa gravedad por cuerpo sin tocar la global, Volcánico afecta NPC y K.O. impone diez segundos. Caer cancela solo deslizamiento/inestabilidad; cerrar o salir elimina todo. Intensidad visual hasta cero y duraciones visibles. Se entrega HITOS_Y_PLAN_DE_ENTREGA.md con los hitos originales 0–6, dependencias, puertas de aceptación y siguiente orden.

**Criterio de aceptación de esta entrega:** medir velocidad/gravedad reales, comprobar recuperación y caída desde asiento, replicar los cinco efectos y eliminar todos al terminar, manteniendo billar/pulseadas/pagos/cierre. La estación completa no forma parte de la integración terminada aquí y queda como próxima entrega explícita.

**Hallazgos:** una sonda alta de Helio tocaba una viga; el registro de contactos permitió distinguir el impulso externo de la gravedad. La sonda libre mide −1,962 m/s² frente a −9,81 normal. Se corrigieron pérdida de inercia al soltar correr, efectos congelados detrás de resultados y un salto de línea literal en HUD. La instrumentación rechazó una API incorrecta durante compilación; la entrega exige cero errores de build/pasos.

**Evaluación senior:** la integración reduce riesgo físico y hace que varios efectos puedan coexistir y explicarse. Las pruebas de caída desde pulseadas protegen la separación entre asiento y participación, esencial en una taberna social. Intensidad cero se aplica localmente sin alterar las reglas de los demás. La evidencia no prueba una barra utilizable: falta preparar, servir y compartir vasos en un flujo humano real. No conviene añadir más contenido antes de cerrar ese recorrido y observar si el caos interrumpe de forma divertida o frustrante. Los modelos siguen provisionales; ni la diversión ni el objetivo de 60 FPS están acreditados.

**Entrega y límites:** versión 0.8.0 de desarrollo, protocolo 8 y paquete Mac universal. Evidencia detallada en Evidence/effects/VALIDACION.md. El documento de hitos conserva la estación completa, cuatro juegos restantes, accesibilidad, pruebas entre equipos y preparación final sin recortar alcance ni asignar fechas no validadas.
