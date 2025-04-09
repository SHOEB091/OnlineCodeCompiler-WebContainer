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

        [HttpPost("run-code")]
        public async Task<ActionResult<UserInputResponse>> RunCode([FromBody] UserInputRequest request)
        {
            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(new UserInputResponse
                    {
                        Success = false,
                        CompilationError = "Code cannot be empty"
                    });
                }

                if (string.IsNullOrEmpty(request.Language))
                {
                    return BadRequest(new UserInputResponse
                    {
                        Success = false,
                        CompilationError = "Language cannot be empty"
                    });
                }

                var result = await _compilerService.RunCodeWithInputAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing code execution request");
                return BadRequest(new UserInputResponse
                {
                    Success = false,
                    CompilationError = ex.Message
                });
            }
        }

        [HttpPost("run-tests")]
        public async Task<ActionResult<TestResult>> RunTests([FromBody] TestRequest request)
        {
            try
            {
                // Validate the request
                if (string.IsNullOrEmpty(request.Code))
                {
                    return BadRequest(new TestResult
                    {
                        Success = false,
                        CompilationError = "Code cannot be empty"
                    });
                }

                if (string.IsNullOrEmpty(request.Language))
                {
                    return BadRequest(new TestResult
                    {
                        Success = false,
                        CompilationError = "Language cannot be empty"
                    });
                }

                if (request.TestCases == null || !request.TestCases.Any())
                {
                    return BadRequest(new TestResult
                    {
                        Success = false,
                        CompilationError = "At least one test case is required"
                    });
                }

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