﻿using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Security;
using System.Text;

namespace DLaB.Common
{
    /// <summary>
    /// Executes Command Line calls, redirecting output to Console
    /// </summary>
    public class ProcessExecutor
    {
        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns></returns>
        public static string ExecuteCmd(string fileName, string arguments)
        {
            return ExecuteCmd(new ProcessExecutorInfo(fileName, arguments));
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns></returns>
        public static string ExecuteCmd(ProcessExecutorInfo info)
        {
            var color = Console.ForegroundColor;

            if (!info.RedirectStandardOutput.HasValue)
            {
                info.RedirectStandardOutput = true;
            }

            if (!info.RedirectStandardError.HasValue)
            {
                info.RedirectStandardError = true;
            }

            var cmdProcess = new Process
            {
                StartInfo = info.GetStartInfo()
            };

            var commandOutput = new StringBuilder();
            var commandOutputLock = new object();
            cmdProcess.ErrorDataReceived += (sender, e) => cmd_Error(e, commandOutput, commandOutputLock);
            cmdProcess.OutputDataReceived += (sender, e) => cmd_DataReceived(e, commandOutput, commandOutputLock);
            cmdProcess.EnableRaisingEvents = true;
            cmdProcess.Start();
            cmdProcess.BeginOutputReadLine();
            cmdProcess.BeginErrorReadLine();

            cmdProcess.StandardInput.WriteLine("exit");  //Execute exit.

            cmdProcess.WaitForExit();
            Console.ForegroundColor = color;
            return commandOutput.ToString();
        }

        private static void cmd_DataReceived(DataReceivedEventArgs e, StringBuilder sb, object sbLock)
        {
            lock (sbLock)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                sb.AppendLine(e?.Data);
                Console.WriteLine(e?.Data);
                Console.ForegroundColor = color;
            }
        }

