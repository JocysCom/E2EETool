using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using System.IO;
using JocysCom.ClassLibrary.Controls;

namespace JocysCom.Tools.E2EETool.Controls
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainControl : UserControl
	{
		public MainControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			Global.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
			UpdateDataControls();
			UpdateFromPropertyChanged(null, true);
			_DelayTimer = new System.Timers.Timer(500);
			_DelayTimer.AutoReset = false;
			_DelayTimer.Elapsed += _DelayTimer_Elapsed;
			FileProcessStatusTextBlock.Text = null;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			if (Global.AppSettings.GenerateKeysOnApplicationStart)
				Security.GenerateKeys();
		}

		void UpdateShowPrivateKey()
		{
			if (Global.AppSettings.ShowPrivateKey)
			{
				PrivateKeyTextBox.Visibility = Visibility.Visible;
				GenerateButton.VerticalAlignment = VerticalAlignment.Center;
				Step1TextBLock.VerticalAlignment = VerticalAlignment.Center;
				Grid.SetRowSpan(GenerateButton, 1);
				Grid.SetRowSpan(Step1TextBLock, 1);
				GenerateButton.Margin = new Thickness(0, 0, 4, 0);
			}
			else
			{
				PrivateKeyTextBox.Visibility = Visibility.Collapsed;
				GenerateButton.VerticalAlignment = VerticalAlignment.Bottom;
				Step1TextBLock.VerticalAlignment = VerticalAlignment.Bottom;
				Grid.SetRowSpan(GenerateButton, 2);
				Grid.SetRowSpan(Step1TextBLock, 2);
				GenerateButton.Margin = new Thickness(0, 0, 4, 4);
			}
		}

		private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			UpdateFromPropertyChanged(e.PropertyName);
		}

		private void UpdateFromPropertyChanged(string propertyName, bool all = false)
		{
			if (all || propertyName == nameof(AppData.ShowPrivateKey))
				UpdateShowPrivateKey();
			if (all || propertyName == nameof(AppData.ShowMessageButtons))
			{
				var visibility = Global.AppSettings.ShowMessageButtons
					? Visibility.Visible
					: Visibility.Collapsed;
				EncryptButton.Visibility = visibility;
				DecryptButton.Visibility = visibility;
			}
		}

		private void GenerateButton_Click(object sender, RoutedEventArgs e)
		{
			Security.GenerateKeys();
		}


		private void Encrypt()
		{
			if (string.IsNullOrEmpty(DataTextBox.Text))
			{
				EncryptedDataTextBox.Text = "";
				return;
			}
			EncryptedDataTextBox.Text = "Encrypting...";
			try
			{
				var encryptedBase64 = Security.Encrypt(DataTextBox.Text);
				// Display the encrypted data.
				EncryptedDataTextBox.Foreground = SystemColors.ControlTextBrush;
				EncryptedDataTextBox.Text = encryptedBase64;
			}
			catch (Exception ex)
			{
				EncryptedDataTextBox.Foreground = new SolidColorBrush(Colors.DarkRed);
				EncryptedDataTextBox.Text = ex.Message;
			}
		}

		private void Decrypt()
		{
			if (string.IsNullOrEmpty(OtherEncryptedDataTextBox.Text))
			{
				OtherDecryptedTextBox.Text = "";
				return;
			}
			OtherDecryptedTextBox.Text = "Decrypting...";
			try
			{
				var decryptedData = Security.Decrypt(OtherEncryptedDataTextBox.Text);
				// Display the decrypted data.
				OtherDecryptedTextBox.Foreground = System.Windows.SystemColors.ControlTextBrush;
				OtherDecryptedTextBox.Text = decryptedData;
			}
			catch (Exception ex)
			{
				OtherDecryptedTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Colors.DarkRed);
				OtherDecryptedTextBox.Text = ex.Message;
			}
		}

		private void CopyPublicKeyButton_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(PublicKeyTextBox.Text, TextDataFormat.Text);
		}

		private void PasteOtherPublicKeyButton_Click(object sender, RoutedEventArgs e)
		{
			OtherPublicKeyTextBox.Text = "";
			OtherPublicKeyTextBox.Text = Clipboard.GetText(TextDataFormat.Text);
		}

		private void PasteEncryptedDataPublicKeyButton_Click(object sender, RoutedEventArgs e)
		{
			OtherEncryptedDataTextBox.Text = "";
			OtherEncryptedDataTextBox.Text = Clipboard.GetText(TextDataFormat.Text);
			DecryptButton_Click(null, null);
		}

		private void CopyEncryptedDataButton_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(EncryptedDataTextBox.Text, TextDataFormat.Text);
		}

		private void PrivateKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!IsLoaded)
				return;
			UpdateDataControls();
			Encrypt();
			Decrypt();
		}

		private void PublicKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!IsLoaded)
				return;
			UpdateDataControls();
			Encrypt();
			Decrypt();
		}

		private void OtherPublicKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!IsLoaded)
				return;
			UpdateDataControls();
			Encrypt();
			Decrypt();
		}

		void UpdateDataControls()
		{
			var enabled =
				!string.IsNullOrEmpty(PrivateKeyTextBox.Text) &&
				!string.IsNullOrEmpty(PublicKeyTextBox.Text) &&
				!string.IsNullOrEmpty(OtherPublicKeyTextBox.Text);
			EncryptButton.IsEnabled = enabled;
			DecryptButton.IsEnabled = enabled;
			EncryptFileButton.IsEnabled = enabled;
			DecryptFileButton.IsEnabled = enabled;
		}

		private void EncryptButton_Click(object sender, RoutedEventArgs e)
			=> Encrypt();

		private void DecryptButton_Click(object sender, RoutedEventArgs e)
			=> Decrypt();

		private void OtherEncryptedDataTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> Decrypt();

		private void EncryptedDataTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
		}

		System.Timers.Timer _DelayTimer;

		private void _DelayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				Encrypt();
			}));
		}

		private void DataTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			_DelayTimer.Stop();
			_DelayTimer.Start();
		}


		private System.Windows.Forms.OpenFileDialog BrowseFileDialog { get; } = new System.Windows.Forms.OpenFileDialog();

		private System.Windows.Forms.OpenFileDialog DecryptFileDialog { get; } = new System.Windows.Forms.OpenFileDialog();

		private System.Windows.Forms.SaveFileDialog EncryptFileDialog { get; } = new System.Windows.Forms.SaveFileDialog();

		private void BrowseFileButton_Click(object sender, EventArgs e)
		{
			var dialog = BrowseFileDialog;
			dialog.SupportMultiDottedExtensions = true;
			dialog.Filter = "All files (*.*)|*.*";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			var fi = Global.AppData.XmlFile;
			if (string.IsNullOrEmpty(dialog.FileName))
				dialog.FileName = System.IO.Path.GetFileNameWithoutExtension(fi.Name);
			if (string.IsNullOrEmpty(dialog.InitialDirectory))
				dialog.InitialDirectory = fi.Directory.FullName;
			dialog.Title = "Browse File...";
			var result = dialog.ShowDialog();
			if (result != System.Windows.Forms.DialogResult.OK)
				return;
			Global.AppSettings.FilePath = dialog.FileName;
			EncryptFileDialog.FileName = null;
			DecryptFileDialog.FileName = null;
		}

		private void DecryptFileButton_Click(object sender, EventArgs e)
		{
			ProcessFile(true);
		}

		private void EncryptFileButton_Click(object sender, EventArgs e)
		{
			ProcessFile(false);
		}

		void ProcessFile(bool decrypt)
		{
			FileProcessStatusTextBlock.Text = "";
			FileProcessStatusTextBlock.Foreground = Step1TextBLock.Foreground;
			// Check source file.
			var sourceFile = Global.AppSettings.FilePath;
			if (string.IsNullOrEmpty(sourceFile))
			{
				MessageBox.Show("Source file path is invalid");
				return;
			}
			var sourceFi = new FileInfo(sourceFile);
			if (!sourceFi.Exists)
			{
				MessageBox.Show("Source file does not exist");
				return;
			}
			// Open dialog.
			var dialog = EncryptFileDialog;
			dialog.SupportMultiDottedExtensions = true;
			dialog.Filter = "Encrypted Base64 File (*.txt)|*.txt|Encrypted Binary File (*.enc)|*.enc|All files (*.*)|*.*";
			dialog.DefaultExt = "*.txt";
			dialog.FilterIndex = 1;
			dialog.RestoreDirectory = true;
			var sourceFilePath = sourceFi.Name;
			if (decrypt)
			{
				if (sourceFi.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
					sourceFi.Name.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
					sourceFilePath = sourceFilePath.Substring(0, sourceFilePath.Length - 4);
			}
			else
			{
				if (!sourceFi.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
					!sourceFi.Name.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
					sourceFilePath = sourceFi.Name + ".txt";
			}
			dialog.FileName = sourceFilePath;
			if (string.IsNullOrEmpty(dialog.InitialDirectory))
				dialog.InitialDirectory = sourceFi.Directory.FullName;
			dialog.Title = decrypt ? "Save Decrypted File" : "Save Encrypted File";
			var result = dialog.ShowDialog();
			if (result != System.Windows.Forms.DialogResult.OK)
				return;
			FileProcessStatusTextBlock.Text = $"{DateTime.Now:HH:MM:ss} " + (decrypt ? "Decrypting..." : "Encrypting...");
			var targetPath = dialog.FileName;
			byte[] sourceBytes;
			// If must decrypt from base64 then...
			if (decrypt && sourceFi.Extension == ".txt")
			{
				var sourceBase64 = File.ReadAllText(sourceFile);
				sourceBytes = Security.FromBase64(sourceBase64);
			}
			else
			{
				sourceBytes = File.ReadAllBytes(sourceFile);
			}
			byte[] targetBytes;
			try
			{
				targetBytes = decrypt
					? Security.Decrypt(sourceBytes)
					: Security.Encrypt(sourceBytes);
			}
			catch (Exception ex)
			{
				FileProcessStatusTextBlock.Text = $"{DateTime.Now:HH:MM:ss} Error: " + ex.Message;
				FileProcessStatusTextBlock.Foreground = new SolidColorBrush(Colors.DarkRed);
				return;
			}
			// If must encrypt to Base64 then...
			if (!decrypt && targetPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
			{
				var encryptedBase64 = Security.ToBase64(targetBytes, Base64HeaderType.Data);
				File.WriteAllText(targetPath, encryptedBase64);
			}
			else
			{
				File.WriteAllBytes(targetPath, targetBytes);
			}
			FileProcessStatusTextBlock.Text = $"{DateTime.Now:HH:MM:ss} File " + (decrypt ? "Decrypted" : "Encrypted");
		}


	}
}
