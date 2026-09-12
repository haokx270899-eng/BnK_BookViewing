using Microsoft.AspNetCore.Mvc;
using PropertyViewing.Api.DTOs;
using PropertyViewing.Application.DTOs;
using PropertyViewing.Application.Interfaces;

namespace PropertyViewing.Api.Controllers;

[ApiController]
[Route("api/viewings")]
public sealed class ViewingsController(IViewingService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(BookingResult), StatusCodes.Status201Created)]
    public async Task<ActionResult<BookingResult>> Book(BookViewingRequest request, CancellationToken cancellationToken)
    {
        var result = await service.BookAsync(new BookViewingCommand(request.PropertyId, request.UserId, request.StartTime), cancellationToken);
        return CreatedAtAction(nameof(Book), new { id = result.Id }, result);
    }
    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<ViewingSlotDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ViewingSlotDto>> Available(int propertyId, DateOnly from, DateOnly to, CancellationToken cancellationToken) => service.GetAvailableAsync(propertyId, from, to, cancellationToken);
}
