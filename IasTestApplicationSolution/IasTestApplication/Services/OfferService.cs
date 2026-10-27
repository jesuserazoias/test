using IasTestApplication.Common;
using IasTestApplication.Data;
using IasTestApplication.Dtos;
using Microsoft.EntityFrameworkCore;

namespace IasTestApplication.Services;

public class OfferService(AppDbContext db) : IOfferService
{
    public const int MaxResults = 20;

    public async Task<OfferSearchResponse> SearchAsync(string? search, CancellationToken ct)
    {
        var term = TextNormalizer.Normalize(search);

        IQueryable<Offer> query = db.Offers.AsNoTracking();
        if (term.Length > 0)
        {
            query = query.Where(o => o.NormalizedCode.Contains(term) || o.NormalizedName.Contains(term));
        }

        // Se piden MaxResults + 1 solo para saber si hay más coincidencias, sin contar todo el catálogo.
        var rows = await query
            .OrderBy(o => o.Code)
            .Take(MaxResults + 1)
            .Select(o => new OfferDto(o.Code, o.Name, o.PremiumCop, o.AvailableSlots))
            .ToListAsync(ct);

        var hasMore = rows.Count > MaxResults;
        var items = hasMore ? rows.Take(MaxResults).ToList() : rows;
        return new OfferSearchResponse(items, items.Count, hasMore);
    }

    public Task<OfferDto?> GetByCodeAsync(string code, CancellationToken ct)
    {
        var normalized = TextNormalizer.Normalize(code);
        return db.Offers.AsNoTracking()
            .Where(o => o.NormalizedCode == normalized)
            .Select(o => new OfferDto(o.Code, o.Name, o.PremiumCop, o.AvailableSlots))
            .FirstOrDefaultAsync(ct);
    }
}
