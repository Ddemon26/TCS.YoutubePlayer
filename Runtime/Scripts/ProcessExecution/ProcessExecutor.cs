using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCS.YoutubePlayer.Exceptions;

namespace TCS.YoutubePlayer.ProcessExecution {
    public class ProcessExecutor : IDisposable {
        string m_ffmpegPath;

        public ProcessExecutor(string ffmpegPath)
            => m_ffmpegPath = ffmpegPath;

        public void UpdateFFmpegPath(string ffmpegPath) {
            m_ffmpegPath = ffmpegPath;
        }

        /// <summary>
        /// Runs an external process asynchronously with optional timeout support
        /// </summary>
        /// <param name="fileName">Path to an executable file</param>
        /// <param name="arguments">Command line arguments</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="timeout">Optional timeout for process execution</param>
        /// <returns>Process result containing exit code and output</returns>
        public Task<ProcessResult> RunProcessAsync(
            string fileName,
            string arguments,
            CancellationToken cancellationToken,
            TimeSpan? timeout = null
        ) {
            if (string.IsNullOrWhiteSpace(fileName)) {
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            }

            if (arguments == null) {
                throw new ArgumentNullException(nameof(arguments));
            }

            // For ffmpeg commands, resolve the full path
            if (fileName == "ffmpeg" && !string.IsNullOrEmpty(m_ffmpegPath)) {
                fileName = m_ffmpegPath;
            }
            
            TaskCompletionSource<ProcessResult> tcs = new();
            var process = new Process {
                StartInfo = {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                },
                EnableRaisingEvents = true,
            };

            SetEnvironmentVariables(process);

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();
            CancellationTokenRegistration ctr = default;
            var startTime = DateTime.UtcNow;
            
            // Set up timeout if specified - don't dispose until process completes
            var timeoutCts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : null;
            var linkedCts = timeoutCts != null 
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
                : null;
            var combinedToken = linkedCts?.Token ?? cancellationToken;

            if (combinedToken.CanBeCanceled) {
                ctr = combinedToken.Register(() => {
                    try {
                        if (!process.HasExited) {
                            process.Kill();
                            
                            string reason = timeoutCts?.Token.IsCancellationRequested == true 
                                ? "timeout" 
                                : "cancellation";
                            Logger.LogWarning(
                                $"[ProcessExecutor] Killed process {Path.GetFileName(fileName)} due to {reason}."
                            );
                        }
                    }
                    catch (InvalidOperationException) {
                        // Already exited
                    }
                    catch (Exception ex) {
                        Logger.LogError($"[ProcessExecutor] Exception trying to kill process: {ex.Message}");
                    }

                    if (timeoutCts?.Token.IsCancellationRequested == true) {
                        tcs.TrySetException(new TimeoutException($"Process {Path.GetFileName(fileName)} timed out after {timeout}"));
                    } else {
                        tcs.TrySetCanceled(combinedToken);
                    }
                });
            }

            process.OutputDataReceived += (_, e) => {
                if (e.Data != null) {
                    stdoutBuilder.AppendLine(e.Data);
                }
            };
            
            process.ErrorDataReceived += (_, e) => {
                if (e.Data != null) {
                    stderrBuilder.AppendLine(e.Data);
                }
            };

            process.Exited += (_, _) => {
                try {
                    var duration = DateTime.UtcNow - startTime;
                    var result = new ProcessResult(
                        process.ExitCode, 
                        stdoutBuilder.ToString(), 
                        stderrBuilder.ToString()
                    );
                    
                    Logger.LogPerformance($"Process {Path.GetFileName(fileName)}", duration);
                    if (result.IsSuccess) {
                        Logger.Log($"[ProcessExecutor] Process {Path.GetFileName(fileName)} completed successfully in {duration.TotalMilliseconds:F0}ms");
                    } else {
                        Logger.LogWarning($"[ProcessExecutor] Process {Path.GetFileName(fileName)} failed with exit code {result.ExitCode} after {duration.TotalMilliseconds:F0}ms");
                    }
                    
                    tcs.TrySetResult(result);
                }
                catch (Exception ex) {
                    Logger.LogError($"[ProcessExecutor] Exception in process exit handler: {ex.Message}");
                    tcs.TrySetException(ex);
                }
                finally {
                    // Dispose resources in proper order
                    try {
                        if (combinedToken.CanBeCanceled) {
                            ctr.Dispose();
                        }
                    }
                    catch (Exception ex) {
                        Logger.LogError($"[ProcessExecutor] Exception disposing cancellation token registration: {ex.Message}");
                    }

                    try {
                        process.Dispose();
                    }
                    catch (Exception ex) {
                        Logger.LogError($"[ProcessExecutor] Exception disposing process: {ex.Message}");
                    }

                    // Dispose cancellation token sources
                    try {
                        linkedCts?.Dispose();
                        timeoutCts?.Dispose();
                    }
                    catch (Exception ex) {
                        Logger.LogError($"[ProcessExecutor] Exception disposing cancellation token sources: {ex.Message}");
                    }
                }
            };

            try {
                if (!File.Exists(fileName)) {
                    throw new YtDlpException($"Executable not found: {fileName}");
                }

                if (!process.Start()) {
                    tcs.TrySetException(
                        new YtDlpException($"Failed to start process: {fileName}")
                    );
                    // Clean up resources since the process failed to start
                    CleanupResources(ctr, process, linkedCts, timeoutCts, combinedToken);
                }
                else {
                    Logger.Log($"[ProcessExecutor] Started process: {fileName} {arguments} (PID: {process.Id})");
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }
            }
            catch (Exception ex) {
                Logger.LogError($"[ProcessExecutor] Exception launching '{fileName} {arguments}': {ex}");
                tcs.TrySetException(
                    new YtDlpException(
                        $"Failed to start process '{Path.GetFileName(fileName)}'. Exception: {ex.Message}", ex
                    )
                );
                // Clean up resources since the process failed to start
                CleanupResources(ctr, process, linkedCts, timeoutCts, combinedToken);
            }

            return tcs.Task;
        }

        void SetEnvironmentVariables(Process process) {
            if (!string.IsNullOrEmpty(m_ffmpegPath)) {
                #if UNITY_2020_1_OR_NEWER
                process.StartInfo.Environment["FFMPEG_LOCATION"] = m_ffmpegPath;
                #else
                process.StartInfo.EnvironmentVariables["FFMPEG_LOCATION"] = m_ffmpegPath;
                #endif
            }
        }

        private static void CleanupResources(
            CancellationTokenRegistration ctr,
            Process process,
            CancellationTokenSource linkedCts,
            CancellationTokenSource timeoutCts,
            CancellationToken combinedToken
        ) {
            try {
                if (combinedToken.CanBeCanceled) {
                    ctr.Dispose();
                }
            }
            catch (Exception ex) {
                Logger.LogError($"[ProcessExecutor] Exception disposing cancellation token registration: {ex.Message}");
            }

            try {
                process?.Dispose();
            }
            catch (Exception ex) {
                Logger.LogError($"[ProcessExecutor] Exception disposing process: {ex.Message}");
            }

            try {
                linkedCts?.Dispose();
                timeoutCts?.Dispose();
            }
            catch (Exception ex) {
                Logger.LogError($"[ProcessExecutor] Exception disposing cancellation token sources: {ex.Message}");
            }
        }

        public void Dispose() {
            // ProcessExecutor doesn't hold any disposable resources
            // Processes are disposed in the Exited event handler
        }
    }
}