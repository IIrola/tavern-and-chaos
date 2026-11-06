# Cambios

## 0.7.0 · Pulseadas, resistencia y segunda mesa

- Dos participantes confirman apertura 5 y apuesta antes de jugar; cuenta atrás 3 s, duelo 15 s y resolución simultánea del marcador.
- Mantener y ritmo manual con igual fuerza, coste y recuperación; tres esfuerzos, resistencia visible y feedback de acierto/fallo/cansancio.
- Ebriedad 0–5 independiente de Laguna Azul: coste doble comunicado antes de consentir, réplica y sobriedad de base. La coctelería completa sigue pendiente.
- Salir conserva participación y reloj, se impide jugar dos mesas; desconexión, cola y pago único. Cierre de sala consulta billar y pulseadas.
- Cámara propia, reglas accesibles, movimiento reducido y resultado económico desglosado. Protocolo 7: todas las copias deben actualizarse.
- Evidencia y límites en Evidence/armwrestling/VALIDACION.md. Cuatro juegos, recetas, accesibilidad completa y aceptación entre equipos permanecen pendientes.

## 0.6.0 · Cierre compartido de sesión

- Fin por tiempo sin cortar el tiro ni una botella ya lanzada: resultado alcanzado pagado, apuesta inconclusa devuelta y apertura conservada.
- Insolvencia evaluada después de pagos pendientes; un premio que devuelve saldo mantiene la sala abierta. Expulsión compartida de seis segundos con movimiento reducido.
- Resultados finales inmutables, incluidos salidos y saldo antes de borrar fondos, descargados por páginas y conservados tras desconectarse el anfitrión.
- Crear otra sala o volver al menú desde resultados; corte de nuevas entradas y acciones en servidor. Protocolo 6, versiones anteriores rechazadas.
- Evidencia y límites en Evidence/session-end/VALIDACION.md; el GDD completo y la validación entre equipos siguen pendientes.

## 0.5.0 · Apuestas y cuentas de sala

- Libro autoritativo de fichas con reservas, devoluciones, botes y pagos únicos; separa ingresos del bar, beneficios de banca y fichas destruidas.
- Billar con apuesta opcional igual por lado. Ambos confirman apertura/apuesta; cualquier cambio de propuesta o asientos exige confirmar de nuevo. Una apertura fallida devuelve todo lo reservado.
- Resultado con bote, premio, apuesta propia y beneficio; estadísticas paginadas incluyen a quienes salieron y separan victorias de mesa de apuestas ganadas.
- Precios del GDD en `EconomySettings`, sin guardar estado mutable en el asset. Protocolo LAN 5; todas las copias deben actualizarse.
- Validación y límites en `Evidence/economy/VALIDACION.md`. El cierre de sesión por tiempo/insolvencia y los otros cinco juegos siguen pendientes.

## 0.4.0 · Bola 8 compartida en desarrollo

- Se continúa desde `main` 0.3.1 conservando primera persona, cuerpos replicados y efectos de impacto.
- Contrato común de mesas aplicado a billar: asientos próximos y libres, organizador, apertura de 10 fichas una vez, cola, retorno tras caída y preparación de siguiente ronda.
- Bola 8 con quince bolas y blanca, anuncios, grupos, faltas, colocación, decisiones de saque y resultado. Reglas puras y variantes WPA/GDD visibles en la mesa.
- El anfitrión simula; los demás reciben bolas cinemáticas interpoladas y eventos de audio. Los clientes envían intención de tiro/colocación y no pueden fijar resultados.
- Una caída conserva la participación y deja terminar el tiro. La ausencia solo cuenta durante el turno; sentarse cancela el minuto. Salida y desconexión tienen resolución definida.
- Pruebas de reglas, dos recorridos con tres procesos y regresión local; alcance y límites en `Evidence/pool-lan/VALIDACION.md`. No acredita aún LAN entre equipos ni completa el GDD.

## 0.3.1 · Primera persona y demo LAN (recibida de main)

