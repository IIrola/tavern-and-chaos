# Seguimiento del GDD completo

85 criterios; el arte final permanece diferido por el usuario. Ninguna prueba de menor alcance acredita un requisito completo. Las evidencias históricas conservan su versión y plataforma; el trabajo actual se resume en [ESTADO_ACTUAL.md](ESTADO_ACTUAL.md).

## 1. Sala y sesión

| ID | Criterio de aceptación | Estado |
|---|---|---|
| S01 | Sala privada LAN, anfitrión-jugador, código/dirección de entrada y máximo seis participantes. | Parcial |
| S02 | Nombre, apariencia y controles antes de entrar; bienvenida y tutorial contextual. | Parcial |
| S03 | Entradas durante sesión, salida segura, reinicio al cambiar de sala, cierre al perder al anfitrión; sin migración. | Parcial |
| S04 | Duración seleccionable 20/30/45/60 minutos; cierre y liquidación de acciones/partidas pendientes. | Parcial |
| S05 | Compatibilidad macOS y Windows con juego cruzado real; teclado/ratón y mando. | Parcial |

## 2. Personaje, cámara e interacción

| ID | Criterio de aceptación | Estado |
|---|---|---|
| P01 | Caminar, correr, mirar, interactuar, agarrar/soltar, cargar/lanzar, golpear y menú. | Parcial |
| P02 | Un objetivo por mira, proximidad y línea de visión; desempate centro, distancia e identificador estable. | Pendiente |
| P03 | Estados exclusivos Libre/Agarrando/Cargando/Caído/Recuperándose/Sentado autorizados por servidor. | Parcial |
| P04 | Caída suelta objeto y cancela carga; recuperación base 3 s, punto libre y restauración de controles/cámara. | Parcial |
| P05 | Cámara en primera persona con mirada libre, sensibilidad/inversión ajustables y prevención de atravesar paredes; cámara dedicada en mesas. | Parcial |
| P06 | Cápsula verificable, animación de movimiento y aproximación física del ragdoll validada en red. | Parcial |

## 3. Golpes y objetos

| ID | Criterio de aceptación | Estado |
|---|---|---|
| C01 | Golpe cargado hasta 1 s, ventana 0.25 s, validación de orientación/obstáculos/alcance y un impacto. | Parcial |
| C02 | Acierto derriba rival, fallo derriba atacante, golpes simultáneos empatan; sin daño ni muerte. | Parcial |
| C03 | Personas sentadas pueden recibir golpe y caer; caída termina duelo sin expulsar de sala. | Pendiente |
| C04 | Un poseedor por objeto; reclamaciones simultáneas, liberación e impulso único limitados por servidor. | Parcial |
| C05 | Botella y silla de masa/colisión distintas; impacto >5 m/s, CCD y un derribo por trayectoria. | Parcial |
| C06 | Roturas/reposición acotadas, limpieza y ningún fragmento físico ilimitado. | Parcial |
| C07 | NPC ocioso/invitado/preparándose/ataque/caída/recuperación, reacción y puntería configurables en servidor. | Pendiente |

## 4. Coctelería y efectos

| ID | Criterio de aceptación | Estado |
|---|---|---|
| B01 | Estación exclusiva, recipiente, hasta cuatro ingredientes con proporciones, agitar, servir, consumir/ofrecer/lanzar. | Pendiente |
| B02 | Laguna Azul: vodka/curazao/hielo, fricción cero y luz azul. | Parcial |
| B03 | Volcánico: tequila/picante/Tabasco, fuego de boca y onda expansiva sobre NPC próximos. | Pendiente |
| B04 | Mezcla Inestable: absenta/energética/cerveza, velocidad 150% 5 s y caída hacia atrás. | Pendiente |
| B05 | Helio: ginebra/carbonatada misterio, gravedad 20% durante 15 s y restauración correcta. | Pendiente |
| B06 | Cuatro licores fuertes: K.O. inmediato 10 s con humo. | Pendiente |
| B07 | Bebida consumida o lanzada aplica efecto una sola vez; también sobre sentados y derribo cuando proceda. | Pendiente |
| B08 | Ebriedad entera 0–5, balanceo y estabilidad por niveles; tropiezos nivel 5 separados al menos 15 s. | Parcial |
| B09 | Efectos con identificador/origen/inicio/duración: renovar mismo, combinar distintos con orden y límites. | Parcial |
| B10 | Restaurar modificadores al caducar/caer/salir/desconectarse; no desplazar voluntariamente a sentados. | Parcial |

## 5. Contrato de mesas

