using naLauncherWPF.App.Model;
using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Ujeby.Common.Tools;

namespace naLauncherWPF.App
{
	/// <summary>
	/// Interaction logic for LauncherWindow2.xaml
	/// </summary>
	public partial class LauncherWindow2 : Window
	{
		public LauncherWindowViewModel2 ViewModel
		{
			get { return this.DataContext as LauncherWindowViewModel2; }
			set { this.DataContext = value; }
		}

		private Timer WindowResizeTimer = new Timer(128) { Enabled = false };
		private Size? WindowSizeBeforeMax { get; set; } = null;

		public LauncherWindow2()
		{
			InitializeComponent();
			WindowResizeTimer.Elapsed += new ElapsedEventHandler(ResizingDone);
		}

		/// <summary>point where mouse drag started</summary>
		private System.Drawing.Point? DragStart = null;

		private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			try
			{
				if (WindowState == WindowState.Normal)
				{
					if (e.ChangedButton == MouseButton.Left)
						DragStart = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X - (int)MainWindow2.Left, System.Windows.Forms.Cursor.Position.Y - (int)MainWindow2.Top);
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void MainWindow_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

		private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
		{
			try
			{
				if (DragStart.HasValue && e.LeftButton == MouseButtonState.Pressed)
				{
					MainWindow2.Left = System.Windows.Forms.Cursor.Position.X - DragStart.Value.X;
					MainWindow2.Top = System.Windows.Forms.Cursor.Position.Y - DragStart.Value.Y;
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
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

		private void MainWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			try
			{
				//DragStart = null;
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

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			try
			{
				WindowResizeTimer.Stop();
				WindowResizeTimer.Start();
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		void ResizingDone(object sender, ElapsedEventArgs e)
		{
			try
			{
				WindowResizeTimer.Stop();

				Application.Current?.Dispatcher.Invoke(() =>
				{
					ViewModel?.RebuildGameGrid();
				});
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void ItemsControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			try
			{
				//foreach (var game in ViewModel.FilteredGames)
				//	game.ViewModel.SetDestination(game.ViewModel.X, game.ViewModel.Y + e.Delta);

				//foreach (var game in ViewModel.FilteredGames)
				//	game.MoveToDestination(1);

				e.Handled = true;

				//var gameGrid = Helpers.UIHelper.FindChild<Grid>(GameGridScrollViewer, "GameGrid");
				//var realScrollableHeight = ((gameGrid.RowDefinitions.Count - 1) / 2) * (Const.GameControlSize.Height + Const.GridBorder) - GameGridScrollViewer.ViewportHeight;

				//TODO scrolling with pgUp/ pgDown or ctrl+pgUp / pgDown should be checked against realScrollableHeight
  
				// "smooth" scrolling
  				Threading.ThreadAndForget(() =>
				{
					var direction = e.Delta < 0 ? -1.0 : 1.0;

					for (var scrollStep = Math.Abs((double)e.Delta); scrollStep > 1; scrollStep = scrollStep / 2)
					{
						var newScroll = /*Math.Min(*/GamesScrollViewer.VerticalOffset - (scrollStep * direction);//, realScrollableHeight);

						GamesScrollViewer.Dispatcher.Invoke(() =>
						{
							GamesScrollViewer.ScrollToVerticalOffset(newScroll);
						});

						//if (newScroll == realScrollableHeight)
						//	break;

						System.Threading.Thread.Sleep(16);
					}
				});
				}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		#region header buttons

		private void HeaderMaximizeLabel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				WindowSizeBeforeMax = ViewModel.WindowSize;
				WindowState = WindowState.Maximized;
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderMinimizeLabel_MouseUp(object sender, MouseButtonEventArgs e)
		{
			try
			{
				if (WindowState == WindowState.Maximized)
				{
					WindowState = WindowState.Normal;

					if (WindowSizeBeforeMax.HasValue)
						ViewModel.WindowSize = WindowSizeBeforeMax.Value;
					else
						ViewModel.WindowSize = new Size(Const.MinWindowSize.Width, Const.MinWindowSize.Height);
				}
				else
					ViewModel.WindowSize = new Size(Const.MinWindowSize.Width, Const.MinWindowSize.Height);
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderCloseLabel_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

		private void HeaderClose_MouseLeave(object sender, MouseEventArgs e)
		{
			try
			{
				Cursor = Cursors.Arrow;
				HeaderCloseShape1.Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xcc, 0xcc, 0xcc));
				HeaderCloseShape2.Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xcc, 0xcc, 0xcc));
				HeaderClose.Background = new SolidColorBrush(Color.FromArgb(0xdd, 0x11, 0x11, 0x11));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderClose_MouseEnter(object sender, MouseEventArgs e)
		{
			try
			{
				Cursor = Cursors.Hand;
				HeaderCloseShape1.Stroke = new SolidColorBrush(Colors.White);
				HeaderCloseShape2.Stroke = new SolidColorBrush(Colors.White);
				HeaderClose.Background = new SolidColorBrush(Color.FromArgb(0xdd, 0xcc, 0x00, 0x00));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderMin_MouseLeave(object sender, MouseEventArgs e)
		{
			try
			{
				Cursor = Cursors.Arrow;
				HeaderMinShape.Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xcc, 0xcc, 0xcc));
				HeaderMin.Background = new SolidColorBrush(Color.FromArgb(0xdd, 0x11, 0x11, 0x11));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderMin_MouseEnter(object sender, MouseEventArgs e)
		{
			try
			{
				Cursor = Cursors.Hand;
				HeaderMinShape.Stroke = new SolidColorBrush(Colors.White);
				HeaderMin.Background = new SolidColorBrush(Color.FromArgb(0xdd, 0x33, 0x33, 0x33));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderMax_MouseLeave(object sender, MouseEventArgs e)
		{
			try
			{
				Cursor = Cursors.Arrow;
				HeaderMaxShape.Stroke = new SolidColorBrush(Color.FromArgb(0xff, 0xcc, 0xcc, 0xcc));
				HeaderMax.Background = new SolidColorBrush(Color.FromArgb(0xdd, 0x11, 0x11, 0x11));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		private void HeaderMax_MouseEnter(object sender, MouseEventArgs e)
		{
			try
			{
				Cursor = Cursors.Hand;
				HeaderMaxShape.Stroke = new SolidColorBrush(Colors.White);
				HeaderMax.Background = new SolidColorBrush(Color.FromArgb(0xdd, 0x33, 0x33, 0x33));
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		#endregion

		private void FilterButton_Click(object sender, RoutedEventArgs e)
		{
			// reset scroll to zero when filter is changed
			GamesScrollViewer.ScrollToVerticalOffset(0);
		}
	}
}
