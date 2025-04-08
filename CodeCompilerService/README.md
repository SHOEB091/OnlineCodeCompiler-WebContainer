# Code Compiler Service

A multi-language code compiler service built with .NET that supports Python, Java, C, C++, C#, and JavaScript.

## Features

- Compile and run code in multiple programming languages
- Support for standard input
- Execution time tracking
- Error handling and reporting
- Docker containerization

## Supported Languages

- Python
- Java
- C
- C++
- C#
- JavaScript

## API Endpoints

### Compile and Run Code

```
POST /api/compiler/compile
```

Request Body:
```json
{
    "code": "print('Hello, World!')",
    "language": "python",
    "input": "optional input"
}
```

Response:
```json
{
    "success": true,
    "output": "Hello, World!",
    "error": null,
    "executionTime": 123
}
```

## Running the Service

### Local Development

1. Install .NET SDK 8.0
2. Clone the repository
3. Run the following commands:
   ```bash
   cd CodeCompilerService
   dotnet restore
   dotnet run
   ```

### Docker

1. Build the Docker image:
   ```bash
   docker build -t code-compiler-service .
   ```

2. Run the container:
   ```bash
   docker run -p 80:80 -p 443:443 code-compiler-service
   ```

## Security Considerations

- The service runs code in a sandboxed environment
- Temporary files are automatically cleaned up
- Input validation is performed on all requests
- Resource limits should be implemented in production

## Integration with Online Assessment Platform

To integrate this service with your online assessment platform:

1. Deploy the service to your infrastructure
2. Make HTTP POST requests to the `/api/compiler/compile` endpoint
3. Handle the response appropriately in your platform

Example integration code:
```csharp
public async Task<CompileResponse> CompileCode(string code, string language, string input = null)
{
    var request = new CompileRequest
    {
        Code = code,
        Language = language,
        Input = input
    };

    using var client = new HttpClient();
    var response = await client.PostAsJsonAsync("https://your-compiler-service/api/compiler/compile", request);
    return await response.Content.ReadFromJsonAsync<CompileResponse>();
}
```

## License

MIT 