using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Ujeby.Common.Tools;

namespace GameLibrary.GameDataProviders
{
	[DataContract]
	public class TimeToBeat
	{
		/// <summary>
		/// completely [seconds]
		/// </summary>
		[DataMember]
		public int? Complete { get; set; }

		/// <summary>
		/// normally [seconds]
		/// </summary>
		[DataMember]
		public int? Normal { get; set; }

		/// <summary>
		/// hastly [seconds]
		/// </summary>
		[DataMember]
		public int? Fast { get; set; }
	}

	[DataContract]
	public class IgdbComData : GameData, IGameSummary, IRatedGame, IGameImage
	{
		public const string ImageCacheDirectory = "IgdbCom";

		public IgdbComData(string gameTitle) : base(gameTitle)
		{

		}

		public override int Priority { get { return 40; } }
		public override string DataType { get { return nameof(IgdbComData); } }

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Developer { get; set; }

		/// <summary>
		/// igdb user rating
		/// </summary>
		[DataMember]
		public int? Rating { get; set; }

		[DataMember]
		public string[] Genres { get; set; }

		[DataMember]
		public TimeToBeat TimeToBeat { get; set; }

		/// <summary>
		/// summary
		/// </summary>
		[DataMember]
		public string Summary { get; set; }

		[DataMember]
		public GameImage Image { get; set; }

		public override void Merge(GameData newData)
		{
			var data2 = newData as IgdbComData;
			if (data2 == null)
				return;

			this.Rating = data2.Rating;

			if (data2.TimeToBeat != null)
			{
				this.TimeToBeat = new TimeToBeat
				{
					Complete = data2.TimeToBeat.Complete,
					Fast = data2.TimeToBeat.Fast,
					Normal = data2.TimeToBeat.Normal,
				};
			}

			if (this.Developer == null)
				this.Developer = data2.Developer;

			if (data2.Image != null && data2.Image.SourceUrl != this.Image?.SourceUrl)
			{
				GameDataProvider.BackupGameImage(this.Image);

				this.Image = new GameImage
				{
					LocalFilename = data2.Image.LocalFilename,
					SourceUrl = data2.Image.SourceUrl,
				};
			}

			if (data2.Genres?.Length > 0)
			{
				this.Genres = new string[data2.Genres.Length];
				data2.Genres.CopyTo(this.Genres, 0);
			}
		}
	}

	/// <summary>
	/// igdb v4
	/// </summary>
	internal class IgdbComDataProvider : GameDataProvider, IGameDataProvider
	{
		public static string ApiBaseUrl { get { return Properties.Settings.Default.IGDBApiUrl; } }

		static string CurrentClassName { get { return typeof(IgdbComDataProvider).Name; } }

		static Dictionary<string, string> GetIgdbHeaders
		{
			get
			{
				return new Dictionary<string, string>
				{
					{ "Client-ID", Properties.Settings.Default.TwitchDevClientId },
					{ "Authorization", $"{ TwitchDev.TokenType } { TwitchDev.AccessToken }" },
				};
			}
		}

		public GameData GetGameData(string gameTitle, bool ignoreLocalCache = false)
		{
			try
			{
				var normalizedGameTitle = Strings.NormalizeString(gameTitle);
				var matches = new List<KeyValuePair<string, MatchPossibility>>();

				// search by title
				dynamic response = JsonConvert.DeserializeObject(WebUtils.Post($"{ ApiBaseUrl }games",
					$"fields name; search \"{ gameTitle.ToLower() }\"; where version_parent = null;",
					GetIgdbHeaders));

				//fields name; search "the outer worlds"; where version_parent = null;
				//[
				//    {
				//        "id": 113114,
				//        "name": "The Outer Worlds"
				//    },
				//    {
				//        "id": 4348,
				//        "name": "Another World"
				//    }
				//]

				if (response == null || response.Count == 0)
					return null;

				if (response.Count < 10)
				{
					for (var i = 0; i < response.Count; i++)
					{
						var id = response[i].id.ToString();
						var name = response[i].name.ToString();

						var normalizedName = Strings.NormalizeString(name);
						if (normalizedName.Contains(normalizedGameTitle))
							matches.Add(new KeyValuePair<string, MatchPossibility>(id,
								new MatchPossibility
								{
									Distance = Strings.DamerauLevenshteinEditDistance(normalizedGameTitle, normalizedName),
									Value = name,
								}));
					}

					// find best match (closest title)
					KeyValuePair<string, MatchPossibility>? bestMatch = null;
					if (matches.Count > 0)
					{
						bestMatch = matches
							.Where(x => x.Value.Distance < normalizedGameTitle.Length)
							.OrderBy(x => x.Value.Distance)
							.FirstOrDefault();
					}

					if (bestMatch.HasValue && bestMatch.Value.Key != null)
						Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ gameTitle }): [{ matches.Count } possible matches, best={ bestMatch.Value.Value.Distance }, { bestMatch.Value.Value.Value }]");
					else if (matches.Count > 0)
					{
						Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ gameTitle }): [{ matches.Count } possible matches, no suitable found]");
						return null;
					}
					else
						return null;

