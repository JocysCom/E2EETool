using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace JocysCom.Tools.E2EETool
{
	internal static class Security
	{
		private static ECDiffieHellmanCng GetNewEcdhProvider(int dwKeySize = 512)
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

		private static CngKeyCreationParameters GetKeyParameters()
		{
			var parameters = new CngKeyCreationParameters();
			parameters.ExportPolicy = CngExportPolicies.AllowPlaintextExport;
			// Tell IIS to use Machine Key store or creation of ECDH service provider will fail.			
			parameters.KeyCreationOptions |= CngKeyCreationOptions.MachineKey;
			return parameters;
		}

		public static void GenerateKeys()
		{
			var ecdh = GetNewEcdhProvider(384);
			Global.AppSettings.YourPublicKey = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPublicBlob), Base64HeaderType.PublicKey);
			Global.AppSettings.YourPrivateKey = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPrivateBlob), Base64HeaderType.PrivateKey);
		}

		public static string Encrypt(string message)
		{
			var dataBytes = Encoding.UTF8.GetBytes(message);
			var encryptedBytes = Encrypt(dataBytes);
			var encryptedBase64 = ToBase64(encryptedBytes, Base64HeaderType.Message);
			return encryptedBase64;
		}

		public static byte[] Encrypt(byte[] dataBytes)
		{
			// Import parameters from BLOB.
			var keyBlob = FromBase64(Global.AppSettings.YourPrivateKey);
			var privateKey = CngKey.Import(keyBlob, CngKeyBlobFormat.EccPrivateBlob);
			var ecdh = new System.Security.Cryptography.ECDiffieHellmanCng(privateKey);
			// Encrypt the passed byte array.
			var otherPartyKeyBlob = FromBase64(Global.AppSettings.OtherPublicKey);
			var otherPartyPublicKey = CngKey.Import(otherPartyKeyBlob, CngKeyBlobFormat.EccPublicBlob);
			var symetricKey = ecdh.DeriveKeyMaterial(otherPartyPublicKey);
			// Append random prefix.
			dataBytes = AddRandom(dataBytes);
			var encryptedBytes = Encrypt(symetricKey, dataBytes);
			return encryptedBytes;
		}

		public static string Decrypt(string base64)
		{
			var encryptedBytes = Security.FromBase64(base64);
			var decryptedBytes = Security.Decrypt(encryptedBytes);
			var decryptedData = System.Text.Encoding.UTF8.GetString(decryptedBytes);
			return decryptedData;
		}

		public static byte[] Decrypt(byte[] dataBytes)
		{
			var keyBlob = FromBase64(Global.AppSettings.YourPrivateKey);
			var privateKey = CngKey.Import(keyBlob, CngKeyBlobFormat.EccPrivateBlob);
			var ecdh = new System.Security.Cryptography.ECDiffieHellmanCng(privateKey);

			// Other key
			var otherPartyKeyBlob = FromBase64(Global.AppSettings.OtherPublicKey);
			var otherPartyPublicKey = CngKey.Import(otherPartyKeyBlob, CngKeyBlobFormat.EccPublicBlob);

			// Decrypt the passed byte array and specify OAEP padding.
			var symetricKey = ecdh.DeriveKeyMaterial(otherPartyPublicKey);

			var decryptedBytes = Decrypt(symetricKey, dataBytes);
			// Remove random prefix.
			decryptedBytes = RemoveRandom(decryptedBytes);
			return decryptedBytes;
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

		public static byte[] FromBase64(string s)
		{
			// Remove headers.
			var lines = s
				.Split(new[] { '\r', '\n' })
				.Where(x => !string.IsNullOrEmpty(x))
				.Where(x => !x.StartsWith("-----"))
				.ToArray();
			s = string.Join("", lines);
			return Convert.FromBase64String(s);
		}

		public static string ToBase64(byte[] bytes, Base64HeaderType headerType)
		{
			var s = Convert.ToBase64String(bytes);
			s = InsertNewLines(s, 64);
			if (headerType == Base64HeaderType.PublicKey && Global.AppSettings.AddBase64KeyHeaders)
			{
				s = "-----BEGIN EC PUBLIC KEY-----\r\n" + s + "\r\n-----END EC PUBLIC KEY-----\r\n";
			}
			if (headerType == Base64HeaderType.PrivateKey && Global.AppSettings.AddBase64KeyHeaders)
			{
				s = "-----BEGIN EC PRIVATE KEY-----\r\n" + s + "\r\n-----END EC PRIVATE KEY-----\r\n";
			}
			if (headerType == Base64HeaderType.Data && Global.AppSettings.AddBase64FileHeaders)
			{
				s = "-----BEGIN ENCRYPTED DATA-----\r\n" + s + "\r\n-----END ENCRYPTED DATA-----\r\n";
			}
			if (headerType == Base64HeaderType.Message && Global.AppSettings.AddBase64MessageHeaders)
			{
				s = "-----BEGIN ENCRYPTED MESSAGE-----\r\n" + s + "\r\n-----END ENCRYPTED MESSAGE-----\r\n";
			}
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


	}
}
