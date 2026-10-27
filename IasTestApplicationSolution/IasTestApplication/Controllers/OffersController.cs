using IasTestApplication.Dtos;
using IasTestApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace IasTestApplication.Controllers;

[ApiController]
[Route("api/offers")]
public class OffersController(IOfferService offers) : ControllerBase
{
    /// <summary>GET /api/offers?search=vida — máximo 20 coincidencias.</summary>
    [HttpGet]
    public async Task<ActionResult<OfferSearchResponse>> Search(
        [FromQuery, System.ComponentModel.DataAnnotations.StringLength(100)] string? search,
        CancellationToken ct)
        => Ok(await offers.SearchAsync(search, ct));

    /// <summary>GET /api/offers/SEG-001 — datos actuales de una oferta (p. ej. al seleccionarla).</summary>
    [HttpGet("{code}")]
    [ProducesResponseType<OfferDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfferDto>> GetByCode(string code, CancellationToken ct)
    {
        var offer = await offers.GetByCodeAsync(code, ct);
        return offer is null ? NotFound(Problem404(code)) : Ok(offer);
    }

    private ProblemDetails Problem404(string code) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Oferta no encontrada",
        Detail = $"No existe la oferta '{code}'."
    };
}
