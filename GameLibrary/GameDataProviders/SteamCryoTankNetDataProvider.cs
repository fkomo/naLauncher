using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using Ujeby.Common.Tools;

namespace GameLibrary.GameDataProviders
{
	[DataContract]
	public class SteamCryoTankNetData : GameData, IGameImage
	{
		public const string ImageCacheDirectory = "SteamCryoTankNet";
		public override int Priority { get { return 50; } }
		public override string DataType { get { return nameof(SteamCryoTankNetData); } }

		public SteamCryoTankNetData(string gameTitle = null) : base(gameTitle)
		{

		}

		public string Url { get { return SourceGameTitle != null ? $"http://steam.cryotank.net/?s={ WebUtility.UrlEncode(SourceGameTitle) }" : null; } }

		[DataMember]
		public GameImage Image { get; set; }

		public override void Merge(GameData newData)
		{
			// TODO SteamCryoTankNetData.Merge 
		}
	}

	internal class SteamCryoTankNetDataProvider : GameDataProvider, IGameDataProvider
	{
		private static string CurrentClassName { get { return typeof(SteamCryoTankNetDataProvider).Name; } }

		public GameData GetGameData(string gameTitle, bool ignoreLocalCache = false)
		{
			try
			{
				var gameData = new SteamCryoTankNetData(gameTitle);

				var matches = new List<KeyValuePair<string, int>>();

				var normalizedGameTitle = Strings.NormalizeString(gameTitle);

				var images = WebUtils.ScrapeImages(gameData.Url);
				foreach (var imageUrl in images)
				{
					var imageName = imageUrl.Substring(imageUrl.LastIndexOf("/"));
					imageName = imageName.Substring(0, imageName.Length - 4); // remove image extension

					var normalizedImageName = Strings.NormalizeString(imageName);
					if (normalizedImageName.Contains(normalizedGameTitle))
						matches.Add(new KeyValuePair<string, int>(imageUrl, Strings.DamerauLevenshteinEditDistance(normalizedGameTitle, normalizedImageName)));
				}

				string bestMatch = null;
				if (matches.Count > 0)
				{
					var bestMatchKeyPair = matches
						.Where(x => x.Value < normalizedGameTitle.Length)
						.OrderBy(x => x.Value)
						.FirstOrDefault();

					bestMatch = bestMatchKeyPair.Key;

					Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ gameTitle }): [{ matches.Count } possible matches, best={ bestMatchKeyPair.Value }:{ bestMatchKeyPair.Key }]");
				}

				if (bestMatch == null)
					return null;

				//if (result == null)
				//{
				//	// try replacing roman numbers with arabic
				//	foreach (var romanNumber in Constants.RomanNumbers)
				//	{
				//		// space from both side of roman number
				//		var newGameTitle = gameTitle.Replace($" { romanNumber.Key } ", romanNumber.Value);
				//		if (newGameTitle != gameTitle)
				//		{
				//			result = GetGameMetadataFromUrl(newGameTitle, GameMetadataSource.GetSteamCryotankNetUrl(newGameTitle));
				//			break;
				//		}

				//		// space on left side and end of string on right side
				//		if (result == null && gameTitle.EndsWith($" { romanNumber.Key}"))
				//		{
				//			newGameTitle = $"{ gameTitle.Substring(0, gameTitle.LastIndexOf(' ')) } { romanNumber.Value }";
				//			if (newGameTitle != gameTitle)
				//			{
				//				result = GetGameMetadataFromUrl(newGameTitle, GameMetadataSource.GetSteamCryotankNetUrl(newGameTitle));
				//				break;
				//			}
				//		}
				//	}
				//}

				gameData.Image = new GameImage
				{
					SourceUrl = bestMatch
				};

				var image = WebUtils.DownloadImage(gameData.Image.SourceUrl, out ImageFormat imageFormat);

				gameData.Image.LocalFilename = SaveGameImage(gameTitle, image, imageFormat, SteamCryoTankNetData.ImageCacheDirectory);

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
			return nameof(SteamCryoTankNetData);
		}
	}
}
