using Microsoft.AspNetCore.Mvc;
using Subiekt.Connector.Contracts;
using Subiekt.Connector.Api.Services;

namespace Subiekt.Connector.Api.Controllers;

[ApiController]
[Route("clients")]
public class ClientsController : ControllerBase
{
    private readonly ISubiektApiClient _api;

    public ClientsController(ISubiektApiClient api) => _api = api;

    /// <summary>List clients (paged).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string[]? filters = null,
        [FromQuery] string? orderBy = null,
        CancellationToken ct = default)
    {
        var result = await _api.GetClientsAsync(pageNumber, pageSize, filters, orderBy, ct);
        return Ok(result);
    }

    /// <summary>Get a single client by Guid ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _api.GetClientAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Create a new client.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientDto dto, CancellationToken ct)
    {
        var result = await _api.CreateClientAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id?.Value }, result);
    }

    /// <summary>Update an existing client (requires ETag via If-Match header).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateClientDto dto,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return BadRequest("If-Match header is required for PUT operations.");

        var etag = ifMatch.Trim('"');
        var result = await _api.UpdateClientAsync(id, dto, etag, ct);
        return Ok(result);
    }

    /// <summary>Delete a client by Guid ID.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _api.DeleteClientAsync(id, ct);
        return NoContent();
    }
}
