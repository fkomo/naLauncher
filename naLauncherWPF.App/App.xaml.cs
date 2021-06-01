using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using Ujeby.Common.Tools;

namespace naLauncherWPF.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		App()
		{
			StartupUri = new System.Uri($"{ naLauncherWPF.App.Properties.Settings.Default.WindowVersion }.xaml", System.UriKind.Relative);
		}

		private static string UserDataFolder
		{
			get
			{
				var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var userDataFolder = Path.Combine(roaming, "Ujeby", "naLauncher" + (Debugger.IsAttached ? "-debug" : null));

				if (!Directory.Exists(userDataFolder))
					Directory.CreateDirectory(userDataFolder);

				return userDataFolder;
			}
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;
			ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

			Log.LogFileName = "naLauncher.log";
			Log.LogFolder = UserDataFolder;

			GameLibrary.GameLibrary.Initialize();
		}
	}
}
