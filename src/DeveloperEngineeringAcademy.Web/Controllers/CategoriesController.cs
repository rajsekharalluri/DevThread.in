using DeveloperEngineeringAcademy.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperEngineeringAcademy.Web.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(CategoryService categories) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() => Ok(categories.GetCategories());
}