- Integración de la demo local con la sala LAN: movimiento, golpe, agarre/lanzamiento de botellas y Laguna Azul, con autoridad en el anfitrión.
- Cámara en primera persona tanto en local como en LAN: mirada hacia arriba, abajo y a los lados, cuerpo orientado con la vista y una cámara propia para cada jugador. Los lanzamientos siguen la dirección vertical y horizontal de la mirada; el billar conserva su cámara cenital de práctica local.
- Corrección del reinicio manteniendo movimiento: R restablece posición, orientación, cámara e inercia. En LAN solo recoloca al jugador que lo solicita; no reinicia la sala ni cancela una caída o un golpe ya soltado.
- Laguna Azul: en LAN cuesta 20 fichas y su efecto dura ocho segundos; repetirla renueva la duración. Mientras está activa, un choque frontal de al menos 3 m/s contra pared o mueble provoca un tropiezo y caída física. El suelo, los roces laterales y empujar lentamente una pared no activan la caída.
- Al caer se suelta la botella, se cancela la carga pendiente y se elimina el efecto de la bebida. La recuperación comienza tras tres segundos y busca un punto libre para volver a estar de pie.
- Corrección de cuerpos desplazados y pies/giros incorrectos en el invitado: se sincronizan las posiciones y rotaciones de las seis partes del cuerpo sin que la interpolación de PhysX sobrescriba la de red. Incluye al anfitrión, al propio invitado y a Bruno.
- Las caídas del invitado muestran la postura física simulada por el anfitrión, en lugar de una inclinación aproximada que podía dejar a Bruno flotando. La botella agarrada sigue la mano del dueño representado en pantalla.
- Partículas y sonido de impactos de puños y botellas en ambas copias, con eventos identificados para evitar repeticiones. También se comunican los sonidos de agarre, lanzamiento, bebida y resbalón.
- Pausa local en LAN, posesión exclusiva de botellas, liberación al salir y limpieza de efectos al volver a entrar. La concesión de 1.000 fichas se mantiene una sola vez por IP y sala; una sala nueva reinicia el registro.
- Compilación Windows 0.3.1 y lanzador `Probar-LAN.cmd` para abrir dos copias en un equipo. Protocolo de sala 3: todas las copias deben usar la misma versión; las antiguas deben cerrarse antes de volver a probar.
- Compilación macOS 0.3.1 universal (Intel y Apple Silicon), con macOS 12.0 como mínimo, ZIP con permisos de ejecución e instrucciones de apertura. Generada desde Windows; ejecución en Mac y juego cruzado pendientes. El resultado de compilación se guarda por separado para conservar la evidencia de la release 0.2.0.
- Exclusión de informes de migración, configuración local de Visual Studio, metadatos locales de skills y ensayos intermedios. Se conservan en Git los informes finales de validación, el código y sus `.meta`, paquetes y configuración del proyecto.

Validación: 23 comprobaciones locales, 54 de integración por loopback y 115 de cuerpos, caídas, cámara y efectos aprobadas. Tras repetir la prueba, el usuario confirmó que las correcciones funcionan. Detalle técnico y límites en [Evidence/pre-hito1/VALIDACION.md](Evidence/pre-hito1/VALIDACION.md); recorrido y conexión en [Docs/LAN_HITO1.md](Docs/LAN_HITO1.md).

Alcance: se conserva el controlador de cápsula con ragdoll pasivo al caer. Los tropiezos descritos son por choque con Laguna Azul, no un sistema general de ebriedad. Siguen pendientes las pruebas LAN entre equipos, latencia/pérdida, juego cruzado y seis personas. El billar sigue siendo local; no se han iniciado las mesas compartidas ni el Hito 2. La release macOS 0.2.0 no contiene estos cambios.

## 0.2.0 · Jugabilidad y UX

- Billar: frenada constante de rodadura y giro, masas iguales, materiales distintos para bolas y bandas, contactos más precisos y detección continua.
- Bocanas biseladas con colisión acorde al borde visible, captura de tronera por la trayectoria y caída breve al embocar. Salirse de la mesa repone la bola sin sumar puntos.
- Tiro bloqueado hasta que todas las bolas permanezcan paradas; límite de recuperación ante atascos. Blanca embocada: colocación manual en un punto válido del paño.
- Apuntado directo con cursor, guía del primer contacto, dirección inicial de la bola objetivo, rebote previsto y taco de alineación. Potencia mediante rueda, flechas o deslizador; un clic dispara.
- Ejercicios de emboque recto, corte, bandas y triángulo de seis; contador de tiros y resultado. Acceso directo desde el menú principal.
- Menús navegables con teclado y bindings de mando, foco visible, Esc pausa dentro del billar y E vuelve a la taberna. Reinicio de ejercicio con confirmación.
- Mapas de entrada separados para caminar, billar y menús; los clics en los paneles no disparan. Cambio de cámara limpio al sentarse o levantarse.
- Los golpes y lanzamientos siguen la dirección de la cámara.
- Se mantienen los personajes y el arte provisional. Los cambios de geometría en las bandas corresponden a la corrección funcional de la mesa.

Validación y límites: `Evidence/VALIDACION.md`. No incluye LAN ni reglamento competitivo de bola 8; la física es una aproximación plana sin efectos laterales ni saltos.
