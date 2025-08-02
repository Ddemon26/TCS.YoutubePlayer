# Error Handling Guide

TCS.YoutubePlayer implements comprehensive error handling with clear differentiation between error types and robust recovery mechanisms. This guide covers error handling patterns, best practices, and troubleshooting.

## Error Types and Classification

### System-Level Errors

#### `TimeoutException`
Thrown when processes exceed their configured timeout duration.

```csharp
try
{
    var result = await processExecutor.RunProcessAsync(
        "yt-dlp", args, token, TimeSpan.FromMinutes(5)
    );
}
catch (TimeoutException ex)
{
    Logger.LogWarning($"Process timed out: {ex.Message}");
    // Handle timeout - possibly retry with longer timeout
}
```

**Common Scenarios:**
- Long video URL processing
- Slow network connections
- Large video file conversions
- External tool downloads

#### `OperationCanceledException`
Thrown when operations are cancelled by user request or system shutdown.

```csharp
try
{
    await youtubePlayer.PlayVideo(url);
}
catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
{
    Logger.LogWarning("Video loading cancelled by user");
    // Clean up UI state
}
```

**Common Scenarios:**
- User-initiated cancellation
- Application shutdown
- Scene changes during loading

### Tool-Specific Errors

#### `YtDlpException`
Thrown for yt-dlp tool-related errors.

```csharp
catch (YtDlpException ex)
{
    Logger.LogError($"yt-dlp error: {ex.Message}");
    
    if (ex.Message.Contains("Requested format is not available"))
    {
        // Try alternative quality
        await TryAlternativeQuality();
    }
    else if (ex.Message.Contains("Video unavailable"))
    {
        // Show user-friendly error
        ShowVideoUnavailableError();
    }
}
```

**Common Scenarios:**
- Invalid video URLs
- Geo-restricted content
- Private or deleted videos
- Unsupported video formats

#### Tool Initialization Errors

```csharp
if (youtubePlayer.InitializationFailed)
{
    Logger.LogError("YoutubePlayer initialization failed");
    
    // Attempt to reinitialize
    await ReinitializeTools();
    
    if (youtubePlayer.InitializationFailed)
    {
        // Show error to user and disable video features
        DisableVideoFeatures();
    }
}
```

### Network-Related Errors

#### Connection Issues
```csharp
catch (HttpRequestException ex)
{
    Logger.LogError($"Network error: {ex.Message}");
    
    if (IsInternetAvailable())
    {
        // Retry with exponential backoff
        await RetryWithBackoff(operation, maxRetries: 3);
    }
    else
    {
        ShowNetworkErrorDialog();
    }
}
```

#### Download Failures
```csharp
catch (Exception ex) when (ex.Message.Contains("download"))
{
    Logger.LogError($"Download failed: {ex.Message}");
    
    // Clear cache and retry
    ClearCache();
    await RetryOperation();
}
```

## Error Handling Patterns

### Comprehensive Error Handling Template

```csharp
public async Task<VideoResult> SafeVideoOperation(string url)
{
    try
    {
        // Check initialization state
        if (!youtubePlayer.IsInitialized)
        {
            if (youtubePlayer.InitializationFailed)
            {
                return VideoResult.InitializationFailed;
            }
            
            // Wait for initialization
            await WaitForInitialization(timeoutSeconds: 30);
        }
        
        // Perform operation with timeout
        var result = await PerformVideoOperation(url);
        return VideoResult.Success(result);
    }
    catch (TimeoutException ex)
    {
        Logger.LogWarning($"Operation timed out: {ex.Message}");
        return VideoResult.Timeout;
    }
    catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
    {
        Logger.LogWarning("Operation cancelled by user");
        return VideoResult.Cancelled;
    }
    catch (YtDlpException ex)
    {
        Logger.LogError($"yt-dlp error: {ex.Message}");
        return VideoResult.ToolError(ex);
    }
    catch (Exception ex)
    {
        Logger.LogError($"Unexpected error: {ex.Message}");
        return VideoResult.UnexpectedError(ex);
    }
}
```

### Retry Logic with Exponential Backoff

