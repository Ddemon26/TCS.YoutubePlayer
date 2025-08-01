namespace TCS.YoutubePlayer.ProcessExecution {
    public readonly struct ProcessResult {
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public ProcessResult(int exitCode, string stdout, string stderr) {
            ExitCode = exitCode;
            StandardOutput = stdout;
            StandardError = stderr;
        }

        public bool IsSuccess => ExitCode == 0;

        public void Deconstruct(out int exitCode, out string stdout, out string stderr) {
            exitCode = ExitCode;
            stdout = StandardOutput;
            stderr = StandardError;
        }
    }
}