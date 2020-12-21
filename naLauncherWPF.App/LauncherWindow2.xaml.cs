using naLauncherWPF.App.Model;
using System;
using System.Windows;
using System.Windows.Input;
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

		public LauncherWindow2()
		{
			InitializeComponent();
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
						DragStart = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
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
					MainWindow2.Left += System.Windows.Forms.Cursor.Position.X - DragStart.Value.X;
					MainWindow2.Top += System.Windows.Forms.Cursor.Position.Y - DragStart.Value.Y;

					DragStart = new System.Drawing.Point(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
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
				DragStart = null;
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
	}
}
