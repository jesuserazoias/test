namespace IasTestApplication.Data;

/// <summary>Venta confirmada de una oferta.</summary>
public class Sale
{
    public Guid Id { get; set; }
    public required string OfferCode { get; set; }

    /// <summary>Prima vigente al momento de la venta (copia histórica).</summary>
    public long PremiumCop { get; set; }

    /// <summary>
    /// Clave de idempotencia enviada por el cliente. Única: un reintento tras un corte de
    /// conexión no genera una segunda venta ni consume un segundo cupo.
    /// </summary>
    public required string IdempotencyKey { get; set; }

    public DateTimeOffset ConfirmedAt { get; set; }
}
