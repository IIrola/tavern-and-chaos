# Validación 0.5.0 — economía y bola 8

Unity 6000.6.8f1, Mono, macOS Universal. El libro y las reglas son puros; la mesa se integra con NGO/UTP y física real del anfitrión. La versión está en desarrollo y no acredita el GDD completo.

## Resultados finales

| Evidencia | Resultado |
|---|---|
| Libro puro | 38 comprobaciones; 1.000 rondas en 14 salas |
| Reglas/asientos | 48 comprobaciones |
| Red, primer lado | 82 comprobaciones, tres procesos, salida 0 |
| Red, otro lado | 80 comprobaciones, tres procesos, salida 0 |
| Regresión local | 86 comprobaciones, salida 0 |
| Entrada aislada | 14 comprobaciones, salida 0 |
| Construcción | Succeeded, resumen/pasos con cero errores, una advertencia |
| Referencias/GUID | 72 archivos, cero errores/advertencias |

Todos los informes finales aprobaron sin fallos ni errores de ejecución.

## Alcance de las comprobaciones

- `ledger.json`: concesión una vez por IP/sala, salida y retorno, reservas atómicas, fallo de apertura, cancelación, empates, premios únicos, fondos de personas que salieron, servicios, caídas, banca 1:1/3:2, límites de enteros y conservación de fichas. El ensayo determinista completa 1.000 rondas en 14 salas; cierra cada sala al quedar menos de dos personas.
- `pool-rules.json`: regresión de las reglas de bola 8 y del contrato de asientos, incluida la colocación del triángulo.
- `processes/` y `processes-side1/`: dos salas independientes de tres procesos Mac en UDP loopback. Cubren ambos lados iniciales, consentimiento con Enter real, rechazo de propuesta antigua y de organización ajena, cambio de apuesta que borra confirmaciones, fallo de apertura con devolución completa, cobro único, bote de 100, ocho emboques físicos hasta ganar, caída y retorno del participante, siguiente persona en espera, revancha con apuesta cero, desconexión e historial de estadísticas recibido en cliente.
- `regression/`: recorrido completo de movimiento, cámara, golpes, bebida de prueba, botellas, recuperación, física de billar, controles y tamaños de pantalla. `pool-physics.json` guarda mediciones de contactos, energía, bandas y reposo.
- `input-check/`: ensayo aislado del sistema de entrada, incluidos clic en UI, apuntado y disparo único.

Los tiros anunciados usan posiciones controladas que aíslan la interacción entre reglas, física y red; los impactos, emboques y ganadores no se inyectan como resultados. La segunda ronda ensaya también un saque del triángulo normal de quince bolas. No es una sesión humana sin guía ni una simulación de red entre equipos. Seis procesos en dos salas no equivalen a una sala de seis.

## Revisión visual y UX

Se revisaron `00-confirmation.png`, `01-table.png`, `02-result.png`, `03-rules.png` y `04-statistics.png`. Se muestran apertura, apuesta, total, saldo y confirmaciones antes del inicio; después, bote, pago y beneficio de apuesta. Los paneles opacos conservan la legibilidad sobre la taberna. Las estadísticas distinguen victoria de mesa, empate y apuesta ganada, con beneficio/pérdida separado de gastos. Se mantiene el historial de quien salió.

La cobertura de paginación de red es de una página con tres conexiones; falta un recorrido de historial largo y varias páginas. Las pruebas de entrada usan dispositivos virtuales y no acreditan un mando físico.

## Construcción y correcciones del ensayo

La inspección de pasos detectó errores `TypeDB` reales en una construcción marcada `Succeeded`: inicialmente 745 y más tarde 3.850. Limpiar solo la caché de construcción no bastó. Se localizaron archivos numerados de metadatos junto a los canónicos, con contenido byte a byte idéntico. Después de conservar fuera del proyecto solo `Library/BuildPlayerData`, el mismo proyecto construyó con cero errores y tres archivos canónicos, sin duplicados. El origen de las copias no está establecido. `build-investigation.json` conserva las mediciones y hashes.

`build-demo.sh` regenera esos metadatos antes de abrir Unity, conservando copia en `.validation`. Rechaza hacerlo con el editor del proyecto abierto. El menú de construcción detecta copias numeradas, y la validación rechaza errores de resumen o pasos aunque Unity anuncie éxito. La [documentación oficial de Unity 6.6](https://docs.unity.com/en-us/engine/6000.6/manual/building-and-publishing/build-clean-build) describe la limpieza de construcción y de Library; el diagnóstico específico de copias numeradas es evidencia local.

Queda una advertencia de Unity por retirada futura de soporte Intel; el binario actual declara ambas arquitecturas. `package.json` identifica el ZIP con tamaño y SHA-256, y se comprobó su integridad.

Una regresión detectó que desactivar el ratón físico no aislaba por completo la selección del control de posición. La acción leía una posición del escritorio en lugar del dispositivo virtual y la protección de UI rechazaba el clic. Se restringen los mapas a los dispositivos del ensayo y se restauran después; la aserción comprueba ahora el dispositivo y el valor de la acción. `input-investigation.json` conserva ese hallazgo.

El ensayo de revancha espera la revisión exacta que ve el cliente antes de cambiar a apuesta cero; una versión anterior actuaba sobre el estado anterior y el servidor la rechazaba correctamente. `round-check-investigation.json` registra la corrección.

## Límites y siguiente trabajo

Faltan cierre por tiempo/insolvencia con presentación compartida, las otras cinco actividades, coctelería completa, privacidad de cartas, pruebas entre equipos y Windows actual, seis personas en una sala, latencia/jitter/pérdida, ausencia real de un minuto, accesibilidad completa y sesiones humanas. No se ha medido el objetivo de 60 FPS. El registro de los 85 criterios sigue en `Docs/REQUISITOS.json`.

## Repetir

1. `./build-demo.sh`: valida cuentas/reglas y construye una aplicación limpia; rechaza errores de resumen y pasos.
2. `python3 run-pool-room-checks.py --visual-host`.
3. `python3 run-pool-room-checks.py --first-side-1`.
4. Player con `-demo-smoke-tests -evidencePath <carpeta> -logFile <registro>` para regresión completa; `-pool-input-only` limita a entrada.
5. `python3 /ruta/a/unity-csharp-scripting/scripts/check_unity_project.py TavernChaos --json` comprueba referencias/GUID.
