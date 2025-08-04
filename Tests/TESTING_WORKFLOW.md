# TCS.YoutubePlayer Testing Workflow

## Overview

This document outlines the comprehensive testing workflow for the TCS.YoutubePlayer Unity package. **ALL logic must be tested and pass before any task/feature/upgrade/revision is considered complete.**

## Test Program Location

The main test program is located at:
```
Assets/TCS.YoutubePlayer/Tests/Program.cs
```

## Running Tests

### Command Line Execution
Navigate to the Tests directory and run:
```bash
cd "Assets/TCS.YoutubePlayer/Tests"
dotnet run
```

### Building and Running
```bash
cd "Assets/TCS.YoutubePlayer/Tests"
dotnet build
dotnet run --no-build
```

## Test Coverage

The comprehensive test suite covers all major components and functionality:

### 1. URL Processing Tests (`YouTubeUrlProcessor`)
- ✅ **TrimYouTubeUrl()** - URL trimming with various formats
- ✅ **TryExtractVideoId()** - Video ID extraction from different URL formats
- ✅ **ValidateUrl()** - URL validation with proper exception handling
- ✅ **SanitizeForShell()** - Shell command sanitization with special characters
- ✅ **ParseExpiryFromUrl()** - Expiry date parsing from URLs

**Test URLs Include:**
- Standard YouTube URLs (`https://www.youtube.com/watch?v=...`)
- Short URLs (`https://youtu.be/...`)
- Playlist URLs with parameters
- Embed URLs
- URLs with timestamps

### 2. YtDlpService Tests (`YtDlpService`)
- ✅ **Service Creation** - Proper instantiation with settings
- ✅ **GetCacheTitle()** - Cache title retrieval functionality
- ✅ **Service Disposal** - Proper resource cleanup
- 🔄 **Note**: Full integration tests require external tools (yt-dlp, ffmpeg)

### 3. Configuration Tests (`YtDlpSettings`)
- ✅ **Default Settings** - Proper default configuration
- ✅ **Preset Creation** - All preset types (Streaming, Download, Audio-only, High Quality)
- ✅ **Settings Cloning** - Deep copy functionality
- ✅ **Fluent API** - Settings modification via fluent interface
- ✅ **TimeRange** - Time range configuration
- ✅ **QualitySelector** - Quality selector implicit conversions

### 4. Cache Tests (`YtDlpUrlCache`)
- ✅ **AddToCache()** - Adding entries to cache
- ✅ **GetCacheTitle()** - Title retrieval from cache
- ✅ **TryGetCachedEntry()** - Cache hit/miss scenarios
- ✅ **Cache Disposal** - Proper cleanup and persistence

### 5. Command Builder Tests (`YtDlpCommandBuilder`)
- ✅ **BuildGetDirectUrlCommand()** - Direct URL command generation
- ✅ **BuildGetTitleCommand()** - Title extraction commands
- ✅ **BuildConversionCommand()** - Video conversion commands
- ✅ **BuildVersionCommand()** - Version check commands
- ✅ **BuildUpdateCommand()** - Update commands
- ✅ **Custom Settings Commands** - Commands with modified settings

## Test Results Interpretation

### Success Criteria
- **All tests must pass** (100% success rate)
- **No exceptions** should be thrown unexpectedly
- **Return code 0** indicates successful test completion
- **Return code 1** indicates test failures

### Test Output Format
```
🔗 URL Processing Tests
======================
✅ PASS TrimYouTubeUrl(https://www.youtube.com/watch?v=dQw4w9WgXcQ) - Result: https://www.youtube.com/watch?v=dQw4w9WgXcQ
✅ PASS TryExtractVideoId(https://www.youtube.com/watch?v=dQw4w9WgXcQ) - VideoId: dQw4w9WgXcQ
...

==================================================
TEST RESULTS SUMMARY
==================================================
Total Tests: 45
✅ Passed: 45
❌ Failed: 0
Success Rate: 100.0%

🎉 ALL TESTS PASSED!
==================================================
```

## Development Workflow

### Before Making Changes
1. **Run existing tests** to establish baseline
2. **Document expected behavior** for new features
3. **Identify test scenarios** that need coverage

### During Development
1. **Run tests frequently** to catch regressions early
2. **Add new tests** for any new functionality
3. **Update existing tests** if behavior changes

### Before Committing Changes
1. **Run full test suite** - all tests must pass
2. **Verify no regressions** in existing functionality
3. **Add tests for new features** or bug fixes
4. **Update test documentation** if needed

## Adding New Tests

### Test Structure Template
```csharp
private static async Task RunNewFeatureTests() {
    Console.WriteLine("🆕 New Feature Tests");
    Console.WriteLine("===================");
    
    try {
        // Setup test data
        var testSubject = new YourNewClass();
        
        // Test basic functionality
        var result = testSubject.DoSomething();
        LogTest("DoSomething basic test", result != null, $"Result: {result}");
        
        // Test edge cases
        LogTest("DoSomething null input", testSubject.DoSomething(null) == expectedValue, "Handles null input");
        
        // Test error conditions
        try {
            testSubject.DoSomethingThatShouldFail();
            LogTest("DoSomethingThatShouldFail", false, "Should have thrown exception");
        }
        catch (ExpectedException) {
            LogTest("DoSomethingThatShouldFail", true, "Correctly threw expected exception");
        }
        
    }
    catch (Exception ex) {
        LogTest("New feature tests", false, $"Test setup failed: {ex.Message}");
    }
    
    Console.WriteLine();
}
```

### Integration with Main Test Suite
1. Add your test method to the `Main()` method:
```csharp
await RunNewFeatureTests();
```

2. Follow the established naming conventions and logging patterns

## Continuous Integration Considerations

### Automated Testing
- Tests can be integrated into CI/CD pipelines
- Exit code 0/1 allows for automated pass/fail detection
- Detailed output helps with debugging failures

### External Dependencies
- Some tests may require yt-dlp and ffmpeg for full integration testing
- Mock implementations are used where external tools aren't available
- Tests are designed to be runnable without internet connectivity for core functionality

## Best Practices

### Test Design
- **Test one thing at a time** - focused, isolated tests
- **Use descriptive test names** - clearly indicate what's being tested
- **Include both positive and negative test cases**
- **Test boundary conditions** and edge cases

### Error Handling
- **Expect and test exceptions** where appropriate
- **Validate error messages** and exception types
- **Test graceful degradation** when external resources fail

### Maintenance
- **Keep tests simple and readable**
- **Update tests when APIs change**
- **Remove obsolete tests** for deprecated functionality
- **Document complex test scenarios**

## Mandatory Testing Checklist

Before any feature/upgrade/revision is considered complete, verify:

- [ ] All existing tests pass
- [ ] New functionality has corresponding tests
- [ ] Edge cases are covered
- [ ] Error conditions are tested
- [ ] Performance implications are considered
- [ ] Integration points are validated
- [ ] Documentation is updated
- [ ] Test coverage is maintained or improved

## Troubleshooting

### Common Issues
1. **Missing Dependencies**: Ensure all required NuGet packages are installed
2. **Path Issues**: Check that file paths are correct for your environment
3. **External Tools**: Some tests may require yt-dlp/ffmpeg installation
4. **Permissions**: Ensure write permissions for cache file creation

### Debugging Failed Tests
1. **Check the detailed output** - failed tests show expected vs actual values
2. **Run individual test sections** by commenting out others
3. **Add additional logging** to isolate issues
4. **Verify test environment setup**

---

**Remember: NO EXCEPTIONS - All logic must be tested and pass before tasks are finished!**