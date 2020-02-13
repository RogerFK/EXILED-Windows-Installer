using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO;
using Path = System.IO.Path;
using System.IO.Compression;
using System.Diagnostics;

namespace EXILEDWinInstaller
{
	/// <summary>
	/// Interaction logic for DownloadWindow.xaml
	/// </summary>
	public partial class DownloadWindow : Window
	{
		private const string steamCmd = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
		public string TmpDirectory;
		public DownloadWindow()
		{
			InitializeComponent();
			TmpDirectory = Directory.GetCurrentDirectory() + "\\temp\\";
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to cancel?", "Cancel the download", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				System.Windows.Application.Current.Shutdown();
				MainWindow.StopAndCancel();
			}
		}
		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			var move = sender as Rectangle;
			var win = Window.GetWindow(move);
			win.DragMove();
		}
		internal void DownloadGame()
		{
			WebClient webClient = new WebClient();
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
				webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(SteamCmdDownloaded);
				if (Directory.Exists(TmpDirectory))
				{
					Directory.Delete(TmpDirectory, true);
				}
				Directory.CreateDirectory(TmpDirectory);
				
				try
				{
					webClient.DownloadFileAsync(new Uri(steamCmd), TmpDirectory + "steamcmd.zip");

				}
				catch (Exception ex)
				{
					MessageBox.Show("Error (Notify us in #support in our Discord with a screenshot)\n---------------------\n " + ex, "Error");
				}
			}));
		}
		void SteamCmdDownloaded(object sender, AsyncCompletedEventArgs e)
		{
			dlTitleBlock.Text = "Extracting steamcmd...";
			downloadBar.IsIndeterminate = true;
			downloadBar.Value = 30;
			Dispatcher.BeginInvoke(new ThreadStart(() => 
			{
				ZipFile.ExtractToDirectory(TmpDirectory + "steamcmd.zip", TmpDirectory);
				dlTitleBlock.Text = "Installing SCP:SL to " + MainWindow.InstallDir;
				Process process = Process.Start(TmpDirectory + "steamcmd.exe", $"+quit"); //+login anonymous +force_install_dir {MainWindow.InstallDir} +app_update 996560 
				process.WaitForExit();
				dlTitleBlock.Text = "Successfully installed the SCP:SL server in " + MainWindow.InstallDir;
			}));
		}

		internal void DownloadExiled()
		{
			throw new NotImplementedException();
		}

		void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			double bytesIn = e.BytesReceived;
			double totalBytes = e.TotalBytesToReceive;
			double percentage = bytesIn / totalBytes * 100;
			dlProgressInfo.Text = "Downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive;
			downloadBar.Value = percentage;
		}
		void DownloadSL() 
		{
			if(!Directory.Exists(MainWindow.InstallDir))
			{
				Directory.CreateDirectory(MainWindow.InstallDir);
			}
		}
	}
}