        private static void cmd_Error(DataReceivedEventArgs e, StringBuilder sb, object sbLock)
        {
            lock (sbLock)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                sb.AppendLine(e?.Data);
                Console.WriteLine(e?.Data);
                Console.ForegroundColor = color;
            }
        }
    }

    /// <summary>
    /// Settings to override default
    /// </summary>
    public class ProcessExecutorInfo
    {

        /// <summary>Gets or sets the verb to use when opening the application or document specified by the <see cref="P:System.Diagnostics.ProcessStartInfo.FileName" /> property.</summary>
        /// <returns>The action to take with the file that the process opens. The default is an empty string (""), which signifies no action.</returns>
        /// <filterpriority>2</filterpriority>
        public string Verb { get; set; }

        /// <summary>Gets or sets the set of command-line arguments to use when starting the application.</summary>
        /// <returns>A single string containing the arguments to pass to the target application specified in the <see cref="P:System.Diagnostics.ProcessStartInfo.FileName" /> property. The default is an empty string (""). On Windows Vista and earlier versions of the Windows operating system, the length of the arguments added to the length of the full path to the process must be less than 2080. On Windows 7 and later versions, the length must be less than 32699.Arguments are parsed and interpreted by the target application, so must align with the expectations of that application. For.NET applications as demonstrated in the Examples below, spaces are interpreted as a separator between multiple arguments. A single argument that includes spaces must be surrounded by quotation marks, but those quotation marks are not carried through to the target application. In include quotation marks in the final parsed argument, triple-escape each mark.</returns>
        /// <filterpriority>1</filterpriority>
        public string Arguments { get; set; }

        /// <summary>Gets or sets a value indicating whether to start the process in a new window.</summary>
        /// <returns>true if the process should be started without creating a new window to contain it; otherwise, false. The default is false.</returns>
        /// <filterpriority>2</filterpriority>
        public bool? CreateNoWindow { get; set; }

        /// <summary>Gets search paths for files, directories for temporary files, application-specific options, and other similar information.</summary>
        /// <returns>A string dictionary that provides environment variables that apply to this process and child processes. The default is null.</returns>
        /// <filterpriority>1</filterpriority>
        public StringDictionary EnvironmentVariables { get; set; }

        /// <summary>Gets or sets a value indicating whether the input for an application is read from the <see cref="P:System.Diagnostics.Process.StandardInput" /> stream.</summary>
        /// <returns>true if input should be read from <see cref="P:System.Diagnostics.Process.StandardInput" />; otherwise, false. The default is false.</returns>
        /// <filterpriority>2</filterpriority>
        public bool? RedirectStandardInput { get; set; }

        /// <summary>Gets or sets a value that indicates whether the textual output of an application is written to the <see cref="P:System.Diagnostics.Process.StandardOutput" /> stream.</summary>
        /// <returns>true if output should be written to <see cref="P:System.Diagnostics.Process.StandardOutput" />; otherwise, false. The default is false.</returns>
        /// <filterpriority>2</filterpriority>
        public bool? RedirectStandardOutput { get; set; }

        /// <summary>Gets or sets a value that indicates whether the error output of an application is written to the <see cref="P:System.Diagnostics.Process.StandardError" /> stream.</summary>
        /// <returns>true if error output should be written to <see cref="P:System.Diagnostics.Process.StandardError" />; otherwise, false. The default is false.</returns>
        /// <filterpriority>2</filterpriority>
        public bool? RedirectStandardError { get; set; }

        /// <summary>Gets or sets the preferred encoding for error output.</summary>
        /// <returns>An object that represents the preferred encoding for error output. The default is null.</returns>
        public Encoding StandardErrorEncoding { get; set; }

        /// <summary>Gets or sets the preferred encoding for standard output.</summary>
        /// <returns>An object that represents the preferred encoding for standard output. The default is null.</returns>
        public Encoding StandardOutputEncoding { get; set; }

        /// <summary>Gets or sets a value indicating whether to use the operating system shell to start the process.</summary>
        /// <returns>true if the shell should be used when starting the process; false if the process should be created directly from the executable file. The default is true.</returns>
        /// <filterpriority>2</filterpriority>
        public bool? UseShellExecute { get; set; }

        /// <summary>Gets or sets the user name to be used when starting the process.</summary>
        /// <returns>The user name to use when starting the process.</returns>
        /// <filterpriority>1</filterpriority>
        public string UserName { get; set; }

        /// <summary>Gets or sets a secure string that contains the user password to use when starting the process.</summary>
        /// <returns>The user password to use when starting the process.</returns>
        /// <filterpriority>1</filterpriority>
        public SecureString Password { get; set; }

        /// <summary>Gets or sets the user password in clear text to use when starting the process.</summary>
        /// <returns>The user password in clear text.</returns>
        public string PasswordInClearText { get; set; }

        /// <summary>Gets or sets a value that identifies the domain to use when starting the process. </summary>
        /// <returns>The Active Directory domain to use when starting the process. The domain property is primarily of interest to users within enterprise environments that use Active Directory.</returns>
        /// <filterpriority>1</filterpriority>
        public string Domain { get; set; }

        /// <summary>Gets or sets a value that indicates whether the Windows user profile is to be loaded from the registry. </summary>
        /// <returns>true if the Windows user profile should be loaded; otherwise, false. The default is false.</returns>
        /// <filterpriority>1</filterpriority>
        public bool? LoadUserProfile { get; set; }

        /// <summary>Gets or sets the application or document to start.</summary>
        /// <returns>The name of the application to start, or the name of a document of a file type that is associated with an application and that has a default open action available to it. The default is an empty string ("").</returns>
        /// <filterpriority>1</filterpriority>
        public string FileName { get; set; }

        /// <summary>When the <see cref="P:System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property is false, gets or sets the working directory for the process to be started. When <see cref="P:System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is true, gets or sets the directory that contains the process to be started.</summary>
        /// <returns>When <see cref="P:System.Diagnostics.ProcessStartInfo.UseShellExecute" /> is true, the fully qualified name of the directory that contains the process to be started. When the <see cref="P:System.Diagnostics.ProcessStartInfo.UseShellExecute" /> property is false, the working directory for the process to be started. The default is an empty string ("").</returns>
        /// <filterpriority>1</filterpriority>
        public string WorkingDirectory { get; set; }

        /// <summary>Gets or sets a value indicating whether an error dialog box is displayed to the user if the process cannot be started.</summary>
        /// <returns>true if an error dialog box should be displayed on the screen if the process cannot be started; otherwise, false. The default is false.</returns>
        /// <filterpriority>2</filterpriority>
        public bool? ErrorDialog { get; set; }

        /// <summary>Gets or sets the window handle to use when an error dialog box is shown for a process that cannot be started.</summary>
        /// <returns>A pointer to the handle of the error dialog box that results from a process start failure.</returns>
        /// <filterpriority>2</filterpriority>
        public IntPtr? ErrorDialogParentHandle { get; set; }

        /// <summary>Gets or sets the window state to use when the process is started.</summary>
        /// <returns>One of the enumeration values that indicates whether the process is started in a window that is maximized, minimized, normal (neither maximized nor minimized), or not visible. The default is Normal.</returns>
        /// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">The window style is not one of the <see cref="T:System.Diagnostics.ProcessWindowStyle" /> enumeration members. </exception>
        /// <filterpriority>2</filterpriority>
        public ProcessWindowStyle? WindowStyle { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExecutorInfo" /> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="arguments">The arguments.</param>
        public ProcessExecutorInfo(string fileName = null, string arguments = null)
        {
            FileName = fileName;
            Arguments = arguments;
            RedirectStandardOutput = true;
            RedirectStandardError = true;
            RedirectStandardInput = true;
            UseShellExecute = false;
            CreateNoWindow = true;
        }

        /// <summary>
        /// Gets the start information.
        /// </summary>
        /// <returns></returns>
        public ProcessStartInfo GetStartInfo()
        {
            var info = new ProcessStartInfo();
            if (Verb != null)
            {
                info.Verb = Verb;
            }

            if (Arguments != null)
            {
                info.Arguments = Arguments;
            }

            if (CreateNoWindow.HasValue)
            {
                info.CreateNoWindow = CreateNoWindow.Value;
            }

            if (EnvironmentVariables != null)
            {
                foreach (string key in EnvironmentVariables.Keys)
                {
                    info.EnvironmentVariables.Add(key, EnvironmentVariables[key]);
                }
            }

            if (RedirectStandardInput.HasValue)
            {
                info.RedirectStandardInput = RedirectStandardInput.Value;
            }

            if (RedirectStandardOutput.HasValue)
            {
                info.RedirectStandardOutput = RedirectStandardOutput.Value;
            }

            if (RedirectStandardError.HasValue)
            {
                info.RedirectStandardError = RedirectStandardError.Value;
            }

            if (StandardErrorEncoding != null)
            {
                info.StandardErrorEncoding = StandardErrorEncoding;
            }

            if (StandardOutputEncoding != null)
            {
                info.StandardOutputEncoding = StandardOutputEncoding;
            }

            if (UseShellExecute.HasValue)
            {
                info.UseShellExecute = UseShellExecute.Value;
            }

            if (UserName != null)
            {
                info.UserName = UserName;
            }

            if (Password != null)
            {
                info.Password = Password;
            }

            if (PasswordInClearText != null)
            {
                ((dynamic) info).PasswordInClearText = PasswordInClearText;
            }

            if (Domain != null)
            {
                info.Domain = Domain;
            }

            if (LoadUserProfile.HasValue)
            {
                info.LoadUserProfile = LoadUserProfile.Value;
            }

            if (FileName != null)
            {
                info.FileName = FileName;
            }

            if (WorkingDirectory != null)
            {
                info.WorkingDirectory = WorkingDirectory;
            }

            if (ErrorDialog.HasValue)
            {
                info.ErrorDialog = ErrorDialog.Value;
            }

            if (ErrorDialogParentHandle.HasValue)
            {
                info.ErrorDialogParentHandle = ErrorDialogParentHandle.Value;
            }

            if (WindowStyle.HasValue)
            {
                info.WindowStyle = WindowStyle.Value;
            }

            return info;
        }
    }
}
