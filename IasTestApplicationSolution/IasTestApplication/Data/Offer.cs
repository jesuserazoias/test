namespace IasTestApplication.Data;

/// <summary>Oferta de seguro de la campaña. Prima fija y cupos limitados.</summary>
public class Offer
{
    public required string Code { get; set; }
    public required string Name { get; set; }

    /// <summary>Prima en pesos colombianos (COP). Entero: el COP no se maneja con decimales.</summary>
    public long PremiumCop { get; set; }

    public int InitialSlots { get; set; }

    /// <summary>Cupos restantes. Solo se modifica mediante un UPDATE atómico condicionado (ver SaleService).</summary>
    public int AvailableSlots { get; set; }

    /// <summary>Código en minúsculas y sin tildes, para búsquedas.</summary>
    public required string NormalizedCode { get; set; }

    /// <summary>Nombre en minúsculas y sin tildes, para búsquedas.</summary>
    public required string NormalizedName { get; set; }
}
