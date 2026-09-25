using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace GenZCoders.Controllers
{
    public class RunCodeRequest
    {
        public int LanguageId { get; set; }
        public string SourceCode { get; set; } = string.Empty;
        public string? Stdin { get; set; }
    }

    // Proxies the code playground to Judge0 so the RapidAPI key stays on the server
    // (it used to be shipped to every browser as VITE_JUDGE0_API_KEY).
    [ApiController]
    [Route("api/[controller]")]
    public class CodeRunnerController : ControllerBase
    {
        private const int MaxSourceLength = 64 * 1024;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public CodeRunnerController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpPost("run")]
        public async Task<IActionResult> Run([FromBody] RunCodeRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SourceCode))
                return BadRequest(new { message = "Source code is required." });
            if (request.SourceCode.Length > MaxSourceLength)
                return BadRequest(new { message = "Source code is too long." });

            var apiKey = _config["Judge0:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "The code runner is not configured." });

            var baseUrl = (_config["Judge0:BaseUrl"] ?? "https://judge0-ce.p.rapidapi.com").TrimEnd('/');
            var host = _config["Judge0:Host"] ?? new Uri(baseUrl).Host;

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/submissions?base64_encoded=false&wait=true")
            {
                Content = JsonContent.Create(new
                {
                    language_id = request.LanguageId,
                    source_code = request.SourceCode,
                    stdin = request.Stdin ?? string.Empty
                })
            };
            message.Headers.Add("X-RapidAPI-Key", apiKey);
            message.Headers.Add("X-RapidAPI-Host", host);

            using var response = await client.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "The code runner failed. Please try again." });

            return Content(body, "application/json");
        }
    }
}
