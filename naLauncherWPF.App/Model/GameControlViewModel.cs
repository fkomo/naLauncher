using GameLibrary;
using naLauncherWPF.App.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ujeby.Common.Tools;
using System.Linq;

namespace naLauncherWPF.App.Model
{
	public class GameControlViewModel : ObservableObject
	{
		private const byte RatingBackgroundAlpha = 0x88;

		private GameInfo Model = null;

		public GameControlViewModel()
		{
			// this constructor is called from GameControl.InitializeComponent()
		}

		public GameControlViewModel(string gameId, Action rebuildGameGrid)
		{
			RebuildGameGrid = rebuildGameGrid;

			Model = GameLibrary.GameLibrary.Games[gameId];

			GameTitle = Model.Title;
		}

		private GameLibrary.Tools.GameSpy GameSpy { get; set; } = null;

		private void RunGame(bool asAdmin)
		{
			try
			{
				if (GameSpy != null)
				{
					GameSpy.Terminate();
					GameSpy = null;
				}

				if (GameLibrary.GameLibrary.RunGame(GameId, asAdmin))
				{
					IsDisabled = true;

					var startTime = DateTime.Now;

					// spy on game executable to get gameplay duration
					GameSpy = GameLibrary.Tools.GameSpy.Create(Model.Title, Model.Shortcut, (elapsedMinutes) =>
					{
						IsDisabled = false;

						GameLibrary.GameLibrary.AddGameTimestamp(GameId, startTime, elapsedMinutes);

						OnPropertyChanged(nameof(GameInfo));
						RebuildGameGrid();
					});

					// gamespy is not running
					if (GameSpy == null)
					{
						IsDisabled = false;

						// record just game start
						GameLibrary.GameLibrary.AddGameTimestamp(GameId, startTime);

						OnPropertyChanged(nameof(GameInfo));
						RebuildGameGrid();
					}
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine(ex.ToString());
			}
		}

		#region Commands

		public ICommand RunCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					RunGame(false);
				},
				() => { return GameSpy?.Running != true && GameTitleReadOnly && !GameLibrary.GameLibrary.Games[GameId].Removed; });
			}
		}

		public ICommand RunAsAdminCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					RunGame(true);
				},
				() => { return GameSpy?.Running != true && GameTitleReadOnly && !GameLibrary.GameLibrary.Games[GameId].Removed; });
			}
		}

		public ICommand MarkAsBeatenCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					try
					{
						GameLibrary.GameLibrary.SetGameAsCompleted(GameId);
						RebuildGameGrid();
					}
					catch (Exception ex)
					{
						Log.WriteLine(ex.ToString());
					}
				},
				() => { return GameTitleReadOnly && !GameLibrary.GameLibrary.Games[GameId].Completed.HasValue; });
			}
		}

		public ICommand BeginRenameCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					OldGameTitle = GameTitle;
					GameTitleReadOnly = false;
				},
				() => { return GameTitleReadOnly; });
			}
		}

		public ICommand EndRenameCommand
		{
			get
			{
				return new DelegateCommandWithInput<bool>((canceled) =>
				{
					GameTitleReadOnly = true;

					if (canceled)
					{
						GameTitle = OldGameTitle;
						return;
					}

					IsDisabled = true;
					GameLibrary.GameLibrary.RenameGame(OldGameId, GameTitle, (newGameId) =>
					{
						IsDisabled = false;

						Log.WriteLine($"GameRenamed({ OldGameId }, { OldGameTitle }): { newGameId ?? "null" }/{ GameTitle }");

						if (string.IsNullOrEmpty(newGameId))
							// restore previous game title
							GameTitle = OldGameTitle;

						else
						{
							// create new model
							Model = GameLibrary.GameLibrary.Games[newGameId];

							OnPropertyChanged(nameof(GameImageSource));
							OnPropertyChanged(nameof(GameInfo));
							OnPropertyChanged(nameof(GameRating));

							RebuildGameGrid();
						}

						OldGameTitle = null;
					});
				});
			}
		}

		public ICommand RemoveCommand
		{
			get
			{
				return new DelegateCommand(
					() =>
					{
						try
						{
							if (MessageBox.Show($"Do you really want to remove '{ GameTitle }' ?", "naLauncher", MessageBoxButton.YesNo) == MessageBoxResult.No)
								return;

							if (!Debugger.IsAttached)
								// show add&remove programs window
								Process.Start("appwiz.cpl");

							GameLibrary.GameLibrary.RemoveGame(GameId);

							OnPropertyChanged(nameof(GameImageSource));

							RebuildGameGrid();
						}
						catch (Exception ex)
						{
							Log.WriteLine(ex.ToString());
						}
					},
					() => { return GameTitleReadOnly && !GameLibrary.GameLibrary.Games[GameId].Removed; });
			}
		}

		public ICommand DeleteCommand
		{
			get
			{
				return new DelegateCommand(
					() =>
					{
						try
						{
							if (MessageBox.Show($"Do you really want to *DELETE* '{ GameTitle }' from library ? { Environment.NewLine }{ Environment.NewLine }*All game statistics and properties will be also removed, there is no turning back!*", "naLauncher", MessageBoxButton.YesNo) == MessageBoxResult.No)
								return;

							GameLibrary.GameLibrary.DeleteGame(GameId);

							RebuildGameGrid();
						}
						catch (Exception ex)
						{
							Log.WriteLine(ex.ToString());
						}
					},
					() => { return GameTitleReadOnly && GameLibrary.GameLibrary.Games[GameId].Removed; });
			}
		}

		public ICommand ChangeImageCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					try
					{
						// TODO ChangeImageCommand

						//var gameId = GetGameId(sender as MenuItem);

						//var dialog = new OpenFileDialog()
						//{
						//	Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif",
						//	DefaultExt = ".png",
						//};
						//var result = dialog.ShowDialog();
						//if (result == true)
						//{
						//	var filename = dialog.FileName;

						//	System.Drawing.Bitmap imageFromFile;
						//	using (var stream = System.IO.File.OpenRead(filename))
						//		imageFromFile = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromStream(stream);

						//	if (GameImageSize.Width / GameImageSize.Height != (double)imageFromFile.Width / (double)imageFromFile.Height)
						//	{
						//		return;
						//	}

						//	GameLibrary.GameLibrary.SetUserImage(gameId, imageFromFile, filename);

						//	GameChanged(gameId);
						//}
					}
					catch (Exception ex)
					{
						Log.WriteLine(ex.ToString());
					}
				},
				() => { return GameTitleReadOnly; });
			}
		}

		public ICommand NextImageCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					try
					{
						// TODO NextImageCommand
					}
					catch (Exception ex)
					{
						Log.WriteLine(ex.ToString());
					}
				},
				() => { return GameTitleReadOnly; });
			}
		}

		public ICommand PrevImageCommand
		{
			get
			{
				return new DelegateCommand(() =>
				{
					try
					{
						// TODO PrevImageCommand
					}
					catch (Exception ex)
					{
						Log.WriteLine(ex.ToString());
					}
				},
				() => { return GameTitleReadOnly; });
			}
		}

		#endregion

		#region Properties

		private bool isDisabled = false; 
		public bool IsDisabled
		{
			get { return isDisabled; }
			set
			{
				isDisabled = value;
				OnPropertyChanged();
			}
		}

		private Action RebuildGameGrid { get; set; } = null;

		private int columnIndex;
		public int ColumnIndex
		{
			get { return columnIndex; }
			set
			{
				columnIndex = value;
				OnPropertyChanged();
			}
		}

		private int rowIndex;
		public int RowIndex
		{
			get { return rowIndex; }
			set
			{
				rowIndex = value;
				OnPropertyChanged();
			}
		}

		public string GameId
		{
			get { return GameLibrary.GameLibrary.GetGameId(Model); }
		}

		public string OldGameId { get { return GameLibrary.GameLibrary.GetGameId(OldGameTitle); } }
		public string OldGameTitle { get; private set; }

		private string gameTitle = null;
		public string GameTitle
		{
			get { return gameTitle; }
			set
			{
				gameTitle = value;
				OnPropertyChanged();
			}
		}

		private bool gameTitleReadOnly = true;
		public bool GameTitleReadOnly
		{
			get { return gameTitleReadOnly; }
			set
			{
				gameTitleReadOnly = value;

				OnPropertyChanged();
				OnPropertyChanged(nameof(GameTitleBackground));
				OnPropertyChanged(nameof(GameTitleFocused));
			}
		}

		public bool GameTitleFocused
		{
			get
			{
				return !GameTitleReadOnly;
			}
		}

		public bool MultipleImages
		{
			get
			{
				return Model?.Images?.Length > 1;
			}
		}

		public Brush GameTitleBackground
		{
			get { return new SolidColorBrush(GameTitleReadOnly ? Color.FromArgb(0xff, 0x18, 0x18, 0x18) : Color.FromArgb(0xff, 0x28, 0x28, 0x28)); }
		}

		public int? GameRating
		{
			get { return Model?.Rating; }
		}

		public Brush GameRatingColor
		{
			get
			{
				if (!GameRating.HasValue)
					return null;

				var ratingColor = GameLibrary.GameDataProviders.SteamDbInfoData.GetMetacriticColor(GameRating.Value);
				ratingColor.A = RatingBackgroundAlpha;

				return new SolidColorBrush(ratingColor);
			}
		}

		public object GameImageSource
		{
			get
			{
				if (GameId != null && GameLibrary.GameLibrary.GameImageCache.TryGetValue(GameId, out System.Drawing.Bitmap gameImageFromCache))
				{
					return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
						gameImageFromCache.GetHbitmap(),
						IntPtr.Zero,
						Int32Rect.Empty,
						BitmapSizeOptions.FromWidthAndHeight((int)Const.GameImageSize.Width, (int)Const.GameImageSize.Height));
				}

				return null;
			}
		}

		public bool IsGameInfoPresent
		{
			get { return gameInfo?.Count() > 0; }
		}

		private IList<Inline> gameInfo = null;
		public IList<Inline> GameInfo
		{
			get
			{
				gameInfo = new List<Inline>();

				if (Model != null)
				{
					var igdbData = Model.CustomData<GameLibrary.GameDataProviders.IgdbComData>();
					if (igdbData != null)
					{
						if (igdbData.Developer != null)
						{
							gameInfo.Add(new Run("Developed by "));
							gameInfo.Add(new Run(igdbData.Developer) { FontWeight = FontWeights.Bold });
							gameInfo.Add(new LineBreak());
						}

						if (igdbData.Genres?.Length > 0)
						{
							gameInfo.Add(new LineBreak());
							gameInfo.Add(new Run(string.Join(" | ", igdbData.Genres)));
							gameInfo.Add(new LineBreak());
						}
					}

					if (Model.Summary != null)
					{
						gameInfo.Add(new LineBreak());
						gameInfo.Add(new Run(Model.Summary));
						gameInfo.Add(new LineBreak());
					}

					gameInfo.Add(new LineBreak());
					gameInfo.Add(new Run($"Added { Strings.TimeStringSince(Model.Added) }"));
					gameInfo.Add(new LineBreak());

					if (Model.PlayCount > 0)
					{
						gameInfo.Add(new Run($"Played { Strings.NumToCountableString(Model.PlayCount) }, last time { Strings.TimeStringSince(Model.LastPlayed.Value) }"));
						gameInfo.Add(new LineBreak());
					}

					if (Model.TotalTimePlayed > 0)
					{
						gameInfo.Add(new Run($"Played for { Strings.DurationString(new TimeSpan(0, Model.TotalTimePlayed, 0)) }"));
						gameInfo.Add(new LineBreak());
					}

					if (Model.GamepadFriendly == true)
					{
						gameInfo.Add(new LineBreak());
						gameInfo.Add(new Run($"Controller support"));
						gameInfo.Add(new LineBreak());
					}

					// remove extensive line breaks at start
					while (gameInfo.FirstOrDefault() is LineBreak)
						gameInfo.RemoveAt(0);

					// remove extensive line breaks at the end
					while (gameInfo.LastOrDefault() is LineBreak)
						gameInfo.RemoveAt(gameInfo.Count - 1);
				}

				return gameInfo;
			}
		}

		public double TextFontSize { get; set; } = Const.GameDescriptionFontSize;
		public double TitleFontSize { get; set; } = Const.GameTilteFontSize;
		public double RatingFontSize { get; set; } = Const.RatingTextFontSize;

		#endregion
	}
}
