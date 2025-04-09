using System.Diagnostics;
using CodeCompilerService.Models;
using System.Text.RegularExpressions;

namespace CodeCompilerService.Services
{
    public class CompilerService : ICompilerService
    {
        private readonly ILogger<CompilerService> _logger;
        private readonly string _tempDirectory;

        public CompilerService(ILogger<CompilerService> logger)
        {
            _logger = logger;
            _tempDirectory = Path.Combine(Path.GetTempPath(), "CodeCompiler");
            Directory.CreateDirectory(_tempDirectory);
        }

        private string GetJavaClassName(string code)
        {
            // Try to find public class declaration
            var match = Regex.Match(code, @"public\s+class\s+(\w+)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "Solution"; // Default class name if no public class found
        }

        private string GetFileName(string language, string code)
        {
            string extension = language.ToLower() switch
            {
                "python" => ".py",
                "java" => ".java",
                "c" => ".c",
                "cpp" => ".cpp",
                "csharp" => ".cs",
                "javascript" => ".js",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };

            string fileName = language.ToLower() switch
            {
                "java" => GetJavaClassName(code),
                _ => $"code_{Guid.NewGuid()}"
            };

            return Path.Combine(_tempDirectory, $"{fileName}{extension}");
        }

        private string GetExecutionCommand(string language, string fileName)
        {
            string baseFileName = Path.GetFileNameWithoutExtension(fileName);
            return language.ToLower() switch
            {
                "python" => $"cd {Path.GetDirectoryName(fileName)} && python3 {Path.GetFileName(fileName)}",
                "java" => $"cd {Path.GetDirectoryName(fileName)} && javac {Path.GetFileName(fileName)} && java -cp {Path.GetDirectoryName(fileName)} {baseFileName}",
                "c" => $"cd {Path.GetDirectoryName(fileName)} && gcc {Path.GetFileName(fileName)} -o {Path.Combine(Path.GetDirectoryName(fileName), "program")} && {Path.Combine(Path.GetDirectoryName(fileName), "program")}",
                "cpp" => $"cd {Path.GetDirectoryName(fileName)} && g++ {Path.GetFileName(fileName)} -o {Path.Combine(Path.GetDirectoryName(fileName), "program")} && {Path.Combine(Path.GetDirectoryName(fileName), "program")}",
                "csharp" => $"cd {Path.GetDirectoryName(fileName)} && dotnet new console --force > /dev/null 2>&1 && dotnet build --nologo --verbosity quiet > /dev/null 2>&1 && dotnet run --no-build --nologo",
                "javascript" => $"cd {Path.GetDirectoryName(fileName)} && node {Path.GetFileName(fileName)}",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }

        private async Task<string> SaveCodeToFile(string code, string language)
        {
            string fileName = GetFileName(language, code);
            if (language.ToLower() == "csharp")
            {
                // For C#, we need to create a proper project structure
                string projectDir = Path.GetDirectoryName(fileName);
                string projectFile = Path.Combine(projectDir, "CodeCompiler.csproj");
                
                // Create the project file
                await File.WriteAllTextAsync(projectFile, @"<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>
</Project>");

                // Create Program.cs in the project directory
                fileName = Path.Combine(projectDir, "Program.cs");
                
                // Ensure the code has proper namespace and class structure
                if (!code.Contains("namespace") && !code.Contains("class"))
                {
                    code = $@"using System;

namespace CodeCompiler
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            {code}
        }}
    }}
}}";
                }
            }
            await File.WriteAllTextAsync(fileName, code);
            return fileName;
        }

        public async Task<UserInputResponse> RunCodeWithInputAsync(UserInputRequest request)
        {
            var response = new UserInputResponse();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                string fileName = await SaveCodeToFile(request.Code, request.Language);
                using var process = new Process();
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{GetExecutionCommand(request.Language, fileName)}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                
                // Write user input
                if (!string.IsNullOrEmpty(request.UserInput))
                {
                    await process.StandardInput.WriteLineAsync(request.UserInput);
                }
                process.StandardInput.Close();

                // Set timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(request.TimeoutSeconds));
                var processTask = process.WaitForExitAsync();
                
                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    process.Kill();
                    response.Error = "Execution timed out";
                    response.Success = false;
                }
                else
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        response.Error = error;
                        response.Success = false;
                    }
                    else
                    {
                        // For C#, filter out the dotnet messages
                        if (request.Language.ToLower() == "csharp")
                        {
                            // Split by newlines and take the last line that's not empty
                            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                            output = lines.LastOrDefault() ?? "";
                        }
                        response.Output = output.Trim();
                        response.Success = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running code with input");
                response.Error = ex.Message;
                response.Success = false;
            }

            stopwatch.Stop();
            return response;
        }

        public async Task<TestResult> RunTestsAsync(TestRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestResult
            {
                TotalTests = request.TestCases.Count
            };

            try
            {
                string fileName = await SaveCodeToFile(request.Code, request.Language);
                var testResults = new List<TestCaseResult>();

                foreach (var testCase in request.TestCases)
                {
                    var testResult = await RunTestCase(fileName, request, testCase);
                    testResults.Add(testResult);
                }

                result.TestResults = testResults;
                result.PassedTests = testResults.Count(r => r.Passed);
                result.Success = result.PassedTests == result.TotalTests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running tests");
                result.CompilationError = ex.Message;
                result.Success = false;
            }

            stopwatch.Stop();
            result.TotalExecutionTime = stopwatch.ElapsedMilliseconds;
            return result;
        }

        private async Task<TestCaseResult> RunTestCase(string fileName, TestRequest request, TestCase testCase)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TestCaseResult
            {
                Input = testCase.Input,
                ExpectedOutput = testCase.ExpectedOutput
            };

            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{GetExecutionCommand(request.Language, fileName)}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                
                // Write input
                await process.StandardInput.WriteLineAsync(testCase.Input);
                process.StandardInput.Close();

                // Set timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(request.TimeoutSeconds));
                var processTask = process.WaitForExitAsync();
                
                if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                {
                    process.Kill();
                    result.Error = "Execution timed out";
                    result.Passed = false;
                }
                else
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        result.Error = error;
                        result.Passed = false;
                    }
                    else
                    {
                        result.ActualOutput = output.Trim();
                        result.Passed = result.ActualOutput == testCase.ExpectedOutput;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                result.Passed = false;
            }

            stopwatch.Stop();
            result.ExecutionTime = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }
} 