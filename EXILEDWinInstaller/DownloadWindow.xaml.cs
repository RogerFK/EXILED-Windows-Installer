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
using System.Threading.Tasks;
using System.Linq;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace EXILEDWinInstaller
{
	/// <summary>
	/// Interaction logic for DownloadWindow.xaml
	/// </summary>
	public partial class DownloadWindow : Window
	{
		private const string steamCmd = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
		private const string exiledGithub = "https://github.com/galaxy119/EXILED/releases/";
		public string TmpDirectory;
		public string AppData;
		private string InstallDir;
		private bool MultiAdmin;
		public DownloadWindow(bool MultiAdmin, string InstallDir)
		{
			InitializeComponent();
			this.InstallDir = InstallDir;
			this.MultiAdmin = MultiAdmin;
			TmpDirectory = Directory.GetCurrentDirectory() + "\\temp\\";
			AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}

		private void CancelClick(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure you want to cancel?", "Cancel the download", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				System.Windows.Application.Current.Shutdown(1);
				MainWindow.EndProgram();
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
			dlTitleBlock.Text = "Downloading SteamCMD...";
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
				webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(SteamCmdDownloaded);
				if (!Directory.Exists(TmpDirectory + "\\steamcmd\\"))
				{
					Directory.CreateDirectory(TmpDirectory + "\\steamcmd\\");
				}
				else
				{
					if (File.Exists(TmpDirectory + "\\steamcmd\\steamcmd.exe"))
					{
						InstallSCPSL();
						return;
					}
					else if (File.Exists(TmpDirectory + "\\steamcmd\\steamcmd.zip"))
					{
						SteamCmdDownloaded();
						return;
					}
				}

				try
				{
					webClient.DownloadFileAsync(new Uri(steamCmd), TmpDirectory + "\\steamcmd\\steamcmd.zip");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error (Notify us in #support in our Discord with a screenshot)\n---------------------\n " + ex, "Error");
				}
			}));
		}

		async void SteamCmdDownloaded(object sender = null, AsyncCompletedEventArgs e = null)
		{
			dlTitleBlock.Text = "Extracting steamcmd...";
			dlProgressInfo.Text = "This may take a while... Please, don't close the SteamCMD windows.";
			downloadBar.IsIndeterminate = true;
			downloadBar.Value = 30;
			await Task.Run(() => ZipFile.ExtractToDirectory(TmpDirectory + "\\steamcmd\\steamcmd.zip", TmpDirectory + "\\steamcmd\\"));
			InstallSCPSL();
		}
		async void InstallSCPSL()
		{
			dlTitleBlock.Text = "Updating SteamCMD...";
			await Task.Run(() =>
			{
				// Just entering SteamCMD updates it.
				Process process = Process.Start(TmpDirectory + "\\steamcmd\\steamcmd.exe", $"+quit");
				process.WaitForExit();
			});

			dlTitleBlock.Text = "Installing SCP:SL to " + InstallDir;
			if (!Directory.Exists(InstallDir))
			{
				Directory.CreateDirectory(InstallDir);
			}
			await Task.Run(() =>
			{
				Process process = Process.Start(TmpDirectory + "\\steamcmd\\steamcmd.exe", $"+login anonymous +force_install_dir {InstallDir} +app_update 996560 +quit");
				process.WaitForExit();
			});

			dlTitleBlock.Text = "Downloading EXILED...";
			DownloadExiled();
		}

		internal void DownloadExiled()
		{
			WebClient webClient = new WebClient();
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
				webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(ExiledDownloaded);

				if (!Directory.Exists(TmpDirectory + "\\EXILED\\"))
				{
					Directory.CreateDirectory(TmpDirectory + "\\EXILED\\");
				}
				try
				{
					// taken from EXILED_Installer	
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(exiledGithub + "latest");
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();

					Stream stream = response.GetResponseStream();
					StreamReader reader = new StreamReader(stream);
					string read = reader.ReadToEnd();
					string[] readArray = read.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
					string thing = readArray.FirstOrDefault(s => s.Contains("EXILED.tar.gz"));
					string sub = Between(thing, "/galaxy119/EXILED/releases/download/", "/EXILED.tar.gz");
					string path = $"{exiledGithub}download/{sub}/EXILED.tar.gz";

					webClient.DownloadFileAsync(new Uri(path), TmpDirectory + "EXILED.tar.gz");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error (Notify us in #support in our Discord with a screenshot)\n---------------------\n " + ex, "Error");
					Application.Current.Shutdown(1);
				}
			}));
		}
		async void ExiledDownloaded(object sender, AsyncCompletedEventArgs e)
		{
			dlTitleBlock.Text = "Extracting EXILED...";
			dlProgressInfo.Text = "This will be done in a few seconds.";
			downloadBar.IsIndeterminate = true;
			downloadBar.Value = 30;
			await Task.Run(() => ExtractTarGz(TmpDirectory + "EXILED.tar.gz", TmpDirectory + "\\EXILED\\"));
			File.Delete(TmpDirectory + "EXILED.tar.gz");
			dlTitleBlock.Text = "Installing EXILED...";
			await Task.Run(() =>
			{
				try
				{
					string EXILEDtmp = TmpDirectory + "\\EXILED\\";
					SafeMove("Assembly-CSharp.dll", EXILEDtmp, InstallDir + "\\SCPSL_Data\\Managed\\");
					int lengthToRemove = EXILEDtmp.Length;
					foreach (string file in Directory.GetFiles(EXILEDtmp))
					{
						SafeMove(file.Substring(lengthToRemove, file.Length), EXILEDtmp, AppData);
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
					Application.Current.Shutdown();
				}
			});
			if (MultiAdmin)
			{
				DownloadMultiAdmin();
			}
			else Success();
		}
		internal void DownloadMultiAdmin()
		{
			dlTitleBlock.Text = "Downloading MultiAdmin...";
			WebClient webClient = new WebClient();
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
				webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(MultiAdminDownloaded);

				if (!Directory.Exists(TmpDirectory + "\\EXILED\\"))
				{
					Directory.CreateDirectory(TmpDirectory + "\\EXILED\\");
				}
				try
				{
					// taken from EXILED_Installer	
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://github.com/Grover-c13/MultiAdmin/releases/latest");
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();

					Stream stream = response.GetResponseStream();
					StreamReader reader = new StreamReader(stream);
					string read = reader.ReadToEnd();
					string[] readArray = read.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
					string thing = readArray.FirstOrDefault(s => s.Contains("MultiAdmin.exe"));
					string sub = Between(thing, "Grover-c13/MultiAdmin/releases/download/", "/MultiAdmin.exe");
					string path = "https://" + $"github.com/Grover-c13/MultiAdmin/releases/download/{sub}/MultiAdmin.exe";
					if (File.Exists(InstallDir + "MultiAdmin.exe")) File.Delete(InstallDir + "MultiAdmin.exe");

					webClient.DownloadFileAsync(new Uri(path), InstallDir + "MultiAdmin.exe");
				}
				catch (Exception ex)
				{
					MessageBox.Show("MultiAdmin download error (Notify us in #support in our Discord with a screenshot)\n---------------------\n " + ex, "Error");
					Application.Current.Shutdown(1);
				}
			}));
		}

		void MultiAdminDownloaded(object sender, AsyncCompletedEventArgs e)
		{
			Success();
		}

		private async void Success()
		{
			dlTitleBlock.Text = "Successfully installed the SCP:SL server";
			downloadBar.Value = 100;
			downloadBar.IsIndeterminate = false;
			dlProgressInfo.Text = "Installed to:" + InstallDir + "\nClosing this window in a few seconds...";
			await Task.Delay(3000);
			AskForShortcuts();
			MainWindow.EndProgram(0);
		}
		internal static void SafeMove(string fileName, string sourceDir, string destinationDir)
		{
			string path = destinationDir + fileName;
			if (File.Exists(path)) File.Delete(path);
			File.Move(sourceDir + fileName, path);
		}

		void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			double bytesIn = e.BytesReceived;
			double totalBytes = e.TotalBytesToReceive;
			double percentage = bytesIn / totalBytes * 100;
			dlProgressInfo.Text = $"Downloaded {e.BytesReceived}B of {e.TotalBytesToReceive}B";
			downloadBar.Value = percentage;
		}

		private static string Between(string str, string firstString, string lastString)
		{
			int pos1 = str.IndexOf(firstString, StringComparison.Ordinal) + firstString.Length;
			int pos2 = str.IndexOf(lastString, StringComparison.Ordinal);
			string finalString = str.Substring(pos1, pos2 - pos1);
			return finalString;
		}

		private static void ExtractTarGz(string filename, string outputDir)
		{
			using (FileStream stream = File.OpenRead(filename))
			{
				ExtractTarGz(stream, outputDir);
			}
		}

		private static void ExtractTarGz(Stream stream, string outputDir)
		{
			using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
			{
				const int chunk = 4096;
				using (MemoryStream memStr = new MemoryStream())
				{
					int read;
					byte[] buffer = new byte[chunk];
					do
					{
						read = gzip.Read(buffer, 0, chunk);
						memStr.Write(buffer, 0, read);
					} while (read == chunk);

					memStr.Seek(0, SeekOrigin.Begin);
					ExtractTar(memStr, outputDir);
				}
			}
		}

		private static void ExtractTar(Stream stream, string outputDir)
		{
			byte[] buffer = new byte[100];
			while (true)
			{
				try
				{
					stream.Read(buffer, 0, 100);
					string name = Encoding.ASCII.GetString(buffer).Trim('\0');
					if (string.IsNullOrWhiteSpace(name))
						break;
					stream.Seek(24, SeekOrigin.Current);
					stream.Read(buffer, 0, 12);
					long size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

					stream.Seek(376L, SeekOrigin.Current);

					string output = Path.Combine(outputDir, name);
					if (!Directory.Exists(Path.GetDirectoryName(output)))
						Directory.CreateDirectory(Path.GetDirectoryName(output));
					if (!name.Equals("./", StringComparison.InvariantCulture))
					{
						using (FileStream str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
						{
							byte[] buf = new byte[size];
							stream.Read(buf, 0, buf.Length);
							str.Write(buf, 0, buf.Length);
						}
					}

					long pos = stream.Position;

					long offset = 512 - (pos % 512);
					if (offset == 512)
						offset = 0;

					stream.Seek(offset, SeekOrigin.Current);
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}

		internal void AskForShortcuts()
		{
			if (MessageBox.Show("Do you want to create shortcuts on your Desktop?", "Enjoy!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string iconDir = appdata + "\\EXILED\\EXILED.ico";

				CreateIcon(iconDir);

				CreateLaunchBat(desktop);
				CreateShortcut("EXILED Plugin Folder", desktop, appdata + "\\Plugins\\", "Place all your plugins here.", iconDir);
				CreateShortcut("EXILED Main Folder", desktop, appdata + "\\EXILED\\", "Configs and alike will be here.", iconDir);
			}
		}

		private void CreateLaunchBat(string path)
		{
			using (StreamWriter writer = new StreamWriter(path + "\\Launch SCPSL Server.bat"))
			{
				writer.WriteLine("cd /D " + InstallDir);
				writer.WriteLine(MultiAdmin ? "MultiAdmin.exe" : "LocalAdmin.exe");
				writer.Close();
				writer.Dispose();
			}
		}

		public void CreateIcon(string path)
		{
			using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				Properties.Resources.EXILED_ico.Save(file);
			}
		}
		public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation, string description, string icon = null)
		{
			string shortcutLocation = Path.Combine(shortcutPath, shortcutName + ".lnk");
			if (File.Exists(shortcutLocation)) File.Delete(shortcutLocation);
			WshShell shell = new WshShell();
			IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
			shortcut.Description = description;
			if (!string.IsNullOrWhiteSpace(icon)) shortcut.IconLocation = icon;

			shortcut.TargetPath = targetFileLocation;
			shortcut.Save();
		}
	}
}
