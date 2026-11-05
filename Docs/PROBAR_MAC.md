# Probar Tavern & Chaos 0.3.1 en Mac

Esta demo incluye primera persona, Laguna Azul con caídas por choque, botellas, golpes y la sala LAN de la versión 0.3.1. El paquete universal contiene Intel x64 y Apple Silicon arm64 y declara **macOS 12.0 o posterior** como versión mínima. Se genera desde Windows con Unity 6000.6.8f1; su ejecución y la conexión Windows–Mac todavía deben comprobarse en un Mac. No necesitas instalar Unity para jugar.

## Abrir la demo

1. Recibe `Tavern-and-Chaos-0.3.1-macOS-Universal.zip` y extráelo completo en tu Mac. No ejecutes la aplicación dentro del ZIP.
2. Abre la carpeta extraída y haz doble clic en `Tavern and Chaos.app`. Comprueba **0.3.1** en el menú. También se incluye `Abrir.command` como acceso alternativo.
3. Elige **Abrir la taberna** para jugar solo o **Crear / unirse a sala LAN** para probar la conexión.

Es una compilación de prueba sin notarización de Apple. Si macOS indica que no puede verificar al desarrollador, tras intentar abrirla puedes usar **Ajustes del Sistema → Privacidad y seguridad → Abrir igualmente**, siempre que hayas recibido este paquete directamente de tu compañero. Referencia: [instrucciones de Apple](https://support.apple.com/en-us/102445). No es necesario desactivar Gatekeeper para todas las aplicaciones.

El ZIP conserva permisos de ejecución. Si el programa con el que se extrae los elimina y aparece un error de permisos, abre Terminal, escribe `chmod u+x ` (con un espacio al final), arrastra el archivo `Tavern and Chaos.app/Contents/MacOS/Tavern and Chaos` hasta Terminal y pulsa Enter. Después abre la aplicación otra vez. Unity advierte de este requisito al [compilar macOS desde Windows](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/apple-mac/macos-delivery/macos-building).

## Controles y prueba

- WASD: caminar; Shift: correr; ratón: mirar arriba, abajo y a los lados.
- E: interactuar con una botella o comprar Laguna Azul en la barra.
- Mantener y soltar clic izquierdo: cargar y lanzar la botella, o golpear si no llevas una. Q: soltar y cancelar la carga.
- R: recolocarte; Esc: pausa local en LAN.
- Laguna Azul cuesta 20 fichas en LAN y dura ocho segundos. Con impulso, un choque frontal contra pared/mueble provoca caída, suelta la botella y elimina el efecto. Los roces y empujar lentamente no deben derribar. La recuperación comienza a los tres segundos.

Comprueba el movimiento y giro del propio cuerpo mirando hacia abajo, las caídas y recuperación de Bruno, y las partículas/sonido al golpear o lanzar una botella.

## LAN con tu compañero

Ambos deben usar la versión **0.3.1** y estar en la misma red local. Uno crea la sala en el puerto **7777** y el otro introduce la IP local del anfitrión (o el código que muestra la sala). `127.0.0.1` solo sirve para conectar dos copias en el mismo ordenador; no sirve para conectar el Mac con el PC. Si el sistema pide permiso de red para la aplicación, autorízalo para esta prueba local.

Para abrir dos copias en el propio Mac se puede ejecutar `open -n ` en Terminal, arrastrar la aplicación y pulsar Enter; repetirlo abre otra instancia. En ese caso se utiliza IP `127.0.0.1` y el mismo puerto.

La conexión desde casas distintas no está configurada. El billar es una práctica local; todavía no hay partidas compartidas de mesa. Los registros del player de macOS quedan en `~/Library/Logs/Tavern Workshop/Tavern and Chaos/Player.log`. Si algo falla, comunica la versión, el modelo de Mac, su versión de macOS y la acción que produjo el fallo.
