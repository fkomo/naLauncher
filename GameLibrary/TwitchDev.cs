using Newtonsoft.Json;
using System;
using Ujeby.Common.Tools;

namespace GameLibrary
{
	public class TwitchDev
	{
		private static string accessToken = null;
		public static string AccessToken
		{
			get
			{
				if (DateTime.Now >= ExpiresIn)
					GetAccessToken();

				return accessToken;
			}
			private set { accessToken = value; }
		}

		public static DateTime ExpiresIn { get; private set; } = DateTime.Now;

		public const string TokenType = "Bearer";
		private const string GrantType = "client_credentials";

		/// <summary>
		/// 
		/// </summary>
		/// <param name="clientId">Properties.Settings.Default.TwitchDevClientId</param>
		/// <param name="clientSecret">Properties.Settings.Default.TwitchDevClientSecret</param>
		/// <returns></returns>
		private static string GetAccessToken(string clientId = null, string clientSecret = null)
		{
			try
			{
				clientId = clientId ?? Properties.Settings.Default.TwitchDevClientId;
				clientSecret = clientSecret ?? Properties.Settings.Default.TwitchDevClientSecret;

				if (clientId == null || clientSecret == null)
					throw new ArgumentNullException();

				var response = Ujeby.Common.Tools.WebUtils.Post($"{ Properties.Settings.Default.TwitchDevOAuth2Url }?client_id={ clientId }&client_secret={ clientSecret }&grant_type={ GrantType }");

				dynamic jsonResponse = JsonConvert.DeserializeObject(response);

				AccessToken = jsonResponse.access_token.ToString();
				ExpiresIn = DateTime.Now.Add(new TimeSpan(0, 0, 0, System.Convert.ToInt32(jsonResponse.expires_in.ToString())));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}

			return AccessToken;
		}
	}
}
