using naLauncherWPF.App.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ujeby.Common.Tools;

namespace naLauncherWPF.App.Controls
{
	// TODO auto-scroll game info text if its too long

	/// <summary>
	/// Interaction logic for GameControl.xaml
	/// </summary>
	public partial class GameControl : UserControl
	{
		public GameControlViewModel ViewModel
		{
			get { return this.DataContext as GameControlViewModel; }
			set
			{
				this.DataContext = value;

				if (ViewModel.MultipleImages)
				{
					GameImageNext.Visibility = Visibility.Hidden;
					GameImagePrev.Visibility = Visibility.Hidden;
				}
				else
				{
					GameImageNext.Visibility = Visibility.Collapsed;
					GameImagePrev.Visibility = Visibility.Collapsed;
				}

				if (!ViewModel.IsGameInfoPresent)
					GameInfoTextBlock.Visibility = Visibility.Collapsed;

				else
					GameInfoTextBlock.Visibility = Visibility.Hidden;

				GameRatingLabel.Visibility = Visibility.Collapsed;
			}
		}

		public GameControl()
		{
			InitializeComponent();
		}

		public GameControl(string gameId, Action rebuildGameGrid, Action<bool> progressStartStop)
		{
			InitializeComponent();

			ViewModel = new GameControlViewModel(gameId, rebuildGameGrid);
		}

		private void GameTitleEdit_MouseLeave(object sender, MouseEventArgs e)
		{
			try
			{
				if (ViewModel.IsGameInfoPresent)
					GameInfoTextBlock.Visibility = Visibility.Collapsed;
				if (ViewModel.GameRating.HasValue)
					GameRatingLabel.Visibility = Visibility.Collapsed;

				Mouse.OverrideCursor = null;
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void GameTitleEdit_MouseEnter(object sender, MouseEventArgs e)
		{
			try
			{
				if (ViewModel.IsGameInfoPresent)
					GameInfoTextBlock.Visibility = Visibility.Visible;
				if (ViewModel.GameRating.HasValue)
					GameRatingLabel.Visibility = Visibility.Visible;

				if (ViewModel.MultipleImages)
				{
					GameImageNext.Visibility = Visibility.Collapsed;
					GameImagePrev.Visibility = Visibility.Collapsed;
				}

				// when game title is read only, set cursor to normal arrow
				if (ViewModel.GameTitleReadOnly)
					Mouse.OverrideCursor = Cursors.Arrow;
				else
					Mouse.OverrideCursor = Cursors.IBeam;
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void Game_MouseEnter(object sender, MouseEventArgs e)
		{
			try
			{
				// TODO next/prev game image
				//if (ViewModel.MultipleImages)
				//{
				//	GameImageNext.Visibility = Visibility.Visible;
				//	GameImagePrev.Visibility = Visibility.Visible;
				//}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void Game_MouseLeave(object sender, MouseEventArgs e)
		{
			try
			{
				// TODO next/prev game image
				//if (ViewModel.MultipleImages)
				//{
				//	GameImageNext.Visibility = Visibility.Collapsed;
				//	GameImagePrev.Visibility = Visibility.Collapsed;
				//}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void GameTitleEdit_KeyUp(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Escape)
				{
					e.Handled = true;
					ViewModel.EndRenameCommand.Execute(true);
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}
	}
}
