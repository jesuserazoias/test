using IasTestApplication.Dtos;

namespace IasTestApplication.Services;

public interface IOfferService
{
    /// <summary>Busca por código o nombre (sin distinguir mayúsculas ni tildes). Máximo 20 resultados.</summary>
    Task<OfferSearchResponse> SearchAsync(string? search, CancellationToken ct);

    Task<OfferDto?> GetByCodeAsync(string code, CancellationToken ct);
}
