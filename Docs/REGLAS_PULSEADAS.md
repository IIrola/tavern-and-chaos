# Pulseadas de la taberna

Variante del GDD confirmada como diseño objetivo: dos personas, 15 segundos, marcador −100..100, ventaja al terminar o extremo alcanzado, cero empate; resistencia, ritmo, alternativa de mantener y consumo doble por ebriedad. Decisiones de esta implementación, fijadas antes de programar:

## Fuente y diferencias

Referencia contrastada: [WAF Rules 2026](https://www.waf-armwrestling.com/wp-content/uploads/2026/10/2026-WAF-Rules.pdf), apartados 5.2/5.3. WAF regula preparación y contacto con la almohadilla; 5.3.5 no limita la duración de un combate. El GDD exige 15 s, marcador y ritmo: aquí rige esa variante explícita, no se anuncia una reproducción completa del torneo WAF. La alineación inicial y la señal de comienzo se representan; el control de agarre, faltas físicas y categorías deportivas no se simula.

## Ritmo y resistencia

Tras confirmar ambos, hay cuenta atrás de 3 s. El duelo dura 15 s efectivos y tiene 20 impulsos, cada 0,75 s; la ventana admite ±0,15 s en tiempo recibido por el servidor. Un impulso por lado y pulso. No se usa la hora declarada por cliente. Ambos lados se resuelven juntos antes de comprobar victoria; el orden de mensajes no decide un empate.

Mantener es el modo inicial accesible: el servidor genera como máximo un intento al coincidir la pulsación mantenida con cada ventana, con latido de conexión corto. Ritmo manual usa una pulsación por ventana. Ambos producen la misma fuerza y coste. Los intervalos entre impulsos cuentan como descanso en ambos modos; no se castiga mantener por una diferencia mecánica de recuperación. Fuera de ritmo, los intentos manuales consumen resistencia, limitados a uno cada 0,2 s; repetir una intención o el mismo pulso no aporta fuerza extra.

Resistencia 0–100. Recupera 12 por segundo fuera del breve intervalo efectivo de esfuerzo (0,12 s). Fuerza suave/normal/fuerte: 6/9/14 por acierto, coste 7/12/19. Fallar manualmente cuesta 4. Ebriedad >0 duplica los costes, nunca la fuerza. Sin resistencia suficiente no hay fuerza; se indica descansar. Son valores de ajuste pendientes de sesiones humanas. El modo y la fuerza se pueden cambiar sin coste monetario; la fuerza elegida afecta el siguiente impulso, no altera uno ya admitido.

## Mesa y economía

Una mesa y dos asientos; organizador, propuesta por revisión, confirmaciones de ambos, apertura 5 separada de apuesta igual opcional por lado. Precio y coste por ebriedad se muestran antes de aceptar. Cambio de apuesta, asientos o coste por ebriedad invalida confirmaciones. Mínimo dos y cobro atómico; fallo de apertura devuelve reservas. Esperar es gratis.

Una persona no ocupa ni juega dos mesas a la vez. Levantarse/caer libera asiento sin abandonar la participación: deja de aportar fuerza, puede volver y el reloj de 15 s continúa. Ambos actúan simultáneamente; un duelo de 15 s no puede alcanzar el límite común de ausencia de 60. Desconexión/expulsión antes de liquidar concede la victoria al otro conectado; si no queda nadie elegible, devuelve apuestas a cuentas presentes y destruye el retorno de las salidas. Un resultado ya pagado queda fijo. Empate devuelve cada apuesta y cuenta empate, sin inventar beneficio.

Al cerrar por tiempo se bloquean entradas nuevas, se deja resolver un impulso ya admitido y se cancela el duelo inconcluso con devolución de apuestas; apertura consumida permanece. Si el duelo ya terminó se paga normalmente. El coordinador de sesión debe consultar ambas mesas, no solo billar, antes de declarar insolvencia o cerrar el libro.

## Ebriedad de base

Se separa de Laguna Azul: la bebida de prueba suma un nivel (0–5), recupera uno cada 30 s desde la última bebida; caer o recolocarse no vuelve sobrio, cambiar de sala sí. El nivel se replica y muestra en número. Los demás incrementos se fijarán con las recetas. Balanceo solo visual ajustable a cero; el nivel 5 permite tropiezos al moverse con intervalo mínimo de 15 s. Esta base no sustituye la coctelería completa pendiente.
