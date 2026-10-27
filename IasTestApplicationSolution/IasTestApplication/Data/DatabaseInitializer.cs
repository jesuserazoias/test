using IasTestApplication.Common;
using Microsoft.EntityFrameworkCore;

namespace IasTestApplication.Data;

public static class DatabaseInitializer
{
    public const int GeneratedOffersCount = 2000;

    /// <summary>
    /// Crea la base si no existe y la puebla solo si está vacía (no pisa ventas ni cupos existentes).
    /// Con <paramref name="reset"/> = true borra todo y vuelve al estado inicial.
    /// </summary>
    public static async Task InitializeAsync(AppDbContext db, bool reset, ILogger logger)
    {
        if (reset)
        {
            logger.LogWarning("Restableciendo la base de datos: se borran ofertas y ventas.");
            await db.Database.EnsureDeletedAsync();
        }

        await db.Database.EnsureCreatedAsync();

        // WAL: lecturas concurrentes con una escritura; el modo queda guardado en el archivo.
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

        if (await db.Offers.AnyAsync())
        {
            return;
        }

        db.Offers.AddRange(BuildSeedOffers());
        await db.SaveChangesAsync();
        logger.LogInformation("Base poblada con {Count} ofertas.", await db.Offers.CountAsync());
    }

    private static IEnumerable<Offer> BuildSeedOffers()
    {
        yield return Create("SEG-001", "Vida Esencial", 50_000, 1);
        yield return Create("SEG-002", "Hogar Tranquilo", 80_000, 10);
        yield return Create("SEG-003", "Accidentes Personal", 35_000, 0);

        for (var n = 1; n <= GeneratedOffersCount; n++)
        {
            yield return Create($"CAM-{n:D4}", $"Protección Campaña {n}", 40_000 + n, 10);
        }
    }

    private static Offer Create(string code, string name, long premium, int slots) => new()
    {
        Code = code,
        Name = name,
        PremiumCop = premium,
        InitialSlots = slots,
        AvailableSlots = slots,
        NormalizedCode = TextNormalizer.Normalize(code),
        NormalizedName = TextNormalizer.Normalize(name)
    };
}
