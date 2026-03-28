using Microsoft.AspNetCore.Mvc;
using Subiekt.Connector.Contracts;
using Subiekt.Connector.Api.Services;

namespace Subiekt.Connector.Api.Controllers;

[ApiController]
[Route("products")]
public class ProductsController : ControllerBase
{
    private readonly ISubiektApiClient _api;

    public ProductsController(ISubiektApiClient api) => _api = api;

    /// <summary>List products (paged).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string[]? filters = null,
        [FromQuery] string? orderBy = null,
        CancellationToken ct = default)
    {
        var result = await _api.GetProductsAsync(pageNumber, pageSize, filters, orderBy, ct);
        return Ok(result);
    }

    /// <summary>Get a single product by Guid ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _api.GetProductAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Create a new product.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        var result = await _api.CreateProductAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id?.Value }, result);
    }

    /// <summary>Update an existing product (requires ETag via If-Match header).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductDto dto,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return BadRequest("If-Match header is required for PUT operations.");

        var etag = ifMatch.Trim('"');
        var result = await _api.UpdateProductAsync(id, dto, etag, ct);
        return Ok(result);
    }

    /// <summary>Delete a product by Guid ID.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _api.DeleteProductAsync(id, ct);
        return NoContent();
    }
}
