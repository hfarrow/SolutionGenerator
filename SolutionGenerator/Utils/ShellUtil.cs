using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace SolutionGen.Utils
{
    /// <summary>
    /// Based on https://medium.com/@equiman/run-a-command-in-external-terminal-with-net-core-cc24e3cc9839
    /// </summary>
    public static class ShellUtil
    {
        public static Process StartProcess(
            ProcessStartInfo psi,
            DataReceivedEventHandler stdOutHandler = null,
            DataReceivedEventHandler stdErrorHandler = null,
            bool redirectStandardInput = false,
            bool waitForExit = false,
            int timeOutSeconds = 180)
        {
            Console.WriteLine("startProcess: {0} {1}\n\tredirectStdIn={2}\n\twaitForExit={3}",
                psi.FileName, psi.Arguments, redirectStandardInput, waitForExit);
            Process process;

            // Based on http://stackoverflow.com/a/22956924
            TimeSpan timeout = TimeSpan.FromSeconds(timeOutSeconds);
            using (var outputWaitHandle = new AutoResetEvent(false))
            {
                using (var errorWaitHandle = new AutoResetEvent(false))
                {
                    process = new Process();
                    // preparing ProcessStartInfo
                    psi.UseShellExecute = false;
                    psi.RedirectStandardOutput = stdOutHandler != null;
                    psi.RedirectStandardError = stdErrorHandler != null;
                    psi.RedirectStandardInput = redirectStandardInput;
                    psi.CreateNoWindow = true;
                    process.StartInfo = psi;
                    process.EnableRaisingEvents = !waitForExit;

                    bool didStart = false;
                    try
                    {
                        if (stdOutHandler != null)
                        {
                            process.OutputDataReceived += (sender, e) =>
                            {
                                if (e.Data == null)
                                {
                                    outputWaitHandle?.Set();
                                }
                                else
                                {
                                    stdOutHandler(sender, e);
                                }
                            };
                        }

                        if (stdErrorHandler != null)
                        {
                            process.ErrorDataReceived += (sender, e) =>
                            {
                                if (e.Data == null)
                                {
                                    errorWaitHandle?.Set();
                                }
                                else
                                {
                                    stdErrorHandler(sender, e);
                                }
                            };
                        }

                        process.Start();
                        didStart = true;
                        Console.WriteLine("Process started...");

                        if (stdOutHandler != null)
                        {
                            process.BeginOutputReadLine();
                        }

                        if (stdErrorHandler != null)
                        {
                            process.BeginErrorReadLine();
                        }

                        if (waitForExit)
                        {
                            if (process.WaitForExit((int) timeout.TotalMilliseconds))
                            {
                                Console.WriteLine("Exit code {0} for process: {1}",
                                    process.ExitCode,
                                    psi.FileName);
                            }
                            else
                            {
                                Log.Error("Process timed out: {0} {1}", psi.FileName,psi.Arguments);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(
                            "There was an unhandled exception while executing a process. See below for details about process '{0} {1}'",
                            psi.FileName, psi.Arguments);
                        Log.Error(ex.ToString());

                        if (ex is Win32Exception win32Ex)
                        {
                            Log.Error(
                                "Win32 Exception Details: NativeErrorCode={0} ErrorCode={1} Message={2} Source={3}",
                                win32Ex.NativeErrorCode, win32Ex.ErrorCode, win32Ex.Message, win32Ex.Source);
                        }

                        throw;
                    }
                    finally
                    {
                        if (waitForExit && didStart)
                        {
                            if (stdOutHandler != null)
                            {
                                outputWaitHandle.WaitOne(timeout);
                            }

                            if (stdErrorHandler != null)
                            {
                                errorWaitHandle.WaitOne(timeout);
                            }

                            if (process.HasExited && process.ExitCode != 0)
                            {
                                throw new Exception(string.Format(
                                    "External process exited with error code: {0} {1}", psi.FileName,
                                    process.ExitCode));
                            }
                        }
                        else if (!waitForExit && didStart)
                        {
                            process.Exited += (sender, args) => onProcessExited(psi);
                        }
                        
                        if (process.HasExited)
                        {
                            onProcessExited(psi);
                            process.Dispose();
                        }
                    }
                }
            }

            return process;
        }

        private static void onProcessExited(ProcessStartInfo psi)
        {
            Console.WriteLine("External process exited: {0}", psi.FileName);
        }
    }
}