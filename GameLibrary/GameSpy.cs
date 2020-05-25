using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Ujeby.Common.Tools;
using Shell32;
using System.Linq;
using System.Collections.Generic;

namespace GameLibrary.Tools
{
	public class GameSpy : IDisposable
	{
		public bool Running { get; private set; } = true;

		private string GameTitle { get; set; }
		private string GameExecutable { get; set; }

		private DateTime StartTime { get; set; }
		Action<int> AfterFinish { get; set; }
		private Thread SpyThread { get; set; } = null;
		private bool Terminated { get; set; } = false;

		private static List<GameSpy> All { get; set; } = new List<GameSpy>();

		public static GameSpy Create(string gameTitle, string gameExecutable, Action<int> afterFinish)
		{
			if (gameExecutable.EndsWith(".url"))
				return null;

			if (gameExecutable.EndsWith(".lnk"))
				gameExecutable = GetExecutableFromShortcut(gameExecutable);
			
			if (string.IsNullOrEmpty(gameExecutable))
				return null;

			var gameSpy = new GameSpy(gameTitle, gameExecutable, afterFinish);
			All.Add(gameSpy);

			return gameSpy;
		}
		
		private GameSpy(string gameTitle, string gameExecutable, Action<int> afterFinish)
		{
			GameTitle = gameTitle;
			GameExecutable = gameExecutable;

			StartTime = DateTime.Now;
			AfterFinish = afterFinish;

			SpyThread = new Thread(
				new ThreadStart(() =>
				{
					Spy();
				}));
			SpyThread.Start();
		}

		private static string GetExecutableFromShortcut(string shortcut)
		{
			try
			{
				var pathOnly = Path.GetDirectoryName(shortcut);
				var filenameOnly = Path.GetFileName(shortcut);

				var folderItem = new Shell().NameSpace(pathOnly)?.ParseName(filenameOnly);
				if (folderItem != null)
					return ((ShellLinkObject)folderItem.GetLink).Path;
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}

			return null;
		}

		private static string[] ProcessesToIgnore = new string[] { "idle", "svchost", "chrome", "conhost" };

		private void Spy()
		{
			Log.WriteLine($"{ nameof(GameSpy) }({ GameExecutable }) started");

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			try
			{
				var executableName = Path.GetFileNameWithoutExtension(GameExecutable).ToLower();

				Thread.Sleep(Properties.Settings.Default.GameSpyInitialWait * 1000);

				while (!Terminated)
				{
					var allProcesses = Process.GetProcesses()
						.Where(p => !ProcessesToIgnore.Contains(p.ProcessName.ToLower()));

					//Debug.WriteLine("-- all processes -----------");
					//foreach (var process in allProcesses.OrderBy(p => p.ProcessName))
					//	Debug.WriteLine(process);
					//Debug.WriteLine("----------------------------");

					var potentialProcesses = allProcesses
						.Where(p => string.Compare(p.ProcessName, executableName, true) == 0 || string.Compare(p.MainWindowTitle, GameTitle, true) == 0)
						.ToArray();
					if (potentialProcesses.Count() < 1)
						break;

					Debug.WriteLine($"{ nameof(GameSpy) }({ string.Join(", ", potentialProcesses.OrderBy(p => p.ProcessName).Select(p => p.ProcessName)) })");

					Thread.Sleep(Properties.Settings.Default.GameSpyCheckTimer * 1000);

					//var counterList = new List<PerformanceCounter>();
					//Process.GetProcesses().ToList().ForEach(p =>
					//{
					//	using (p)
					//	{
					//		if (counterList.FirstOrDefault(c => c.InstanceName == p.ProcessName) == null)
					//			counterList.Add(new PerformanceCounter("Process", "% Processor Time", p.ProcessName, true));
					//	}
					//});

					//long topUsage = 0;
					//string topProcess = null;
					//counterList.ForEach(c =>
					//{
					//	try
					//	{
					//		if (!new string[] { "idle", "system" }.Contains(c.InstanceName.Trim().ToLower()) && c.RawValue > topUsage)
					//		{
					//			topUsage = c.RawValue;
					//			topProcess = c.InstanceName;
					//		}
					//	}
					//	catch (InvalidOperationException) { /* some will fail */ }
					//});

					//Debug.WriteLine($"topProcess: { topProcess }, { topUsage }%");
				}
			}
			catch (Exception ex)
			{
				Terminated = true;
				Log.WriteLine(ex.ToString());
			}
			finally
			{
				stopwatch.Stop();

				var elapsedMinutes = (int)(stopwatch.ElapsedMilliseconds / 60 / 1000);
				Log.WriteLine($"{ nameof(GameSpy) }({ GameExecutable }) finished after { elapsedMinutes } minutes");

				AfterFinish(Terminated ? 0 : elapsedMinutes);
				Running = false;
			}
		}

		public void Terminate()
		{
			try
			{
				if (SpyThread == null)
					return;

				Terminated = true;
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		public void Dispose()
		{
			Terminate();
		}

		public static void TerminateAll()
		{
			foreach (var gameSpy in All)
				gameSpy.Terminate();

			All.Clear();
		}
	}
}
