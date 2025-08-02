# Performance Monitoring Guide

TCS.YoutubePlayer includes comprehensive performance monitoring capabilities to help developers optimize video playback operations and identify performance bottlenecks. This guide covers monitoring features, metrics interpretation, and optimization strategies.

## Overview

The performance monitoring system automatically tracks and logs key metrics across all major operations, providing valuable insights into system performance and helping identify areas for optimization.

## Automatic Performance Tracking

### Process Execution Metrics

All external process executions (yt-dlp, FFmpeg) are automatically timed and logged:

```csharp
// Automatic logging in ProcessExecutor
Logger.LogPerformance($"Process {Path.GetFileName(fileName)}", duration);

// Example output:
// "[YoutubePlayer] [PERF] yt-dlp took 1247.50ms"
// "[YoutubePlayer] Process yt-dlp completed successfully in 1247ms"
// "[YoutubePlayer] Process ffmpeg failed with exit code 1 after 2341ms"
```

### Key Metrics Tracked

#### Process Execution Times
- **yt-dlp URL extraction**: Time to extract video stream URLs
- **FFmpeg conversion**: Time for video format conversion
- **Tool downloads**: Time for external tool acquisition
- **Initialization**: Complete system initialization duration

#### Success/Failure Rates
- Process exit codes and success rates
- Timeout vs completion statistics
- Error frequency and types

#### Resource Usage
- Memory allocation patterns
- Process lifecycle management
- Resource cleanup timing

## Performance Logging API

### Logger.LogPerformance

The primary method for logging performance metrics:

```csharp
public static void LogPerformance(string operation, TimeSpan duration)
{
    LogInternal($"[PERF] {operation} took {duration.TotalMilliseconds:F2}ms", LogType.Log);
}
```

**Usage Example:**
```csharp
var startTime = DateTime.UtcNow;
await SomeOperation();
var duration = DateTime.UtcNow - startTime;
Logger.LogPerformance("Video URL Processing", duration);
```

### Custom Performance Tracking

Implement custom performance tracking for your operations:

```csharp
public class PerformanceTracker : IDisposable
{
    private readonly string operationName;
    private readonly DateTime startTime;
    
    public PerformanceTracker(string operationName)
    {
        this.operationName = operationName;
        this.startTime = DateTime.UtcNow;
    }
    
    public void Dispose()
    {
        var duration = DateTime.UtcNow - startTime;
        Logger.LogPerformance(operationName, duration);
    }
}

// Usage
using (new PerformanceTracker("Custom Video Operation"))
{
    await CustomVideoOperation();
} // Automatically logs performance when disposed
```

## Performance Metrics Interpretation

### Process Execution Times

#### Normal Performance Ranges

| Operation | Expected Range | Warning Threshold | Critical Threshold |
|-----------|---------------|-------------------|-------------------|
| URL Extraction | 1-5 seconds | > 10 seconds | > 30 seconds |
| Video Conversion | 5-60 seconds | > 120 seconds | > 300 seconds |
| Tool Download | 10-60 seconds | > 120 seconds | > 300 seconds |
| Initialization | 5-30 seconds | > 60 seconds | > 120 seconds |

#### Performance Indicators

```csharp
public static class PerformanceAnalyzer
{
    public static PerformanceStatus AnalyzeOperationTime(string operation, TimeSpan duration)
    {
        var thresholds = GetThresholds(operation);
        
        if (duration > thresholds.Critical)
            return PerformanceStatus.Critical;
        else if (duration > thresholds.Warning)
            return PerformanceStatus.Warning;
        else if (duration > thresholds.Expected)
            return PerformanceStatus.Slow;
        else
            return PerformanceStatus.Normal;
    }
    
    private static (TimeSpan Expected, TimeSpan Warning, TimeSpan Critical) GetThresholds(string operation)
    {
        return operation.ToLower() switch
        {
            "url_extraction" => (TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)),
            "video_conversion" => (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5)),
            "tool_download" => (TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5)),
            "initialization" => (TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2)),
            _ => (TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1))
        };
    }
}
```

