using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ujeby.Common.Tools;

namespace naLauncherWPF.App
{
	/// <summary>
	/// Interaction logic for LoadingWindow.xaml
	/// </summary>
	public partial class LoadingWindow : Window
	{
		public LoadingWindow()
		{
			Log.WriteLineDebug("LoadingWindow()");

			InitializeComponent();
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			Log.WriteLineDebug("LoadingWindow.ContentRendered()");

			GameLibrary.GameLibrary.Initialize();

			var launcherWindow = new LauncherWindow2();

			this.Hide();
			Log.WriteLineDebug("LoadingWindow.Hide()");

			launcherWindow.ShowDialog();

			this.Close();
			Log.WriteLineDebug("LoadingWindow.Close()");
		}
	}
}
