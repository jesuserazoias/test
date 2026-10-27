using IasTestApplication.Common;
using IasTestApplication.Data;
using IasTestApplication.Dtos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace IasTestApplication.Services;

/// <summary>
/// Confirmación de ventas. Garantías:
/// - Nunca se vende más de los cupos existentes, aunque varios asesores vendan a la vez
///   (UPDATE atómico "AvailableSlots = AvailableSlots - 1 WHERE AvailableSlots &gt; 0" + CHECK en BD).
/// - Un reintento con la misma Idempotency-Key no duplica la venta ni gasta otro cupo.
/// - Venta y descuento de cupo ocurren en una sola transacción: o ambos o ninguno.
/// </summary>
public class SaleService(AppDbContext db, ILogger<SaleService> logger) : ISaleService
{
    // No recibe CancellationToken a propósito: si el cliente pierde la conexión a mitad de la
    // confirmación, la operación termina igual (commit o rollback limpio) y el cliente puede
    // reintentar con la misma clave para conocer el resultado.
    public async Task<ConfirmSaleResult> ConfirmAsync(string offerCode, string idempotencyKey)
    {
        var normalizedCode = TextNormalizer.Normalize(offerCode);

        // Camino rápido para reintentos.
        var existing = await FindByKeyAsync(idempotencyKey);
        if (existing is not null)
        {
            return await ReplayAsync(existing, normalizedCode);
        }

        var offer = await db.Offers.AsNoTracking()
            .FirstOrDefaultAsync(o => o.NormalizedCode == normalizedCode);
        if (offer is null)
        {
            return new ConfirmSaleResult(ConfirmSaleStatus.OfferNotFound);
        }

        await using var tx = await db.Database.BeginTransactionAsync();

        // La primera sentencia de la transacción es una escritura: SQLite toma el bloqueo de
        // escritura de inmediato y las ventas concurrentes se serializan (esperan, no fallan).
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            OfferCode = offer.Code,
            PremiumCop = offer.PremiumCop,
            IdempotencyKey = idempotencyKey,
            ConfirmedAt = DateTimeOffset.UtcNow
        };
        db.Sales.Add(sale);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Otra solicitud con la misma clave ganó la carrera: se devuelve su resultado.
            await tx.RollbackAsync();
            db.ChangeTracker.Clear();
            existing = await FindByKeyAsync(idempotencyKey);
            if (existing is null)
            {
                throw;
            }
            return await ReplayAsync(existing, normalizedCode);
        }

        var updated = await db.Offers
            .Where(o => o.Code == offer.Code && o.AvailableSlots > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.AvailableSlots, o => o.AvailableSlots - 1));

        if (updated == 0)
        {
            await tx.RollbackAsync(); // deshace también el insert de la venta
            logger.LogInformation("Oferta {Code} sin cupos; venta rechazada.", offer.Code);
            return new ConfirmSaleResult(ConfirmSaleStatus.SoldOut);
        }

        await tx.CommitAsync();
        logger.LogInformation("Venta {SaleId} confirmada para {Code}.", sale.Id, offer.Code);

        return new ConfirmSaleResult(ConfirmSaleStatus.Created, ToDto(sale, offer.Name, isReplay: false));
    }

    private Task<Sale?> FindByKeyAsync(string key) =>
        db.Sales.AsNoTracking().FirstOrDefaultAsync(s => s.IdempotencyKey == key);

    private async Task<ConfirmSaleResult> ReplayAsync(Sale existing, string requestedNormalizedCode)
    {
        if (TextNormalizer.Normalize(existing.OfferCode) != requestedNormalizedCode)
        {
            return new ConfirmSaleResult(ConfirmSaleStatus.IdempotencyKeyConflict);
        }

        var name = await db.Offers.AsNoTracking()
            .Where(o => o.Code == existing.OfferCode)
            .Select(o => o.Name)
            .FirstAsync();

        return new ConfirmSaleResult(ConfirmSaleStatus.Replayed, ToDto(existing, name, isReplay: true));
    }

    private static SaleDto ToDto(Sale sale, string offerName, bool isReplay) =>
        new(sale.Id, sale.OfferCode, offerName, sale.PremiumCop, sale.ConfirmedAt, isReplay);

    // SQLITE_CONSTRAINT = 19
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException { SqliteErrorCode: 19 };
}
