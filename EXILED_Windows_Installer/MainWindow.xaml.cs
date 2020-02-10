using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace EXILED_Windows_Installer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public string InstallDir = "C:\\SCP-SL-Server\\Managed\\";
		private readonly string ErrorFile = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "error.log");
		public MainWindow()
		{
			// The constructor is where we check if EXILED is already installed.
			InitializeComponent();
			RefreshInstallButton();
		}

		private void OnInstallButton(object sender, RoutedEventArgs e)
		{

		}

		private void BrowseButton(object sender, RoutedEventArgs e)
		{
			var FolderPicker = new CommonOpenFileDialog
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
		private void RefreshInstallButton() {
			if (Directory.Exists(System.IO.Path.Combine(InstallDir, "Managed")) && File.Exists(System.IO.Path.Combine(InstallDir, "\\SCPSL.exe")))
			{
				InstallButton.Content = "Install EXILED";
			}
			else
			{
				InstallButton.Content = "Download and\ninstall EXILED";
			}
		}

		private void ClickGitHubLink(object sender, MouseButtonEventArgs e)
		{
			try
			{
				// lol https://github.com/dotnet/runtime/issues/28005
				System.Diagnostics.Process.Start("explorer.exe", @"https://www.github.com/galaxy119/EXILED");
			}
			catch (Exception ex)
			{
				File.AppendAllText(ErrorFile, ex.ToString());
			}
		}
	}
}
