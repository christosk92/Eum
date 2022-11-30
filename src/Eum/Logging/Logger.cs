using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Eum.Helpers;
using Eum.Library.Logger.Helpers;
using Medialoc.Shared.Helpers;

namespace Eum.Logging;

public sealed class S_Log 
{
    #region PropertiesAndMembers

    private readonly object Lock = new();

    private long On = 1;

    private int LoggingFailedCount = 0;
    private static S_Log? _instance;

    private LogLevel MinimumLevel { get; set; } = LogLevel.Critical;

    private HashSet<LogMode> Modes { get; } = new HashSet<LogMode>();

    public string FilePath { get; private set; } = "Log.txt";

    public string EntrySeparator { get; private set; } = Environment.NewLine;

    /// <summary>
    /// Gets the GUID instance.
    ///
    /// <para>You can use it to identify which software instance created a log entry. It gets created automatically, but you have to use it manually.</para>
    /// </summary>
    private Guid InstanceGuid { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the maximum log file size in KB.
    /// </summary>
    /// <remarks>Default value is approximately 10 MB. If set to <c>0</c>, then there is no maximum log file size.</remarks>
    private long MaximumLogFileSize { get; set; } = 10_000;

    #endregion PropertiesAndMembers

    #region Initializers

    public S_Log()
    {
        _instance = this;
    }

    /// <summary>
    /// Initializes the logger with default values.
    /// <para>
    /// Default values are set as follows:
    /// <list type="bullet">
    /// <item>For RELEASE mode: <see cref="MinimumLevel"/> is set to <see cref="LogLevel.Info"/>, and logs only to file.</item>
    /// <item>For DEBUG mode: <see cref="MinimumLevel"/> is set to <see cref="LogLevel.Debug"/>, and logs to file, debug and console.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="logLevel">Use <c>null</c> to use default <see cref="LogLevel"/> or a custom value to force non-default <see cref="LogLevel"/>.</param>
    public void InitializeDefaults(string filePath, LogLevel? logLevel = null)
    {
        SetFilePath(filePath);

#if RELEASE
			SetMinimumLevel(logLevel ??= LogLevel.Info);
			SetModes(LogMode.Console, LogMode.File);

#else
        SetMinimumLevel(logLevel ??= LogLevel.Debug);
        SetModes(LogMode.Debug, LogMode.Console, LogMode.File);
#endif
        MaximumLogFileSize = MinimumLevel == LogLevel.Trace ? 0 : 10_000;
    }

    public void SetMinimumLevel(LogLevel level) => MinimumLevel = level;

    public void SetModes(params LogMode[] modes)
    {
        if (Modes.Count != 0)
        {
            Modes.Clear();
        }

        if (modes is null)
        {
            return;
        }

        foreach (var mode in modes)
        {
            Modes.Add(mode);
        }
    }

    public void SetFilePath(string filePath) => FilePath = Guard.NotNullOrEmptyOrWhitespace(nameof(filePath), filePath, trim: true);

    public void SetEntrySeparator(string entrySeparator) => EntrySeparator = Guard.NotNull(nameof(entrySeparator), entrySeparator);

    /// <summary>
    /// KB
    /// </summary>
    public void SetMaximumLogFileSize(long sizeInKb) => MaximumLogFileSize = sizeInKb;

    #endregion Initializers
    
    
    #region Methods

    public void TurnOff() => Interlocked.Exchange(ref On, 0);

    public void TurnOn() => Interlocked.Exchange(ref On, 1);

    public bool IsOn() => Interlocked.Read(ref On) == 1;

    #endregion Methods

    public static S_Log Instance => _instance ??= new S_Log();
    /// <summary>
    /// Logs exception string without any user message.
    /// </summary>
    private void Log(Exception exception, LogLevel level, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        Log(level, exception.ToString(), callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);
    }
    public void LogInfo(string message, [CallerFilePath] string callerFilePath = "", 
        [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        Log(LogLevel.Info, message, callerFilePath: callerFilePath,
            callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);
    }

    /// <summary>
    /// Logs user message concatenated with exception string.
    /// </summary>
    private void Log(string message, Exception ex, LogLevel level, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
    {
        Log(level, message: $"{message} Exception: {ex}", callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);
    }

    	public void Log(LogLevel level, string message,
            int additionalEntrySeparators = 0,
            bool additionalEntrySeparatorsLogFileOnlyMode = true, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
	{
		try
		{
			if (Modes.Count == 0 || !IsOn())
			{
				return;
			}

			if (level < MinimumLevel)
			{
				return;
			}

			message = Guard.Correct(message);
			var category = string.IsNullOrWhiteSpace(callerFilePath) ? "" : $"{EnvironmentHelpers.ExtractFileName(callerFilePath)}.{callerMemberName} ({callerLineNumber})";

			var messageBuilder = new StringBuilder();
			messageBuilder.Append($"{DateTime.UtcNow.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {level.ToString().ToUpperInvariant()}\t");

			if (message.Length == 0)
			{
				if (category.Length == 0) // If both empty. It probably never happens though.
				{
					messageBuilder.Append($"{EntrySeparator}");
				}
				else // If only the message is empty.
				{
					messageBuilder.Append($"{category}{EntrySeparator}");
				}
			}
			else
			{
				if (category.Length == 0) // If only the category is empty.
				{
					messageBuilder.Append($"{message}{EntrySeparator}");
				}
				else // If none of them empty.
				{
					messageBuilder.Append($"{category}\t{message}{EntrySeparator}");
				}
			}

			var finalMessage = messageBuilder.ToString();

			for (int i = 0; i < additionalEntrySeparators; i++)
			{
				messageBuilder.Insert(0, EntrySeparator);
			}

			var finalFileMessage = messageBuilder.ToString();
			if (!additionalEntrySeparatorsLogFileOnlyMode)
			{
				finalMessage = finalFileMessage;
			}

			lock (Lock)
			{
				if (Modes.Contains(LogMode.Console))
				{
					lock (Console.Out)
					{
						var color = Console.ForegroundColor;
						switch (level)
						{
							case LogLevel.Warning:
								color = ConsoleColor.Yellow;
								break;

							case LogLevel.Error:
							case LogLevel.Critical:
								color = ConsoleColor.Red;
								break;

							default:
								break; // Keep original color.
						}

						Console.ForegroundColor = color;
						Console.Write(finalMessage);
						Console.ResetColor();
					}
				}

				if (Modes.Contains(LogMode.Debug))
				{
					Debug.Write(finalMessage);
				}

				if (!Modes.Contains(LogMode.File))
				{
					return;
				}

				IoHelpers.EnsureContainingDirectoryExists(FilePath);

				if (MaximumLogFileSize > 0)
				{
					if (File.Exists(FilePath))
					{
						var sizeInBytes = new FileInfo(FilePath).Length;
						if (sizeInBytes > 1000 * MaximumLogFileSize)
						{
							File.Delete(FilePath);
						}
					}
				}

				File.AppendAllText(FilePath, finalFileMessage);
			}
		}
		catch (Exception ex)
		{
			if (Interlocked.Increment(ref LoggingFailedCount) == 1) // If it only failed the first time, try log the failure.
			{
				LogDebug($"Logging failed: {ex}");
			}

			// If logging the failure is successful then clear the failure counter.
			// If it's not the first time the logging failed, then we do not try to log logging failure, so clear the failure counter.
			Interlocked.Exchange(ref LoggingFailedCount, 0);
		}
	}
        /// <summary>
        /// Logs a string message at <see cref="LogLevel.Error"/> level.
        ///
        /// <para>For errors and exceptions that cannot be handled.</para>
        /// </summary>
        /// <remarks>These messages indicate a failure in the current activity or operation (such as the current HTTP request), not an application-wide failure.</remarks>
        /// <example>Log message such as: "Cannot insert record due to duplicate key violation."</example>
        public  void LogError(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => Log(LogLevel.Error, message, callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

        public void LogError(Exception exception, string callerFilePath = "", string callerMemberName = "", int callerLineNumber = -1) => Log(exception, LogLevel.Error, callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

        /// <summary>
        /// Logs <paramref name="message"/> with <paramref name="exception"/> using <see cref="Exception.ToString()"/> concatenated to it at <see cref="LogLevel.Error"/> level.
        ///
        /// <para>For errors and exceptions that cannot be handled.</para>
        /// </summary>
        /// <remarks>These messages indicate a failure in the current activity or operation (such as the current HTTP request), not an application-wide failure.</remarks>
        /// <example>Log message such as: "Cannot insert record due to duplicate key violation."</example>
        public void LogError(string message, Exception exception, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
            => Log(message, exception, LogLevel.Error, callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

        
        /// <summary>
        /// Logs the <paramref name="exception"/> using <see cref="Exception.ToString()"/> at <see cref="LogLevel.Debug"/> level.
        ///
        /// <para>For information that is valuable only to a developer debugging an issue.</para>
        /// </summary>
        /// <remarks>These messages may contain sensitive application data and so should not be enabled in a production environment.</remarks>
        /// <example>For example: <c>Credentials: {"User":"SomeUser", "Password":"P@ssword"}</c></example>
        public void LogDebug(Exception exception, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => Log(exception, LogLevel.Debug, callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

        public void LogDebug(string message, string callerFilePath = "", 
            string callerMemberName = "", int callerLineNumber = -1) =>
            Log(LogLevel.Debug, message, callerFilePath: callerFilePath,
                callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

        public void LogTrace(string message, string callerFilePath = "", string callerMemberName = "",
            int callerLineNumber = -1) => Log(LogLevel.Trace, message, callerFilePath: callerFilePath,
            callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

        public void LogWarning(Exception exception, string callerFilePath = "", string callerMemberName = "",
            int callerLineNumber = -1) =>Log(exception, LogLevel.Warning, callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

        public void LogWarning(string message,string callerFilePath = "", string callerMemberName = "",
            int callerLineNumber = -1)
            =>Log(LogLevel.Warning, message, callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);
        public void LogTrace(Exception exception, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "",
	        [CallerLineNumber] int callerLineNumber = -1) => 
	        Log(exception, LogLevel.Trace, callerFilePath: callerFilePath, callerMemberName: callerMemberName, callerLineNumber: callerLineNumber);

}