					// get best matching game by id
					response = JsonConvert.DeserializeObject(WebUtils.Post($"{ ApiBaseUrl }games",
							$"fields name, artworks, cover, genres, total_rating, summary, involved_companies; where id = { bestMatch.Value.Key };",
							GetIgdbHeaders));

					//fields name, artworks, cover, genres, total_rating, summary, time_to_beat, involved_companies; where id = 113114;
					//[
					//    {
					//        "id": 113114,
					//        "artworks": [
					//            7445,
					//            8052
					//        ],
					//        "cover": 83213,
					//        "genres": [
					//            5,
					//            12
					//        ],
					//		  "involved_companies": [
					//			  72030,
					//			  72031,
					//	  		  85735
					//		  ],
					//        "name": "The Outer Worlds",
					//        "summary": "In The Outer Worlds, you awake from hibernation on a colonist ship that was lost in transit to Halcyon, the furthest colony from Earth located at the edge of the galaxy, only to find yourself in the midst of a deep conspiracy threatening to destroy it. As you explore the furthest reaches of space and encounter various factions, all vying for power, the character you decide to become will determine how this player-driven story unfolds. In the corporate equation for the colony, you are the unplanned variable.",
					//        "time_to_beat": 113114,
					//        "total_rating": 87.91014054637175
					//    }
					//]