| ID | Criterio de aceptación | Estado |
|---|---|---|
| T01 | Una mesa por juego, identificador/capacidad/asientos/reglas/partida; Disponible→Preparación→En curso→Resultado. | Parcial |
| T02 | Sentarse requiere proximidad, asiento libre y estado elegible; soltar objeto, guardar retorno y cambiar cámara local. | Parcial |
| T03 | Entrar a partida solo al comenzar; lista de espera; participante puede levantarse y volver si hay asiento. | Parcial |
| T04 | Solo corre ausencia si coincide con turno: >60 s expulsa de mesa, volver cancela y nueva salida reinicia. | Parcial |
| T05 | Sin límite de turno sentado; caída no abandona la partida, resuelve primero el tiro ya iniciado. | Parcial |
| T06 | Organizador primer asiento y transferencia al siguiente; resultado, revancha y salida. | Parcial |
| T07 | Rechazar fase/turno/asiento/parámetros/secuencia indebidos sin mutación; vistas públicas y manos privadas. | Parcial |

## 6. Votación

| ID | Criterio de aceptación | Estado |
|---|---|---|
| V01 | Cualquier jugador propone expulsión; objetivo no vota, >mitad de participantes originales presentes, plazo 15 s. | Parcial |
| V02 | Entradas nuevas no votan ni cuentan; salidas invalidan votos y recalculan mayoría; salir objetivo cancela. | Parcial |
| V03 | Expulsado puede volver; reasignación de victoria al siguiente y casos sin siguiente/empates definidos. | Pendiente |

## 7. Billar bola 8

| ID | Criterio de aceptación | Estado |
|---|---|---|
| G01 | Reglamento fuente elegido y diferencias del proyecto visibles; dos lados, quince bolas y blanca, seis troneras. | Completo |
| G02 | Triángulo con 8 centro, lisa/rayada en esquinas; primer turno aleatorio. | Completo |
| G03 | Saque legal: color embocado o cuatro bolas en banda; saque inválido permite aceptar o repetir. | Parcial |
| G04 | Blanca en saque: bola en mano tras línea; 8 en saque se repone, con falta se aplica bola en mano tras línea. | Parcial |
| G05 | Mesa abierta tras saque; primer emboque anunciado legal asigna grupos; no contactar primero 8 en mesa abierta. | Parcial |
| G06 | Anunciar bola y tronera; primer contacto propio o 8 al limpiar grupo, luego banda o emboque; conservar/cambiar turno. | Parcial |
| G07 | Faltas de blanca/fuera/primer contacto/ausencia de contacto o banda/fuera de turno; bola en mano y reposición de saltadas. | Parcial |
| G08 | Ganar con 8 legal anunciada tras grupo; perder por 8 prematura/otra tronera/falta/fuera. | Parcial |
| G09 | Servidor simula y replica bolas; solo intenciones, reposo sostenido, límite de recuperación y cortes/desconexiones seguros. | Parcial |
| G10 | Práctica local separada de partida oficial; físicas verificadas y ebriedad no altera dirección confirmada a escondidas. | Parcial |

## 8. Blackjack

| ID | Criterio de aceptación | Estado |
|---|---|---|
| BJ01 | 2–5 contra crupier, 52 cartas sin comodín, mezcla servidor, dos cartas y carta de crupier oculta. | Pendiente |
| BJ02 | As 1/11, figuras 10, pedir/plantarse/pasarse; crupier pide ≤16 y planta ≥17 incluido blando. | Pendiente |
| BJ03 | Natural superior a 21 multicarte, empate de naturales; sin split/doble/seguro/rendición inicial. | Pendiente |
| BJ04 | Apuesta par ≥26, normal 1:1/natural 3:2/empate devolución; banca infinita y tratamiento de ausencia/desconexión. | Pendiente |

## 9. Póker

| ID | Criterio de aceptación | Estado |
|---|---|---|
| PK01 | 2–6, Texas Hold’em, baraja 52, dos privadas/cinco comunitarias y preflop/flop/turn/river. | Pendiente |
| PK02 | Evaluación mejor cinco, jerarquías y desempates; ciegas, entrada y saldo cero definidos. | Pendiente |
| PK03 | Fold/check/call/bet/raise/all-in solo legales; mínima subida según última completa. | Pendiente |
| PK04 | Botes laterales por contribución, empates y ficha indivisible a izquierda del botón; conservación exhaustivamente probada. | Pendiente |
| PK05 | Sin apuestas de sesión usa fichas de juego no convertibles; privacidad y liquidación de ausentes/desconectados. | Pendiente |

## 10. Dominó

