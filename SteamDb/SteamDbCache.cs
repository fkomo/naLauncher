using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Xml;
using Ujeby.Common.Tools;

namespace SteamDb
{
	public class SteamApp
	{
		private SteamApp()
		{

		}

		public SteamApp(string cacheLine)
		{
			var parts = cacheLine.Split(';');

			Id = parts[0];
			Type = parts[1];
			NormalizedTitle = parts[2];
			Title = parts[3];
		}

		public static SteamApp Create(string id, string type, string title)
		{
			return new SteamApp
			{
				Id = id,
				Type = type,
				Title = title,
				NormalizedTitle = Strings.NormalizeString(title)
			};
		}

		public string Id { get; private set; }
		public string Type { get; private set; }
		public string NormalizedTitle { get; private set; }
		public string Title { get; private set; }

		public string SteamPoweredApiUrl {  get { return $"http://store.steampowered.com/api/appdetails?appids={ Id }"; } }

		public override string ToString()
		{
			return $"{ Id };{ Type };{ NormalizedTitle };{ Title }";
		}
	}

	public class SteamDbCache
	{
		private static string CurrentClassName { get { return typeof(SteamDbCache).Name; } }

		public const string DefaultFileName = "SteamDb.cache";

		public string[] ValidTypes { get; private set; }

		public SortedDictionary<int, SteamApp> Entries { get; private set; } = new SortedDictionary<int, SteamApp>();

		public SteamDbCache(string file, string[] steamAppTypes = null, bool scrapeMissing = false)
		{
			ValidTypes = steamAppTypes ?? new string[] { };

			Load(file);

			if (scrapeMissing)
			{
				LoadMissing();
				StartScrapping();
			}
		}

		public SteamApp GetByTitle(string title, bool ignoreLocalCache = false)
		{
			if (ignoreLocalCache)
				return FindBestMatch(title, QuerySteamDb(title));

            return FindBestMatch(title, Entries.Values.ToArray()) ?? FindBestMatch(title, QuerySteamDb(title));
		}

		private SteamApp FindBestMatch(string title, SteamApp[] steamApps)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var bestMatch = null as SteamApp;
			var result = new List<KeyValuePair<SteamApp, int>>();
			var normalizedTitle = Strings.NormalizeString(title);

			foreach (var steamApp in steamApps)
			{
				if (steamApp.NormalizedTitle == normalizedTitle)
				{
					bestMatch = steamApp;
					break;
				}

				if (steamApp.NormalizedTitle.Contains(normalizedTitle))
					result.Add(new KeyValuePair<SteamApp, int>(steamApp, Strings.DamerauLevenshteinEditDistance(normalizedTitle, steamApp.NormalizedTitle)));
			}

