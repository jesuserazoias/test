using System.ComponentModel.DataAnnotations;

namespace IasTestApplication.Dtos;

public record ConfirmSaleRequest(
    [Required, StringLength(20, MinimumLength = 1)] string OfferCode);

/// <param name="IsReplay">
/// true si la solicitud ya había sido procesada (reintento con la misma Idempotency-Key):
/// se devuelve la venta original y no se consume otro cupo.
/// </param>
public record SaleDto(
    Guid SaleId,
    string OfferCode,
    string OfferName,
    long PremiumCop,
    DateTimeOffset ConfirmedAt,
    bool IsReplay);
