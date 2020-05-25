using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Ujeby.Common.Tools;

namespace SteamDb
{
	public class SteamApi
	{
		/// <summary>
		/// returns dictionary, key=steamAppId, value=play_time
		/// </summary>
		/// <param name="steamApiKey"></param>
		/// <param name="steamId"></param>
		/// <returns></returns>
		public static Dictionary<string, int> ListPlayTimes(string steamApiKey, string steamId)
		{
			var result = new Dictionary<string, int>();

			var steamOwnedGamesUrl = $"http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={ steamApiKey }&steamid={ steamId }";
			var response = WebUtils.SilentWebRequest(steamOwnedGamesUrl);
			if (response != null)
			{
				dynamic deserializedResponse = JsonConvert.DeserializeObject(response);

				for (var i = 0; i < Int32.Parse(deserializedResponse.response.game_count.ToString()); i++)
				{
					var steamAppId = deserializedResponse.response.games[i].appid.ToString();
					var playTimeForEver = Int32.Parse(deserializedResponse.response.games[i].playtime_forever.ToString());

					result.Add(steamAppId, playTimeForEver);
				}
			}

			return result;
		}
	}
}
