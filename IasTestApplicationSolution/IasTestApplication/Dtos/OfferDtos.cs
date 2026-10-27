namespace IasTestApplication.Dtos;

public record OfferDto(string Code, string Name, long PremiumCop, int AvailableSlots);

/// <param name="Items">Máximo 20 coincidencias.</param>
/// <param name="HasMore">true si existen más coincidencias que las mostradas (afinar la búsqueda).</param>
public record OfferSearchResponse(IReadOnlyList<OfferDto> Items, int ReturnedCount, bool HasMore);