					if (response?.Count == 1)
					{
						var rating = response[0].total_rating?.ToString();

						var genres = GetGenres(response[0].genres);
						var developer = GetDeveloper(response[0].involved_companies);
						var timesToBeat = GetTimesToBeat(response[0].time_to_beat);
						var image = GetImage(gameTitle, response[0].cover);

						var gameData = new IgdbComData(gameTitle)
						{
							Id = bestMatch.Value.Key,
							Summary = response[0].summary?.ToString(),
							Rating = string.IsNullOrEmpty(rating) ? null as int? : (int)double.Parse(rating),
							Developer = developer,
							Genres = genres,
							TimeToBeat = timesToBeat,
							Image = image,
						};

						return gameData;
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ gameTitle }): { ex.ToString() }");
			}

			return null;
		}

		TimeToBeat GetTimesToBeat(dynamic time_to_beat)
		{
			if (time_to_beat == null)
				return null;

			dynamic tResponse = JsonConvert.DeserializeObject(WebUtils.Post($"{ ApiBaseUrl }time_to_beats",
				$"fields *; where id = { time_to_beat };",
				GetIgdbHeaders));

			//https://api-v3.igdb.com/time_to_beats 
			//fields*; where id = 113114;
			//[
			//    {
			//        "id": 113114,
			//        "completely": 126000,
			//        "hastly": 0,
			//        "normally": 92100
			//    }
			//]

			if (tResponse.Count < 1)
				return null;

			var result = new TimeToBeat();

			if (tResponse[0].completely != "0")
				result.Complete = int.Parse(tResponse[0].completely.ToString());

			if (tResponse[0].hastly != "0")
				result.Fast = int.Parse(tResponse[0].hastly.ToString());

			if (tResponse[0].normally != "0")
				result.Normal = int.Parse(tResponse[0].normally.ToString());

			if (!result.Complete.HasValue && !result.Normal.HasValue && !result.Fast.HasValue)
				return null;

			return result;
		}

		static ConcurrentDictionary<string, string> Genres { get; set; } = new ConcurrentDictionary<string, string>();

		string[] GetGenres(dynamic genres)
		{
			if (genres == null)
				return null;

			var result = new List<string>();
			for (var g = 0; g < genres.Count; g++)
			{
				var genre = null as string;
				if (Genres.TryGetValue(genres[g].ToString(), out genre))
					result.Add(genre);

				else
				{
					dynamic gResponse = JsonConvert.DeserializeObject(WebUtils.Post($"{ ApiBaseUrl }genres",
						$"fields name; where id = { genres[g] };",
						GetIgdbHeaders));

					//https://api-v3.igdb.com/genres
					//fields name; where id = 5;
					//[
					//    {
					//        "id": 5,
					//        "name": "Shooter",
					//    }
					//]

					genre = gResponse[0].name?.ToString();

					result.Add(genre);
					Genres.AddOrUpdate(genres[g].ToString() as string, genre, (id, oldValue) => genre);
				}
			}

			return result.ToArray();
		}

		string GetDeveloper(dynamic involvedCompanies)
		{
			if (involvedCompanies != null)
			{
				for (var ic = 0; ic < involvedCompanies.Count; ic++)
				{
					dynamic icResponse = JsonConvert.DeserializeObject(WebUtils.Post($"{ ApiBaseUrl }involved_companies",
						$"fields company, developer; where id = { involvedCompanies[ic] };",
						GetIgdbHeaders));

					//https://api-v3.igdb.com/involved_companies
					//fields company, dewveloper; where id = 72030;
					//[
					//    {
					//        "id": 72030,
					//        "company": 47,
					//        "developer": true,
					//	}
					//]

					if (icResponse[0].developer != "true")
						continue;

					dynamic cResponse = JsonConvert.DeserializeObject(WebUtils.Post($"{ ApiBaseUrl }companies",
						$"fields name; where id = { icResponse[0].company };",
						GetIgdbHeaders));

					//https://api-v3.igdb.com/companies
					//fields name; where id = 47;
					//[
					//    {
					//        "id": 47,
					//        "name": "Obsidian Entertainment"
					//	}
					//]

					return cResponse[0].name;
				}
			}

			return null;
		}

		GameImage GetImage(string gameTitle, dynamic cover)
		{
			if (cover != null)
			{
				dynamic response = JsonConvert.DeserializeObject(WebUtils.Post($"{ ApiBaseUrl }covers",
					$"fields id, url, width, height; where id = { cover.ToString() };",
					GetIgdbHeaders));

				//https://api-v3.igdb.com/covers
				//fields*; where id = 83213;
				//[
				//    {
				//        "id": 83213,
				//        "alpha_channel": false,
				//        "animated": false,
				//        "game": 113114,
				//        "height": 1080,
				//        "image_id": "co1s7h",
				//        "url": "//images.igdb.com/igdb/image/upload/t_thumb/co1s7h.jpg",
				//        "width": 810
				//    }
				//]

				if (response != null)
				{
					var imageUrl = "https:" + response[0].url.ToString().Replace("t_thumb", "t_original").ToString();

					// download image
					var image = WebUtils.DownloadImage(imageUrl, out System.Drawing.Imaging.ImageFormat imageFormat);

					if (image != null)
					{
						var gameImage = new GameImage
						{
							SourceUrl = imageUrl,
						};

						gameImage.LocalFilename = SaveGameImage(gameTitle, image, imageFormat, IgdbComData.ImageCacheDirectory);

						return gameImage;
					}
				}
			}

			//https://api-v3.igdb.com/artworks
			//fields*; where id = 7445;
			//[
			//    {
			//        "id": 7445,
			//        "alpha_channel": false,
			//        "animated": false,
			//        "game": 113114,
			//        "height": 1080,
			//        "image_id": "ar5qt",
			//        "url": "//images.igdb.com/igdb/image/upload/t_thumb/ar5qt.jpg",
			//        "width": 1920
			//    }
			//]

			return null;
		}

		public string GetGameDataType()
		{
			return nameof(IgdbComData);
		}
	}
}