using System;
using System.Windows;
using Ujeby.Common.Tools;
using System.Windows.Input;
using naLauncherWPF.App.Model;
using System.Windows.Controls;

namespace naLauncherWPF.App
{
	// TODO skin context menus
	// TODO skin question message boxes
	// TODO application context menu
	// TODO merge game library with another one (from different device)
	// TODO upload/download library to/from google drive

	public enum SmoothScrollingMode
	{
		Default,
		Linear,
		Exponential
	}

	/// <summary>
	/// Interaction logic for LauncherWindow.xaml
	/// </summary>
	public partial class LauncherWindow : Window
	{
		public SmoothScrollingMode SmoothScrolling { get; set; } = SmoothScrollingMode.Exponential;

		public LauncherWindowViewModel ViewModel
		{
			get { return this.DataContext as LauncherWindowViewModel; }
			set 
			{ 
				this.DataContext = value; 
			}
		}

		public LauncherWindow()
		{
			InitializeComponent();

			if (ViewModel != null)
			{
				var screenSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
				if (ViewModel.WindowSize.Width >= screenSize.Width || ViewModel.WindowSize.Height >= screenSize.Height)
				{
					ViewModel.WindowSize = new Size(screenSize.Width, screenSize.Height);
					WindowState = WindowState.Maximized;
				}
			}
		}

		/// <summary>point where mouse drag started</summary>
		private System.Drawing.Point? DragStart = null;

		private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (WindowState == WindowState.Normal)
				{
					if (e.ChangedButton == MouseButton.Left)
						DragStart = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void MainWindow_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (DragStart.HasValue && e.LeftButton == MouseButtonState.Pressed)
				{
					MainWindow.Left += System.Windows.Forms.Cursor.Position.X - DragStart.Value.X;
					MainWindow.Top += System.Windows.Forms.Cursor.Position.Y - DragStart.Value.Y;

					DragStart = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void MainWindow_MouseUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (WindowState == WindowState.Normal)
				{
					if (e.ChangedButton == MouseButton.Left)
						DragStart = null;
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
		{
			try
			{
				DragStart = null;
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void MainWindow_KeyUp(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.Key == Key.Escape)
				{
					if (ViewModel.MinimizeOnClose)
						WindowState = WindowState.Minimized;

					else
						Close();
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
				ViewModel?.Save();
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void GameGridScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			try
			{
				if (SmoothScrolling == SmoothScrollingMode.Default)
					return;

				e.Handled = true;

				var gameGrid = Helpers.UIHelper.FindChild<Grid>(GameGridScrollViewer, "GameGrid");
				var realScrollableHeight = ((gameGrid.RowDefinitions.Count - 1) / 2) * (Const.GameControlSize.Height + Const.GridBorder) - GameGridScrollViewer.ViewportHeight;

				// TODO scrolling with pgUp/pgDown or ctrl+pgUp/pgDown should be checked against realScrollableHeight

				// "smooth" scrolling
				Threading.ThreadAndForget(() =>
				{
					var direction = e.Delta < 0 ? -1.0 : 1.0;

					if (SmoothScrolling == SmoothScrollingMode.Linear)
					{
						int scrollStep = 4;
						for (var i = 0; i < Math.Abs(e.Delta) / scrollStep; i++)
						{
							var newScroll = Math.Min(GameGridScrollViewer.VerticalOffset - (scrollStep * direction), realScrollableHeight);

							GameGridScrollViewer.Dispatcher.Invoke(() =>
							{
								GameGridScrollViewer.ScrollToVerticalOffset(newScroll);
							});

							if (newScroll == realScrollableHeight)
								break;

							System.Threading.Thread.Sleep(10);
						}
					}
					else if (SmoothScrolling == SmoothScrollingMode.Exponential)
					{
						for (var scrollStep = Math.Abs((double)e.Delta); scrollStep > 1; scrollStep = scrollStep / 2)
						{
							var newScroll = Math.Min(GameGridScrollViewer.VerticalOffset - (scrollStep * direction), realScrollableHeight);

							GameGridScrollViewer.Dispatcher.Invoke(() =>
							{
								GameGridScrollViewer.ScrollToVerticalOffset(newScroll);
							});

							if (newScroll == realScrollableHeight)
								break;

							System.Threading.Thread.Sleep(32);
						}
					}
				});
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderCloseLabel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				Close();
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderMaximizeLabel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			WindowState = WindowState.Maximized;
		}

		private void HeaderMinimizeLabel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			WindowState = WindowState.Normal;
			ViewModel.WindowSizeWidth = 494;
			ViewModel.WindowSizeHeight = 960;
		}
	}
}
