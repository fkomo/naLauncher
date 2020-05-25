using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Xml;
using Ujeby.Common.Tools;

namespace GameLibrary.GameDataProviders
{
	[DataContract]
	public class SalenautsComData : GameData, IGameImage
	{
		public const string ImageCacheDirectory = "SalenautsCom";
		public override int Priority { get { return 50; } }
		public override string DataType { get { return nameof(SalenautsComData); } }

		public static string SearchUrl(string gameTitle)
		{
			return $"https://salenauts.com/pl/games/?Game%5Btitle%5D={ WebUtility.UrlEncode(gameTitle) }";
		}

		public override void Merge(GameData newData)
		{
			// TODO SalenautsComData.Merge 
		}

		public SalenautsComData(string gameTitle = null, string salenautsGameUrl = null) : base(gameTitle)
		{
			Url = salenautsGameUrl;
		}

		[DataMember]
		public string Url { get; private set; }

		[DataMember]
		public GameImage Image { get; set; }
	}

	internal class SalenautsComDataProvider : GameDataProvider, IGameDataProvider
	{
		private static string CurrentClassName { get { return typeof(SalenautsComDataProvider).Name; } }

		public GameData GetGameData(string gameTitle, bool ignoreLocalCache = false)
		{
			// TODO SalenautsComDataProvider is broken
			return null;

			try
			{
				var response = WebUtils.WebRequest(SalenautsComData.SearchUrl(gameTitle));
				if (response == null)
					return null;

				var matches = new List<KeyValuePair<string, int>>();
				var normalizedGameTitle = Strings.NormalizeString(gameTitle);

				var lastGameStart = 0;
				while (true)
				{
					try
					{
						lastGameStart = response.IndexOf("<div class=\"title\">", lastGameStart);
						if (lastGameStart < 0)
							break;

						var gameRow = response.Substring(lastGameStart, response.IndexOf("</div>", lastGameStart) - lastGameStart + "</div>".Length);
						lastGameStart++;

						var localGameTitle = gameRow.Substring(gameRow.IndexOf("</span>") + "</span>".Length)
							.Replace("</a></div>", string.Empty);

						var localGameUrl = gameRow.Substring(0, gameRow.IndexOf("\"><span"))
							.Substring(gameRow.IndexOf("<a href=\"") + "<a href=\"".Length);
						if (string.IsNullOrEmpty(localGameUrl))
							continue;

						localGameUrl = $"https://salenauts.com{ localGameUrl }";

						var normalizedLocalGameTitle = Strings.NormalizeString(localGameTitle);
						if (normalizedLocalGameTitle.Contains(normalizedGameTitle))
							matches.Add(new KeyValuePair<string, int>(localGameUrl, Strings.DamerauLevenshteinEditDistance(normalizedGameTitle, normalizedLocalGameTitle)));
					}
					catch (Exception ex)
					{
						Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ gameTitle }): { ex.ToString() }");
					}
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

				var gameResponse = WebUtils.WebRequest(bestMatch);
				if (gameResponse == null)
					return null;

				var gameImageStart = gameResponse.IndexOf("<div class=\"game-icon");
				if (gameImageStart < 0)
					return null;

				var gameImage = gameResponse.Substring(gameImageStart, gameResponse.IndexOf("</div>", gameImageStart) - gameImageStart + "</div>".Length);

				var gameImageXmlDocument = new XmlDocument();
				gameImageXmlDocument.LoadXml(gameImage);

				var gameImageUrl = gameImageXmlDocument.FirstChild?.FirstChild?.Attributes["src"]?.Value;

				if (!gameImageUrl.StartsWith("http"))
					gameImageUrl = $"https://salenauts.com{ gameImageUrl }";

				if (string.IsNullOrEmpty(gameImageUrl))
					return null;

				var gameData = new SalenautsComData(gameTitle, bestMatch)
				{
					Image = new GameImage
					{
						SourceUrl = gameImageUrl
					}
				};

				var image = WebUtils.DownloadImage(gameData.Image.SourceUrl, out ImageFormat imageFormat);

				gameData.Image.LocalFilename = SaveGameImage(gameTitle, image, imageFormat, SalenautsComData.ImageCacheDirectory);

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
			return nameof(SalenautsComData);
		}
	}
}
