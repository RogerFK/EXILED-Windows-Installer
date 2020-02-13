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

namespace EXILEDWinInstaller
{
	/// <summary>
	/// Interaction logic for DownloadWindow.xaml
	/// </summary>
	public partial class DownloadWindow : Window
	{
		public DownloadWindow()
		{
			InitializeComponent();
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to cancel?", "Cancel the download", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				System.Windows.Application.Current.Shutdown();
				MainWindow.StopAndCancel();
			}
		}

		internal void UpdateProgress(double percentage)
		{
			downloadBar.Value = percentage;
		}

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			var move = sender as Rectangle;
			var win = Window.GetWindow(move);
			win.DragMove();
		}
		public void Download(string url)
		{
			WebClient webClient = new WebClient();
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				// webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler();
				if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\temp\\steamcmd\\"))
				{
					Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\temp\\steamcmd\\");
				}
				try
				{
					webClient.DownloadFileAsync(new Uri(url), Directory.GetCurrentDirectory() + "\\temp\\steamcmd\\steamcmd.zip");

				}
				catch (Exception ex)
				{
					MessageBox.Show("Error (Notify the developer with a screenshot)\n---------------------\n " + ex);
				}
			}));
			webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCompleted);
		}
		void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			downloadBar.Value = 100;
		}
	}
}