```csharp
public async Task<T> RetryWithBackoff<T>(
    Func<Task<T>> operation,
    int maxRetries = 3,
    TimeSpan baseDelay = default)
{
    if (baseDelay == default)
        baseDelay = TimeSpan.FromSeconds(1);
    
    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (attempt < maxRetries && IsRetryableError(ex))
        {
            var delay = TimeSpan.FromMilliseconds(
                baseDelay.TotalMilliseconds * Math.Pow(2, attempt)
            );
            
            Logger.LogWarning($"Attempt {attempt + 1} failed, retrying in {delay.TotalSeconds}s: {ex.Message}");
            await Task.Delay(delay);
        }
    }
    
    throw new InvalidOperationException($"Operation failed after {maxRetries + 1} attempts");
}

private bool IsRetryableError(Exception ex)
{
    return ex is TimeoutException ||
           ex is HttpRequestException ||
           (ex is YtDlpException ytEx && ytEx.Message.Contains("network"));
}
```

### Graceful Degradation

```csharp
public async Task<PlaybackResult> PlayVideoWithFallback(string url)
{
    try
    {
        // Try preferred quality first
        return await PlayVideoAtQuality(url, VideoQuality.HD720);
    }
    catch (YtDlpException ex) when (ex.Message.Contains("format"))
    {
        Logger.LogWarning("HD720 not available, trying fallback quality");
        
        try
        {
            // Fallback to lower quality
            return await PlayVideoAtQuality(url, VideoQuality.Medium480);
        }
        catch (YtDlpException)
        {
            // Final fallback to auto quality
            return await PlayVideoAtQuality(url, VideoQuality.Auto);
        }
    }
}
```

## Error Recovery Mechanisms

### Tool Reinitialization

```csharp
public async Task<bool> ReinitializeTools()
{
    try
    {
        Logger.Log("Reinitializing external tools...");
        
        // Cancel any ongoing operations
        cancellationTokenSource.Cancel();
        cancellationTokenSource = new CancellationTokenSource();
        
        // Clear cache
        YtDlpUrlCache.ClearCache();
        
        // Reinitialize tools
        await YtDlpExternalTool.InitializeToolsAsync(cancellationTokenSource.Token);
        
        Logger.Log("Tools reinitialized successfully");
        return true;
    }
    catch (Exception ex)
    {
        Logger.LogError($"Tool reinitialization failed: {ex.Message}");
        return false;
    }
}
```

### Cache Recovery

```csharp
public void RecoverFromCacheCorruption()
{
    try
    {
        // Clear corrupted cache
        YtDlpUrlCache.ClearCache();
        
        // Reinitialize cache
        YtDlpUrlCache.Initialize();
        
        Logger.Log("Cache recovered successfully");
    }
    catch (Exception ex)
    {
        Logger.LogError($"Cache recovery failed: {ex.Message}");
        
        // Disable caching as fallback
        YtDlpUrlCache.DisableCaching();
    }
}
```

### Resource Cleanup

```csharp
public void CleanupResourcesOnError()
{
    try
    {
        // Stop any playing video
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // Cancel ongoing operations
        if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
        {
            cancellationTokenSource.Cancel();
        }
        
        // Clear temporary files
        CleanupTempFiles();
        
        Logger.Log("Resources cleaned up successfully");
    }
    catch (Exception ex)
    {
        Logger.LogError($"Resource cleanup failed: {ex.Message}");
    }
}
```

## Timeout vs Cancellation Handling

### Differentiation Logic

The system clearly differentiates between timeouts and user cancellations:

```csharp
// In ProcessExecutor
if (combinedToken.CanBeCanceled)
{
    ctr = combinedToken.Register(() => {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                
                string reason = timeoutCts?.Token.IsCancellationRequested == true 
                    ? "timeout" 
                    : "cancellation";
                Logger.LogWarning($"Killed process {fileName} due to {reason}.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Exception trying to kill process: {ex.Message}");
        }

        if (timeoutCts?.Token.IsCancellationRequested == true)
        {
            tcs.TrySetException(new TimeoutException($"Process {fileName} timed out after {timeout}"));
        }
        else
        {
            tcs.TrySetCanceled(cancellationToken);
        }
    });
}
```

### Handling Different Cancellation Types

```csharp
public async Task HandleCancellationTypes()
{
    using var userCts = new CancellationTokenSource();
    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
        userCts.Token, timeoutCts.Token
    );
    
    try
    {
        await LongRunningOperation(combinedCts.Token);
    }
    catch (OperationCanceledException)
    {
        if (timeoutCts.Token.IsCancellationRequested)
        {
            Logger.LogWarning("Operation timed out");
            // Handle timeout scenario
            ShowTimeoutMessage();
        }
        else if (userCts.Token.IsCancellationRequested)
        {
            Logger.LogWarning("Operation cancelled by user");
            // Handle user cancellation
            ShowCancellationMessage();
        }
    }
}
```

