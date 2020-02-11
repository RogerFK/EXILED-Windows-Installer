using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
			if (MessageBox.Show("Cancel the download", "Are you sure you want to cancel?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
			{
				MainWindow.StopAndCancel();
			}
		}

		internal void UpdateProgress(double percentage)
		{
			downloadBar.Value = percentage;
		}
	}
}