### Performance Patterns

#### Healthy Performance Pattern
```
[YoutubePlayer] [PERF] yt-dlp took 1247.50ms
[YoutubePlayer] [PERF] ffmpeg took 2341.20ms
[YoutubePlayer] [PERF] Video URL Processing took 3588.70ms
```

#### Performance Issues Pattern
```
[YoutubePlayer] [PERF] yt-dlp took 15247.50ms  // Warning: Slow URL extraction
[YoutubePlayer] [PERF] ffmpeg took 95341.20ms  // Critical: Very slow conversion
[YoutubePlayer] [WARNING] Process yt-dlp killed due to timeout  // Timeout issues
```

## Performance Optimization Strategies

### 1. Timeout Configuration

Configure appropriate timeouts based on operation type and expected performance:

```csharp
public static class OptimalTimeouts
{
    public static readonly Dictionary<string, TimeSpan> Timeouts = new()
    {
        ["url_extraction"] = TimeSpan.FromMinutes(3),
        ["video_conversion_small"] = TimeSpan.FromMinutes(5),
        ["video_conversion_large"] = TimeSpan.FromMinutes(15),
        ["tool_download"] = TimeSpan.FromMinutes(5),
        ["initialization"] = TimeSpan.FromMinutes(2)
    };
    
    public static TimeSpan GetOptimalTimeout(string operation, long? fileSizeBytes = null)
    {
        if (operation == "video_conversion" && fileSizeBytes.HasValue)
        {
            // Adjust timeout based on file size
            var sizeMB = fileSizeBytes.Value / (1024 * 1024);
            if (sizeMB > 100)
                return Timeouts["video_conversion_large"];
        }
        
        return Timeouts.GetValueOrDefault(operation, TimeSpan.FromMinutes(5));
    }
}
```

### 2. Caching Optimization

Monitor cache performance and optimize based on metrics:

```csharp
public class CachePerformanceMonitor
{
    private int cacheHits = 0;
    private int cacheMisses = 0;
    
    public void RecordCacheHit()
    {
        Interlocked.Increment(ref cacheHits);
    }
    
    public void RecordCacheMiss()
    {
        Interlocked.Increment(ref cacheMisses);
    }
    
    public void LogCacheStatistics()
    {
        var total = cacheHits + cacheMisses;
        var hitRate = total > 0 ? (double)cacheHits / total * 100 : 0;
        
        Logger.LogPerformance($"Cache Hit Rate: {hitRate:F1}%", TimeSpan.Zero);
        
        if (hitRate < 50)
        {
            Logger.LogWarning("Low cache hit rate detected. Consider increasing cache size or retention time.");
        }
    }
}
```

### 3. Process Pool Management

Implement process pooling for frequently used operations:

```csharp
public class ProcessPool
{
    private readonly ConcurrentQueue<ProcessExecutor> availableExecutors = new();
    private readonly SemaphoreSlim semaphore;
    
    public ProcessPool(int maxConcurrentProcesses = 3)
    {
        semaphore = new SemaphoreSlim(maxConcurrentProcesses);
    }
    
    public async Task<ProcessResult> ExecuteWithPool(
        string fileName, 
        string arguments, 
        CancellationToken token,
        TimeSpan? timeout = null)
    {
        await semaphore.WaitAsync(token);
        
        try
        {
            using var performanceTracker = new PerformanceTracker($"Pooled {fileName}");
            
            if (!availableExecutors.TryDequeue(out var executor))
            {
                executor = new ProcessExecutor(ffmpegPath);
            }
            
            var result = await executor.RunProcessAsync(fileName, arguments, token, timeout);
            
            // Return executor to pool if still valid
            availableExecutors.Enqueue(executor);
            
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
```

### 4. Adaptive Quality Selection

Implement adaptive quality selection based on performance metrics:

```csharp
public class AdaptiveQualitySelector
{
    private readonly Dictionary<VideoQuality, List<TimeSpan>> performanceHistory = new();
    
    public VideoQuality SelectOptimalQuality(string videoUrl)
    {
        // Analyze historical performance for different qualities
        var avgPerformance = performanceHistory
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Average(ts => ts.TotalMilliseconds));
        
        // Select quality with best performance/quality ratio
        var optimalQuality = avgPerformance
            .Where(kvp => kvp.Value < 10000) // Under 10 seconds
            .OrderByDescending(kvp => (int)kvp.Key) // Prefer higher quality
            .FirstOrDefault().Key;
        
        return optimalQuality != default ? optimalQuality : VideoQuality.Auto;
    }
    
    public void RecordPerformance(VideoQuality quality, TimeSpan duration)
    {
        if (!performanceHistory.ContainsKey(quality))
            performanceHistory[quality] = new List<TimeSpan>();
        
        var history = performanceHistory[quality];
        history.Add(duration);
        
        // Keep only recent performance data
        if (history.Count > 10)
            history.RemoveAt(0);
    }
}
```

## Real-Time Performance Monitoring

### Performance Dashboard

Create a real-time performance monitoring dashboard:

```csharp
public class PerformanceDashboard : MonoBehaviour
{
    [Header("Performance Metrics")]
    public Text avgProcessTimeText;
    public Text successRateText;
    public Text cacheHitRateText;
    public Slider performanceIndicator;
    
    private readonly List<TimeSpan> recentProcessTimes = new();
    private int successfulOperations = 0;
    private int totalOperations = 0;
    
    void Start()
    {
        // Update dashboard every 5 seconds
        InvokeRepeating(nameof(UpdateDashboard), 1f, 5f);
    }
    
    public void RecordOperation(TimeSpan duration, bool successful)
    {
        recentProcessTimes.Add(duration);
        if (recentProcessTimes.Count > 20)
            recentProcessTimes.RemoveAt(0);
        
        totalOperations++;
        if (successful)
            successfulOperations++;
    }
    
    private void UpdateDashboard()
    {
        if (recentProcessTimes.Count > 0)
        {
            var avgTime = recentProcessTimes.Average(ts => ts.TotalMilliseconds);
            avgProcessTimeText.text = $"Avg Time: {avgTime:F0}ms";
            
            // Update performance indicator (green = fast, red = slow)
            var normalizedPerformance = Mathf.Clamp01(1f - (float)(avgTime / 10000)); // 10s = worst
            performanceIndicator.value = normalizedPerformance;
        }
        
        if (totalOperations > 0)
        {
            var successRate = (float)successfulOperations / totalOperations * 100;
            successRateText.text = $"Success Rate: {successRate:F1}%";
        }
    }
}
```

### Performance Alerts

Implement automatic alerts for performance issues:

```csharp
public class PerformanceAlerting
{
    private readonly Queue<TimeSpan> recentOperations = new();
    private const int MaxHistorySize = 10;
    
    public void CheckPerformanceAlert(string operation, TimeSpan duration)
    {
        recentOperations.Enqueue(duration);
        if (recentOperations.Count > MaxHistorySize)
            recentOperations.Dequeue();
        
        // Check for sustained poor performance
        if (recentOperations.Count >= 5)
        {
            var avgDuration = recentOperations.Average(ts => ts.TotalMilliseconds);
            var threshold = GetPerformanceThreshold(operation);
            
            if (avgDuration > threshold.TotalMilliseconds)
            {
                TriggerPerformanceAlert(operation, TimeSpan.FromMilliseconds(avgDuration), threshold);
            }
        }
    }
    
    private void TriggerPerformanceAlert(string operation, TimeSpan avgDuration, TimeSpan threshold)
    {
        Logger.LogWarning($"[PERFORMANCE ALERT] {operation} averaging {avgDuration.TotalSeconds:F1}s " +
                         $"(threshold: {threshold.TotalSeconds:F1}s)");
        
        // Optionally trigger UI notification or automatic optimization
        OnPerformanceAlert?.Invoke(operation, avgDuration);
    }
    
    public static event Action<string, TimeSpan> OnPerformanceAlert;
}
```

