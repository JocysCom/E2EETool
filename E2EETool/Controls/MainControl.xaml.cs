using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Security.Cryptography;
using System.ComponentModel;
using System.IO;
using System.Text;
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
			UpdateShowPrivateKey();
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
				GenerateKeys();
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
			if (e.PropertyName == nameof(AppData.ShowPrivateKey))
				UpdateShowPrivateKey();
		}

		public void GenerateKeys()
		{
			var ecdh = GetNewEcdhProvider(384);
			Global.AppSettings.YourPublicKey = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPublicBlob));
			Global.AppSettings.YourPrivateKey = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPrivateBlob));
		}

		private void GenerateButton_Click(object sender, RoutedEventArgs e)
		{
			GenerateKeys();
		}

		private static RNGCryptoServiceProvider _random = new RNGCryptoServiceProvider();
		private static int _randomSize = 256 / 8;

		public static byte[] AddRandom(byte[] bytes)
		{
			// Append random prefix.
			var ms = new MemoryStream();
			var randomBytes = new byte[_randomSize];
			_random.GetBytes(randomBytes);
			ms.Write(randomBytes);
			ms.Write(bytes);
			var resultBytes = ms.ToArray();
			ms.Dispose();
			return resultBytes;
		}

		public static byte[] RemoveRandom(byte[] bytes)
		{
			var data = new byte[bytes.Length - _randomSize];
			Array.Copy(bytes, _randomSize, data, 0, data.Length);
			return data;
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
				var dataBytes = System.Text.Encoding.UTF8.GetBytes(DataTextBox.Text);
				var encryptedBytes = Encrypt(dataBytes);
				var encryptedBase64 = ToBase64(encryptedBytes);
				// Display the encrypted data.
				EncryptedDataTextBox.Foreground = System.Windows.SystemColors.ControlTextBrush;
				EncryptedDataTextBox.Text = encryptedBase64;
			}
			catch (Exception ex)
			{
				EncryptedDataTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Colors.DarkRed);
				EncryptedDataTextBox.Text = ex.Message;
			}
		}

		private byte[] Encrypt(byte[] dataBytes)
		{
			// Import parameters from BLOB.
			var keyBlob = System.Convert.FromBase64String(PrivateKeyTextBox.Text);
			var privateKey = CngKey.Import(keyBlob, CngKeyBlobFormat.EccPrivateBlob);
			var ecdh = new System.Security.Cryptography.ECDiffieHellmanCng(privateKey);
			// Encrypt the passed byte array.
			var otherPartyKeyBlob = System.Convert.FromBase64String(OtherPublicKeyTextBox.Text);
			var otherPartyPublicKey = CngKey.Import(otherPartyKeyBlob, CngKeyBlobFormat.EccPublicBlob);
			var symetricKey = ecdh.DeriveKeyMaterial(otherPartyPublicKey);
			// Append random prefix.
			dataBytes = AddRandom(dataBytes);
			var encryptedBytes = Encrypt(symetricKey, dataBytes);
			return encryptedBytes;
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
				var encryptedBytes = System.Convert.FromBase64String(OtherEncryptedDataTextBox.Text);
				var decryptedBytes = Decrypt(encryptedBytes);
				var decryptedData = System.Text.Encoding.UTF8.GetString(decryptedBytes);
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

		private byte[] Decrypt(byte[] dataBytes)
		{
			var keyBlob = System.Convert.FromBase64String(PrivateKeyTextBox.Text);
			var privateKey = CngKey.Import(keyBlob, CngKeyBlobFormat.EccPrivateBlob);
			var ecdh = new System.Security.Cryptography.ECDiffieHellmanCng(privateKey);

			// Other key
			var otherPartyKeyBlob = System.Convert.FromBase64String(OtherPublicKeyTextBox.Text);
			var otherPartyPublicKey = CngKey.Import(otherPartyKeyBlob, CngKeyBlobFormat.EccPublicBlob);

			// Decrypt the passed byte array and specify OAEP padding.
			var symetricKey = ecdh.DeriveKeyMaterial(otherPartyPublicKey);
			var symetricKeyBase64 = ToBase64(symetricKey);

			var decryptedBytes = Decrypt(symetricKey, dataBytes);
			// Remove random prefix.
			decryptedBytes = RemoveRandom(decryptedBytes);
			return decryptedBytes;
		}

		ECDiffieHellmanCng GetNewEcdhProvider(int dwKeySize = 512)
		{
			var alg = CngAlgorithm.ECDiffieHellmanP256;
			if (dwKeySize == 384)
				alg = CngAlgorithm.ECDiffieHellmanP384;
			if (dwKeySize == 512)
				alg = CngAlgorithm.ECDiffieHellmanP521;
			var key = CngKey.Create(alg, null, GetKeyParameters());
			var ecdh = new ECDiffieHellmanCng(key);
			// Create a new instance of ECDHCryptoServiceProvider.
			return ecdh;
		}

		CngKeyCreationParameters GetKeyParameters()
		{
			var parameters = new CngKeyCreationParameters();
			parameters.ExportPolicy = CngExportPolicies.AllowPlaintextExport;
			// Tell IIS to use Machine Key store or creation of ECDH service provider will fail.			
			parameters.KeyCreationOptions |= CngKeyCreationOptions.MachineKey;
			return parameters;
		}


		/// <summary>
		/// Encrypt string with AES-256 by using password.
		/// </summary>
		/// <param name="password">String password.</param>
		/// <param name="bytes">Bytes to encrypt.</param>
		/// <returns>Encrypted bytes.</returns>
		public static byte[] Encrypt(byte[] password, byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			var encryptor = GetTransform(password, true);
			var encryptedBytes = CipherStreamWrite(encryptor, bytes);
			encryptor.Dispose();
			// Return encrypted bytes.
			return encryptedBytes;
		}

		/// <summary>
		/// Decrypt string with AES-256 by using password key.
		/// </summary>
		/// <param name="password">String password.</param>
		/// <param name="encryptedBytes">Encrypted bytes.</param>
		/// <returns>Decrypted bytes.</returns>
		public static byte[] Decrypt(byte[] password, byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			var decryptor = GetTransform(password, false);
			var decryptedBytes = CipherStreamWrite(decryptor, bytes);
			decryptor.Dispose();
			// Return encrypted bytes.
			return decryptedBytes;
		}


		private static ICryptoTransform GetTransform(byte[] password, bool encrypt)
		{
			// Create an instance of the AES class. 
			var provider = new AesCryptoServiceProvider();
			// Calculate salt to make it harder to guess key by using a dictionary attack.
			var salt = SaltFromPassword(password);
			// Generate Secret Key from the password and salt.
			// Note: Set number of iterations to 10 in order for JavaScript example to work faster.
			// Rfc2898DeriveBytes generator based on HMACSHA1 by default.
			// Ability to specify HMAC algorithm is available since .NET 4.7.2
			var secretKey = new Rfc2898DeriveBytes(password, salt, 10);
			// 32 bytes (256 bits) for the secret key and
			// 16 bytes (128 bits) for the initialization vector (IV).
			var key = secretKey.GetBytes(provider.KeySize / 8);
			var iv = secretKey.GetBytes(provider.BlockSize / 8);
			secretKey.Dispose();
			// Create a cryptor from the existing SecretKey bytes.
			var cryptor = encrypt
				? provider.CreateEncryptor(key, iv)
				: provider.CreateDecryptor(key, iv);
			return cryptor;
		}

		/// <summary>
		/// Generate salt from password.
		/// </summary>
		/// <param name="password">Password string.</param>
		/// <returns>Salt bytes.</returns>
		private static byte[] SaltFromPassword(byte[] passwordBytes)
		{
			var algorithm = new HMACSHA256();
			algorithm.Key = passwordBytes;
			var salt = algorithm.ComputeHash(passwordBytes);
			algorithm.Dispose();
			return salt;
		}

		/// <summary>
		/// Encrypt/Decrypt with Write method.
		/// </summary>
		/// <param name="cryptor"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		private static byte[] CipherStreamWrite(ICryptoTransform cryptor, byte[] input)
		{
			var inputBuffer = new byte[input.Length];
			// Copy data bytes to input buffer.
			System.Buffer.BlockCopy(input, 0, inputBuffer, 0, inputBuffer.Length);
			// Create a MemoryStream to hold the output bytes.
			// CWE-404: Improper Resource Shutdown or Release
			// Note: False Positive: cryptoStream.Close() will close underlying MemoryStream automatically.
			var stream = new System.IO.MemoryStream();
			// Create a CryptoStream through which we are going to be processing our data.
			var cryptoStream = new CryptoStream(stream, cryptor, CryptoStreamMode.Write);
			// Start the encrypting or decrypting process.
			cryptoStream.Write(inputBuffer, 0, inputBuffer.Length);
			// Finish encrypting or decrypting.
			cryptoStream.FlushFinalBlock();
			// Convert data from a memoryStream into a byte array.
			var outputBuffer = stream.ToArray();
			cryptoStream.Close();
			// Underlying streams will be closed by default.
			//stream.Close();
			return outputBuffer;
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

		public static string ToBase64(byte[] bytes)
		{
			var s = Convert.ToBase64String(bytes);
			s = InsertNewLines(s, 64);
			return s;
		}

		public static byte[] FromBase64(string s)
		{
			return Convert.FromBase64String(s);
		}

		public static string InsertNewLines(string s, int len)
		{
			var sb = new StringBuilder(s.Length + (s.Length / len) + 1);
			int start;
			for (start = 0; start < s.Length - len; start += len)
			{
				sb.Append(s.Substring(start, len));
				sb.Append(Environment.NewLine);
			}
			sb.Append(s.Substring(start));
			return sb.ToString();
		}

		//public static void LoadAndMonitor(INotifyPropertyChanged source, string sourceProperty, System.Windows.Controls.Control control, System.Windows.DependencyProperty controlProperty = null)
		//{
		//	if (controlProperty == null)
		//	{
		//		if (control is System.Windows.Controls.TextBox)
		//			controlProperty = System.Windows.Controls.TextBox.TextProperty;
		//		if (control is System.Windows.Controls.CheckBox)
		//			controlProperty = System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty;
		//		if (control is System.Windows.Controls.ComboBox || control is System.Windows.Controls.ListBox)
		//			controlProperty = System.Windows.Controls.Primitives.Selector.SelectedValueProperty;
		//	}
		//	var binding = new System.Windows.Data.Binding(sourceProperty);
		//	binding.Source = source;
		//	binding.IsAsync = true;
		//	control.SetBinding(controlProperty, binding);
		//}

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
				sourceBytes = FromBase64(sourceBase64);
			}
			else
			{
				sourceBytes = File.ReadAllBytes(sourceFile);
			}
			byte[] targetBytes;
			try
			{
				targetBytes = decrypt
					? Decrypt(sourceBytes)
					: Encrypt(sourceBytes);
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
				var encryptedBase64 = ToBase64(targetBytes);
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
