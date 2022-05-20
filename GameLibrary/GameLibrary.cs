using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Ujeby.Common.Tools;
using SteamDb;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using GameLibrary.GameDataProviders;
using GameLibrary.Properties;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;

namespace GameLibrary
{
	public enum GameFilter
	{
		All = 0,
		Installed = 1,
		Removed = 2,
		Beaten = 3,
		Unbeaten = 4,
		WithControllerSupport = 5,
		Unidentified = 6,

		Count
	}

	public enum GameOrder
	{
		Title = 0,
		PlayTime = 1,
		PlayCount = 2,
		Rating = 3,
		LastPlayed = 4,
		BeatTime = 5,
		DateAdded = 6,

		Count
	}

	internal enum DefaultImage
	{
		NotFound,
	}

	public class GameLibrary
	{
		const string BackupExtension = ".backup-";
		const string TimestampFormat = "yyyyMMddHHmmssffff";
		const string GamesLibraryFileName = "naLauncher-lib.xml";

		public static ConcurrentDictionary<string, GameInfo> Games { get; private set; } = new ConcurrentDictionary<string, GameInfo>();

		/// <summary>
		/// static initialization
		/// </summary>
		public static void Initialize()
		{
			Log.WriteLineDebug("GameLibrary.Initialize()");

			Log.WriteLine($"------------------------------------------------------------------------------------------------------------------------");

			var userSteamDbCacheFile = Path.Combine(UserDataFolder, SteamDbCache.DefaultFileName);
			var applicationSteamDbCacheFile = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "Content", SteamDbCache.DefaultFileName);

			// copy steamdb cache to user data folder
			if (!File.Exists(userSteamDbCacheFile) || new FileInfo(userSteamDbCacheFile).Length < new FileInfo(applicationSteamDbCacheFile).Length)
				File.Copy(applicationSteamDbCacheFile, userSteamDbCacheFile, true);

			// create game data providers
			var allowedDataProviders = Settings.Default.GameDataProviders.Split(';');

			if (allowedDataProviders.Contains(new UserDataProvider().GetGameDataType()))
				GameDataProviders.Add(new UserDataProvider());

			if (allowedDataProviders.Contains(new SteamDbInfoDataProvider().GetGameDataType()))
				GameDataProviders.Add(new SteamDbInfoDataProvider());

			if (allowedDataProviders.Contains(new SteamCryoTankNetDataProvider().GetGameDataType()))
				GameDataProviders.Add(new SteamCryoTankNetDataProvider());

			if (allowedDataProviders.Contains(new SalenautsComDataProvider().GetGameDataType()))
				GameDataProviders.Add(new SalenautsComDataProvider());

			if (allowedDataProviders.Contains(new IgdbComDataProvider().GetGameDataType()))
				GameDataProviders.Add(new IgdbComDataProvider());

			// load embedded images
			LoadImageFromEmbeddedResource("image-not-found.png", DefaultImage.NotFound);
		}

		/// <summary>
		/// returns filtered and ordered list of games
		/// </summary>
		/// <param name="titleFilter"></param>
		/// <param name="filter"></param>
		/// <param name="order"></param>
		/// <param name="ascending"></param>
		/// <returns></returns>
		public static string[] ListGames(string filterCommand, GameFilter filter, GameOrder order, bool ascending)
		{
			var games = Games.AsEnumerable();

			if (!string.IsNullOrEmpty(filterCommand))
			{
				try
				{
					var commands = filterCommand.Trim().ToLower().Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var command in commands)
					{
						if (command.StartsWith("*"))
						{
							// remove all white spaces in string
							var c = command.Replace(" ", string.Empty);
							//var c = Regex.Replace(command, @"\s+", string.Empty);

							var op = GetOperatorFromCommand(c);

							// DATE filters
							if (c.Contains("*beaten"))
							{
								if (GetDateFromCommand(c.Replace($"*beaten{ op }", string.Empty), out int? year, out int? month, out int? day))
								{
									games = games.Where(g => EvaluateDates(g.Value.Completed, op, year, month, day));
									Debug.WriteLine($"beaten{ op }{ year }/{ month }/{ day }");
								}
							}
							else if (c.Contains("*added"))
							{
								if (GetDateFromCommand(c.Replace($"*added{ op }", string.Empty), out int? year, out int? month, out int? day))
								{
									games = games.Where(g => EvaluateDates(g.Value.Added, op, year, month, day));
									Debug.WriteLine($"added{ op }{ year }/{ month }/{ day }");
								}
							}
							else if (c.Contains("*played"))
							{
								if (GetDateFromCommand(c.Replace($"*played{ op }", string.Empty), out int? year, out int? month, out int? day))
								{
									games = games.Where(g => EvaluateDates(g.Value.DateTimesPlayed, op, year, month, day));
									Debug.WriteLine($"played{ op }{ year }/{ month }/{ day }");
								}
							}

							// NUMBER filters
							else if (c.Contains("*playcount"))
							{
								if (GetNumberFromCommand(c.Replace($"*playcount{ op }", string.Empty), out int? count))
								{
									games = games.Where(g => EvaluateNum(g.Value.PlayCount, op, count.Value));
									Debug.WriteLine($"playcount{ op }{ count }");
								}
							}
							else if (c.Contains("*rating"))
							{
								if (GetNumberFromCommand(c.Replace($"*rating{ op }", string.Empty), out int? rating))
								{
									games = games.Where(g => EvaluateNum(g.Value.Rating, op, rating.Value));
									Debug.WriteLine($"rating{ op }{ rating }");
								}
							}
						}
						else
							// filter by game title
							games = games.Where(g => g.Value.Title.ToLower().Contains(command));
					}
				}
				catch (Exception)
				{
					//Log.WriteLine(ex.ToString());
					games = Games.AsEnumerable();
				}
			}

