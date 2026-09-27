using DeveloperEngineeringAcademy.Core;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperEngineeringAcademy.Web.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(ISearchService search) : ControllerBase
{
    [HttpGet]
    public IActionResult Search([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<SearchResult>());
        return Ok(search.Search(q, page, pageSize));
    }
}
