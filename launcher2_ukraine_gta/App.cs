using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;

namespace launcher2_ukraine_gta;

public class App : Application
{
	static App()
	{
		AppDomain.CurrentDomain.AssemblyResolve += ResolveLegacyLauncherAssembly;
	}

	private static Assembly ResolveLegacyLauncherAssembly(object sender, ResolveEventArgs args)
	{
		AssemblyName requestedAssembly = new AssemblyName(args.Name);
		if (string.Equals(requestedAssembly.Name, "LEGION GTA", StringComparison.OrdinalIgnoreCase))
		{
			return Assembly.GetExecutingAssembly();
		}
		return null;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public void InitializeComponent()
	{
	}

	private static void WriteFatalLog(Exception ex)
	{
		try
		{
			string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher.log");
			File.AppendAllText(path, $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss.fff}] [FATAL] {ex}\r\n");
		}
		catch
		{
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
	public static void Main()
	{
		// Remove forced admin requirement to reduce antivirus false positives
		// Admin rights will be requested only when needed
		App app = new App();
		try
		{
			MainWindow mainWindow = new MainWindow();
			mainWindow.Opacity = 1.0;

			app.Run(mainWindow);
		}
		catch (Exception ex)
		{
			WriteFatalLog(ex);
			throw;
		}
	}

	private static bool IsRunningAsAdmin()
	{
		try
		{
			WindowsIdentity current = WindowsIdentity.GetCurrent();
			WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
			return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	private static void RestartAsAdministrator()
	{
		try
		{
			string executablePath = Process.GetCurrentProcess().MainModule?.FileName;
			if (string.IsNullOrWhiteSpace(executablePath))
			{
				return;
			}
			string args = BuildArguments(Environment.GetCommandLineArgs().Skip(1));
			Process.Start(new ProcessStartInfo
			{
				FileName = executablePath,
				Arguments = args,
				UseShellExecute = true,
				Verb = "runas",
				WorkingDirectory = Path.GetDirectoryName(executablePath)
			});
		}
		catch
		{
		}
	}

	private static string BuildArguments(IEnumerable<string> args)
	{
		if (args == null)
		{
			return string.Empty;
		}
		return string.Join(" ", args.Select(QuoteArgument));
	}

	private static string QuoteArgument(string argument)
	{
		if (string.IsNullOrEmpty(argument))
		{
			return "\"\"";
		}
		if (!argument.Contains(" ") && !argument.Contains("\t") && !argument.Contains("\""))
		{
			return argument;
		}
		return "\"" + argument.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
	}

	private static void DoEvents()
	{
		DispatcherFrame frame = new DispatcherFrame();
		Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
		Dispatcher.PushFrame(frame);
	}

	private static object ExitFrame(object frame)
	{
		((DispatcherFrame)frame).Continue = false;
		return null;
	}
}