			if (bestMatch == null && result.Count > 0)
			{
				var bestMatchKeyPair = result.Where(x => x.Value < normalizedTitle.Length).OrderBy(x => x.Value).FirstOrDefault();
				bestMatch = bestMatchKeyPair.Key;

				if (bestMatch != null)
					Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ title } in { steamApps.Length } apps): [{ result.Count } possible matches, best={ bestMatchKeyPair.Value}:{ bestMatchKeyPair.Key }]");
			}

			stopwatch.Stop();
			var duration = stopwatch.ElapsedMilliseconds;
			Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ title } in { steamApps.Length } apps): [{ bestMatch }] in { duration }ms");

			return bestMatch;
		}

		public void Save(string file = null)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			file = file ?? CurrentCacheFile;
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			File.WriteAllLines(file, Entries.Select(x => x.Value.ToString()).ToArray());

			stopwatch.Stop();
			var duration = stopwatch.ElapsedMilliseconds;
			Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ file }) in { duration }ms");
		}

		public void StopScrapping()
		{
			stopScrapping = true;
			ScrappingThread?.Join();
			ScrappingThread = null;
		}

		public void StartScrapping()
		{
			StopScrapping();

			stopScrapping = false;
			ScrappingThread = new Thread(() => ScrapeMissing());
			ScrappingThread.Start();
		}

		#region private properties

		private const int MaxSteamAppId = 999999;
		private string CurrentCacheFile { get; set; }
		private string CurrentMissingCacheFile { get { return CurrentCacheFile + ".missing"; } }
		private bool stopScrapping = false;
		private Thread ScrappingThread = null;
		private static Random random = new Random((int)DateTime.UtcNow.Ticks);
		private List<string> Missing { get; set; }

		private static object entriesLock = new object();

		#endregion

		#region private methods

		/// <summary>
		/// query https://steamdb.info/search/?q=... for new SteamAppId-s
		/// also adds result to cache
		/// </summary>
		/// <param name="gameTitle"></param>
		/// <returns></returns>
		private SteamApp[] QuerySteamDb(string gameTitle)
		{
			var result = new List<SteamApp>();

			var url = $"https://steamdb.info/search/?q={ WebUtility.UrlEncode(gameTitle) }";
			var response = WebUtils.WebRequest(url);
			if (response == null)
				return result.ToArray();

			var tableElementStart = response.IndexOf("<tbody hidden>");
			if (tableElementStart < 0)
				return result.ToArray();

			var tableBodyElement = response.Substring(tableElementStart, response.IndexOf("</tbody>", tableElementStart) - tableElementStart + "</tbody>".Length);
			tableBodyElement = tableBodyElement.Replace("<tbody hidden>", "<tbody>");

			var decodedAndCleaned = WebUtility.HtmlDecode(tableBodyElement).Replace("&", string.Empty);

			var xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(decodedAndCleaned);

			foreach (XmlNode steamAppNode in xmlDocument.DocumentElement.ChildNodes)
			{
				var type = steamAppNode.ChildNodes[1].InnerText.ToLower();

				if (!ValidTypes.Contains(type))
					continue;

				var steamAppId = steamAppNode.Attributes["data-appid"].Value;

				var title = steamAppNode.ChildNodes[2].InnerText;

				// remove muted part of title
				title = Strings.RemoveFromTo(title, "<i class=\"muted\">", "</i>").Trim();
				title = title.Trim('\n');

				result.Add(SteamApp.Create(steamAppId, type, title));
			}

			// add new steam apps to cache
			foreach (var newSteamApp in result)
				AddOrUpdateSteamApp(newSteamApp);

			return result.ToArray();
		}

		private void Load(string file)
		{
			var stopwatch = new Stopwatch();
            stopwatch.Start();

			Entries = new SortedDictionary<int, SteamApp>();

			var index = 0;
			var cacheLines = File.ReadAllLines(file);
			foreach (var line in cacheLines)
			{
				try
				{
					var steamApp = new SteamApp(line);
					if (ValidTypes == null || ValidTypes.Contains(steamApp.Type.ToLower()))
						Entries.Add(Int32.Parse(steamApp.Id), steamApp);
				}
				catch (Exception ex)
				{
					Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ file }): index={ index }, line='{ line }', ex={ ex }");
				}
				index++;
			}

			stopwatch.Stop();
			var duration = stopwatch.ElapsedMilliseconds;
			Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ file }): [{ Entries.Count } entries] in { duration }ms");

			CurrentCacheFile = file;
		}

		private void LoadMissing()
		{
			if (File.Exists(CurrentMissingCacheFile))
			{
				Missing = File.ReadAllLines(CurrentMissingCacheFile).ToList();
				Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ CurrentMissingCacheFile }): [{ Missing.Count } entries]");
			}
		}

		private void SaveMissing(string file = null)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			file = file ?? CurrentMissingCacheFile;
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			File.WriteAllLines(file, Missing);

			stopwatch.Stop();
			var duration = stopwatch.ElapsedMilliseconds;
			Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ file }) in { duration }ms");
		}

		/// <summary>
		/// generates array of missing SteamAppId
		/// </summary>
		private void GenerateMissing()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Missing = new List<string>();
			for (var steamAppId = 10; steamAppId < MaxSteamAppId; steamAppId += 10)
			{
				if (stopScrapping)
					return;

				if (Entries.Keys.Any(key => key == steamAppId))
					continue;

				Missing.Add(steamAppId.ToString());
			}

			stopwatch.Stop();

			var duration = stopwatch.ElapsedMilliseconds;
			Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }(): [{ Missing.Count } entries] in { duration }ms");

			File.WriteAllLines(CurrentCacheFile + ".missing", Missing);
		}

		/// <summary>
		/// continous scrapping of missing steam apps
		/// </summary>
		private void ScrapeMissing()
		{
			Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }() started in background thread ...");

			if (!File.Exists(CurrentMissingCacheFile))
				GenerateMissing();

			while (!stopScrapping)
			{
				try
				{
					FindNewSteamApp();
					SaveMissing();
				}
				catch (Exception ex)
				{
					Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }() { ex.ToString() }");
				}
			}
		}

		private bool FindNewSteamApp()
		{
			for (var i = random.Next(0, Missing.Count - 1); i < Missing.Count && !stopScrapping; ++i)
			{
				var missingParts = Missing[i].Split(';');
				var missingSteamAppId = missingParts[0];

				// if steam app is no longer missing, remove it from missing list
				if (Entries.Any(e => e.Key == Int32.Parse(missingSteamAppId)))
				{
					Missing.RemoveAt(i);
					return false;
				}

				// if steam app was recently checked
				if (missingParts.Length > 1 && DateTime.Parse(missingParts[1]).AddDays(7) > DateTime.Now)
					continue;
				
				var newSteamApp = GetSteamApp(missingSteamAppId);
				if (newSteamApp != null)
				{
					// woohoo new steam app found
					AddOrUpdateSteamApp(newSteamApp, i);
					return true;
				}
				else
					Missing[i] = $"{ missingSteamAppId };{ DateTime.Now.ToShortDateString() }";
			}

			return false;
        }

		private void AddOrUpdateSteamApp(SteamApp newSteamApp, int? missingIndex = null)
		{
			lock (entriesLock)
			{
				// check if id and name is already in cache
				if (Entries.Any(e => e.Key == Int32.Parse(newSteamApp.Id) && e.Value.NormalizedTitle == newSteamApp.NormalizedTitle))
					return;

				Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ newSteamApp.ToString() })");

				if (Entries.Any(e => e.Key == Int32.Parse(newSteamApp.Id)))
                    Entries.Remove(Int32.Parse(newSteamApp.Id));

				Entries.Add(Int32.Parse(newSteamApp.Id), newSteamApp);
				Save();

				if (missingIndex.HasValue)
				{
					Missing.RemoveAt(missingIndex.Value);
					SaveMissing();
				}
			}
		}

		private SteamApp GetSteamApp(string steamAppId, string[] steamAppTypes = null)
		{
			// rate limiter, 200 requests/5 min
			System.Threading.Thread.Sleep(5000);

			var steamDbUrl = $"http://store.steampowered.com/api/appdetails?appids={ steamAppId }";

			var response = WebUtils.SilentWebRequest(steamDbUrl);

			dynamic deserializedResponse = JsonConvert.DeserializeObject(response);
			if (deserializedResponse == null || deserializedResponse[steamAppId].success == false)
				return null;

			string appType = deserializedResponse[steamAppId].data.type.ToString();
			if (steamAppTypes != null && !steamAppTypes.Contains(appType))
				return null;

			return SteamApp.Create(
				steamAppId,
				appType,
				deserializedResponse[steamAppId].data.name.ToString()
			);
		}

		#endregion
	}
}