## Diagnostic and Logging

### Enhanced Error Logging

```csharp
public static class ErrorLogger
{
    public static void LogError(Exception ex, string context, object additionalData = null)
    {
        var errorData = new
        {
            Context = context,
            ExceptionType = ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            AdditionalData = additionalData,
            Timestamp = DateTime.UtcNow
        };
        
        Logger.LogError($"[ERROR] {context}: {ex.GetType().Name} - {ex.Message}");
        
        if (additionalData != null)
        {
            Logger.LogError($"[ERROR] Additional Data: {JsonUtility.ToJson(additionalData)}");
        }
        
        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        Logger.LogError($"[ERROR] Stack Trace: {ex.StackTrace}");
        #endif
    }
}
```

### Performance Impact Logging

```csharp
public static void LogPerformanceImpact(string operation, TimeSpan duration, bool wasSuccessful)
{
    var status = wasSuccessful ? "SUCCESS" : "FAILED";
    Logger.LogPerformance($"{operation} ({status})", duration);
    
    if (duration > TimeSpan.FromSeconds(10))
    {
        Logger.LogWarning($"[PERFORMANCE] {operation} took unusually long: {duration.TotalSeconds:F1}s");
    }
}
```

## Best Practices

### 1. Always Check Initialization State

```csharp
// Before any video operation
if (!youtubePlayer.IsInitialized && !youtubePlayer.InitializationFailed)
{
    await WaitForInitialization();
}

if (youtubePlayer.InitializationFailed)
{
    throw new InvalidOperationException("YoutubePlayer is not properly initialized");
}
```

### 2. Use Appropriate Timeouts

```csharp
// Different operations need different timeouts
var timeouts = new Dictionary<string, TimeSpan>
{
    ["url_extraction"] = TimeSpan.FromMinutes(3),
    ["video_conversion"] = TimeSpan.FromMinutes(15),
    ["tool_download"] = TimeSpan.FromMinutes(5),
    ["quick_operation"] = TimeSpan.FromSeconds(30)
};
```

### 3. Implement Circuit Breaker Pattern

```csharp
public class CircuitBreaker
{
    private int failureCount = 0;
    private DateTime lastFailureTime = DateTime.MinValue;
    private readonly int threshold;
    private readonly TimeSpan timeout;
    
    public async Task<T> Execute<T>(Func<Task<T>> operation)
    {
        if (failureCount >= threshold && 
            DateTime.UtcNow - lastFailureTime < timeout)
        {
            throw new InvalidOperationException("Circuit breaker is open");
        }
        
        try
        {
            var result = await operation();
            failureCount = 0; // Reset on success
            return result;
        }
        catch
        {
            failureCount++;
            lastFailureTime = DateTime.UtcNow;
            throw;
        }
    }
}
```

### 4. User-Friendly Error Messages

```csharp
public static string GetUserFriendlyErrorMessage(Exception ex)
{
    return ex switch
    {
        TimeoutException => "The operation took too long. Please check your internet connection and try again.",
        YtDlpException ytEx when ytEx.Message.Contains("unavailable") => "This video is not available or has been removed.",
        YtDlpException ytEx when ytEx.Message.Contains("private") => "This video is private and cannot be played.",
        OperationCanceledException => "The operation was cancelled.",
        _ => "An unexpected error occurred. Please try again."
    };
}
```

## Troubleshooting Common Issues

### Issue: Initialization Timeout

**Symptoms:** `InitializationFailed` is true, tools don't download

**Solutions:**
1. Check internet connection
2. Verify firewall/antivirus settings
3. Try manual tool installation
4. Increase initialization timeout

### Issue: Video Loading Timeout

**Symptoms:** Videos fail to load, timeout exceptions

**Solutions:**
1. Increase timeout for video operations
2. Check video URL validity
3. Try different video quality
4. Verify yt-dlp is up to date

### Issue: Resource Leaks

**Symptoms:** Memory usage increases over time

**Solutions:**
1. Ensure proper disposal of ProcessExecutor
2. Cancel operations before starting new ones
3. Clear cache periodically
4. Monitor resource usage in logs

## See Also

- **[ProcessExecutor Documentation](process-executor.md)**: Detailed process execution features
- **[YoutubePlayer API](../api/youtube-player.md)**: Main component API reference
- **[Performance Monitoring](performance-monitoring.md)**: Performance optimization guide
- **[System Overview](../core/overview.md)**: Architecture and error recovery mechanisms