| ID | Criterio de aceptación | Estado |
|---|---|---|
| D01 | 2–4, doble seis 28, siete por persona; pozo solo a dos; inicio doble mayor o mayor suma. | Pendiente |
| D02 | Extremos legales, robar hasta jugar/vaciar y pasar; victoria por mano vacía o bloqueo con menor suma y empate compartido. | Pendiente |
| D03 | Reglamento fuente, disposición no determina legalidad, tratamiento de ausencia/desconexión. | Pendiente |

## 11. Ve a Pescar

| ID | Criterio de aceptación | Estado |
|---|---|---|
| F01 | 2–5, 52 cartas, siete con 2/3 o cinco con 4/5; pedir rango propio a rival, entregar todas de ese rango. | Pendiente |
| F02 | Acierto repite, fallo roba y termina salvo rango pedido; cuatro cartas forman libro público. | Pendiente |
| F03 | Mano vacía roba al comenzar si hay mazo; final manos/mazo vacíos, mayor libros y empate; ausencia definida. | Pendiente |

## 12. Pulseadas

| ID | Criterio de aceptación | Estado |
|---|---|---|
| A01 | Dos aceptan duelo de 15 s, marcador −100..100, victoria por extremo/ventaja y cero empate. | Parcial |
| A02 | Ritmo y resistencia con recuperación, alternativa mantener sin machacar; ebriedad duplica coste comunicado. | Parcial |
| A03 | Servidor valida tiempos/límites, levantarse no abandona; desconexiones y liquidación definidos. | Parcial |

## 13. Economía, sesión y estadísticas

| ID | Criterio de aceptación | Estado |
|---|---|---|
| E01 | Libro servidor, 1000 fichas una vez por IP/sala; salida destruye disponible, apuesta sigue en bote; sin recarga. | Parcial |
| E02 | Aperturas: billar 10, blackjack 10, póker 10, dominó 5, pesca 5, pulseadas 5; bebida 20 al servir. | Parcial |
| E03 | Organizador paga apertura una vez, apuestas separadas, mínimo participantes antes de cobrar y devolución si falla apertura. | Parcial |
| E04 | Apuesta opcional salvo blackjack; precio/efecto mostrado; botes conservados y emisiones de banca infinita registradas. | Parcial |
| E05 | Fin por tiempo o nadie puede pagar, esperando resoluciones/retornos; bloquear nuevas acciones y liquidar sin fuga de fichas. | Parcial |
| E06 | Estadísticas servidor de victorias/empates/apuestas/fichas/caídas/bebidas, incluidos salidos, una victoria por mesa. | Parcial |
| E07 | Cinemática compartida de expulsión del bar por insolvencia, fin y opción nueva sala/menú. | Parcial |

## 14. Arquitectura y calidad

| ID | Criterio de aceptación | Estado |
|---|---|---|
| Q01 | Unity y paquetes fijados con licencia y backend; decisión LTS y URP explícita, build repetible con resultado inspeccionado. | Parcial |
| Q02 | Servidor anfitrión autoritativo, propiedad/estado/turno/frecuencia/secuencias; ninguna mano secreta enviada a ajenos. | Parcial |
| Q03 | Replicación/interpolación y validación de entrada; objetos/billar solo simulados en servidor, latencia/jitter/pérdida medidos. | Parcial |
| Q04 | Módulos Core/Player/Interactions/Bar/Tables/juegos/Presentation/Content; reglas puras y configuración ScriptableObject. | Parcial |
| Q05 | Ajustes locales versionados, valores por defecto si inválidos, sin guardar secretos ni analíticas por defecto. | Parcial |
| Q06 | Taberna 24×18 aprox., pasillos 2 m y actividades legibles; low poly rockera; modelos finales diferidos por el usuario. | Parcial |
| Q07 | Audio maestro/música/ambiente/efectos/UI separados, variantes y señales visuales; canciones aportadas manualmente después. | Pendiente |
| Q08 | UI responsive, teclado/mando completos, foco, texto ampliable, contraste, sensibilidad, inversión, cámara, reasignación y reducción de efectos. | Parcial |
| Q09 | Probar 16:9, 16:10 y ultrawide; controles temporizados con margen y alternativa mantener. | Parcial |
| Q10 | Pruebas reglas/propiedades/contabilidad/privacidad/red/física/accesibilidad y sesiones sin guía con nuevas personas. | Parcial |
| Q11 | Medir objetivo 60 FPS en Mac/Windows a cuatro y seis personas; CPU/GPU/cuerpos/red/correcciones registrados. | Pendiente |
| Q12 | Corte vertical probado 2–4: entrar, moverse, bebida, partida billar completa y salir sin bloqueo ni posesión/asiento duplicados. | Pendiente |
