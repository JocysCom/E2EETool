using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Security.Cryptography;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using JocysCom.ClassLibrary.Configuration;
using System.IO;
using System.Text;

namespace JocysCom.Tools.E2EETool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			var s = Properties.Settings.Default;
			LoadAndMonitor(s, nameof(s.IsAlwaysOnTop), AlwaysOnTopCheckBox);
			LoadAndMonitor(s, nameof(s.ShowPrivateKey), ShowPrivateKeyCheckBox);
			ShowPrivateKeyCheckBox_Checked(null, null);
			AlwaysOnTopCheckBox_Checked(null, null);
			UpdateDataControls();
			var ai = new AssemblyInfo();
			Title = ai.GetTitle(false, false, true, false, false);
			HelpBodyLabel.Text = ai.Description;
			_DelayTimer = new System.Timers.Timer(500);
			_DelayTimer.AutoReset = false;
			_DelayTimer.Elapsed += _DelayTimer_Elapsed;
		}

		private void GenerateButton_Click(object sender, RoutedEventArgs e)
		{
			var ecdh = GetNewEcdhProvider(384);
			PublicKeyTextBox.Text = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPublicBlob));
			PrivateKeyTextBox.Text = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPrivateBlob));
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
				// Import parameters from BLOB.
				var keyBlob = System.Convert.FromBase64String(PrivateKeyTextBox.Text);
				var privateKey = CngKey.Import(keyBlob, CngKeyBlobFormat.EccPrivateBlob);
				var ecdh = new System.Security.Cryptography.ECDiffieHellmanCng(privateKey);

				// Encrypt the passed byte array.
				var otherPartyKeyBlob = System.Convert.FromBase64String(OtherPublicKeyTextBox.Text);
				var otherPartyPublicKey = CngKey.Import(otherPartyKeyBlob, CngKeyBlobFormat.EccPublicBlob);

				var symetricKey = ecdh.DeriveKeyMaterial(otherPartyPublicKey);
				var symetricKeyBase64 = ToBase64(symetricKey);

				var dataBytes = System.Text.Encoding.UTF8.GetBytes(DataTextBox.Text);
				// Append random prefix.
				dataBytes = AddRandom(dataBytes);
				var encryptedBytes = Encrypt(symetricKey, dataBytes);
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
				var keyBlob = System.Convert.FromBase64String(PrivateKeyTextBox.Text);
				var privateKey = CngKey.Import(keyBlob, CngKeyBlobFormat.EccPrivateBlob);
				var ecdh = new System.Security.Cryptography.ECDiffieHellmanCng(privateKey);

				// Other key
				var otherPartyKeyBlob = System.Convert.FromBase64String(OtherPublicKeyTextBox.Text);
				var otherPartyPublicKey = CngKey.Import(otherPartyKeyBlob, CngKeyBlobFormat.EccPublicBlob);

				// Decrypt the passed byte array and specify OAEP padding.
				var symetricKey = ecdh.DeriveKeyMaterial(otherPartyPublicKey);
				var symetricKeyBase64 = ToBase64(symetricKey);

				var encryptedBytes = System.Convert.FromBase64String(OtherEncryptedDataTextBox.Text);
				var decryptedBytes = Decrypt(symetricKey, encryptedBytes);
				// Remove random prefix.
				decryptedBytes = RemoveRandom(decryptedBytes);
				var decryptedData = System.Text.Encoding.UTF8.GetString(decryptedBytes);

				OtherDecryptedTextBox.Foreground = System.Windows.SystemColors.ControlTextBrush;
				OtherDecryptedTextBox.Text = decryptedData;
			}
			catch (Exception ex)
			{
				OtherDecryptedTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Colors.DarkRed);
				OtherDecryptedTextBox.Text = ex.Message;
			}
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

		private void AlwaysOnTopCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			Topmost = AlwaysOnTopCheckBox.IsChecked ?? false;
		}

		private void ShowPrivateKeyCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			var show = ShowPrivateKeyCheckBox.IsChecked ?? false;
			if (show)
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

		private void HyperLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
		=> OpenUrl(e.Uri.AbsoluteUri);

		public void OpenUrl(string url)
		{
			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					Process.Start("xdg-open", url);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", url);
				}
				else
				{
					// throw 
				}
			}
			catch (System.ComponentModel.Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
					MessageBox.Show(noBrowser.Message);
			}
			catch (System.Exception other)
			{
				MessageBox.Show(other.Message);
			}
		}

		public static string ToBase64(byte[] bytes)
		{
			var s = Convert.ToBase64String(bytes);
			s =  InsertNewLines(s, 64);
			return s;
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

		public static void LoadAndMonitor(INotifyPropertyChanged source, string sourceProperty, System.Windows.Controls.Control control, System.Windows.DependencyProperty controlProperty = null)
		{
			if (controlProperty == null)
			{
				if (control is System.Windows.Controls.TextBox)
					controlProperty = System.Windows.Controls.TextBox.TextProperty;
				if (control is System.Windows.Controls.CheckBox)
					controlProperty = System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty;
				if (control is System.Windows.Controls.ComboBox || control is System.Windows.Controls.ListBox)
					controlProperty = System.Windows.Controls.Primitives.Selector.SelectedValueProperty;
			}
			var binding = new System.Windows.Data.Binding(sourceProperty);
			binding.Source = source;
			binding.IsAsync = true;
			control.SetBinding(controlProperty, binding);
		}

		private void PrivateKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateDataControls();
			Encrypt();
			Decrypt();
		}

		private void PublicKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			UpdateDataControls();
			Encrypt();
			Decrypt();
		}

		private void OtherPublicKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
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
		}

		private void Window_Closing(object sender, CancelEventArgs e)
			=> Properties.Settings.Default.Save();

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
	}
}
