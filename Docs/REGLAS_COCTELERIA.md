# Coctelería — decisiones de implementación

El GDD conserva sus cinco combinaciones y sus efectos. Estas decisiones completan proporciones, tiempos y reglas pendientes antes de implementar la estación. Son parámetros de diseño para probar con personas; no describen bebidas reales.

## Preparación y precio

Una estación y un recipiente en preparación por persona. La estación admite un dueño próximo, de pie y con manos libres; otra persona ve que está ocupada. Elegir vaso es cosmético. Se añaden hasta cuatro ingredientes distintos, de 1 a 10 medidas por ingrediente; se pueden reducir o quitar. Las proporciones se calculan sobre la suma de medidas. Cambiar mezcla después de agitar exige agitar de nuevo. Agitar tarda 0,75 s del servidor. Cancelar, salir, caer, alejarse o desconectarse libera estación sin cobrar.

Servir cuesta 20 fichas, una vez, y produce un vaso identificado por el servidor en la mano libre. Primero se validan recipiente, mezcla agitada, proximidad, saldo y espacio para el objeto; después se cobra. Un fallo no deja un cargo sin vaso. La vista muestra receta o mezcla libre, proporciones, efecto, ebriedad y total antes de servir. Los atajos de receta solo rellenan ingredientes y no sirven ni cobran automáticamente.

Cada vaso se puede consumir, ofrecer para que otra persona lo acepte, soltar, recoger o lanzar. Solo quien lo posee decide consumo/lanzamiento. Ofrecer no obliga a consumir: el destinatario debe aceptar, estar cerca y tener manos libres. Un vaso lanzado aplica una sola vez su efecto al primer actor alcanzado, incluidos sentados o caídos, sin aceptación adicional, como requiere el GDD. Un impacto contra escenario derrama y consume el vaso. El alcance y línea de visión se comprueban en el servidor.

Máximo 12 vasos servidos vivos. Cada uno caduca a los 30 s; volando se limita a 6 s. Un vaso ya servido y pendiente de uso retrasa una expulsión por insolvencia hasta uso/caducidad, para permitir terminar la compra; no genera fichas. El límite de sesión cancela mezclas y descarta vasos sin lanzar; un vaso soltado/lanzado termina su recorrido acotado. No se devuelve un servicio ya servido. La sala nueva elimina objetos y efectos anteriores.

## Recetas y proporciones

Cada receta exige exactamente sus ingredientes originales y una proporción mínima por ingrediente. Si la combinación o proporción no cumple una receta, es mezcla libre sin efecto especial; conserva el precio visible. El catálogo y mínimos son editables, se validan y se congelan al abrir sala.

| Receta | Mínimos | Duración/efecto | Ebriedad |
|---|---|---|---:|
| Laguna Azul | Vodka, curazao e hielo, cada uno ≥10 % | 8 s, fricción de pies cero y luz azul; choque frontal fuerte provoca caída | +1 |
| El Volcánico | Tequila, salsa picante y Tabasco, cada uno ≥10 % | 3 s de fuego visual; una onda al aplicar, radio 3,5 m, impulso máximo 4 m/s sobre NPC visibles | +1 |
| Mezcla Inestable | Absenta, energética y cerveza, cada una ≥10 % | Velocidad ×1,5 durante 5 s; al finalizar, caída con impulso hacia atrás de 3 m/s | +2 |
| Helio | Ginebra y carbonatada misterio, cada una ≥20 % | Gravedad ×0,2 durante 15 s; impulso inicial vertical 2 m/s para que se perciba al estar de pie | +1 |
| Veneno / K.O. | Cuatro licores fuertes distintos; cualquier proporción no nula | Colapso inmediato, humo y recuperación no antes de 10 s | +5 |

En este catálogo de ficción, los cuatro licores fuertes son vodka, tequila, absenta y ginebra. Mezcla libre con algún licor/cerveza/curazao añade +1 de ebriedad; sin alcohol añade 0. Las mezclas no crean un sexto efecto implícito. El orden de ingredientes no cambia la receta.

## Efectos y restauración

Identificador de efecto, identificador de vaso/origen, inicio y vencimiento quedan en servidor. Consumir y colisionar no pueden reutilizar un vaso. Repetir un efecto renueva su duración; no multiplica el modificador. Efectos distintos coexisten. Orden fijo: estado de caída, velocidad, gravedad, fricción y presentación. Velocidad máxima de locomoción 8,4 m/s; impulsos acotados. El servidor conserva la dirección confirmada; oscilación y sacudida son solo visuales.

Caer cancela deslizamiento e inestabilidad pendientes; no vuelve sobrio ni borra efectos distintos como humo/fuego/gravedad. Una inestabilidad cancelada por caída no derriba de nuevo al expirar. Helio modifica aceleración de cápsula y añade compensación de gravedad a los cuerpos dinámicos al caer; no cambia Physics.gravity global ni inventa Rigidbody.gravityScale. Al expirar deja de compensar, conservando los demás efectos. Sentado no hay desplazamiento por modificadores; K.O. o la caída de inestabilidad sí liberan el asiento y respetan participación/partida en curso.

La sobriedad conserva la base de 0.7: nivel entero 0–5 y un nivel recuperado cada 30 s desde la última bebida. Nivel 5 puede tropezar al moverse, separado al menos 15 s. Las visuales tienen intensidad hasta cero y movimiento reducido; no alteran fuerza, gravedad ni temporizadores del servidor.

Las estadísticas separan servicio cobrado al servir de bebida consumida por la persona. Recibir un vaso lanzado aplica efecto/ebriedad, pero no inventa un consumo voluntario en ese contador. Todas las decisiones necesitan validación nativa, en red y humana antes de declarar completa la coctelería.
