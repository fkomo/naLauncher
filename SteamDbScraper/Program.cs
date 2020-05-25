using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using Ujeby.Common.Tools;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SteamDbScraper
{
	class Program
	{
		private static string CurrentClassName { get { return typeof(Program).Name; } }

		private static string[] SteamAppTypes = new string[]
		{
			"dlc",
			"game",
		};

		static void Main(string[] args)
		{
			Log.LogFileName = "SteamDbScraper.log";
			Log.LogFolder = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Directory.FullName;
			Log.WriteToConsole = true;

			if (Debugger.IsAttached)
				args = new[] { @"c:\filip\desktop\SteamDb.cache", "-s" };

			if (args.Length == 1)
			{
				var inputFile = args[0];

				var processed = PostProcessCache(inputFile);
				var garbageRemoved = RemoveGarbage(processed);
				var sorted = Sort(garbageRemoved);

				ListAppTypes(garbageRemoved);
			}
			else if (args.Length == 2)
			{
				var inputFile = args[0];
				if (args[1] == "-s")
				{
					var steamDbCache = new SteamDb.SteamDbCache(inputFile, new string[] { "game", "dlc" }, true);
					Console.WriteLine("scrapping ... press any key to stop.");
					Console.ReadKey();

					Console.WriteLine("stopping, please wait ...");
					steamDbCache.StopScrapping();
				}
			}
			else if (args.Length == 3)
			{
				var outputFile = args[0];
				var firstAppId = Int32.Parse(args[1]);
				var lastAppId = Int32.Parse(args[2]);

				var offset = firstAppId - ((int)(firstAppId / 10) * 10);

				for (var appId = firstAppId; appId < lastAppId; ++appId)
				{
					var steamApp = GetSteamApp(appId);
					if (string.IsNullOrEmpty(steamApp))
					{
						Console.Write(appId + " ");

						if (appId % 10 == 0)
							appId += 10 + offset;
						else
							appId = ((int)(appId / 10) * 10) + 10 + offset;

						appId--;
						continue;
					}

					var line = $"{ appId };{ steamApp }";
					File.AppendAllLines(outputFile, new[] { line });
					Console.WriteLine(Environment.NewLine + line);
				}
			}
			else
				Console.WriteLine("nothing to do ...");

			Console.WriteLine("finished");
		}

		public static string[] ListAppTypes(string cacheFile)
		{
			var types = new List<string>();

			var allLines = File.ReadAllLines(cacheFile);
			foreach (var line in allLines)
			{
				var lineParts = line.Split(';');
				var type = lineParts[1];

				if (!types.Contains(type))
				{
					types.Add(type);
					Console.WriteLine(type);
				}
			}

			return types.ToArray();
		}

		public static string RemoveGarbage(string cacheFile)
		{
			var outputFile = cacheFile + ".cleaned";
			if (File.Exists(outputFile))
				return outputFile;

			var titleCache = new Dictionary<string, string>();

			var allLines = File.ReadAllLines(cacheFile);
			foreach (var line in allLines)
			{
				if (string.IsNullOrEmpty(line))
					continue;

				try
				{
					var lineParts = line.Split(';');

					var type = lineParts[1];
					var title = lineParts[2];

					if (string.IsNullOrEmpty(title) || !SteamAppTypes.Contains(type))
						continue;

					if (!titleCache.ContainsKey(title))
						titleCache.Add(title, line);
				}
				catch (Exception ex)
				{
					Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ cacheFile }): line={ line }, { ex.ToString() }");
				}
			}

			File.WriteAllLines(outputFile, titleCache.OrderBy(x => Int32.Parse(x.Value.Split(';')[0])).Select(x => x.Value).ToArray());

			return outputFile;
        }
		
		public static string Sort(string cacheFile)
		{
			var outputFile = cacheFile + ".sorted";
			if (File.Exists(outputFile))
				return outputFile;
			
			var newCache = new SortedDictionary<int, string>();
			foreach (var line in File.ReadAllLines(cacheFile))
				newCache.Add(Int32.Parse(line.Split(';')[0]), line);

			File.WriteAllLines(outputFile, newCache.Select(x => x.Value).ToArray());
			return outputFile;
        }

		public static string PostProcessCache(string cacheFile)
		{
			var outputFile = cacheFile + ".postprocessed";
			if (File.Exists(outputFile))
				return outputFile;

			var allLines = File.ReadAllLines(cacheFile);
			foreach (var line in allLines)
			{
				try
				{
					var lineParts = line.Split(';');

					var steamAppId = lineParts[0];

					var newLine = string.Empty;
					if (lineParts.Length <= 3)
					{
						if (lineParts.Length == 2)
							lineParts = $"{ steamAppId };{ GetSteamApp(Int32.Parse(steamAppId)) }".Split(';');

						var appType = lineParts[2];
						var title = lineParts[1];

						newLine = $"{ steamAppId };{ appType };{ Strings.NormalizeString(title) };{ title }";
					}
					else if (lineParts.Length == 4)
						newLine = line;

					File.AppendAllLines(outputFile, new[] { newLine });
					Console.WriteLine(newLine);
                }
				catch (Exception ex)
				{
					Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ cacheFile }): line={ line }, { ex.ToString() }");
				}
			}

			return outputFile;
        }

		#region private methods
		
		private static string GetSteamApp(int steamAppId)
		{
			// rate limiter, 200 requests/5 min
			Thread.Sleep(1600);

			var steamDbUrl = $"http://store.steampowered.com/api/appdetails?appids={ steamAppId }";

			var response = WebUtils.SilentWebRequest(steamDbUrl);

			dynamic deserializedResponse = JsonConvert.DeserializeObject(response);
			if (deserializedResponse == null || deserializedResponse[steamAppId.ToString()].success == false)
				return null;

			return $"{ deserializedResponse[steamAppId.ToString()].data.name };{ deserializedResponse[steamAppId.ToString()].data.type }";
		}

		#endregion
	}
}
