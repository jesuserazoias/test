# Venta de seguros de campaña (prototipo local)

## Tecnología
| Elemento | Decisión |
|---|---|
| Runtime / SDK | .NET 10 (`net10.0`), SDK 10.0.x |
| Web | ASP.NET Core Web API con controladores |
| Acceso a datos | EF Core 10.0.14 (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Almacenamiento | SQLite, archivo `IasTestApplication/App_Data/campaign.db` (ruta en `Database:Path`) |

Se eligió SQLite porque es un archivo local sin instalar nada, es transaccional y permite hacer el descuento de cupos de forma atómica.

## Ejecutar
```powershell
cd IasTestApplication
dotnet run            # http://localhost:5100
```
Swagger UI: `http://localhost:5100/swagger` (JSON en `/swagger/v1/swagger.json`). Solo se habilita en `Development`; el perfil de `launchSettings.json` ya lo abre al ejecutar.

Al arrancar crea la base y la puebla **solo si está vacía**. Hay ejemplos de llamadas en `IasTestApplication/IasTestApplication.http`.

## Preparar / restablecer los datos
Datos: `SEG-001/002/003` + 2000 ofertas `CAM-0001`…`CAM-2000` (nombre `Protección Campaña n`, prima `40000 + n`, 10 cupos), generadas por código en `Data/DatabaseInitializer.cs`.

Restablecer (borra ofertas **y ventas**, y deja los cupos iniciales). Cualquiera de estas opciones:
```powershell
dotnet run -- --Database:Reset=true      # ojo: lo hace en cada arranque mientras la opción esté activa
```
o detener la aplicación, borrar la carpeta `IasTestApplication/App_Data` y volver a ejecutar.

## Qué se conserva al reiniciar y límites
- Se conserva todo: ofertas, cupos restantes y ventas (están en el archivo SQLite).
- Límites: un único proceso/máquina (archivo local, sin réplicas ni varios servidores); sin migraciones (`EnsureCreated`: si cambia el modelo hay que restablecer la base); las escrituras se serializan (suficiente para un prototipo, no para alto volumen); sin respaldo automático.

## API
| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/offers?search=` | Busca por código o nombre (sin importar mayúsculas ni tildes). Máx. 20; `hasMore` indica si hay más coincidencias. |
| GET | `/api/offers/{code}` | Datos actuales de una oferta (cupos al día). |
| POST | `/api/sales` | Confirma una venta. Body `{ "offerCode": "SEG-001" }`, cabecera `Idempotency-Key` obligatoria. |

Respuestas de `POST /api/sales`: `201` venta creada · `200` reintento, misma venta (`isReplay: true`) · `400` datos inválidos · `404` oferta inexistente · `409` sin cupos (`type: sold-out`) o clave usada con otra oferta (`type: idempotency-key-conflict`).

## Concurrencia y conexiones inestables
- **Sobreventa:** el cupo se descuenta con `UPDATE ... SET AvailableSlots = AvailableSlots - 1 WHERE AvailableSlots > 0` dentro de la misma transacción que registra la venta. Si no afecta ninguna fila, la venta se revierte y se responde `409`. Además la BD tiene `CHECK (AvailableSlots >= 0)`.
- **Reintentos:** el cliente genera un GUID al iniciar la confirmación y lo envía en `Idempotency-Key`. Si la respuesta se pierde, reintenta con la misma clave y obtiene la venta original sin gastar otro cupo. Si es una venta nueva, debe usar una clave nueva.
- **Desconexión del cliente:** la confirmación no se cancela a mitad de camino; termina (o se revierte completa) y el reintento muestra el resultado real.
