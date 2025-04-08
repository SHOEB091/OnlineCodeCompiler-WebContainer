using System.Diagnostics;
using CodeCompilerService.Models;

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
                        // Clean up the output for C#
                        if (request.Language.ToLower() == "csharp")
                        {
                            // Split by newlines and take the last non-empty line
                            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                            output = lines.LastOrDefault() ?? string.Empty;
                        }
                        
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

        private string GetExecutionCommand(string language, string fileName)
        {
            return language.ToLower() switch
            {
                "python" => $"cd {Path.GetDirectoryName(fileName)} && python3 {Path.GetFileName(fileName)}",
                "java" => $"cd {Path.GetDirectoryName(fileName)} && javac {Path.GetFileName(fileName)} && java -cp {Path.GetDirectoryName(fileName)} Main",
                "c" => $"cd {Path.GetDirectoryName(fileName)} && gcc {Path.GetFileName(fileName)} -o {Path.Combine(Path.GetDirectoryName(fileName), "program")} && {Path.Combine(Path.GetDirectoryName(fileName), "program")}",
                "cpp" => $"cd {Path.GetDirectoryName(fileName)} && g++ {Path.GetFileName(fileName)} -o {Path.Combine(Path.GetDirectoryName(fileName), "program")} && {Path.Combine(Path.GetDirectoryName(fileName), "program")}",
                "csharp" => $"cd {Path.GetDirectoryName(fileName)} && dotnet build --nologo --verbosity quiet >/dev/null && dotnet run --no-build --nologo",
                "javascript" => $"cd {Path.GetDirectoryName(fileName)} && node {Path.GetFileName(fileName)}",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }

        private async Task<string> SaveCodeToFile(string code, string language)
        {
            string fileName = GetFileName(language);
            
            // For Java, check if we need to rename the file before writing
            if (language.ToLower() == "java" && code.Contains("public class Main"))
            {
                fileName = Path.Combine(_tempDirectory, "Main.java");
            }
            // For C#, create a proper project structure
            else if (language.ToLower() == "csharp")
            {
                string projectDir = Path.GetDirectoryName(fileName);
                string projectName = Path.GetFileNameWithoutExtension(fileName);
                
                // Create project file
                string projectFile = Path.Combine(projectDir, $"{projectName}.csproj");
                await File.WriteAllTextAsync(projectFile, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <SuppressNETCoreSdkPreviewMessage>true</SuppressNETCoreSdkPreviewMessage>
    <NoWarn>CS8602</NoWarn>
  </PropertyGroup>
</Project>");

                // Create Program.cs with null-safe code
                fileName = Path.Combine(projectDir, "Program.cs");
                code = code.Replace("Console.ReadLine().Split()", "Console.ReadLine()?.Split() ?? Array.Empty<string>()");
            }
            
            await File.WriteAllTextAsync(fileName, code);
            return fileName;
        }

        private string GetFileName(string language)
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

            return Path.Combine(_tempDirectory, $"code_{Guid.NewGuid()}{extension}");
        }
    }
} 