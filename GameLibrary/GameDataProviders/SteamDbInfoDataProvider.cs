using GameLibrary.Properties;
using Newtonsoft.Json;
using SteamDb;
using System;
using System.IO;
using System.Runtime.Serialization;
using Ujeby.Common.Tools;

namespace GameLibrary.GameDataProviders
{
	[DataContract]
	public class SteamDbInfoData : GameData, IRatedGame, IGameSummary, IControllerSupported, IGameImage
	{
		public const string ImageCacheDirectory = "SteamDbInfo";
		public override int Priority { get { return 100; } }
		public override string DataType { get { return nameof(SteamDbInfoData); } }

		public SteamDbInfoData(string gameTitle = null) : base(gameTitle)
		{
			
		}

		[DataMember]
		public string AppId { get; set; }

		/// <summary>
		/// steamApi short_description
		/// </summary>
		[DataMember]
		public string Summary { get; set; }

		/// <summary>
		/// metacritic score
		/// </summary>
		[DataMember]
		public int? Rating { get; set; }

		/// <summary>
		/// rating colors:
		/// 
		/// 100-75 = #66cc33 (green)
		/// 74-50 = #ffcc33 (yellow)
		/// 49-0 = #ff0000 (red)
		/// </summary>
		/// <param name="score"></param>
		/// <returns></returns>
		public static System.Windows.Media.Color GetMetacriticColor(int score)
		{
			if (score >= 75)
				return System.Windows.Media.Color.FromArgb(0xff, 0x66, 0xcc, 0x33);

			else if (score < 50)
				return System.Windows.Media.Color.FromArgb(0xff, 0xff, 0x00, 0x00);

			else
				return System.Windows.Media.Color.FromArgb(0xff, 0xff, 0xcc, 0x33);
		}

		public override void Merge(GameData newData)
		{
			// TODO SteamDbInfoData.Merge
		}

		/// <summary>
		/// total play time in minutes
		/// </summary>
		[DataMember]
		public int PlayTimeForEver { get; set; }

		public string Url { get { return $"https://steamdb.info/app/{ AppId }/"; } }

		[DataMember]
		public bool? GamepadFriendly { get; set; }

		[DataMember]
		public GameImage Image { get; set; }
	}

	internal class SteamDbInfoDataProvider : GameDataProvider, IGameDataProvider
	{
		private static string CurrentClassName { get { return typeof(SteamDbInfoDataProvider).Name; } }

		/// <summary>
		/// supported steam application types
		/// </summary>
		private static string[] SupportedSteamDbAppTypes { get; set; } = new string[]
		{
			"game",
			"dlc"
		};

		private static SteamDbCache CurrentSteamDbCache { get; set; } = null;

		static SteamDbInfoDataProvider()
		{
			CurrentSteamDbCache = new SteamDbCache(Path.Combine(GameLibrary.UserDataFolder, SteamDbCache.DefaultFileName), SupportedSteamDbAppTypes, Settings.Default.ScrapeSteamDb);
		}

		public GameData GetGameData(string gameTitle, bool ignoreLocalCache = false)
		{
			try
			{
				var steamApp = CurrentSteamDbCache.GetByTitle(gameTitle, ignoreLocalCache);
				if (steamApp == null)
					return null;

				// NOTE steampowered-api rate limiting! (200 requests / 5 min)
				var response = WebUtils.SilentWebRequest(steamApp.SteamPoweredApiUrl);
				if (response == null)
					return null;

				var gameData = new SteamDbInfoData(steamApp.Title)
				{
                    AppId = steamApp.Id,
				};

				dynamic deserializedResponse = JsonConvert.DeserializeObject(response);
				if (deserializedResponse == null || deserializedResponse[gameData.AppId].success == false)
					return null;

				gameData.Image = new GameImage
				{
					SourceUrl = deserializedResponse[gameData.AppId].data.header_image.ToString()
				};

				// download image
				System.Drawing.Imaging.ImageFormat imageFormat;
				var image = WebUtils.DownloadImage(gameData.Image.SourceUrl, out imageFormat);

				gameData.Image.LocalFilename = SaveGameImage(gameTitle, image, imageFormat, SteamDbInfoData.ImageCacheDirectory);

				// description
				gameData.Summary = Strings.StripHTML(System.Net.WebUtility.HtmlDecode(deserializedResponse[gameData.AppId].data.short_description.ToString()));
				gameData.Summary = gameData.Summary?.Replace("\r\n", " ")?.Replace("\n", " ")?.Replace("  ", " ");

				// metacritic score
				if (deserializedResponse[gameData.AppId].data.metacritic != null)
					gameData.Rating = Int32.Parse(deserializedResponse[gameData.AppId].data.metacritic.score.ToString());

				// controller support
				if (deserializedResponse[gameData.AppId].data.controller_support != null)
					gameData.GamepadFriendly = deserializedResponse[gameData.AppId].data.controller_support.ToString() == "full";

				return gameData;
			}
			catch (Exception ex)
			{
				Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ gameTitle }): { ex.ToString() }");
			}

			return null;
		}

		public string GetGameDataType()
		{
			return nameof(SteamDbInfoData);
		}

		public static void StopScrapping()
		{
			CurrentSteamDbCache?.StopScrapping();
		}
	}
}
