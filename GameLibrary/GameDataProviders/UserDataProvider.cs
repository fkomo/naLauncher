using System;
using System.IO;
using System.Runtime.Serialization;

namespace GameLibrary.GameDataProviders
{
	[DataContract]
	public class UserData : GameData, IGameImage, IControllerSupported
	{
		public override int Priority { get { return 1000; } }
		public override string DataType { get { return nameof(UserData); } }

		public UserData(string gameTitle) : base(gameTitle)
		{

		}

		[DataMember]
		public bool? GamepadFriendly { get; set; }

		[DataMember]
		public GameImage Image { get; set; }

		public override void Merge(GameData newData)
		{
			// TODO UserData.Merge
		}
	}

	internal class UserDataProvider : GameDataProvider, IGameDataProvider
	{
		public GameData GetGameData(string gameTitle, bool ignoreLocalCache)
		{
			// jpg
			var imageFileFromCache = Path.Combine(ImageCacheDirectory, gameTitle) + ".jpg";
			if (!ImageExists(imageFileFromCache))
			{
				// png
				imageFileFromCache = Path.Combine(ImageCacheDirectory, gameTitle) + ".png";
				if (!ImageExists(imageFileFromCache))
				{
					// try other image formats ... bmp/gif?
					imageFileFromCache = null;
				}
			}

			if (imageFileFromCache != null)
				return new UserData(gameTitle)
				{
					Image = new GameImage { LocalFilename = imageFileFromCache }
				};

			return null;
		}

		public string GetGameDataType()
		{
			return nameof(UserData);
        }
	}
}
