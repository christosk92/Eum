using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Eum.Logging;
using Medialoc.Delivery.UI.Helpers;
using Medialoc.Shared.Helpers;
using Microsoft.Win32;

namespace Eum.Library.Logger.Helpers;

public static class EnvironmentHelpers
{
	[Flags]
	private enum EXECUTION_STATE : uint
	{
		ES_AWAYMODE_REQUIRED = 0x00000040,
		ES_CONTINUOUS = 0x80000000,
		ES_DISPLAY_REQUIRED = 0x00000002,
		ES_SYSTEM_REQUIRED = 0x00000001
	}

	// appName, dataDir
	private static ConcurrentDictionary<string, string> DataDirDict { get; } = new ConcurrentDictionary<string, string>();

	// Do not change the output of this function. Backwards compatibility depends on it.
	public static string GetDataDir(string appName)
	{
		if (DataDirDict.TryGetValue(appName, out string? dataDir))
		{
			return dataDir;
		}

		string directory;

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var home = Environment.GetEnvironmentVariable("HOME");
			if (!string.IsNullOrEmpty(home))
			{
				directory = Path.Combine(home, "." + appName.ToLowerInvariant());
				S_Log.Instance.LogInfo($"Using HOME environment variable for initializing application data at `{directory}`.");
			}
			else
			{
				throw new DirectoryNotFoundException("Could not find suitable datadir.");
			}
		}
		else
		{
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");
			if (!string.IsNullOrEmpty(localAppData))
			{
				directory = Path.Combine(localAppData, appName);
				S_Log.Instance.LogInfo($"Using APPDATA environment variable for initializing application data at `{directory}`.");
			}
			else
			{
				throw new DirectoryNotFoundException("Could not find suitable datadir.");
			}
		}

		if (Directory.Exists(directory))
		{
			DataDirDict.TryAdd(appName, directory);
			return directory;
		}

		S_Log.Instance.LogInfo($"Creating data directory at `{directory}`.");
		Directory.CreateDirectory(directory);

		DataDirDict.TryAdd(appName, directory);
		return directory;
	}

	/// <summary>
	/// Gets medialoc delivery <c>datadir</c> parameter from:
	/// <list type="bullet">
	/// <item><c>APPDATA</c> environment variable on Windows, and</item>
	/// <item><c>HOME</c> environment variable on other platforms.</item>
	/// </list>
	/// </summary>
	/// <returns><c>datadir</c> or empty string.</returns>
	public static string GetDefaultDeliveryCoreDataDirOrEmptyString()
	{
		string directory = "";

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var localAppData = Environment.GetEnvironmentVariable("APPDATA");
			if (!string.IsNullOrEmpty(localAppData))
			{
				directory = Path.Combine(localAppData, "Medialoc Delivery");
			}
			else
			{
				S_Log.Instance.LogDebug($"Could not find suitable default datadir.");
			}
		}
		else
		{
			var home = Environment.GetEnvironmentVariable("HOME");
			if (!string.IsNullOrEmpty(home))
			{
				directory = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
					? Path.Combine(home, "Library", "Application Support", "Medialoc Delivery")
					: Path.Combine(home, ".medialocdelivery"); // Linux
			}
			else
			{
				S_Log.Instance.LogDebug($"Could not find suitable default datadir.");
			}
		}

		return directory;
	}

	// This method removes the path and file extension.
	//
	// Given the releases are currently built using Windows, the generated assemblies contain
	// the hardcoded "C:\Users\User\Desktop\Delivery\.......\FileName.cs" string because that
	// is the real path of the file, it doesn't matter what OS was targeted.
	// In Windows and Linux that string is a valid path and that means Path.GetFileNameWithoutExtension
	// can extract the file name but in the case of OSX the same string is not a valid path so, it assumes
	// the whole string is the file name.
	public static string ExtractFileName(string callerFilePath)
	{
		var lastSeparatorIndex = callerFilePath.LastIndexOf("\\");
		if (lastSeparatorIndex == -1)
		{
			lastSeparatorIndex = callerFilePath.LastIndexOf("/");
		}

		var fileName = callerFilePath;

		if (lastSeparatorIndex != -1)
		{
			lastSeparatorIndex++;
			fileName = callerFilePath[lastSeparatorIndex..]; // From lastSeparatorIndex until the end of the string.
		}

		var fileNameWithoutExtension = fileName.TrimEnd(".cs", StringComparison.InvariantCultureIgnoreCase);
		return fileNameWithoutExtension;
	}

	/// <summary>
	/// Executes a command with Bourne shell.
	/// https://stackoverflow.com/a/47918132/2061103
	/// </summary>
	public static async Task ShellExecAsync(string cmd, bool waitForExit = true)
	{
		var escapedArgs = cmd.Replace("\"", "\\\"");

		var startInfo = new ProcessStartInfo
		{
			FileName = "/usr/bin/env",
			Arguments = $"sh -c \"{escapedArgs}\"",
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		};

		if (waitForExit)
		{
			// using var process = new ProcessAsync(startInfo);
			// process.Start();
			//
			// await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
			//
			// if (process.ExitCode != 0)
			// {
			// 	S_Log.Instance.LogError($"{nameof(ShellExecAsync)} command: {cmd} exited with exit code: {process.ExitCode}, instead of 0.");
			// }
		}
		else
		{
			using var process = Process.Start(startInfo);
		}
	}
    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
        dictionary.GetValueOrDefault(key, default!);

    public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        if (dictionary is null)
        {
            throw new Exception();
        }

        return dictionary.TryGetValue(key, out TValue? value) ? value : defaultValue;
    }

    public static string GetFullBaseDirectory()
	{
		var fullBaseDirectory = Path.GetFullPath(AppContext.BaseDirectory);

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			if (!fullBaseDirectory.StartsWith("/"))
			{
				fullBaseDirectory = fullBaseDirectory.Insert(0, "/");
			}
		}

		return fullBaseDirectory;
	}

	public static string GetExecutablePath()
	{
		var fullBaseDir = GetFullBaseDirectory();
		var deliveryFileName = Path.Combine(fullBaseDir, Consts.ExecutableName);
        deliveryFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{deliveryFileName}.exe" : $"{deliveryFileName}";
		if (File.Exists(deliveryFileName))
		{
			return deliveryFileName;
		}
		var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new NullReferenceException("Assembly or Assembly's Name was null.");
		var fluentExecutable = Path.Combine(fullBaseDir, assemblyName);
		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{fluentExecutable}.exe" : $"{fluentExecutable}";
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

	/// <summary>
	/// Reset the system sleep timer, this method has to be called from time to time to prevent sleep.
	/// It does not prevent the display to turn off.
	/// </summary>
	public static async Task ProlongSystemAwakeAsync()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			// Reset the system sleep timer.
			var result = SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED);
			if (result == 0)
			{
				throw new InvalidOperationException("SetThreadExecutionState failed.");
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			// Prevent macOS system from idle sleep and keep it for 1 second. This will reset the idle sleep timer.
			string shellCommand = $"caffeinate -i -t 1";
			await ShellExecAsync(shellCommand, waitForExit: true).ConfigureAwait(false);
		}
	}
}

public static class Consts
{
    public static readonly Version ClientVersion = new(1, 0, 1, 4);

    public const string ExecutableName = "Medialoc.Delivery.UI";

    public const string AppName = "Medialoc Delivery";

}
