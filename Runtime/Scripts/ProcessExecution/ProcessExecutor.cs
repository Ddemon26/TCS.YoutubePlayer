using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TCS.YoutubePlayer.Exceptions;
using Logger = TCS.YoutubePlayer.Utils.Logger;

namespace TCS.YoutubePlayer.ProcessExecution {
    public class ProcessExecutor : IDisposable {
        readonly string m_ffmpegPath;

        public ProcessExecutor(string ffmpegPath) {
            m_ffmpegPath = ffmpegPath;
        }

        public Task<ProcessResult> RunProcessAsync(
            string fileName,
            string arguments,
            CancellationToken cancellationToken
        ) {
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
                    StandardErrorEncoding = Encoding.UTF8
                },
                EnableRaisingEvents = true
            };

            SetEnvironmentVariables(process);

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();
            CancellationTokenRegistration ctr = default;

            if (cancellationToken.CanBeCanceled) {
                ctr = cancellationToken.Register(() => {
                    try {
                        if (!process.HasExited) {
                            process.Kill();
                            Logger.LogWarning(
                                $"[ProcessExecutor] Killed process {Path.GetFileName(fileName)} due to cancellation."
                            );
                        }
                    }
                    catch (InvalidOperationException) {
                        // Already exited
                    }
                    catch (Exception ex) {
                        Logger.LogError($"[ProcessExecutor] Exception trying to kill process: {ex.Message}");
                    }

                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            process.OutputDataReceived += (_, e) => {
                if (e.Data != null)
                    stdoutBuilder.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (_, e) => {
                if (e.Data != null)
                    stderrBuilder.AppendLine(e.Data);
            };

            process.Exited += (_, _) => {
                var result = new ProcessResult(
                    process.ExitCode, 
                    stdoutBuilder.ToString(), 
                    stderrBuilder.ToString()
                );
                tcs.TrySetResult(result);
                
                if (cancellationToken.CanBeCanceled)
                    ctr.Dispose();
                process.Dispose();
            };

            try {
                if (!File.Exists(fileName))
                    throw new YtDlpException($"Executable not found: {fileName}");

                if (!process.Start()) {
                    tcs.TrySetException(
                        new YtDlpException($"Failed to start process: {fileName}")
                    );
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
            }

            return tcs.Task;
        }

        void SetEnvironmentVariables(Process process) {
            #if UNITY_2020_1_OR_NEWER
            process.StartInfo.Environment["FFMPEG_LOCATION"] = m_ffmpegPath;
            #else
            process.StartInfo.EnvironmentVariables["FFMPEG_LOCATION"] = _ffmpegPath;
            #endif
        }

        public void Dispose() {
            // ProcessExecutor doesn't hold any disposable resources
            // Processes are disposed in the Exited event handler
        }
    }
}