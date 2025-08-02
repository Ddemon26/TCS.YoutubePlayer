# ProcessExecutor - Enhanced Process Management

The ProcessExecutor class provides robust external process execution with advanced features including timeout support, performance monitoring, and enhanced error handling.

## Overview

ProcessExecutor is a core component of TCS.YoutubePlayer that manages external tool execution (yt-dlp, FFmpeg) with enterprise-grade reliability and monitoring capabilities.

## Key Features

- **Timeout Support**: Optional timeout parameter for process execution
- **Performance Monitoring**: Automatic execution time tracking and logging
- **Enhanced Cancellation Handling**: Clear differentiation between timeout and user cancellation
- **Resource Management**: Proper disposal patterns preventing resource leaks
- **Comprehensive Logging**: Detailed process lifecycle logging

## Class Declaration

```csharp
namespace TCS.YoutubePlayer.ProcessExecution
{
    public class ProcessExecutor : IDisposable
    {
        public ProcessExecutor(string ffmpegPath);
        public Task<ProcessResult> RunProcessAsync(
            string fileName,
            string arguments,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null
        );
        public void UpdateFFmpegPath(string ffmpegPath);
        public void Dispose();
    }
}
```

## Constructor

### `ProcessExecutor(string ffmpegPath)`

Creates a new ProcessExecutor instance with the specified FFmpeg path.

**Parameters:**
- `ffmpegPath` - Path to the FFmpeg executable

**Example:**
```csharp
var executor = new ProcessExecutor(@"C:\Tools\ffmpeg\bin\ffmpeg.exe");
```

## Methods

### `RunProcessAsync` - Enhanced Process Execution

```csharp
public Task<ProcessResult> RunProcessAsync(
    string fileName,
    string arguments,
    CancellationToken cancellationToken,
    TimeSpan? timeout = null
)
```

Executes an external process asynchronously with comprehensive monitoring and control.

**Parameters:**
- `fileName` - Path to executable file
- `arguments` - Command line arguments
- `cancellationToken` - Cancellation token for user-initiated cancellation
- `timeout` - Optional timeout for automatic process termination

**Returns:** `Task<ProcessResult>` containing exit code and output

**Throws:**
- `ArgumentException` - If fileName is null or empty
- `ArgumentNullException` - If arguments is null
- `TimeoutException` - If process execution exceeds the specified timeout
- `OperationCanceledException` - If cancelled via cancellation token
- `YtDlpException` - If process fails to start or executable not found

## Enhanced Features

### Timeout Support

The timeout parameter allows automatic termination of long-running processes:

```csharp
// Example: yt-dlp with 5-minute timeout
var result = await executor.RunProcessAsync(
    "yt-dlp",
    "--extract-flat --dump-json https://youtube.com/watch?v=abc123",
    cancellationToken,
    TimeSpan.FromMinutes(5)
);
```

#### Timeout vs Cancellation

The system clearly differentiates between timeout and user cancellation:

```csharp
try
{
    var result = await executor.RunProcessAsync(fileName, args, token, timeout);
}
catch (TimeoutException ex)
{
    // Process was terminated due to timeout
    Logger.LogWarning($"Process timed out: {ex.Message}");
}
catch (OperationCanceledException ex)
{
    // Process was cancelled by user
    Logger.LogWarning($"Process cancelled by user: {ex.Message}");
}
```

### Performance Monitoring

#### Automatic Time Tracking

Every process execution is automatically timed and logged:

```csharp
// Automatic logging output examples:
// "[YoutubePlayer] [PERF] yt-dlp took 1247.50ms"
// "[YoutubePlayer] Process yt-dlp completed successfully in 1247ms"
// "[YoutubePlayer] Process ffmpeg failed with exit code 1 after 2341ms"
```

#### Performance Metrics

The following metrics are automatically tracked:

- **Execution Duration**: Total time from process start to completion
- **Success/Failure Status**: Exit code analysis
- **Resource Usage**: Process lifecycle management timing
- **Cancellation Reason**: Whether timeout or user-initiated

#### Accessing Performance Data

```csharp
var startTime = DateTime.UtcNow;
var result = await executor.RunProcessAsync(fileName, args, token, timeout);
var duration = DateTime.UtcNow - startTime;

if (result.IsSuccess)
{
    Logger.Log($"Process completed successfully in {duration.TotalMilliseconds:F0}ms");
}
```

### Resource Management

#### Enhanced Disposal

ProcessExecutor implements proper disposal patterns:

```csharp
using var executor = new ProcessExecutor(ffmpegPath);
var result = await executor.RunProcessAsync(fileName, args, token, timeout);
// Automatic cleanup when using block exits
```

#### Process Cleanup

Each process is properly cleaned up regardless of completion method:

- Normal completion
- Timeout termination
- User cancellation
- Exception scenarios

## Usage Examples

### Basic Usage

