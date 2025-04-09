using CodeCompilerService.Models;

namespace CodeCompilerService.Services
{
    public interface ICompilerService
    {
        Task<TestResult> RunTestsAsync(TestRequest request);
        Task<UserInputResponse> RunCodeWithInputAsync(UserInputRequest request);
    }
} 