using GameLibrary;
using naLauncherWPF.App.Controls;
using naLauncherWPF.App.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Ujeby.Common.Tools;

namespace naLauncherWPF.App.Model
{
	public class LauncherWindowViewModel2 : ObservableObject
	{
		public LauncherWindowViewModel2()
		{
			GameLibrary.GameLibrary.Load();

			using (var tb = new TimedBlock($"LauncherWindowViewModel2({ GameLibrary.GameLibrary.Games.Count })#CreateAllGameControls"))
			{
				// all controls must be created on STA thread, so better create controls for all games now
				allGames = GameLibrary.GameLibrary.Games
					.Select(g =>
						new GameControl(g.Key, RebuildGameGrid, ProgressBarStartStop)
						{
							Width = Const.GameControlSize.Width,
							Height = Const.GameControlSize.Height
						})
					.ToArray();
			}

			RebuildGameGrid();

			ProgressBarStartStop(true);

			// update games
			GameLibrary.GameLibrary.UpdateAll(
				(gameId) => GameUpdated(gameId),
				() =>
				{
					try
					{
						ProgressBarStartStop(false);

						Application.Current?.Dispatcher.Invoke(() =>
						{
							RebuildGameGrid();
						});
					}
					catch (Exception ex)
					{
						Log.WriteLine(ex.ToString());
					}
				});

			// TEST
			//using (var tb = new TimedBlock($"LauncherWindowViewModel2({ GameLibrary.GameLibrary.Games.Count })#CreateAllGameControls"))
			//{
			//	var allControls = new List<TestUserControl>();
			//	foreach (var imageFile in System.IO.Directory.EnumerateFiles(@"c:\Users\filip\AppData\Roaming\Ujeby\naLauncher\ImageCache\SteamDbInfo\", "*.jpg"))
			//	{
			//		System.Drawing.Bitmap imageFromFile;
			//		using (Stream stream = File.OpenRead(imageFile))
			//			imageFromFile = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromStream(stream);

			//		allControls.Add(new TestUserControl
			//		{
			//			ViewModel = new TestViewModel
			//			{
			//				ImageBitmap = imageFromFile,
			//				Left = Rng.Next(0, (int)(windowSize.Width - Const.GameControlSize.Width)),
			//				Top = Rng.Next(0, (int)(windowSize.Height - Const.GameControlSize.Height - (32 + 8 + 56)))
			//			}
			//		});
			//	}

			//	AllControls = allControls.ToArray();
			//}
		}

		// TEST
		//private TestUserControl[] allControls = new TestUserControl[] { };
		//public TestUserControl[] AllControls
		//{
		//	get { return allControls; }
		//	private set
		//	{
		//		allControls = value?.ToArray();
		//		OnPropertyChanged();
		//	}
		//}

		public void Save()
		{
			GameLibrary.Tools.GameSpy.TerminateAll();

			Properties.Settings.Default.Order = (int)order;
			Properties.Settings.Default.Filter = (int)filter;
			Properties.Settings.Default.OrderAscending = isOrderAscending;

			Properties.Settings.Default.WindowPosition =
				new System.Drawing.Point((int)Application.Current.MainWindow.Left, (int)Application.Current.MainWindow.Top);
			//Properties.Settings.Default.WindowSize =
			//	new System.Drawing.Size((int)Application.Current.MainWindow.Width, (int)Application.Current.MainWindow.Height);

			Properties.Settings.Default.Save();
		}

		private void ProgressBarStartStop(bool isRunning)
		{
			IsProgressBarRunning = isRunning;
		}

		private void RebuildGameGrid()
		{
			Application.Current?.Dispatcher.Invoke(() =>
			{
				using (var tb = new TimedBlock($"RebuildGameGrid()"))
				{
					FilteredGameIds = GameLibrary.GameLibrary.ListGames(titleFilter, filter, order, isOrderAscending);
					FilteredGames = allGames.Where(g => FilteredGameIds.Contains(g.ViewModel.GameId)).ToArray();
				}
			});
		}