			switch (filter)
			{
				case GameFilter.All: break;
				case GameFilter.Installed: games = games.Where(g => !g.Value.Removed); break;
				case GameFilter.Removed: games = games.Where(g => g.Value.Removed); break;
				case GameFilter.Beaten: games = games.Where(g => g.Value.Completed.HasValue); break;
				case GameFilter.Unbeaten: games = games.Where(g => !g.Value.Completed.HasValue && !g.Value.Removed); break;
				case GameFilter.WithControllerSupport: games = games.Where(g => g.Value.GamepadFriendly == true && !g.Value.Removed); break;
				case GameFilter.Unidentified: games = games.Where(g => g.Value.Image == null); break;

				default:
					throw new NotImplementedException(filter.ToString());
			}

			switch (order)
			{
				case GameOrder.Title: games = games.OrderBy(g => g.Value.Title); break;
				case GameOrder.PlayTime: games = games.OrderBy(g => g.Value.TotalTimePlayed); break;
				case GameOrder.PlayCount: games = games.OrderBy(g => g.Value.PlayCount); break;
				case GameOrder.Rating: games = games.OrderBy(g => g.Value.Rating); break;
				case GameOrder.BeatTime: games = games.OrderBy(g => g.Value.BeatenIn); break;
				case GameOrder.DateAdded: games = games.OrderBy(g => g.Value.Added); break;
				case GameOrder.LastPlayed:
					{
						games = games
							.Where(g => !g.Value.LastPlayed.HasValue)
							.OrderByDescending(g => g.Value.Added)
							.Concat(games
								.Where(g => g.Value.LastPlayed.HasValue)
								.OrderBy(g => g.Value.LastPlayed));
					}
					break;

				default:
					throw new NotImplementedException(order.ToString());
			}

			var result = games.Select(g => g.Key);
			if (!ascending)
				result = result.Reverse();

