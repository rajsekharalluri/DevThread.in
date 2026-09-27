using DeveloperEngineeringAcademy.Core;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperEngineeringAcademy.Web.Controllers;

[ApiController]
[Route("api/content")]
public sealed class ValidationController(IContentValidator validator) : ControllerBase
{
    [HttpGet("validate")]
    public IActionResult Validate() => Ok(validator.Validate());
}
