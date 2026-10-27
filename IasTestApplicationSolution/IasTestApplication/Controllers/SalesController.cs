using IasTestApplication.Dtos;
using IasTestApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace IasTestApplication.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController(ISaleService sales) : ControllerBase
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private const int MaxKeyLength = 100;

    /// <summary>
    /// POST /api/sales con cabecera Idempotency-Key (un valor único por intento de venta, p. ej. un GUID
    /// generado al abrir la confirmación). Si la conexión falla, el cliente reintenta con la MISMA clave.
    /// 201 venta creada · 200 reintento (misma venta) · 404 oferta inexistente ·
    /// 409 sin cupos o clave reutilizada con otra oferta · 400 datos inválidos.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SaleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleDto>> Confirm(
        [FromBody] ConfirmSaleRequest request,
        [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey)
    {
        var key = idempotencyKey?.Trim() ?? string.Empty;
        if (key.Length == 0 || key.Length > MaxKeyLength)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Idempotency-Key inválida",
                detail: $"La cabecera {IdempotencyHeader} es obligatoria (1 a {MaxKeyLength} caracteres).");
        }

        var result = await sales.ConfirmAsync(request.OfferCode, key);

        return result.Status switch
        {
            ConfirmSaleStatus.Created => Created($"/api/sales/{result.Sale!.SaleId}", result.Sale),
            ConfirmSaleStatus.Replayed => Ok(result.Sale),
            ConfirmSaleStatus.OfferNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Oferta no encontrada",
                detail: $"No existe la oferta '{request.OfferCode}'.",
                type: "offer-not-found"),
            ConfirmSaleStatus.SoldOut => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Sin cupos disponibles",
                detail: $"La oferta '{request.OfferCode}' ya no tiene cupos.",
                type: "sold-out"),
            ConfirmSaleStatus.IdempotencyKeyConflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Idempotency-Key ya utilizada",
                detail: "Esta clave ya se usó para otra oferta. Genere una clave nueva para una venta distinta.",
                type: "idempotency-key-conflict"),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
