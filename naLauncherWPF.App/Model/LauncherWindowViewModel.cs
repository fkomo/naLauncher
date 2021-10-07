using GameLibrary;
using naLauncherWPF.App.Controls;
using naLauncherWPF.App.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ujeby.Common.Tools;
using Ujeby.Common.Tools.Extensions;

namespace naLauncherWPF.App.Model
{
	public class LauncherWindowViewModel : ObservableObject
	{
		public LauncherWindowViewModel()
		{
			Log.WriteLineDebug("LauncherWindowViewModel()");

			GameLibrary.GameLibrary.Load();

			using (var tb = new TimedBlock($"LauncherWindowViewModel({ GameLibrary.GameLibrary.Games.Count })#CreateAllGameControls"))
			{
				// all controls must be created on STA thread, so better create controls for all games now
				allGameControls = GameLibrary.GameLibrary.Games
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
		}

		public void Save()
		{
			GameLibrary.Tools.GameSpy.TerminateAll();

			Properties.Settings.Default.Order = (int)order;
			Properties.Settings.Default.Filter = (int)filter;
			Properties.Settings.Default.OrderAscending = isOrderAscending;

			Properties.Settings.Default.GridSize = GridSize;
			Properties.Settings.Default.WindowPosition =
				new System.Drawing.Point((int)Application.Current.MainWindow.Left, (int)Application.Current.MainWindow.Top);

			Properties.Settings.Default.WindowSize =
				new System.Drawing.Size((int)Application.Current.MainWindow.Width, (int)Application.Current.MainWindow.Height);

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
				using (var tb = new TimedBlock($"LauncherWindowViewModel.RebuildGameGrid()"))
				{
					FilteredGameIds = GameLibrary.GameLibrary.ListGames(titleFilter, filter, order, isOrderAscending);

					var _filteredGameControls = new List<GameControl>();
					foreach (var gameId in filteredGameIds)
						_filteredGameControls.Add(allGameControls.Single(g => g.ViewModel.GameId == gameId));

					FilteredGameControls = _filteredGameControls;
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
					FilteredGameControls.SingleOrDefault(g => g.ViewModel.GameId == gameId).ViewModel = new GameControlViewModel(gameId, RebuildGameGrid);
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

		private IList<RowDefinition> rowDefinitions = null;
		public IList<RowDefinition> RowDefinitions
		{
			get { return rowDefinitions; }
			set
			{
				rowDefinitions = value;
				OnPropertyChanged();
			}
		}

		private IList<ColumnDefinition> columnDefinitions = null;
		public IList<ColumnDefinition> ColumnDefinitions
		{
			get { return columnDefinitions; }
			set
			{
				columnDefinitions = value;
				OnPropertyChanged();
			}
		}

		private GameControl[] allGameControls = null;

		private GameControl[] filteredGameControls = null;
		public IList<GameControl> FilteredGameControls
		{
			get { return filteredGameControls; }
			private set
			{
				filteredGameControls = value.ToArray();

				if (filteredGameControls != null)
				{
					using (var tb = new TimedBlock($"LauncherWindowViewModel.FilteredGameControls({ filteredGameControls.Length })#UpdateGridLayout"))
					{
						var rowCount = filteredGameControls.Length / GridSize;
						if (filteredGameControls.Length % GridSize != 0)
							rowCount++;

						// rows, for example: 8 | 260 | 16 | ... | 16 | 260 | 8
						RowDefinitions = EnumerableExtensions.Pad(() => { return new RowDefinition { Height = new GridLength(Const.GridBorder / 2) }; },
						EnumerableExtensions.Join(() => { return new RowDefinition { Height = new GridLength(Const.GridBorder) }; },
							EnumerableExtensions.RepeatNew(() => { return new RowDefinition { Height = new GridLength(Const.GameControlSize.Height) }; }, rowCount)));

						// columns, for example: 16 | 460 | 16 | ... | 16 | 460 | 16
						ColumnDefinitions = EnumerableExtensions.Pad(() => { return new ColumnDefinition { Width = new GridLength(Const.GridBorder) }; },
							EnumerableExtensions.Join(() => { return new ColumnDefinition { Width = new GridLength(Const.GridBorder) }; },
								EnumerableExtensions.RepeatNew(() => { return new ColumnDefinition(); }, GridSize)));

						Debug.WriteLine($"GameGrid { ColumnDefinitions.Count() }x{ RowDefinitions.Count() }");
					}
				}

				if (filteredGameControls != null)
				{
					using (var tb = new TimedBlock($"LauncherWindowViewModel.FilteredGameControls({ filteredGameControls.Length })#UpdateGameControlPositions"))
					{
						for (var i = 0; i < filteredGameControls.Length; i++)
						{
							filteredGameControls[i].ViewModel.Y = 1 + (i / GridSize) * 2;
							filteredGameControls[i].ViewModel.X = 1 + (i % GridSize) * 2;
						}
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

		private Size windowSize = new Size(Properties.Settings.Default.WindowSize.Width, Properties.Settings.Default.WindowSize.Height);
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

				var newGridSize = Math.Max(1, (int)(windowSize.Width - (2 * Const.GridBorder + 2)) / (int)(Const.GameImageSize.Width + Const.GridBorder));
				if (GridSize != newGridSize && FilteredGameControls.Count() >= newGridSize)
					GridSize = newGridSize;
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

		private int gridSize = Properties.Settings.Default.GridSize;
		public int GridSize
		{
			get { return gridSize; }
			set
			{
				gridSize = value;
				OnPropertyChanged();

				RebuildGameGrid();				
			}
		}

		/// <summary>if true window is minimized on close (esc key) otherwise it is closed</summary>
		public bool MinimizeOnClose { get; set; } = false;

		#endregion
	}
}
