using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using Path = System.IO.Path;

namespace EXILEDWinInstaller
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		internal static string InstallDir = "C:\\SCP-SL-Server\\"; // defaults to this
		private readonly string ErrorFile = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "error.log");

		private bool mustDownload;
		public static DownloadWindow dlWindow;
		public static WebClient webClient;
		public MainWindow()
		{
			// The constructor is where we check if EXILED is already installed.
			InitializeComponent();
			InstallButton.Click += OnInstallButton;
			RefreshInstallButton();
		}
		private void OnInstallButton(object sender, RoutedEventArgs e)
		{
			dlWindow = new DownloadWindow();
			dlWindow.Show();
			if(mustDownload) 
			{
				dlWindow.DownloadGame();
			}
			else 
			{
				dlWindow.DownloadExiled();
			}
		}

		private void BrowseButton(object sender, RoutedEventArgs e)
		{
			/*var FolderPicker = new CommonOpenFileDialog
			{
				Title = "Pick a folder to install the server to...",
				IsFolderPicker = true,
				DefaultDirectory = InstallDir,
				InitialDirectory = InstallDir,

				AddToMostRecentlyUsedList = false,
				EnsureFileExists = true,
				EnsurePathExists = true,
				EnsureReadOnly = false,
				EnsureValidNames = true,
				Multiselect = false,
				ShowPlacesList = true
			};

			if (FolderPicker.ShowDialog() == CommonFileDialogResult.Ok)
			{
				FileNameTextBox.Text = InstallDir = FolderPicker.FileName;
				RefreshInstallButton();
			}*/
			using (var fldrDlg = new System.Windows.Forms.FolderBrowserDialog())
			{
				//fldrDlg.Filter = "Png Files (*.png)|*.png";
				//fldrDlg.Filter = "Excel Files (*.xls, *.xlsx)|*.xls;*.xlsx|CSV Files (*.csv)|*.csv"

				if (fldrDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					FileNameTextBox.Text = InstallDir = fldrDlg.SelectedPath;
					RefreshInstallButton();
				}
			}
		}


		private void TestingWarn(object sender, RoutedEventArgs e)
		{
			if(sender == testingReleaseCheckbox) {
				if (MessageBox.Show("Are you sure you want to use the latest testing release?\n\nWarning: some EXILED features might be broken. Don't use unless a plugin requires it.",
				"Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					testingReleaseCheckbox.IsChecked = true;
				}
				else
				{
					testingReleaseCheckbox.IsChecked = false;
				}
			}
		}
		// Feel free to suggest a more correct way to do this.
		private void RefreshInstallButton() 
		{
			if (!Directory.Exists(System.IO.Path.Combine(InstallDir, "Managed")) && !System.IO.File.Exists(System.IO.Path.Combine(InstallDir, "SCPSL.exe")))
			{
				forceInstall.IsEnabled = false;
				forceInstall.Foreground = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));
				forceInstall.Opacity = 0.5;
				InstallButton.Content = "Download and\ninstall EXILED";
				mustDownload = true;
				return;
			}

			forceInstall.IsEnabled = true;
			forceInstall.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 240, 255));
			forceInstall.Opacity = 1;
			if (forceInstall.IsChecked ?? false)
			{
				mustDownload = true;
				InstallButton.Content = "Download and\ninstall EXILED";
			}
			else 
			{
				mustDownload = false;
				InstallButton.Content = "Install EXILED";
			}
		}
		private void ClickGitHubLink(object sender, MouseButtonEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start("explorer.exe", @"https://www.github.com/galaxy119/EXILED");
			}
			catch
			{
				MessageBox.Show("We can't open your web explorer for you.\nVisit https://www.github.com/galaxy119/EXILED or search it in Google.");
			}
		}

		private void CheckForceInstallToggle(object sender, RoutedEventArgs e)
		{
			RefreshInstallButton();
		}

		private void FocusCross(object sender, MouseEventArgs e)
		{
			cross.Source = new BitmapImage(new Uri(@"\images\focused_x.png", UriKind.Relative));
		}

		private void UnfocusCross(object sender, MouseEventArgs e)
		{
			cross.Source = new BitmapImage(new Uri(@"\images\unfocused_x.png", UriKind.Relative));
		}

		private void ExitApp(object sender, MouseButtonEventArgs e)
		{
			Application.Current.Shutdown(0);
		}

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			var move = sender as Rectangle;
			var win = Window.GetWindow(move);
			win.DragMove();
		}

		private void FileNameEnter(object sender, KeyEventArgs e)
		{
			if (e.Key != System.Windows.Input.Key.Enter) return;
			switch (FileNameTextBox.Text.ToLower()) {
				case "chiptune":
					FileNameTextBox.Text = InstallDir;
					MessageBox.Show("no chiptune for u");
					return;
			}
			// Yeah, this installer only works locally. Use EXILED_Installer.exe if you can't use a GUI instead.
			if (!Uri.IsWellFormedUriString(FileNameTextBox.Text, UriKind.Absolute) && !FileNameTextBox.Text.StartsWith("http") && !FileNameTextBox.Text.StartsWith("ftp"))
			{
				MessageBox.Show("Invalid path. Please introduce a valid path (for example: C:\\SCPSL\\", "Error");
				FileNameTextBox.Text = InstallDir;
				return;
			}
			InstallDir = FileNameTextBox.Text;
		}

		private void ClickDiscordLink(object sender, MouseButtonEventArgs e)
		{
			System.Diagnostics.Process.Start("explorer.exe", @"https://discord.gg/PyUkWTg");
		}

		internal static void StopAndCancel()
		{
			Application.Current.Shutdown(1);
		}
		internal static void Success()
		{
			if (MessageBox.Show("Do you want to create shortcuts on your Desktop?", "Enjoy!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				CreateShortcut("SCP: Secret Laboratory Server", desktop, InstallDir, "");
				CreateShortcut("EXILED Plugin Folder", desktop, appdata + "\\Plugins\\", "Place all your plugins here.");
				CreateShortcut("EXILED Main Folder", desktop, appdata + "\\EXILED\\", "Place all your plugins here.");
			}
		}
		public static void CreateShortcut(string shortcutName, string shortcutPath, string targetFileLocation, string description, string icon = null)
		{
			string shortcutLocation = Path.Combine(shortcutPath, shortcutName + ".lnk");
			WshShell shell = new WshShell();
			IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);
			shortcut.Description = description;
			if(!string.IsNullOrWhiteSpace(icon)) shortcut.IconLocation = icon;
			
			shortcut.TargetPath = targetFileLocation;
			shortcut.Save();
		}
	}
}
