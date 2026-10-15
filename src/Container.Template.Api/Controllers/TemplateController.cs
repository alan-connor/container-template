using Microsoft.AspNetCore.Mvc;

namespace Container.Template.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class TemplateController : ControllerBase
    {
        private readonly ILogger<TemplateController> _logger;

        public TemplateController(ILogger<TemplateController> logger)
        {
            _logger = logger;
        }

        [HttpGet("hello")]
        public IActionResult GetHelloWorld()
        {
            _logger.LogInformation("Hello World endpoint called");

            var response = new
            {
                Message = "Hello World",
                Timestamp = DateTime.UtcNow,
                ServerName = Environment.MachineName,
                ApplicationName = "Container.Template.Api",
                Details = new
                {
                    RequestId = HttpContext.TraceIdentifier,
                    StatusCode = 200,
                    IsSuccessful = true
                },
                Tags = new[] { "greeting", "test", "api" }
            };

            return Ok(response);
        }
    }
}
