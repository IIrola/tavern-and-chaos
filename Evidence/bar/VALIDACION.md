# Coctelería — dominio en desarrollo de 0.8

Unity 6000.6.8f1 compiló las clases y ejecutó TavernChaos.Editor.BarValidation.Run con salida 0. rules.json contiene 76 comprobaciones aprobadas y ledger-regression.json conserva las 38 comprobaciones anteriores del libro, sin fallos. La revisión de referencias/GUID se ejecuta por separado. El catálogo real está en Assets/Resources/CocktailCatalog.asset y contiene las cinco combinaciones originales.

Se comprueban proporciones, orden indiferente, cuatro ingredientes, mezcla libre, K.O. con cuatro licores fuertes a cualquier proporción no nula, tiempos explícitos del GDD y catálogo congelado. Se verifican porcentajes de 10/20 % guardados por Unity y aislamiento de la copia de sala frente al asset. La estación solo admite un dueño, revisiones actuales, recipiente y mezcla no vacía, 0,75 s de agitado y nueva confirmación de mezcla después de cambios.

El servicio usa el SessionLedger real como dominio: preparar/cancelar no cobra; servir crea un vaso y debita 20 una vez; saldo insuficiente o capacidad/estado no admisible no cobra. Un vaso identifica contenido, dueño, origen y tiempo, se ofrece/acepta o deja/recoge sin volver a cobrar, y se consume o impacta una sola vez. El contador de consumo corresponde a quien bebe, separado de quien paga el servicio. Hay máximo doce vivos, caducidad 30 s y vuelo máximo 6 s. Desconectar al lanzador no borra un vuelo ya soltado. El cierre descarta lo no lanzado conservando el servicio, deja resolver vuelo y espera la aplicación pendiente; no crea fichas.

Los efectos se prueban como datos: renovación sin multiplicar, coexistencia con multiplicadores acotados, caída que cancela deslizamiento/inestabilidad y preserva otros, expiración por efecto y evento de caída de inestabilidad que no se pierde al aplicar otro efecto justo al vencer.

Estas pruebas no ejecutan cuerpos de vasos ni aplican fuerzas a TavernActor. No demuestran fricción cero real, gravedad reducida de cápsula/ragdoll, velocidad 150 %, K.O. de 10 s, explosión, VFX, UI o transporte de coctelería. Todo ello queda para integración y validación nativa. La demo pública continúa en 0.7.0; el GDD permanece incompleto.
