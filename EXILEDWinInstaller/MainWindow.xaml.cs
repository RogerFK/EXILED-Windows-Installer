using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using File = System.IO.File;
using System.Security;

namespace EXILEDWinInstaller
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static string InstallDir = "C:\\SCP-SL Server\\"; // defaults to this

		private bool mustDownload;
		internal static DownloadWindow dlWindow;
		internal static MainWindow Instance;
		private readonly bool exiledFound;
		private bool MultiAdmin
		{
			get => multiAdminCheckBox.IsChecked ?? false || MultiAdminInstalled;
		}
		private bool MultiAdminInstalled => File.Exists(InstallDir + "MultiAdmin.exe");

		public string HighestCreatedFolder
		{
			get
			{
				Stack<int> indexes = new Stack<int>();
				string currentFolder = InstallDir;
				for (int i = 0; i < currentFolder.Length; i++)
				{
					if (currentFolder[i] == '\\')
					{
						indexes.Push(i);
					}
				}
				while (indexes.Count > 0)
				{
					currentFolder = currentFolder.Substring(0, indexes.Pop());
					if (Directory.Exists(currentFolder))
					{
						return currentFolder + "\\";
					}
				}
				return null;
			}
		}

		public MainWindow()
		{
			// The constructor is where we check if EXILED is already installed.
			if (Instance != null)
			{
				MessageBox.Show("The installer is already running.", "Already running.", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			InitializeComponent();
			InstallButton.Click += OnInstallButton;
			exiledFound = File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\EXILED\\EXILED.dll");
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "-update":
						shortcutButton.IsChecked = false;
						shortcutButton.IsEnabled = false;
						multiAdminCheckBox.IsChecked = false;
						FileNameTextBox.IsReadOnly = true;
						FileNameTextBox.Width = Width - (FileNameTextBox.Margin.Left * 2);
						BrowseXButton.IsEnabled = false;
						BrowseXButton.Content = string.Empty;
						BrowseXButton.Visibility = Visibility.Hidden;
						ShortcutMsg.Inlines.Clear();
						var run = new Run("Can't create shortcuts when updating");
						run.Foreground = new SolidColorBrush(Color.FromArgb(230, 100, 100, 100));
						ShortcutMsg.Inlines.Add(run);
						break;
					case "-dir":
						i++;
						if (i >= args.Length)
						{
							MessageBox.Show("Parameter for -dir not found.\nUsage: -dir \"C:\\My Server Folder\"",
											"No directory", MessageBoxButton.OK, MessageBoxImage.Error);
							break;
						}
						string tmpDir = args[i];
						FileNameTextBox.Text = tmpDir.Trim('"', '\'');
						if (CheckDirectory())
						{
							InstallDir = FileNameTextBox.Text;
						}
						break;
				}
			}
			FileNameTextBox.Text = InstallDir;

			RefreshInstallButton();
			Instance = this;
		}

		private void RefreshMultiAdminText()
		{
			if (MultiAdminInstalled)
			{
				multiAdminCheckBox.Content = "Update MultiAdmin";
			}
			else multiAdminCheckBox.Content = "Download MultiAdmin";
		}

		private void OnInstallButton(object sender, RoutedEventArgs e)
		{
			if (dlWindow != null) return;
			if (CheckDirectory())
			{
				dlWindow = new DownloadWindow(MultiAdmin, InstallDir,
					testingReleaseCheckbox.IsChecked ?? false, mustDownload, shortcutButton.IsChecked ?? false);
				dlWindow.ShowDialog();
			}
		}

		private void BrowseButton(object sender, RoutedEventArgs e)
		{
			using (var folder = new System.Windows.Forms.FolderBrowserDialog())
			{
				if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					if (folder.SelectedPath[folder.SelectedPath.Length - 1] != '\\') folder.SelectedPath += '\\';
					FileNameTextBox.Text = InstallDir = folder.SelectedPath;
					RefreshInstallButton();
				}
			}
		}


		private void TestingWarn(object sender, RoutedEventArgs e)
		{
			if (sender == testingReleaseCheckbox)
			{
				if (MessageBox.Show("Are you sure you want to use the latest testing release?\n\nWarning: some EXILED features might be broken. Don't use unless a plugin requires it.",
				"Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
				InstallButtonText.Inlines.Clear();
				mustDownload = true;
			}
			else
			{
				forceInstall.IsEnabled = true;
				forceInstall.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 240, 255));
				forceInstall.Opacity = 1;
				mustDownload = forceInstall.IsChecked ?? false;
			}
			ChangeText(mustDownload);
			RefreshMultiAdminText();
		}
		private void ChangeText(bool download)
		{
			InstallButtonText.Inlines.Clear();
			if (download)
			{
				Run run = new Run($"{(forceInstall.IsChecked ?? false ? "Update" : "Download")} SCP:SL and install ")
				{
					Foreground = new SolidColorBrush(Color.FromRgb(229, 229, 229))
				};
				InstallButtonText.Inlines.Add(run);
				run = new Run("E")
				{
					Foreground = new SolidColorBrush(Color.FromRgb(214, 51, 48))
				};
				InstallButtonText.Inlines.Add(run);
				run = new Run("XILED")
				{
					Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
				};
				InstallButtonText.Inlines.Add(run);
			}
			else
			{
				Run run = new Run(exiledFound ? "Update " : "Install ")
				{
					Foreground = new SolidColorBrush(Color.FromRgb(229, 229, 229))
				};
				InstallButtonText.Inlines.Add(run);
				run = new Run("E")
				{
					Foreground = new SolidColorBrush(Color.FromRgb(214, 51, 48))
				};
				InstallButtonText.Inlines.Add(run);
				run = new Run("XILED")
				{
					Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
				};
				InstallButtonText.Inlines.Add(run);
			}
		}
		private void ImageGitHubLink(object sender, MouseButtonEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(@"https://www.github.com/galaxy119/EXILED");
			} catch
			{
				MessageBox.Show("We can't open your web explorer for you.\nVisit https://www.github.com/galaxy119/EXILED or search it in Google.", "Can't open link", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void CheckForceInstallToggle(object sender, RoutedEventArgs e) => RefreshInstallButton();

		private void FocusCross(object sender, MouseEventArgs e) => cross.Source = new BitmapImage(new Uri(@"\images\focused_x.png", UriKind.Relative));

		private void UnfocusCross(object sender, MouseEventArgs e) => cross.Source = new BitmapImage(new Uri(@"\images\unfocused_x.png", UriKind.Relative));

		private void ExitApp(object sender, MouseButtonEventArgs e) => Application.Current.Shutdown(0);

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			var move = sender as Rectangle;
			var win = Window.GetWindow(move);
			win.DragMove();
		}

		private void FileNameEnter(object sender, KeyEventArgs e)
		{
			if (e.Key != System.Windows.Input.Key.Enter) return;
			switch (FileNameTextBox.Text.ToLower())
			{
				case "chiptune":
					FileNameTextBox.Text = InstallDir;
					MessageBox.Show("no chiptune for u");
					return;
			}
			CheckDirectory();
		}

		private bool CheckDirectory()
		{
			// Yeah, this installer only works locally. Use EXILED_Installer.exe if you can't use a GUI instead.
			if (!ValidWinPath(FileNameTextBox.Text)
				|| FileNameTextBox.Text[FileNameTextBox.Text.Length - 1] == '.'
				|| FileNameTextBox.Text[FileNameTextBox.Text.Length - 1] == ' '
				|| FileNameTextBox.Text.StartsWith("http")
				|| FileNameTextBox.Text.StartsWith("ftp"))
			{
				MessageBox.Show("Invalid path. Please introduce a valid path\n(Example of a valid path: C:\\SCPSL\\MyServer", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				FileNameTextBox.Text = InstallDir;
				return false;
			}

			// The actual directory
			if (FileNameTextBox.Text[FileNameTextBox.Text.Length - 1] != '\\')
			{
				FileNameTextBox.Text += '\\';
			}
			string oldDir = InstallDir;
			InstallDir = FileNameTextBox.Text;

			// Get the highest folder to check for permissions
			string upmostParentFolder = HighestCreatedFolder;
			if (HighestCreatedFolder == null)
			{
				MessageBox.Show("Invalid path. Please introduce a valid path\n(Example of a valid path: C:\\SCPSL\\MyServer", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				FileNameTextBox.Text = InstallDir = oldDir;
			}
			try
			{
				File.Create(upmostParentFolder + "test.txt");
				File.Delete(upmostParentFolder + "test.txt");
			} catch 
			{
				MessageBox.Show("ERROR: You can't install your server in: " + upmostParentFolder + ".\r\n\r\nIf you feel this is an error, make the folder from the Windows Explorer and select it here.", "Access denied.", MessageBoxButton.OK, MessageBoxImage.Error);
				FileNameTextBox.Text = InstallDir = oldDir;
			}

			RefreshInstallButton();
			return true;
		}

		private bool ValidWinPath(string path)
		{
			// credit: https://stackoverflow.com/questions/24702677/regular-expression-to-match-a-valid-absolute-windows-directory-containing-spaces
			// I don't know any Regex at all but I think the [ .] part doesn't work in C#.
			Regex reg = new Regex("^[a-zA-Z]:\\\\(((?![<>:\"/\\\\|?*]).)+((?<![ .])\\\\)?)*$");
			return reg.IsMatch(path);
		}
		private void ClickDiscordLink(object sender, MouseButtonEventArgs e) => System.Diagnostics.Process.Start(@"https://discord.gg/PyUkWTg");
		private void RentServer(object sender, MouseButtonEventArgs e) => System.Diagnostics.Process.Start(@"https://exiled.host/");

		internal static void EndProgram(int code = 1) => Application.Current.Shutdown(code);

		private void InstallerGitHubLink(object sender, MouseButtonEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(@"https://github.com/RogerFK/EXILED-Windows-Installer");
			} catch
			{
				MessageBox.Show("We can't open your web explorer for you.\nVisit https://github.com/RogerFK/EXILED-Windows-Installer or search it in Google.", "Can't open link", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
