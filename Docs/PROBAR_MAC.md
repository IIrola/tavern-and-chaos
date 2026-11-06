# Probar Tavern & Chaos 0.9.0 en Mac

Descarga `Tavern-and-Chaos-0.9.0-macOS-Universal.dmg` desde [la release 0.9.0](https://github.com/IIrola/tavern-and-chaos/releases/tag/v0.9.0). Abre el DMG y arrastra **Tavern and Chaos.app** al acceso **Applications**. Después expulsa la imagen y abre la app desde Aplicaciones. También puedes descargar el ZIP de esa misma release, extraerlo completo y abrir Tavern and Chaos.app. El menú debe mostrar 0.9.0. Es universal para Intel y Apple Silicon, con macOS 12.0 como mínimo; se ha probado en Apple Silicon. No necesitas Unity.

La aplicación de desarrollo no está firmada ni notarizada. Si macOS no verifica al desarrollador, consulta las [instrucciones de Apple](https://support.apple.com/en-us/102445) para abrir una aplicación de origen conocido. No cambies la protección general del sistema.

## Probar con otra persona

Ambos usan 0.9.0 y la misma red local. Uno crea sala y el otro introduce su código o dirección. La dirección 127.0.0.1 sirve solamente para copias en el mismo ordenador. Se puede entrar mientras la sala está abierta; máximo seis. La validación actual de esta iteración usa tres procesos en el mismo Mac; faltan pruebas físicas entre equipos y Windows actual.

WASD camina, Shift corre, ratón mira, E interactúa y Q suelta. Mantén y suelta clic para golpear o lanzar. R recoloca solo a quien lo pide si está de pie y libre; Esc abre pausa local sin congelar a los demás.

En pulseadas, acércate a un asiento y pulsa E. Ambos confirman con Enter y el organizador inicia después. Apertura 5 más apuesta opcional. Espacio mantiene; M cambia a ritmo manual; flechas izquierda/derecha eligen fuerza. Dura 15 s después de una cuenta atrás de 3. La resistencia se recupera entre impulsos; ebriedad duplica el coste. H abre reglas y E permite levantarse y volver. El reloj continúa. El resultado muestra devolución y ganancia/pérdida neta.

En billar, E sienta, ambos confirman el total con Enter y el organizador inicia. Apertura 10 más apuesta opcional. Tab elige bola, C tronera; ratón apunta, rueda/flechas cambian potencia y clic tira. H abre reglas y E levanta. Las faltas y el resultado los decide el anfitrión.

En la barra pulsa E para abrir la estación (solo una persona a la vez): elige recipiente, ajusta ingredientes (o usa un atajo de receta), Agitar y Servir (20 fichas, solo al servir). Con el vaso: E bebe, F lo ofrece a quien tengas delante (acepta con E, gratis), Q lo deja, E junto a un vaso apoyado lo recoge y mantener/soltar clic lo lanza. Esc cierra la estación sin pagar. Cada receta aplica su efecto; la ebriedad baja uno cada 30 s.

Al acabarse el tiempo, se resuelven acciones admitidas y se devuelven apuestas inconclusas. Por insolvencia se muestra una expulsión compartida. Los resultados se consultan por páginas y permiten crear otra sala o salir. La evidencia y límites están en [VALIDACION.md](../Evidence/effects/VALIDACION.md).

La versión 0.8 integra efectos físicos y su réplica, pero mantiene pendiente la estación de preparación completa. Ajustes permite bajar la intensidad visual hasta cero. El alcance por entregas está en [HITOS_Y_PLAN_DE_ENTREGA.md](HITOS_Y_PLAN_DE_ENTREGA.md).
