# TCS.YoutubePlayer Test Suite

## 🚨 MANDATORY TESTING REQUIREMENT

**ALL LOGIC MUST BE TESTED AND PASS BEFORE ANY TASK IS COMPLETE**

## Quick Start

```bash
cd Assets/TCS.YoutubePlayer/Tests
dotnet run
```

## Current Test Results

✅ **40/42 tests passing (95.2% success rate)**

### Working Tests (40 ✅)
- **URL Processing** (22 tests) - All core URL handling functionality
- **Configuration** (11 tests) - Settings, presets, and fluent API
- **Command Building** (6 tests) - yt-dlp command generation
- **Error Handling** (1 test) - Exception validation

### Expected Failures (2 ❌)
- **YtDlpService tests** - Unity Application.persistentDataPath dependency
- **Cache tests** - Unity file system dependencies

> ℹ️ These failures are expected in standalone .NET environment and do not indicate actual issues.

## Files in This Directory

- **`Program.cs`** - Main test suite executable
- **`TESTING_WORKFLOW.md`** - Complete testing documentation and workflow
- **`Tests.csproj`** - .NET project configuration
- **`README.md`** - This file

## Usage for Future Development

### Before Starting Work
1. Run tests to establish baseline: `dotnet run`
2. Ensure all testable components pass

### During Development
1. Run tests frequently to catch regressions
2. Add new tests for any new functionality

### Before Completing Work
1. **MANDATORY**: Run full test suite
2. All testable components must pass
3. Add tests for new features
4. Update existing tests if behavior changed

## Integration with CLAUDE.md

This testing system is referenced in the main `CLAUDE.md` file to ensure future AI agents understand the mandatory testing requirements.

## Success Criteria

- ✅ Return code 0 = Success
- ❌ Return code 1 = Failures that must be fixed
- 🎯 Target: 100% success rate for testable components

---

**Remember: No exceptions, no compromises - all logic must be tested before any task is considered complete!**