## Performance Profiling in Development

### Detailed Profiling

Enable detailed profiling in development builds:

```csharp
#if DEVELOPMENT_BUILD || UNITY_EDITOR
public class DetailedProfiler
{
    private static readonly Dictionary<string, List<ProfileEntry>> profileData = new();
    
    public static void StartProfiling(string operation)
    {
        if (!profileData.ContainsKey(operation))
            profileData[operation] = new List<ProfileEntry>();
        
        profileData[operation].Add(new ProfileEntry
        {
            StartTime = DateTime.UtcNow,
            Operation = operation
        });
    }
    
    public static void EndProfiling(string operation)
    {
        if (profileData.ContainsKey(operation))
        {
            var entries = profileData[operation];
            var lastEntry = entries.LastOrDefault(e => !e.EndTime.HasValue);
            
            if (lastEntry != null)
            {
                lastEntry.EndTime = DateTime.UtcNow;
                var duration = lastEntry.EndTime.Value - lastEntry.StartTime;
                Logger.LogPerformance($"[DETAILED] {operation}", duration);
            }
        }
    }
    
    public static void DumpProfilingData()
    {
        foreach (var kvp in profileData)
        {
            var completedEntries = kvp.Value.Where(e => e.EndTime.HasValue);
            if (completedEntries.Any())
            {
                var avgDuration = completedEntries.Average(e => (e.EndTime.Value - e.StartTime).TotalMilliseconds);
                var minDuration = completedEntries.Min(e => (e.EndTime.Value - e.StartTime).TotalMilliseconds);
                var maxDuration = completedEntries.Max(e => (e.EndTime.Value - e.StartTime).TotalMilliseconds);
                
                Logger.Log($"[PROFILE] {kvp.Key}: Avg={avgDuration:F2}ms, Min={minDuration:F2}ms, Max={maxDuration:F2}ms");
            }
        }
    }
    
    private class ProfileEntry
    {
        public string Operation { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
#endif
```

## Best Practices for Performance Monitoring

### 1. Monitor Key Metrics

Focus on metrics that impact user experience:
- Video loading time
- Initialization duration
- Error rates
- Cache efficiency

### 2. Set Appropriate Baselines

Establish performance baselines for different scenarios:
- Different video qualities
- Various network conditions
- Different device capabilities

### 3. Implement Gradual Degradation

Use performance metrics to implement graceful degradation:

```csharp
public VideoQuality DetermineOptimalQuality(PerformanceMetrics metrics)
{
    if (metrics.AverageProcessingTime > TimeSpan.FromSeconds(30))
        return VideoQuality.Small360; // Fallback to lowest quality
    else if (metrics.AverageProcessingTime > TimeSpan.FromSeconds(15))
        return VideoQuality.Medium480;
    else if (metrics.AverageProcessingTime > TimeSpan.FromSeconds(8))
        return VideoQuality.HD720;
    else
        return VideoQuality.HD1080;
}
```

### 4. Regular Performance Reviews

Schedule regular performance reviews based on collected metrics:
- Weekly performance summaries
- Monthly trend analysis
- Quarterly optimization planning

## Troubleshooting Performance Issues

### Common Performance Problems

#### Slow URL Extraction
- **Symptoms**: yt-dlp operations taking > 10 seconds
- **Solutions**: Check network connectivity, update yt-dlp, implement caching

#### Memory Leaks
- **Symptoms**: Increasing memory usage over time
- **Solutions**: Verify proper disposal, monitor process cleanup, implement resource pooling

#### High CPU Usage
- **Symptoms**: Sustained high CPU during video operations
- **Solutions**: Limit concurrent operations, optimize process parameters, implement queuing

## See Also

- **[ProcessExecutor Documentation](process-executor.md)**: Process execution performance features
- **[Error Handling Guide](error-handling.md)**: Error handling and recovery patterns
- **[System Overview](../core/overview.md)**: Architecture and performance considerations
- **[YoutubePlayer API](../api/youtube-player.md)**: Main component performance features