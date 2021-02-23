using System;
using System.Collections.Generic;
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
using System.Security.Cryptography;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using JocysCom.ClassLibrary.Configuration;

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
			Title = ai.GetTitle(false, false, true,false, false);
			HelpBodyLabel.Text = ai.Description;
		}

		private void GenerateButton_Click(object sender, RoutedEventArgs e)
		{
			var ecdh = GetNewEcdhProvider(384);
			PublicKeyTextBox.Text = System.Convert.ToBase64String(ecdh.Key.Export(CngKeyBlobFormat.EccPublicBlob));
			PrivateKeyTextBox.Text = System.Convert.ToBase64String(ecdh.Key.Export(CngKeyBlobFormat.EccPrivateBlob));
		}

		private void EncryptButton_Click(object sender, RoutedEventArgs e)
		{
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
				var symetricKeyBase64 = System.Convert.ToBase64String(symetricKey);

				var dataBytes = System.Text.Encoding.UTF8.GetBytes(DataTextBox.Text);
				var encryptedBytes = Encrypt(symetricKey, dataBytes);
				var encryptedBase64 = System.Convert.ToBase64String(encryptedBytes);

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

		private void DecryptButton_Click(object sender, RoutedEventArgs e)
		{
			DecryptedTextBox.Text = "Decrypting...";
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
				var symetricKeyBase64 = System.Convert.ToBase64String(symetricKey);

				var encryptedBytes = System.Convert.FromBase64String(OtherEncryptedDataTextBox.Text);
				var decryptedBytes = Decrypt(symetricKey, encryptedBytes);
				var decryptedData = System.Text.Encoding.UTF8.GetString(decryptedBytes);

				DecryptedTextBox.Foreground = System.Windows.SystemColors.ControlTextBrush;
				DecryptedTextBox.Text = decryptedData;
			}
			catch (Exception ex)
			{
				DecryptedTextBox.Foreground = new SolidColorBrush(System.Windows.Media.Colors.DarkRed);
				DecryptedTextBox.Text = ex.Message;
			}
		}

		protected CngKey GetEcdhKey(bool includePrivateParameters)
		{
			CngKey key = null;
			//var format = includePrivateParameters
			//	? CngKeyBlobFormat.EccPrivateBlob
			//	: CngKeyBlobFormat.EccPublicBlob;
			//var keyParams = KeyTextBox.Text;
			//	// Import parameters from BLOB.
			//	var keyBlob = System.Convert.FromBase64String(keyParams);
			//	key = CngKey.Import(keyBlob, format);
			//// Export ECDH key to ECDHParameters and include:
			////    false - Only public key required for encryption.
			////    true  - Private key required for decryption.
			return key;
		}


		System.Security.Cryptography.ECDiffieHellmanCng GetNewEcdhProvider(int dwKeySize = 512)
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
		{
			OpenUrl(e.Uri.AbsoluteUri);
		}

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

		private void PublicKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> UpdateDataControls();

		private void OtherPublicKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> UpdateDataControls();

		private void PrivateKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
			=> UpdateDataControls();

		void UpdateDataControls()
		{
			var enabled =
				!string.IsNullOrEmpty(PrivateKeyTextBox.Text) &&
				!string.IsNullOrEmpty(PublicKeyTextBox.Text) &&
				!string.IsNullOrEmpty(OtherPublicKeyTextBox.Text);
			EncryptButton.IsEnabled = enabled;
			DecryptButton.IsEnabled = enabled;
		}

	}
}