			return result.ToArray();
		}

		private static bool EvaluateDates(DateTime[] op1, string op, int? year, int? month, int? day)
		{
			return op1.Any(d => EvaluateDates(d, op, year, month, day));
		}

		private static bool EvaluateDates(DateTime? op1, string op, int? year, int? month, int? day)
		{
			switch (op)
			{
				case "=": return op1.HasValue &&
					(!year.HasValue || op1.Value.Year == year.Value) &&
					(!month.HasValue || op1.Value.Month == month.Value) &&
					(!day.HasValue || op1.Value.Day == day.Value);

				case "<": return op1.HasValue &&
					(!year.HasValue || op1.Value.Year < year.Value) &&
					(!month.HasValue || op1.Value.Month < month.Value) &&
					(!day.HasValue || op1.Value.Day < day.Value);

				case ">": return op1.HasValue &&
					(!year.HasValue || op1.Value.Year > year.Value) &&
					(!month.HasValue || op1.Value.Month > month.Value) &&
					(!day.HasValue || op1.Value.Day > day.Value);
			}

			return false;
		}

		private static bool EvaluateNum(int? op1, string op, int op2)
		{
			if (!op1.HasValue)
				return false;

			switch (op)
			{
				case "=": return op1.Value == op2;
				case ">": return op1.Value > op2;
				case "<": return op1.Value < op2;
			}

			return false;
		}

		private static bool GetNumberFromCommand(string commandNumber, out int? number)
		{
			number = null;

			try
			{
				if (Int32.TryParse(commandNumber, out int n))
					number = n;

				return number.HasValue;
			}
			catch
			{
				return false;
			}
		}

		private static string GetOperatorFromCommand(string command)
		{
			if (command.Contains("="))
				return "=";
			else if (command.Contains("<"))
				return "<";
			else if (command.Contains(">"))
				return ">";

			return null;
		}

		/// <summary>
		/// parse command date in format YEAR/MONTH/DAY
		/// </summary>
		/// <param name="commandDate">format: YEAR/MONTH/DAY</param>
		/// <param name="year"></param>
		/// <param name="month"></param>
		/// <param name="day"></param>
		/// <returns>true if at least one part is parsed</returns>
		private static bool GetDateFromCommand(string commandDate, out int? year, out int? month, out int? day)
		{
			year = null;
			month = null;
			day = null;

			try
			{
				var parts = commandDate.Split('/');
				if (Int32.TryParse(parts[0], out int y))
					year = y;

				if (parts.Length > 1 && Int32.TryParse(parts[1], out int m))
					month = m;

				if (parts.Length > 2 && Int32.TryParse(parts[2], out int d))
					day = d;

				return year.HasValue || month.HasValue || day.HasValue;
			}
			catch
			{
				return false;
			}
		}

		public static ConcurrentDictionary<string, Bitmap> GameImageCache { get; private set; } = new ConcurrentDictionary<string, Bitmap>();

		static ConcurrentDictionary<string, Bitmap> ImageCache { get; set; } = new ConcurrentDictionary<string, Bitmap>();

		static string BackupDirectory
		{
			get
			{

				var backupDirectory = Path.Combine(UserDataFolder, "GameLibraryBackup");
				if (!Directory.Exists(backupDirectory))
					Directory.CreateDirectory(backupDirectory);

				return backupDirectory;
			}
		}

		static string GameLibraryFile { get { return Path.Combine(UserDataFolder, GamesLibraryFileName); } }
		public static string GamesDirectory { get { return Directory.Exists(Settings.Default.GamesDirectory) ? Settings.Default.GamesDirectory : null; } }

		static List<IGameDataProvider> GameDataProviders { get; set; } = new List<IGameDataProvider>();

		/// <summary>
		/// data folder
		/// </summary>
		public static string UserDataFolder
		{
			get
			{
				var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var userDataFolder = Path.Combine(roaming, "Ujeby", "naLauncher" + (Debugger.IsAttached ? "-debug" : null));

				if (!Directory.Exists(userDataFolder))
					Directory.CreateDirectory(userDataFolder);

				return userDataFolder;
			}
		}

		static void LoadImageFromEmbeddedResource(string imageName, DefaultImage defaultImage)
		{
			var fileStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"GameLibrary.Content.{ imageName }");
			var image = (Bitmap)Image.FromStream(fileStream);

			ImageCache.AddOrUpdate(defaultImage.ToString(), image, (key, oldImage) => image);
		}

		/// <summary>
		/// dispose
		/// </summary>
		public static void Dispose()
		{
			SteamDbInfoDataProvider.StopScrapping();
		}

		/// <summary>
		/// supported file extensions that will be loaded in Load() method
		/// </summary>
		static string[] SupportedGameExtensions { get; set; } = new string[]
		{
			".lnk",
			".exe",
			".url",
			".cmd",
			".bat"
		};

		/// <summary>
		/// load and prepare whole game library
		/// </summary>
		/// <returns></returns>
		public static bool Load()
		{
			return LoadGames(Settings.Default.LibraryToLoad) && LoadGameImages();
		}

		/// <summary>
		/// load game library from xml and add new games 
		/// </summary>
		/// <returns>games to update (without images)</returns>
		static bool LoadGames(string libraryToLoad = null)
		{
			using (var tb = new TimedBlock($"GameLibrary.LoadGames({ libraryToLoad })"))
			{
				try
				{
					Games.Clear();

					// deserialize library
					DeserializeLibrary(libraryToLoad); 
					
					if (!string.IsNullOrEmpty(GamesDirectory))
					{
						// add new games from current games folder
						foreach (var gameShortcut in Directory.GetFiles(GamesDirectory, "*.*", SearchOption.AllDirectories))
						{
							var fileInfo = new FileInfo(gameShortcut);

							if (!SupportedGameExtensions.Contains(fileInfo.Extension.ToLower()))
								continue;

							var gameTitle = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);
							var gameId = GetGameId(gameTitle);

							if (!Games.ContainsKey(gameId))
							{
								var newGameInfo =
									new GameInfo
									{
										Title = gameTitle,
										Shortcut = gameShortcut,
										Added = DateTime.Now,
									};

								// add new game
								Games.AddOrUpdate(gameId, newGameInfo, (key, oldGameInfo) => newGameInfo);
							}

							// update game shortcut everytime
							Games[gameId].Shortcut = gameShortcut;
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					Log.WriteLine(ex.ToString());
				}
			}

			return false;
		}

		/// <summary>
		/// load existing game images to cache
		/// </summary>
		/// <returns></returns>
		static bool LoadGameImages()
		{
			using (var tb = new TimedBlock($"GameLibrary.LoadGameImages({ Games.Count })"))
			{
				try
				{
					GameImageCache.Clear();

					Parallel.ForEach(Games.Keys, gameId =>
					{
						UpdateGameImageCache(gameId, Games[gameId].Image);
					});

					return true;
				}
				catch (Exception ex)
				{
					Log.WriteLine(ex.ToString());
				}
			}

			return false;
		}

		/// <summary>
		/// deserialize specified library file
		/// </summary>
		/// <param name="libraryToLoad"></param>
		static void DeserializeLibrary(string libraryToLoad = null)
		{
			if (string.IsNullOrEmpty(libraryToLoad))
				libraryToLoad = GameLibraryFile;

			Games.Clear();

			if (File.Exists(libraryToLoad))
			{
				string libraryXml;

				// load games from library backup
				//if (libraryToLoad.Contains(BackupExtension))
				//	libraryXml = Utils.Decompress(File.ReadAllBytes(libraryToLoad));
				//else
					libraryXml = File.ReadAllText(libraryToLoad);

				Games = Utils.Deserialize<ConcurrentDictionary<string, GameInfo>>(libraryXml);

				// check library for corruption
				CheckLibrary(Games);

				// fix library
				Games = FixLibrary(Settings.Default.FixLibrary, Games);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="games"></param>
		private static void CheckLibrary(ConcurrentDictionary<string, GameInfo> games)
		{
			var gameTitlesToFix = new List<string>();

			// mark games with not-existing shortcuts as removed
			foreach (var game in games.Where(g => g.Value.Shortcut != null && !File.Exists(g.Value.Shortcut)))
			{
				game.Value.Shortcut = null;
				Log.WriteLine($"CheckLibrary({ game.Value.Shortcut }): shortcut not found -> fixed");
			}

			foreach (var game in games)
			{
				if (game.Value.Shortcut != null)
				{
					var fileInfo = new FileInfo(game.Value.Shortcut);

					// check if shortcut corresponds with game title
					var gameTitleFromShortcut = fileInfo.Name.Replace(fileInfo.Extension, string.Empty);
					if (gameTitleFromShortcut != game.Value.Title)
						throw new Exception($"CheckLibrary({ game.Value.Title }, { fileInfo.Name }): invalid game+shortcut -> manual fix needed!");

					// find games that points to the same shortcut
					if (games.Values.Count(g => g.Shortcut != null && g.Shortcut == game.Value.Shortcut) > 1)
					{
						Log.WriteLine($"CheckLibrary({ game.Value.Shortcut }): multiple games found");
						// TODO delete older games and let only the newest ?
					}
				}

				// find games that have corrupted game ids
				var trueGameId = GetGameId(game.Value.Title);
				if (game.Key != trueGameId)
				{
					if (games.TryGetValue(trueGameId, out GameInfo duplicateGame))
						throw new Exception($"CheckLibrary({ game.Value.Title }): { game.Key } != { trueGameId } -> duplicate found!");

					else
						gameTitlesToFix.Add(game.Value.Title);
				}
			}

			// fix game ids
			foreach (var gameTitleToFix in gameTitlesToFix)
			{
				var oldGameId = Games.Single(v => v.Value.Title == gameTitleToFix).Key;
				var trueGameId = GetGameId(gameTitleToFix);

				if (Games.TryRemove(oldGameId, out GameInfo removedGame))
				{
					Games.AddOrUpdate(trueGameId, removedGame, (id, oldValue) => removedGame);
					Log.WriteLine($"CheckLibrary({ gameTitleToFix }): { oldGameId } != { trueGameId } -> fixed");
				}
				else
					throw new Exception($"CheckLibrary({ gameTitleToFix }): { oldGameId } != { trueGameId } -> remove failed!");
			}
		}

		/// <summary>
		/// fix differences in old vs new library
		/// </summary>
		/// <param name="oldLibrary"></param>
		/// <param name="newLibrary"></param>
		/// <returns></returns>
		static ConcurrentDictionary<string, GameInfo> FixLibrary(string oldLibrary, ConcurrentDictionary<string, GameInfo> newLibrary)
		{
			using (var tb = new TimedBlock($"GameLibrary.FixLibrary({ (string.IsNullOrEmpty(oldLibrary) ? "null" : oldLibrary) }, { newLibrary.Count })"))
			{
				try
				{
					var oldLibraryXml = new XmlDocument();

					if (!string.IsNullOrEmpty(oldLibrary))
						oldLibraryXml.Load(oldLibrary);

					var nsmgr = new XmlNamespaceManager(oldLibraryXml.NameTable);
					nsmgr.AddNamespace("a", "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
					nsmgr.AddNamespace("i", "http://www.w3.org/2001/XMLSchema-instance");
					nsmgr.AddNamespace("d3p1", "http://schemas.datacontract.org/2004/07/GameLibrary");
					nsmgr.AddNamespace("d4p1", "http://schemas.datacontract.org/2004/07/GameLibrary.GameDataProviders");

					foreach (var newGame in newLibrary)
					{
						if (newGame.Value.LastDataUpdate == null)
							newGame.Value.LastDataUpdate = new ConcurrentDictionary<string, DateTime>();

						else
						{
							// remove last update values of missing data
							var lastUpdateKeys = newGame.Value.LastDataUpdate.Keys.ToArray();
							foreach (var lastUpdateKey in lastUpdateKeys)
							{
								if (!newGame.Value.Data.Any(d => d.DataType == lastUpdateKey))
									newGame.Value.LastDataUpdate.TryRemove(lastUpdateKey, out DateTime value);
							}
						}

						// merge old library
						if (!string.IsNullOrEmpty(oldLibrary))
						{
							var oldGame = oldLibraryXml.DocumentElement
							.SelectSingleNode($"/a:ArrayOfKeyValueOfstringGameInfo7FtjRveh/a:KeyValueOfstringGameInfo7FtjRveh[a:Key='{ newGame.Key }']/a:Value", nsmgr);
							if (oldGame == null)
							{
								Log.WriteLine($"deserializedGame { newGame.Value.Title } not found in old library");
								continue;
							}

							var steamDbData = newGame.Value.CustomData<SteamDbInfoData>();
							if (steamDbData != null)
							{
								if (Int32.TryParse(oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:SteamDbInfoData']/d4p1:MetacriticScore", nsmgr)?.InnerText ?? string.Empty, out int rating))
									steamDbData.Rating = rating;

								steamDbData.Summary = steamDbData.Summary ?? oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:SteamDbInfoData']/d4p1:Description", nsmgr)?.InnerText;
								if (steamDbData.Summary == string.Empty)
									steamDbData.Summary = null;

								steamDbData.AppId = steamDbData.AppId ?? oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:SteamDbInfoData']/d4p1:AppId", nsmgr)?.InnerText;

								steamDbData.SourceGameTitle = steamDbData.SourceGameTitle ?? oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:SteamDbInfoData']/d4p1:SourceGameTitle", nsmgr)?.InnerText;
								if (steamDbData.SourceGameTitle == string.Empty)
									steamDbData.SourceGameTitle = null;

								if (bool.TryParse(oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:SteamDbInfoData']/d4p1:GamepadFriendly", nsmgr)?.InnerText ?? string.Empty, out bool gf))
									steamDbData.GamepadFriendly = gf;
							}

							var salenautsComData = newGame.Value.CustomData<SalenautsComData>();
							if (salenautsComData != null)
							{
								salenautsComData.SourceGameTitle = salenautsComData.SourceGameTitle ?? oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:SalenautsComData']/d4p1:SourceGameTitle", nsmgr)?.InnerText;
								if (salenautsComData.SourceGameTitle == string.Empty)
									salenautsComData.SourceGameTitle = null;
							}

							var userData = newGame.Value.CustomData<UserData>();
							if (userData != null)
							{
								userData.SourceGameTitle = userData.SourceGameTitle ?? oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:UserData']/d4p1:SourceGameTitle", nsmgr)?.InnerText;
								if (userData.SourceGameTitle == string.Empty)
									userData.SourceGameTitle = null;

								if (userData.SourceGameTitle == null)
									userData.SourceGameTitle = newGame.Value.Title;

								if (bool.TryParse(oldGame.SelectSingleNode($"d3p1:Data/d4p1:GameData[@i:type='d4p1:UserData']/d4p1:GamepadFriendly", nsmgr)?.InnerText ?? string.Empty, out bool gf))
									userData.GamepadFriendly = gf;
							}
						}
					}

					return newLibrary;
				}
				catch (Exception ex)
				{
					Log.WriteLine(ex.ToString());
					return null;
				}
			}
		}

		public static string GetGameId(GameInfo gameInfo)
		{
			return GetGameId(gameInfo?.Title);
		}

		public static string GetGameId(string gameTitle)
		{
			return Hashing.GetHash(gameTitle);
		}

		/// <summary>
		/// update games with missing images / informations
		/// </summary>
		/// <param name="gameUpdated"></param>
		public static void UpdateAll(Action<string> gameUpdated, Action updateAllFinished)
		{
			try
			{
				// update only installed games with no data
				var gamesToUpdate = Games
					.Where(g => !g.Value.Removed && g.Value.Data.Length == 0)
					.Select(g => g.Key);

				Threading.ThreadAndForget(() =>
				{
					using (var tb = new TimedBlock($"GameLibrary.Update({ gamesToUpdate.Count() })"))
					{
						try
						{
							//foreach (var gameId in gamesToUpdate)
							Parallel.ForEach(gamesToUpdate, gameId =>
							{
								if (UpdateGame(gameId))
									gameUpdated(gameId);
							}
							);

							UpdatePlayTimeFromSteamLibrary();
							Save();

							updateAllFinished();
						}
						catch (Exception ex)
						{
							Log.WriteLine(ex.ToString());
						}
					}
				});
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// save current library and create backup of old library file
		/// </summary>
		public static void Save()
		{
			using (var tb = new TimedBlock($"GameLibrary.Save({ GameLibraryFile })"))
			{
				try
				{
					// backup old library
					if (File.Exists(GameLibraryFile))
					{
						//var newBackup = Utils.Compress(File.ReadAllText(GameLibraryFile));
						//File.WriteAllBytes(Path.Combine(BackupDirectory, GamesLibraryFileName + $"{ BackupExtension }{ DateTime.Now.ToString(TimestampFormat) }"), newBackup);

						var newBackup = File.ReadAllText(GameLibraryFile);
						File.WriteAllText(Path.Combine(BackupDirectory, GamesLibraryFileName + $"{ BackupExtension }{ DateTime.Now.ToString(TimestampFormat) }"), newBackup);

						// keep only last week backup files
						var oldBackupFiles = Directory.GetFiles(BackupDirectory, $"*{ BackupExtension }*");
						foreach (var backupToDelete in BackupsOlderThan(7))
						{
							Log.WriteLine($"DeleteBackup({ backupToDelete })");
							File.Delete(backupToDelete);
						}
					}

					// save current library
					File.WriteAllText(GameLibraryFile, Utils.GetFormattedXml(Utils.Serialize(Games)));

					// TODO mark naLauncher-lib.xml as readonly

					var stats = new string[]
					{
						$"{ nameof(IgdbComData) }={ Games.Count(g => g.Value.Data.Any(d => d is IgdbComData)) }",
						$"{ nameof(SteamDbInfoData) }={ Games.Count(g => g.Value.Data.Any(d => d is SteamDbInfoData)) }",
						$"{ nameof(SteamCryoTankNetData) }={ Games.Count(g => g.Value.Data.Any(d => d is SteamCryoTankNetData)) }",
						$"{ nameof(SalenautsComData) }={ Games.Count(g => g.Value.Data.Any(d => d is SalenautsComData)) }",
						$"{ nameof(UserData) }={ Games.Count(g => g.Value.Data.Any(d => d is UserData)) }",
					};
					Log.WriteLine($"GameDataStats(All={ Games.Count }, { string.Join(", ", stats) })");
				}
				catch (Exception ex)
				{
					Log.WriteLine(ex.ToString());
				}
			}
		}

		#region backups

		/// <summary>
		/// get ordered list of existing backup library files (newest first)
		/// </summary>
		/// <returns></returns>
		public static IOrderedEnumerable<string> BackupsOlderThan(int olderThanDays = 0)
		{
			return Directory.GetFiles(BackupDirectory, $"*{ BackupExtension }*")
				.Where(f => DateTime.Now.Subtract(GetBackupDate(f)).TotalDays > olderThanDays)
				.OrderByDescending(f => f);
		}

		/// <summary>
		/// get library backup date from filename
		/// </summary>
		/// <param name="backupFile"></param>
		/// <returns></returns>
		public static DateTime GetBackupDate(string backupFile)
		{
			return DateTime.ParseExact(backupFile.Substring(backupFile.LastIndexOf($"{ BackupExtension }") + $"{ BackupExtension }".Length), TimestampFormat, null);
		}

		#endregion

		/// <summary>
		/// remove game (delete shortcut from games directory and create grayscaled image)
		/// </summary>
		/// <param name="gameId"></param>
		public static void RemoveGame(string gameId)
		{
			try
			{
				Log.WriteLine($"RemoveGame({ gameId })");

				if (File.Exists(Games[gameId].Shortcut))
					File.Delete(Games[gameId].Shortcut);

				Games[gameId].Shortcut = null;

				Save();

				UpdateGameImageCache(gameId, Games[gameId].Image);
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// completely deletes game from library
		/// </summary>
		/// <param name="gameId"></param>
		public static bool DeleteGame(string gameId)
		{
			var game = Games[gameId];

			Log.WriteLine($"DeleteGame({ gameId }, { game.Title })");

			if (!Games.TryRemove(gameId, out GameInfo oldGame))
				return false;

			Save();
			return true;
		}

		/// <summary>
		/// rename game and forece-update its data
		/// </summary>
		/// <param name="gameId"></param>
		/// <param name="newGameTitle"></param>
		/// <param name="gameChanged"></param>
		public static void RenameGame(string gameId, string newGameTitle, Action<string> gameChanged)
		{
			var oldGameTitle = Games[gameId].Title;

			Log.WriteLine($"RenameGame({ gameId }, { oldGameTitle } -> { newGameTitle })");

			// check if new title already exists
			if (string.IsNullOrEmpty(newGameTitle) || (newGameTitle != oldGameTitle && Games.Any(g => g.Value.Title.ToLower() == newGameTitle.ToLower())))
			{
				gameChanged(null);
				return;
			}

			if (Games.TryRemove(gameId, out GameInfo oldGame))
			{
				if (oldGame.Shortcut != null && !File.Exists(oldGame.Shortcut))
					oldGame.Shortcut = null;

				var newShortcut = oldGame.Shortcut;

				if (oldGame.Shortcut != null)
				{
					newShortcut = Path.Combine(GamesDirectory, $"{ newGameTitle }{ new FileInfo(oldGame.Shortcut).Extension }");
					File.Move(oldGame.Shortcut, newShortcut);
				}

				var newGame = new GameInfo
				{
					Added = oldGame.Added,
					Completed = oldGame.Completed,
					TimeStamps = oldGame.TimeStamps,
					Title = newGameTitle,
					Shortcut = newShortcut,
					Data = new GameData[] { },
					LastDataUpdate = new ConcurrentDictionary<string, DateTime>(),
				};

				var newGameId = GetGameId(newGame);
				Games.AddOrUpdate(newGameId, newGame, (id, oldValue) => newGame);

				Threading.ThreadAndForget(() =>
				{
					UpdateGame(newGameId, true);
					Save();

					gameChanged(newGameId);
				});
			}
		}

		/// <summary>
		/// run game
		/// </summary>
		/// <param name="gameId"></param>
		/// <param name="asAdministrator"></param>
		/// <returns>true if game was removed or something changed, otherwise false</returns>
		public static bool RunGame(string gameId, bool asAdministrator = false)
		{
			try
			{
				if (gameId == null)
					return false;

				var game = Games[gameId];
				if (game.Shortcut == null)
					return false;

				Log.WriteLine($"RunGame({ game.Title }, { (asAdministrator ? nameof(asAdministrator) : "normalUser") })");

				var processInfo = new ProcessStartInfo(game.Shortcut);

				if (asAdministrator)
				{
					processInfo.UseShellExecute = true;
					processInfo.Verb = "runas";
				}

				try
				{
					//if (!Debugger.IsAttached)
					Process.Start(processInfo);

					return true;
				}
				catch (Win32Exception ex)
				{
					if (ex.NativeErrorCode == 1223)
					{
						// The operation was canceled by the user.
					}
					else
						Log.WriteLine(ex.ToString());
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}

			return false;
		}

		public static void AddGameTimestamp(string gameId, DateTime start, int durationInMinutes = 0)
		{
			try
			{
				var game = Games[gameId];
				game.TimeStamps += durationInMinutes > 0 ? $"{ start.ToString(TimestampFormat) }+{ durationInMinutes };" : $"{ start.ToString(TimestampFormat) };";

				Save();
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		/// <summary>
		/// update game
		/// </summary>
		/// <param name="gameId"></param>
		/// <param name="forceUpdate"></param>
		public static bool UpdateGame(string gameId, bool forceUpdate = false)
		{
			// for example Fallout 1 is on gog as "Fallout", but on steam its "Fallout: A Post Nuclear Role Playing Game". 
			// which leads to false positive, "Fallout 3" from steam.

			// check other data providers (or update present, if image is missing)
			var changed = false;

			foreach (var gameDataProvider in GameDataProviders)
				changed |= UpdateGameData(gameId, gameDataProvider, forceUpdate);

			var gameImages = Games[gameId].Data
				.Where(d => d is IGameImage)
				.Select(d => (d as IGameImage).Image);

			// download game image again if needed
			foreach (var gameImage in gameImages)
				changed |= DownloadGameImage(gameImage);

			// update image in cache
			if (changed)
				UpdateGameImageCache(gameId, Games[gameId].Image);

			return changed;
		}

		public static void SetGameAsCompleted(string gameId)
		{
			Games[gameId].Completed = DateTime.Now;
			Save();
		}

		static bool UpdateGameData(string gameId, IGameDataProvider gameDataProvider, bool forceUpdate = false)
		{
			if (!forceUpdate
				&& Games[gameId].LastDataUpdate.TryGetValue(gameDataProvider.GetGameDataType(), out DateTime lastUpdate)
				&& lastUpdate.AddDays(Settings.Default.MaxGameDataAge) > DateTime.Now)
				return false;

			var oldData = Games[gameId].Data.SingleOrDefault(d => d.DataType == gameDataProvider.GetGameDataType());

			// add new data
			if (oldData == null)
			{
				var newData = gameDataProvider.GetGameData(Games[gameId].Title);
				if (newData != null)
				{
					Games[gameId].Data = Games[gameId].Data.Concat(new GameData[] { newData }).ToArray();

					// set last update to current time
					Games[gameId].LastDataUpdate.AddOrUpdate(gameDataProvider.GetGameDataType(), DateTime.UtcNow, (id, oldValue) => DateTime.UtcNow);
					return true;
				}
			}
			else
			{
				// TODO merge new data with old
				//oldData.Merge(newData);

				Games[gameId].LastDataUpdate.AddOrUpdate(gameDataProvider.GetGameDataType(), DateTime.UtcNow, (id, oldValue) => DateTime.UtcNow);
			}

			return false;
		}

		/// <summary>
		/// update play time from steam library
		/// </summary>
		/// <param name="steamApiKey"></param>
		/// <param name="steamId"></param>
		static void UpdatePlayTimeFromSteamLibrary()
		{
			using (var tb = new TimedBlock("GameLibrary.UpdatePlayTimeFromSteamLibrary()"))
			{
				try
				{
					var playTimes = SteamApi.ListPlayTimes(Settings.Default.SteamApiKey, Settings.Default.UserSteamId);
					foreach (var playTime in playTimes)
					{
						var steamAppId = playTime.Key;
						var game = Games.Values
							.SingleOrDefault(g => g.Data
								.SingleOrDefault(d => d is SteamDbInfoData && (d as SteamDbInfoData).AppId == steamAppId) != null);

						if (game == null)
							continue;

						var steamData = game.CustomData<SteamDbInfoData>();
						if (steamData != null)
							steamData.PlayTimeForEver = playTime.Value;
					}
				}
				catch (Exception ex)
				{
					Log.WriteLine(ex.ToString());
				}
			}
		}

		/// <summary>
		/// download game image
		/// </summary>
		/// <param name="gameImage"></param>
		static bool DownloadGameImage(GameImage gameImage)
		{
			if (gameImage != null && !GameDataProvider.ImageExists(gameImage.LocalFilename) && !string.IsNullOrEmpty(gameImage.SourceUrl))
			{
				var image = WebUtils.DownloadImage(gameImage.SourceUrl, out ImageFormat imageFormat);
				if (image != null)
				{
					GameDataProvider.SaveImage(gameImage.LocalFilename, image, imageFormat);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// update game image in cache
		/// </summary>
		/// <param name="gameId"></param>
		/// <param name="gameImage"></param>
		/// <returns></returns>
		static bool UpdateGameImageCache(string gameId, GameImage gameImage)
		{
			try
			{
				if (gameImage != null)
				{
					if (!File.Exists(gameImage.LocalFilename) && gameImage.SourceUrl != null)
						DownloadGameImage(gameImage);

					if (File.Exists(gameImage.LocalFilename))
					{
						Bitmap imageFromFile;
						// existing game image file
						using (Stream stream = File.OpenRead(gameImage.LocalFilename))
							imageFromFile = (Bitmap)System.Drawing.Bitmap.FromStream(stream);

						// removed game
						if (Games[gameId].Removed)
							imageFromFile = (Bitmap)Ujeby.Common.Tools.Graphics.MakeGrayscale3(imageFromFile);

						// update game image in cache
						GameImageCache.AddOrUpdate(gameId, imageFromFile, (key, oldValue) => imageFromFile);

						return true;
					}
				}

				// image not found
				if (ImageCache.TryGetValue(DefaultImage.NotFound.ToString(), out Bitmap notFoundImage))
					GameImageCache.AddOrUpdate(gameId, notFoundImage, (key, oldValue) => notFoundImage);

				return true;
			}
			catch (Exception ex)
			{
				Log.WriteLine($"UpdateGameImageCache({ Games[gameId].Title }, { gameImage })");
				Log.WriteLine(ex.ToString());

				return false;
			}
		}

		public static bool SetUserImage(string gameId, Bitmap image, string sourceFile)
		{
			var game = Games[gameId];

			if (GameImageCache.TryRemove(gameId, out Bitmap oldImage))
			{
				var imageFormat = Ujeby.Common.Tools.Graphics.GetImageFormat(new FileInfo(sourceFile).Extension);
				var imageLocation = GameDataProvider.SaveGameImage(game.Title, image, imageFormat);

				var newGameImage = new GameImage
				{
					LocalFilename = imageLocation,
					SourceUrl = sourceFile,
				};

				var oldUserData = game.CustomData<UserData>();
				if (oldUserData != null)
					oldUserData.Image = newGameImage;
				else
					game.Data = game.Data.Concat(new GameData[] { new UserData(game.Title) { Image = newGameImage } }).ToArray();

				return UpdateGameImageCache(gameId, newGameImage);
			}

			return false;
		}
	}
}
