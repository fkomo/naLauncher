using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using Ujeby.Common.Tools;

namespace GameLibrary.GameDataProviders
{
	public interface IGameSummary
	{
		string Summary { get; set; }
	}

	public interface IRatedGame
	{
		int? Rating { get; set; }
	}

	public interface IGameImage
	{
		GameImage Image { get; set; }
	}

	public interface IControllerSupported
	{
		bool? GamepadFriendly { get; set; }
	}

	public class MatchPossibility
	{
		public int Distance { get; set; }
		public string Value { get; set; }
	}

	[DataContract]
	public class GameImage
	{
		[DataMember]
		public string LocalFilename { get; set; }

		[DataMember]
		public string SourceUrl { get; set; }

		public override string ToString()
		{
			return $"[{ LocalFilename }, { SourceUrl }]";
		}
	}

	[KnownType(typeof(SalenautsComData))]
	[KnownType(typeof(SteamDbInfoData))]
	[KnownType(typeof(SteamCryoTankNetData))]
	[KnownType(typeof(IgdbComData))]
	[KnownType(typeof(UserData))]
	[DataContract]
	public abstract class GameData
	{
		[DataMember]
		public string SourceGameTitle { get; set; }

		public GameData(string gameSourceTitle = null)
		{
			SourceGameTitle = gameSourceTitle;
		}

		public abstract int Priority { get; }

		public abstract string DataType { get; }

		public abstract void Merge(GameData newData);
	}

	interface IGameDataProvider
	{
		GameData GetGameData(string gameTitle, bool ignoreLocalCache = false);

		string GetGameDataType();
	}

	internal class GameDataProvider
	{
		const string BackupExtension = ".backup-";
		const string TimestampFormat = "yyyyMMddHHmmssffff";

		static string CurrentClassName { get { return typeof(GameDataProvider).Name; } }

		const int DESIRED_GAME_IMAGE_DPI = 72;

		public static string ImageCacheDirectory
		{
			get
			{
				var directory = Path.Combine(GameLibrary.UserDataFolder, "ImageCache");
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);

				return directory;
			}
		}

		public static string SaveGameImage(string gameTitle, Image image, ImageFormat imageFormat, string source = null)
		{
			var newImagePath = ImageCacheDirectory;
			if (source != null)
				newImagePath = Path.Combine(newImagePath, source);

			newImagePath = Path.Combine(newImagePath, $"{ gameTitle }.{ Ujeby.Common.Tools.Graphics.GetImageExtension(imageFormat) }");

			SaveImage(newImagePath, image, imageFormat);

			return newImagePath;
		}

		public static bool ImageExists(string gameImageFile)
		{
			try
			{
				if (!string.IsNullOrEmpty(gameImageFile) && File.Exists(gameImageFile))
					return true;
			}
			catch (Exception ex)
			{
				Log.WriteLine($"{ CurrentClassName }.{ Utils.GetCurrentMethodName() }({ gameImageFile }):false { ex.ToString() }");
			}

			return false;
		}

		public static void SaveImage(string imagePath, Image image, ImageFormat imageFormat)
		{
			var directory = new FileInfo(imagePath).Directory.FullName;
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			if (File.Exists(imagePath))
				File.Delete(imagePath);

			// change dpi, if needed
			if (image.HorizontalResolution != DESIRED_GAME_IMAGE_DPI || image.VerticalResolution != DESIRED_GAME_IMAGE_DPI)
			{
				var fixedImage = new Bitmap(image);
				(fixedImage as Bitmap).SetResolution(DESIRED_GAME_IMAGE_DPI, DESIRED_GAME_IMAGE_DPI);

				// save new image to ImageCacheDirectory
				fixedImage.Save(imagePath, imageFormat);
			}
			else
				image.Save(imagePath, imageFormat);
		}

		/// <summary>
		/// backup game image
		/// </summary>
		/// <param name="gameImage"></param>
		public static void BackupGameImage(GameImage gameImage)
		{
			if (gameImage?.LocalFilename != null && File.Exists(gameImage.LocalFilename))
			{
				var newFilename = $"{ gameImage.LocalFilename }{ BackupExtension }{ DateTime.Now.ToString(TimestampFormat) }";
				File.Copy(gameImage.LocalFilename, newFilename);

				Log.WriteLine($"BackupGameImage({ gameImage }): { newFilename }");
			}
		}
	}
}
