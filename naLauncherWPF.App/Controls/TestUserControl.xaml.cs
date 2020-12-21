using naLauncherWPF.App.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace naLauncherWPF.App.Controls
{
	public class TestViewModel : ObservableObject
	{
		private Bitmap imageBitmap = null;
		public Bitmap ImageBitmap
		{
			get { return imageBitmap; }
			set
			{
				imageBitmap = value;
				OnPropertyChanged(nameof(GameImageSource));
			}
		}

		public double Top { get; set; }
		public double Left { get; set; }

		public object GameImageSource
		{
			get
			{
				if (imageBitmap == null)
					return null;

				return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
					ImageBitmap.GetHbitmap(),
					IntPtr.Zero,
					Int32Rect.Empty,
					BitmapSizeOptions.FromWidthAndHeight((int)Const.GameImageSize.Width, (int)Const.GameImageSize.Height));
			}
		}
	}

	/// <summary>
	/// Interaction logic for TestUserControl.xaml
	/// </summary>
	public partial class TestUserControl : UserControl
	{
		public TestViewModel ViewModel
		{
			get { return this.DataContext as TestViewModel; }
			set { this.DataContext = value; }
		}

		public TestUserControl()
		{
			InitializeComponent();
		}

		private void ImageMouseEnter(object sender, MouseEventArgs e)
		{

		}

		private void Game_MouseEnter(object sender, MouseEventArgs e)
		{

		}

		private void Game_MouseLeave(object sender, MouseEventArgs e)
		{

		}
	}
}
