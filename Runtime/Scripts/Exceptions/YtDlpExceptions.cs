using System;

namespace TCS.YoutubePlayer.Exceptions {
    public class YtDlpException : Exception {
        public YtDlpException(string message) : base(message) { }
        public YtDlpException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    public sealed class InvalidYouTubeUrlException : ArgumentException {
        public InvalidYouTubeUrlException(string message, string paramName)
            : base(message, paramName) { }
    }

    public sealed class ProcessExecutionException : YtDlpException {
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public ProcessExecutionException(string message, int exitCode, string stdout, string stderr) 
            : base(message) {
            ExitCode = exitCode;
            StandardOutput = stdout;
            StandardError = stderr;
        }
    }

    public sealed class ConfigurationException : YtDlpException {
        public string ConfigPath { get; }

        public ConfigurationException(string message, string configPath) : base(message) {
            ConfigPath = configPath;
        }

        public ConfigurationException(string message, string configPath, Exception innerException) 
            : base(message, innerException) {
            ConfigPath = configPath;
        }
    }

    public sealed class CacheException : YtDlpException {
        public string CachePath { get; }

        public CacheException(string message, string cachePath) : base(message) {
            CachePath = cachePath;
        }

        public CacheException(string message, string cachePath, Exception innerException) 
            : base(message, innerException) {
            CachePath = cachePath;
        }
    }
}