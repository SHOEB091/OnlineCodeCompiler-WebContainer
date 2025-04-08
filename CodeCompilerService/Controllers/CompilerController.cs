using Microsoft.AspNetCore.Mvc;
using CodeCompilerService.Models;
using CodeCompilerService.Services;

namespace CodeCompilerService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompilerController : ControllerBase
    {
        private readonly ICompilerService _compilerService;
        private readonly ILogger<CompilerController> _logger;

        public CompilerController(ICompilerService compilerService, ILogger<CompilerController> logger)
        {
            _compilerService = compilerService;
            _logger = logger;
        }

        [HttpPost("run-tests")]
        public async Task<ActionResult<TestResult>> RunTests([FromBody] TestRequest request)
        {
            try
            {
                var result = await _compilerService.RunTestsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing test request");
                return BadRequest(new TestResult
                {
                    Success = false,
                    CompilationError = ex.Message
                });
            }
        }
    }
} 