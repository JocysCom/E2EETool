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
			Global.AppSettings.YourPublicKey = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPublicBlob),
				Global.AppSettings.AddBase64KeyHeaders ? Base64HeaderType.PublicKey : Base64HeaderType.None);
			Global.AppSettings.YourPrivateKey = ToBase64(ecdh.Key.Export(CngKeyBlobFormat.EccPrivateBlob),
				Global.AppSettings.AddBase64KeyHeaders ? Base64HeaderType.PublicKey : Base64HeaderType.None);
		}

		public static string Encrypt(string message, Base64HeaderType headerType)
		{
			var dataBytes = Encoding.UTF8.GetBytes(message);
			var encryptedBytes = Encrypt(dataBytes);
			var encryptedBase64 = ToBase64(encryptedBytes, headerType);
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

		private static int _randomSize = 256 / 8;

		public static byte[] AddRandom(byte[] bytes)
		{
			// Append random prefix.
			var ms = new MemoryStream();
			var randomBytes = new byte[_randomSize];
			RandomNumberGenerator.Fill(randomBytes);
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
			if (headerType == Base64HeaderType.PublicKey)
				s = "-----BEGIN EC PUBLIC KEY-----\r\n" + s + "\r\n-----END EC PUBLIC KEY-----\r\n";
			if (headerType == Base64HeaderType.PrivateKey)
				s = "-----BEGIN EC PRIVATE KEY-----\r\n" + s + "\r\n-----END EC PRIVATE KEY-----\r\n";
			if (headerType == Base64HeaderType.Data)
				s = "-----BEGIN ENCRYPTED DATA-----\r\n" + s + "\r\n-----END ENCRYPTED DATA-----\r\n";
			if (headerType == Base64HeaderType.Message)
				s = "-----BEGIN ENCRYPTED MESSAGE-----\r\n" + s + "\r\n-----END ENCRYPTED MESSAGE-----\r\n";
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

		// FIPS-approved authenticated encryption:
		//   Key derivation: PBKDF2-HMAC-SHA256, 600,000 iterations (OWASP 2023)
		//   Cipher:         AES-256-GCM (authenticated, no padding, FIPS-approved)
		//   Output layout:  [16 salt][12 nonce][ciphertext][16 tag]
		private const int Pbkdf2Iterations = 600_000;
		private const int SaltSize = 16;
		private const int NonceSize = 12; // AES-GCM standard nonce
		private const int TagSize = 16;   // AES-GCM standard tag
		private const int KeySize = 32;   // AES-256

		/// <summary>
		/// Encrypt bytes with AES-256-GCM using a password-derived key.
		/// </summary>
		public static byte[] Encrypt(byte[] password, byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			// Fresh random salt and nonce for every encryption.
			var salt = RandomNumberGenerator.GetBytes(SaltSize);
			var nonce = RandomNumberGenerator.GetBytes(NonceSize);
			var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);
			var ciphertext = new byte[bytes.Length];
			var tag = new byte[TagSize];
			using (var aes = new AesGcm(key, TagSize))
				aes.Encrypt(nonce, bytes, ciphertext, tag);
			CryptographicOperations.ZeroMemory(key);
			// Pack [salt][nonce][ciphertext][tag].
			var output = new byte[SaltSize + NonceSize + ciphertext.Length + TagSize];
			Buffer.BlockCopy(salt, 0, output, 0, SaltSize);
			Buffer.BlockCopy(nonce, 0, output, SaltSize, NonceSize);
			Buffer.BlockCopy(ciphertext, 0, output, SaltSize + NonceSize, ciphertext.Length);
			Buffer.BlockCopy(tag, 0, output, SaltSize + NonceSize + ciphertext.Length, TagSize);
			return output;
		}

		/// <summary>
		/// Decrypt bytes produced by <see cref="Encrypt(byte[], byte[])"/>.
		/// </summary>
		public static byte[] Decrypt(byte[] password, byte[] bytes)
		{
			if (bytes == null)
				throw new ArgumentNullException(nameof(bytes));
			if (bytes.Length < SaltSize + NonceSize + TagSize)
				throw new CryptographicException("Ciphertext is too short.");
			var cipherLen = bytes.Length - SaltSize - NonceSize - TagSize;
			var salt = new byte[SaltSize];
			var nonce = new byte[NonceSize];
			var ciphertext = new byte[cipherLen];
			var tag = new byte[TagSize];
			Buffer.BlockCopy(bytes, 0, salt, 0, SaltSize);
			Buffer.BlockCopy(bytes, SaltSize, nonce, 0, NonceSize);
			Buffer.BlockCopy(bytes, SaltSize + NonceSize, ciphertext, 0, cipherLen);
			Buffer.BlockCopy(bytes, SaltSize + NonceSize + cipherLen, tag, 0, TagSize);
			var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);
			var plaintext = new byte[cipherLen];
			using (var aes = new AesGcm(key, TagSize))
				aes.Decrypt(nonce, ciphertext, tag, plaintext);
			CryptographicOperations.ZeroMemory(key);
			return plaintext;
		}

	}
}
