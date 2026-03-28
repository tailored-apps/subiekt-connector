using Microsoft.AspNetCore.Mvc;
using Subiekt.Connector.Contracts;
using Subiekt.Connector.Api.Services;

namespace Subiekt.Connector.Api.Controllers;

[ApiController]
[Route("documents")]
public class DocumentsController : ControllerBase
{
    private readonly ISubiektApiClient _api;

    public DocumentsController(ISubiektApiClient api) => _api = api;

    /// <summary>List documents (paged).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string[]? filters = null,
        [FromQuery] string? orderBy = null,
        CancellationToken ct = default)
    {
        var result = await _api.GetDocumentsAsync(pageNumber, pageSize, filters, orderBy, ct);
        return Ok(result);
    }

    /// <summary>Get a single document by Guid ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _api.GetDocumentAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Create a new document.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentDto dto, CancellationToken ct)
    {
        var result = await _api.CreateDocumentAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id?.Value }, result);
    }

    /// <summary>Update an existing document (requires ETag via If-Match header).</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDocumentDto dto,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return BadRequest("If-Match header is required for PUT operations.");

        var etag = ifMatch.Trim('"');
        var result = await _api.UpdateDocumentAsync(id, dto, etag, ct);
        return Ok(result);
    }

    /// <summary>Delete a document by Guid ID.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _api.DeleteDocumentAsync(id, ct);
        return NoContent();
    }

    /// <summary>Trigger printing for a document.</summary>
    [HttpPost("{id:guid}/printing")]
    public async Task<IActionResult> Print(Guid id, [FromBody] DocumentPrintingSettingsDto request, CancellationToken ct)
    {
        await _api.PrintDocumentAsync(id, request, ct);
        return Ok(new { message = "Print job submitted." });
    }
}
