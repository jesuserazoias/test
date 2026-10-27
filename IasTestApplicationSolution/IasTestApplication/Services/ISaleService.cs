using IasTestApplication.Dtos;

namespace IasTestApplication.Services;

public enum ConfirmSaleStatus
{
    Created,
    /// <summary>Reintento de una venta ya registrada con la misma clave.</summary>
    Replayed,
    OfferNotFound,
    SoldOut,
    /// <summary>La misma Idempotency-Key ya se usó para otra oferta.</summary>
    IdempotencyKeyConflict
}

public record ConfirmSaleResult(ConfirmSaleStatus Status, SaleDto? Sale = null);

public interface ISaleService
{
    Task<ConfirmSaleResult> ConfirmAsync(string offerCode, string idempotencyKey);
}
