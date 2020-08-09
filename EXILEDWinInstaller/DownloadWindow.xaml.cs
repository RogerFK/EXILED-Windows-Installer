using IWshRuntimeLibrary;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

using File = System.IO.File;
using Path = System.IO.Path;

namespace EXILEDWinInstaller
{
	/// <summary>
	/// Interaction logic for DownloadWindow.xaml
	/// </summary>
	public partial class DownloadWindow : Window
	{
		const string STEAMCMD_DOWNLOAD_URL = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";
		const string EXILED_DOWNLOAD_URL = "https://github.com/galaxy119/EXILED/releases/";
		const string MULTIADMIN_DOWNLOAD_URL = "https://github.com/ServerMod/MultiAdmin/releases/latest";

		public static string tempPath = Directory.GetCurrentDirectory() + "\\temp\\";
		public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).TrimEnd('\\');

		private readonly bool multiAdmin;
		private readonly bool testing;
		private readonly bool shortcuts;

		private string installDir;
		private bool userCancelling;

		public DownloadWindow(bool multiAdmin, string installDir, bool testing, bool mustDownload, bool shortcuts)
		{
			InitializeComponent();

			this.installDir = installDir;
			this.multiAdmin = multiAdmin;
			this.testing = testing;
			this.shortcuts = shortcuts;

			try
			{
				if (mustDownload)
				{
					DownloadGame();
				}
				else
				{
					DownloadExiled();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Send this error in #support, and ping RogerFK: \n" + ex, "Unexpected error!");
			}
		}

		/////////////////////////////////////
		//			 UI ELEMENTS		   //
		/////////////////////////////////////
		private void CancelClick(object sender, RoutedEventArgs e)
		{
			userCancelling = true;
			if (MessageBox.Show("Are you sure you want to cancel?", "Cancel the download", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				System.Windows.Application.Current.Shutdown(1);
				MainWindow.EndProgram();
			}
			userCancelling = false;
		}
		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			var move = sender as Rectangle;
			var win = Window.GetWindow(move);
			win.DragMove();
		}
		/////////////////////////////////////
		//		   DOWNLOAD METHODS		   //
		/////////////////////////////////////
		internal void DownloadGame()
		{
			dlTitleBlock.Text = "Downloading SteamCMD...";
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				WebClient webClient = new WebClient();
				webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
				webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(SteamCmdDownloaded);
				if (!Directory.Exists(tempPath + "\\steamcmd\\"))
				{
					Directory.CreateDirectory(tempPath + "\\steamcmd\\");
				}
				else
				{
					if (File.Exists(tempPath + "\\steamcmd\\steamcmd.exe"))
					{
						InstallSCPSL();
						return;
					}
					else if (File.Exists(tempPath + "\\steamcmd\\steamcmd.zip"))
					{
						SteamCmdDownloaded();
						return;
					}
				}

				try
				{
					webClient.DownloadFileAsync(new Uri(STEAMCMD_DOWNLOAD_URL), tempPath + "\\steamcmd\\steamcmd.zip");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error downloading the game\n(Notify us in #support in our Discord with a screenshot)\n---------------------\n " + ex, "Error");
				}
			}));
		}

		private async Task WaitForUser()
		{
			while (userCancelling)
			{
				dlTitleBlock.Text = "Awaiting for your response...";
				dlProgressInfo.Text = "Awaiting for the user's input...";
				await Task.Delay(50);
			}
		}

		async void SteamCmdDownloaded(object sender = null, AsyncCompletedEventArgs e = null)
		{
			await WaitForUser();
			dlTitleBlock.Text = "Extracting steamcmd...";
			dlProgressInfo.Text = "This may take a while... Please, don't close the SteamCMD windows.";
			downloadBar.IsIndeterminate = true;
			downloadBar.Value = 30;
			await Task.Run(() => ZipFile.ExtractToDirectory(tempPath + "\\steamcmd\\steamcmd.zip", tempPath + "\\steamcmd\\"));
			InstallSCPSL();
		}

		async void InstallSCPSL()
		{
			dlTitleBlock.Text = "Updating SteamCMD...";
			await Task.Run(() =>
			{
				// Just entering SteamCMD updates it.
				Process process = Process.Start(tempPath + "\\steamcmd\\steamcmd.exe", $"+quit");
				process.WaitForExit();
			});
			await WaitForUser();
			dlTitleBlock.Text = "Installing SCP:SL to " + installDir;
			if (!Directory.Exists(installDir))
			{
				Directory.CreateDirectory(installDir);
			}
			await Task.Run(() =>
			{
				Process process = Process.Start(tempPath + "\\steamcmd\\steamcmd.exe", $"+login anonymous \"+force_install_dir \\\"{installDir}\\\"\" +app_update 996560 +quit");
				process.WaitForExit();
			});
			await WaitForUser();
			DownloadExiled();
		}

		internal void DownloadExiled()
		{
			dlTitleBlock.Text = "Downloading EXILED...";
			dlProgressInfo.Text = "This will take a second...";
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				WebClient webClient = new WebClient();
				webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
				webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(ExiledDownloaded);
				if (!Directory.Exists(tempPath + "\\EXILED\\"))
				{
					Directory.CreateDirectory(tempPath + "\\EXILED\\");
				}
				try
				{
					string webPage = EXILED_DOWNLOAD_URL + (testing ? "" : "latest");
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webPage);
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();

					Stream stream = response.GetResponseStream();
					StreamReader reader = new StreamReader(stream);
					string read = reader.ReadToEnd();
					string[] readArray = read.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
					string thing = readArray.FirstOrDefault(s => s.Contains("Exiled.tar.gz"));
					string sub = Between(thing, "/galaxy119/EXILED/releases/download/", "/Exiled.tar.gz");
					string path = $"{EXILED_DOWNLOAD_URL}download/{sub}/EXILED.tar.gz";

					webClient.DownloadFileAsync(new Uri(path), tempPath + "EXILED.tar.gz");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error while downloading EXILED\n(Notify us in #support in our Discord with a screenshot)\n---------------------\n " + ex, "Error");
					Application.Current.Shutdown(1);
				}
			}));
		}

		async void ExiledDownloaded(object sender, AsyncCompletedEventArgs e)
		{
			await WaitForUser();
			dlTitleBlock.Text = "Extracting EXILED...";
			dlProgressInfo.Text = "This will be done in a few seconds.";
			downloadBar.IsIndeterminate = true;
			downloadBar.Value = 30;

			await Task.Run(() => ExtractTarGz(tempPath + "EXILED.tar.gz", tempPath + "\\EXILED\\"));
			await WaitForUser();

			File.Delete(tempPath + "EXILED.tar.gz");
			dlTitleBlock.Text = "Installing EXILED...";
			await Task.Run(() =>
			{
				try
				{
					string EXILEDtmp = tempPath + "EXILED\\";
					SafeMove("Assembly-CSharp.dll", EXILEDtmp, installDir + "SCPSL_Data\\Managed\\");
					MoveDirectory(EXILEDtmp, appDataPath);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error while installing EXILED\n(Notify us in #support in our Discord with a screenshot)" + ex.ToString());
					this.Close();
				}
			});
			if (multiAdmin)
			{
				await WaitForUser();
				DownloadMultiAdmin();
			}
			else Success();
		}

		internal void DownloadMultiAdmin()
		{
			dlTitleBlock.Text = "Downloading MultiAdmin...";
			Dispatcher.BeginInvoke(new ThreadStart(() =>
			{
				try
				{
					WebClient webClient = new WebClient();
					webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
					webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(MultiAdminDownloaded);
					if (!Directory.Exists(tempPath + "\\EXILED\\"))
					{
						Directory.CreateDirectory(tempPath + "\\EXILED\\");
					}
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(MULTIADMIN_DOWNLOAD_URL);
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();

					Stream stream = response.GetResponseStream();
					StreamReader reader = new StreamReader(stream);
					string read = reader.ReadToEnd();
					string[] readArray = read.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
					string thing = readArray.FirstOrDefault(s => s.Contains("MultiAdmin.exe"));
					string sub = Between(thing, "ServerMod/MultiAdmin/releases/download/", "/MultiAdmin.exe");
					string path = "https://" + $"github.com/ServerMod/MultiAdmin/releases/download/{sub}/MultiAdmin.exe";
					if (File.Exists(installDir + "MultiAdmin.exe")) File.Delete(installDir + "MultiAdmin.exe");

					webClient.DownloadFileAsync(new Uri(path), installDir + "MultiAdmin.exe");
				}
				catch (Exception ex)
				{
					MessageBox.Show("MultiAdmin download error (Notify us in #support in our Discord with a screenshot)\n---------------------\n " + ex, "Error");
					Application.Current.Shutdown(1);
				}
			}));
		}

		void MultiAdminDownloaded(object sender, AsyncCompletedEventArgs e) => Success();

		private async void Success()
		{
			dlTitleBlock.Text = "Creating shortcuts...";
			downloadBar.Value = 66.0;
			downloadBar.IsIndeterminate = true;
			dlProgressInfo.Text = "This will take a little...";
			await Task.Run(() => CreateShortcuts());
			dlTitleBlock.Text = "Successfully installed.";
			downloadBar.Value = 100;
			downloadBar.IsIndeterminate = false;
			dlProgressInfo.Text = "Installed to: " + installDir + "\nClosing this window in a few seconds...";
			await Task.Delay(3000);
			if (MessageBox.Show("Enjoy your new EXILED server!\nDon't forget to join us on Discord! Do you want to join our Discord now?", "Installed succesfully!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				try { Process.Start("https://discord.gg/PyUkWTg"); }
				catch { Process.Start("explorer.exe", "https://discord.gg/PyUkWTg"); }
			}
			Application.Current.Shutdown(0);
		}

		/////////////////////////////////////
		//		   GENERIC METHODS	       //
		/////////////////////////////////////
		// pretty cool, credit to: https://stackoverflow.com/a/2553245/11000333
		// sligthly modified to make a "copy directory" routine
		public static void MoveDirectory(string source, string target, bool copy = false)
		{
			var sourcePath = source.TrimEnd('\\', ' ');
			var targetPath = target.TrimEnd('\\', ' ');
			var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
								 .GroupBy(s => Path.GetDirectoryName(s));
			foreach (var folder in files)
			{
				var targetFolder = folder.Key.Replace(sourcePath, targetPath);
				if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);
				foreach (var file in folder)
				{
					var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
					if (File.Exists(targetFile)) File.Delete(targetFile);
					if (!copy) File.Move(file, targetFile);
					else if (copy) File.Copy(file, targetFile);
				}
			}
			Directory.Delete(source, true);
		}
		internal static void SafeMove(string fileName, string sourceDir, string destinationDir)
		{
			string path = destinationDir + fileName;
			if (File.Exists(path)) File.Delete(path);
			if (!Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);
			File.Move(sourceDir + fileName, path);
		}

		void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			double bytesIn = e.BytesReceived;
			double totalBytes = e.TotalBytesToReceive;
			double percentage = bytesIn / totalBytes * 100;
			dlProgressInfo.Text = $"Downloaded {(e.BytesReceived / 1000.0):0.000}KB of {(e.TotalBytesToReceive / 1000.0):0.000)}KB";
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

		internal void CreateShortcuts()
		{
			if (shortcuts)
			{
				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				string iconDir = appDataPath + "\\EXILED\\EXILED.ico";
				string launchIconDir = installDir + "EXILEDLauncher.ico";
				string updateIconDir = appDataPath + "\\EXILED\\EXILED Installer\\UpdateExiled.ico";
				if(!Directory.Exists(appDataPath + "\\EXILED\\EXILED Installer\\")) {
					Directory.CreateDirectory(appDataPath + "\\EXILED\\EXILED Installer\\");
				}
				try
				{
					CreateIcon(iconDir, Properties.Resources.EXILED);
					CreateIcon(launchIconDir, Properties.Resources.EXILEDLauncher);
					CreateIcon(updateIconDir, Properties.Resources.UpdateEXILED);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error while creating the icons (send this to RogerFK!): " + ex);
				}
				try
				{
					CreateLaunchBat(installDir);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error while creating the launch.bat file (send this to RogerFK!): " + ex);
				}
				try
				{
					CreateUpdateBat(appDataPath + "\\EXILED\\");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error while creating the update.bat file (send this to RogerFK!): " + ex);
				}
				try
				{
					CreateShortcut("EXILED Plugin Folder", desktop, appDataPath + "\\Plugins\\", "Place all your plugins here.", iconDir);
					CreateShortcut("EXILED Main Folder", desktop, appDataPath + "\\EXILED\\", "Configs and alike will be here.", iconDir);
					CreateShortcut("Launch SCPSL Server", desktop, installDir + "launch.bat", "Launch your SCP:SL Server", launchIconDir);
					CreateShortcut("Update SCPSL Server", desktop, appDataPath + "\\EXILED\\update.bat", "Update your SCP:SL Server, EXILED or MultiAdmin", updateIconDir);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error while creating the Desktop shortcuts (send this to RogerFK!): " + ex);
				}
			}
		}

		private void CreateUpdateBat(string path)
		{
			string exiledInstallerFiles = path + "EXILED Installer\\";
			if (!Directory.Exists(exiledInstallerFiles))
			{
				MoveDirectory(tempPath, exiledInstallerFiles + "temp", true);
				string updaterLocation = exiledInstallerFiles + "installer.exe";
				if (File.Exists(updaterLocation)) File.Delete(updaterLocation);
				File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, exiledInstallerFiles + "installer.exe");
				if (File.Exists(appDataPath + "\\EXILED\\update.bat")) File.Delete(appDataPath + "\\EXILED\\update.bat");
				File.WriteAllText(appDataPath + "\\EXILED\\update.bat",
				"@echo off\n"
					+ ":: Auto-generated by EXILED Installer for Windows.\n"
					+ ":: We encourage you to not edit/delete this file unless you really need to.\n"
					+ $":: You may edit the {(multiAdmin ? "MultiAdmin" : "LocalAdmin")} input parameters like so:\n"
					+ $":: {(multiAdmin ? "MultiAdmin.exe" : "LocalAdmin.exe")} -yourParameter1 -yourParameter2 1234\n\n"
					+ $"\"%~dp0EXILED Installer\\installer.exe\" -update -dir \"{installDir}\"");
			}
		}

		private void CreateLaunchBat(string path)
		{
			using (StreamWriter writer = new StreamWriter(path + "launch.bat"))
			{
				writer.Write("@echo off\n"
					+ ":: Auto-generated by EXILED Installer for Windows.\n"
					+ ":: Please, don't move or remove this file or your shortcuts will malfunction.\n"
					+ $":: You may edit the {(multiAdmin ? "MultiAdmin" : "LocalAdmin")} input parameters like so:\n"
					+ $":: {(multiAdmin ? "MultiAdmin.exe" : "LocalAdmin.exe")} -yourParameter1 -yourParameter2 1234\n\n"
					+ "cd /D %~dp0\n"
					+ (multiAdmin ? "MultiAdmin.exe" : "LocalAdmin.exe"));
				writer.Close();
			}
		}

		public void CreateIcon(string path, System.Drawing.Icon icon)
		{
			using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				icon.Save(file);
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
