
using System.Windows;

namespace naLauncherWPF.App
{
	internal class Const
	{
		/// <summary>gap between game images [pixels]</summary>
		public const double GridBorder = 16;

		public const double Scale = 1;

		/// <summary>actual size of game image</summary>
		readonly public static Size GameImageSize = new Size(460 * Scale, 215 * Scale);

		/// <summary>size of game info element, on the bottom of game image</summary>
		readonly public static Size GameInfoSize = new Size(460 * Scale, 45 * Scale);

		/// <summary>size of game info element, on the bottom of game image</summary>
		readonly public static Size RatingSize = new Size(45 * Scale, 45 * Scale);

		/// <summary>size of whole game cell (image + info, ...)</summary>
		readonly public static Size GameControlSize = new Size(GameImageSize.Width, GameImageSize.Height + GameInfoSize.Height);

		readonly public static Size MinWindowSize = new Size(
			1 + GridBorder + GameControlSize.Width + GridBorder + 1,
			1 + 32/*HeaderLabel.Height*/ + GridBorder + GameControlSize.Height + GridBorder + 32/*GameFilterInput.Height*/ + GridBorder + 1);

		readonly public static double GameDescriptionFontSize = 14 * Scale;
		readonly public static double GameTilteFontSize = 15 * Scale;
		readonly public static double RatingTextFontSize = 20 * Scale;

		readonly public static double WindowHeaderFontSize = 14;
		readonly public static double WindowTextFontSize = 12;
	}
}
