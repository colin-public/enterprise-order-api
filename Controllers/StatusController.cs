using Microsoft.AspNetCore.Mvc;

namespace enterprise_order_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetStatus() => Ok(new
    {
        status = "healthy",
        service = "enterprise-order-api"
    });
}