```csharp
var executor = new ProcessExecutor(@"C:\Tools\ffmpeg\bin\ffmpeg.exe");

try
{
    var result = await executor.RunProcessAsync(
        "yt-dlp",
        "--version",
        CancellationToken.None
    );
    
    if (result.IsSuccess)
    {
        Console.WriteLine($"yt-dlp version: {result.StandardOutput}");
    }
}
finally
{
    executor.Dispose();
}
```

### With Timeout

```csharp
var timeout = TimeSpan.FromMinutes(2);

try
{
    var result = await executor.RunProcessAsync(
        "yt-dlp",
        "--extract-flat --dump-json " + videoUrl,
        cancellationToken,
        timeout
    );
    
    // Process completed within timeout
    return ProcessVideoInfo(result.StandardOutput);
}
catch (TimeoutException)
{
    Logger.LogWarning("Video processing timed out after 2 minutes");
    return null;
}
```

### With Cancellation

```csharp
using var cts = new CancellationTokenSource();

// Cancel after 30 seconds
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var result = await executor.RunProcessAsync(
        "ffmpeg",
        "-i input.webm -c:v libx264 output.mp4",
        cts.Token,
        TimeSpan.FromMinutes(5) // 5-minute timeout
    );
}
catch (OperationCanceledException)
{
    Logger.LogWarning("Conversion cancelled by user");
}
catch (TimeoutException)
{
    Logger.LogWarning("Conversion timed out");
}
```

### FFmpeg Path Handling

```csharp
var executor = new ProcessExecutor(@"C:\Tools\ffmpeg\bin\ffmpeg.exe");

// When fileName is "ffmpeg", the executor automatically uses the configured path
var result = await executor.RunProcessAsync(
    "ffmpeg", // Automatically resolved to full path
    "-version",
    CancellationToken.None
);

// Update FFmpeg path at runtime
executor.UpdateFFmpegPath(@"C:\NewPath\ffmpeg.exe");
```

## Error Handling Best Practices

### Comprehensive Error Handling

```csharp
public async Task<ProcessResult> SafeProcessExecution(
    string fileName, 
    string arguments,
    CancellationToken token,
    TimeSpan? timeout = null)
{
    try
    {
        return await executor.RunProcessAsync(fileName, arguments, token, timeout);
    }
    catch (TimeoutException ex)
    {
        Logger.LogWarning($"Process {fileName} timed out: {ex.Message}");
        // Handle timeout scenario
        return ProcessResult.Failed;
    }
    catch (OperationCanceledException ex) when (token.IsCancellationRequested)
    {
        Logger.LogWarning($"Process {fileName} cancelled by user: {ex.Message}");
        // Handle user cancellation
        return ProcessResult.Cancelled;
    }
    catch (YtDlpException ex)
    {
        Logger.LogError($"Tool execution failed: {ex.Message}");
        // Handle tool-specific errors
        return ProcessResult.Failed;
    }
    catch (Exception ex)
    {
        Logger.LogError($"Unexpected error in process execution: {ex.Message}");
        // Handle unexpected errors
        return ProcessResult.Failed;
    }
}
```

### Logging and Diagnostics

Monitor process execution through built-in logging:

```csharp
// Enable detailed logging in development
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    // Performance metrics are automatically logged
    // Process lifecycle events are logged
    // Error details are logged with context
#endif
```

## Integration with YoutubePlayer

ProcessExecutor is seamlessly integrated into the YoutubePlayer workflow:

```csharp
// Automatic usage in YoutubePlayer
public class YtDlpExternalTool
{
    private static readonly ProcessExecutor s_processExecutor = new(ffmpegPath);
    
    public static async Task<string> GetDirectUrlAsync(string url, CancellationToken token)
    {
        // Automatic timeout handling for video URL extraction
        var result = await s_processExecutor.RunProcessAsync(
            ytDlpPath,
            $"--get-url {url}",
            token,
            TimeSpan.FromMinutes(3) // 3-minute timeout for URL extraction
        );
        
        return result.IsSuccess ? result.StandardOutput.Trim() : null;
    }
}
```

## Performance Optimization Tips

1. **Set Appropriate Timeouts**: Use reasonable timeouts based on operation type
   - URL extraction: 1-3 minutes
   - Video conversion: 5-15 minutes
   - Tool downloads: 2-5 minutes

2. **Monitor Performance Logs**: Use logged metrics to identify bottlenecks

3. **Proper Resource Management**: Always dispose ProcessExecutor instances

4. **Handle Cancellation Gracefully**: Implement proper cancellation handling for better user experience

## Thread Safety

ProcessExecutor is thread-safe and can be used concurrently. However, each instance should typically be used for related operations to maintain FFmpeg path consistency.

## See Also

- **[YoutubePlayer API](../api/youtube-player.md)**: Main player component
- **[System Overview](../core/overview.md)**: Architecture overview
- **[Error Handling Guide](error-handling.md)**: Comprehensive error handling patterns
- **[Performance Monitoring](performance-monitoring.md)**: Performance optimization guide