		private void GameUpdated(string gameId)
		{
			try
			{
				var game = GameLibrary.GameLibrary.Games[gameId];
				Log.WriteLine($"GameChanged({ gameId }, { game.Title })");

				Application.Current?.Dispatcher.Invoke(() =>
				{
					FilteredGames.SingleOrDefault(g => g.ViewModel.GameId == gameId).ViewModel = new GameControlViewModel(gameId, RebuildGameGrid);
					RebuildGameGrid();
				});
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		#region Commands

		public ICommand ChangeOrderCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					Order = ((GameOrder)(((int)order + 1) % (int)GameOrder.Count)).ToString();
				});
			}
		}

		public ICommand ChangeOrderDirectionCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					isOrderAscending = !isOrderAscending;
					OnPropertyChanged(nameof(OrderDirection));

					RebuildGameGrid();
				});
			}
		}

		public ICommand ChangeFilterCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					Filter = ((GameFilter)(((int)filter + 1) % (int)GameFilter.Count)).ToString();
				});
			}
		}

		#endregion

		#region Properties

		private static Random Rng = new Random();

		private GameControl[] allGames = new GameControl[] { };

		private GameControl[] filteredGames = new GameControl[] { };
		public GameControl[] FilteredGames
		{
			get { return filteredGames; }
			private set
			{
				filteredGames = value?.ToArray();
				if (filteredGames != null)
				{
					for (var i = 0; i < filteredGames.Length; i++)
					{
						filteredGames[i].ViewModel.X = Rng.Next(0, (int)(windowSize.Width - Const.GameControlSize.Width));
						filteredGames[i].ViewModel.Y = Rng.Next(0, (int)(windowSize.Height - Const.GameControlSize.Height - (32 + 8 + 56)));
					}
				}

				OnPropertyChanged();
			}
		}

		private string[] filteredGameIds = null;
		public string[] FilteredGameIds
		{
			get { return filteredGameIds; }
			private set
			{
				filteredGameIds = value;
				OnPropertyChanged();
			}
		}

		private bool isProgressBarRunning = false;
		public bool IsProgressBarRunning
		{
			get { return isProgressBarRunning; }
			private set
			{
				isProgressBarRunning = value;
				OnPropertyChanged();
			}
		}

		private GameFilter filter = (GameFilter)Properties.Settings.Default.Filter;
		public string Filter
		{
			get { return filter.ToString(); }
			private set
			{
				filter = (GameFilter)Enum.Parse(typeof(GameFilter), value);
				OnPropertyChanged();

				RebuildGameGrid();
			}
		}

		private GameOrder order = (GameOrder)Properties.Settings.Default.Order;
		public string Order
		{
			get { return order.ToString(); }
			private set
			{
				order = (GameOrder)Enum.Parse(typeof(GameOrder), value);
				OnPropertyChanged();

				RebuildGameGrid();
			}
		}

		private bool isOrderAscending = Properties.Settings.Default.OrderAscending;
		public string OrderDirection
		{
			get { return isOrderAscending ? "▲" : "▼"; }
		}

		private string titleFilter = null;
		public string TitleFilter
		{
			get { return titleFilter; }
			set
			{
				titleFilter = value;
				OnPropertyChanged();

				RebuildGameGrid();
			}
		}

		public double HeaderFontSize { get; private set; } = Const.WindowHeaderFontSize;
		public double TextFontSize { get; private set; } = Const.WindowTextFontSize;

		private Size windowSize = new Size(1920/*Properties.Settings.Default.WindowSize.Width*/, 1080/*Properties.Settings.Default.WindowSize.Height*/);
		public Size WindowSize
		{
			get { return windowSize; }
			set
			{
				windowSize = value;

				OnPropertyChanged(nameof(WindowSizeWidth));
				OnPropertyChanged(nameof(WindowSizeHeight));
			}
		}

		public double WindowSizeWidth
		{
			get { return WindowSize.Width; }
			set
			{
				windowSize.Width = value;
				OnPropertyChanged();
			}
		}

		public double WindowSizeHeight
		{
			get { return WindowSize.Height; }
			set
			{
				windowSize.Height = value;

				OnPropertyChanged();
			}
		}

		public Size MinWindowSize
		{
			get { return Const.MinWindowSize; }
		}

		private Point windowPosition = new Point(Properties.Settings.Default.WindowPosition.X, Properties.Settings.Default.WindowPosition.Y);
		public Point WindowPosition
		{
			get { return windowPosition; }
			set
			{
				windowPosition = value;

				OnPropertyChanged();
			}
		}

		public double WindowPositionX
		{
			get { return WindowPosition.X; }
			set
			{
				windowPosition.X = value;
				OnPropertyChanged();
			}
		}

		public double WindowPositionY
		{
			get { return WindowPosition.Y; }
			set
			{
				windowPosition.Y = value;
				OnPropertyChanged();
			}
		}

		/// <summary>if true window is minimized on close (esc key) otherwise it is closed</summary>
		public bool MinimizeOnClose { get; set; } = false;

		#endregion
	}
}
