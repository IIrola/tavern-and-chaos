# Integración previa al Hito 1

Unity 6000.6.8f1, Windows x64, Mono, versión de trabajo **0.3.1**. El usuario realizó una primera prueba del Hito 1 y notificó desfases de cuerpos/caídas en el invitado y ausencia de efectos de golpe. Tras compilar, verificar automáticamente y repetir su prueba, **el usuario confirmó que las correcciones funcionan**. Esta confirmación corresponde a dos copias en un equipo; no demuestra conectividad entre máquinas. No se avanzó al contrato de mesas ni al Hito 2.

## Resultados finales

- `build-windows.json`: compilación `Succeeded`, cero errores y cero advertencias.
- `local-final/player-tests.json`: **23 comprobaciones aprobadas**, cero fallos y cero errores de ejecución en el ejecutable Windows. Cámara, reinicio manteniendo movimiento, Laguna Azul, recuperación, práctica de billar, golpe, lanzamiento vertical y cancelación de carga con Q.
- `run-20261104-225520/summary.json`: **54 verificaciones aprobadas** entre dos procesos Windows por UDP loopback, cero fallos y cero errores de ejecución. Incluyen reglas de sala, entrada, aparición sin solapamiento, reclamaciones de botella, reinicio individual con confirmación, cobro/efecto, choque y caída replicados, recuperación, lanzamiento vertical, salida con objeto y retorno sin estado antiguo.
- `replication-20261104-230256/summary.json`: **115 comprobaciones aprobadas** (31 anfitrión, 84 invitado), cero fallos y cero errores de ejecución. Se comprueban las posiciones y rotaciones renderizadas de cabeza, torso, brazos y piernas de los dos jugadores y Bruno; movimiento/giro, tres ciclos de impacto/caída/recuperación de Bruno (puño del anfitrión, puño del invitado y botella del anfitrión), caída propia y del anfitrión, cámara propia y eventos de impacto sin repeticiones en ambas copias.
- En esa prueba, la separación máxima de las partes de pie respecto a la última postura recibida fue **0,0173 m** durante movimiento y la separación de la cámara respecto al ojo renderizado fue **0 m**. La primera medida incluye la interpolación entre instantáneas; no es una medición de latencia ni del error frente al estado simultáneo del servidor. Las caídas usan interpolación de las seis partes físicas; las aserciones admiten hasta 0,25 m y 25 grados frente a la última instantánea para tolerar cambios de postura entre muestras. En los tres ciclos de Bruno se observaron máximos de 0,088 m y 16,8 grados. El detalle por ciclo consta en el informe del invitado.
- Las nueve capturas `impact-*.png`, `fallen-*.png` y `recovered-*.png` de esa carpeta proceden de la cámara del invitado renderizada a una textura; muestran el mundo y no incluyen el HUD de IMGUI. Se inspeccionaron impacto, caída por puño, caída por botella y recuperación. Bruno aparece tumbado en el suelo y vuelve a estar de pie.
- Los eventos de partículas y llamadas a reproducción de audio se verificaron en ambas copias, una vez por impacto. El ensayo automático comprueba la ejecución del sonido, no la salida física de altavoces. Posteriormente el usuario confirmó de forma general que las correcciones funcionan, sin aportar mediciones adicionales.
- Las pruebas LAN usan colocaciones controladas en el servidor para aislar cada componente. No son una sesión humana ni prueban conectividad entre máquinas, latencia/jitter/pérdida artificiales, seis personas o rendimiento sostenido. El RTT de 33 ms del ensayo básico es una muestra local, no una promesa de respuesta de movimiento.
- Las últimas reconstrucciones solo ajustaron el ensayo de caídas: despejar la vista y alternar quién golpea. La lógica de juego coincide con la de los ensayos de 54 verificaciones de integración y 23 locales; la compilación final ejecutó las 115 comprobaciones de representación y efectos.
- Metadatos: sin GUID duplicados ni archivos de contenido sin `.meta` en `TavernChaos/Assets`.

## Compilación para Mac 0.3.1

`build-macos.json`: compilación universal **Succeeded**, cero errores y una advertencia de Unity por la retirada futura del soporte Intel. El ejecutable, UnityPlayer y el código nativo de Burst contienen las arquitecturas Intel x64 y Apple Silicon arm64. `Info.plist` declara versión 0.3.1 y macOS mínimo 12.0. Se añadió únicamente Mac Build Support (Mono) para el editor 6000.6.8f1, sin cambiar la versión de Unity ni los paquetes del proyecto.

El ZIP `Builds/macOS/Tavern-and-Chaos-0.3.1-macOS-Universal.zip` incluye la aplicación completa, `Abrir.command` y las [instrucciones para Mac](../../Docs/PROBAR_MAC.md). Se verifica cada archivo comprimido frente a su original y se conservan los permisos Unix de ejecución. El resumen y SHA-256 se registran en `macos-package.json`. La compilación se realizó en `.validation`; la escena y la configuración originales se conservaron.

Esta comprobación es de compilación, contenido y empaquetado desde Windows. **El player no se ha ejecutado en macOS**, ni se ha validado una conexión Windows–Mac. La aplicación de prueba no está notarizada por Apple. La evidencia de la demo macOS 0.2.0 en `Evidence/build-report.json` permanece intacta.

## Conservación de escena

La compilación final se hizo en `.validation`, una copia ignorada por Git, para conservar la sesión abierta del editor. Los archivos C# de la copia y del proyecto coinciden. La escena original y la usada para compilar conservan el mismo SHA-256 antes y después del proceso:

`0CAA28F56228864A3465CBDD9EE008CCC77872F135E70D1FAC0C51A670A8B567`

La compilación no invoca la regeneración de escena. La preparación explícita no reemplaza una escena existente.

## Entrega y siguiente paso

Ejecutable: `Builds/Windows/Tavern and Chaos.exe`. Lanzador manual: `Probar-LAN.cmd`. Guía y lista de aceptación: `Docs/LAN_HITO1.md`. Cierra ambas copias antiguas y comprueba **0.3.1** en los dos menús: el protocolo de sala cambió a 3. Usa el puerto 7777; para una máquina, IP 127.0.0.1.

Desde el invitado, repite movimiento/giro del anfitrión y del propio cuerpo, golpes repetidos a Bruno con recuperación entre ellos y lanzamiento de botella. Comprueba las caídas de ambos jugadores y partículas/sonido en las dos copias. Para repetir las regresiones: `./run-lan-checks.ps1` y `./run-lan-checks.ps1 -Replication -Visual`.

**Correcciones del Hito 1: confirmadas por el usuario en su prueba local de dos copias.** LAN entre equipos, latencia/pérdida y seis participantes siguen pendientes. El controlador sigue siendo cápsula con caída física, según la decisión confirmada; no se presenta como active ragdoll de locomoción.
