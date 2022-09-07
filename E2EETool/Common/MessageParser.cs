using MSAA;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace JocysCom.Tools.E2EETool
{
	public class MessageParser
	{
		public Regex Base64Regex = new Regex("^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$");
		public List<MessageItem> Items = new List<MessageItem>();

		/// <summary>
		/// Add incomming messages manually.
		/// </summary>
		public void AddMessage(string message, MessageType type)
		{
			var item = new MessageItem();
			item.Message = message;
			item.IsOld = true;
			item.MessageType = type;
			Items.Add(item);
		}

		public void ParseMessages(IEnumerable<MsaaItem> list, out List<MessageItem> newItems)
		{
			_ParseMessages(list, out newItems);
			// If this collection of old skype items not complete yet then...
			if (!OldItemsComplete)
				OldItemsComplete = true;
		}

		private void _ParseMessages(IEnumerable<MsaaItem> list, out List<MessageItem> newItems)
		{
			newItems = new List<MessageItem>();
			if (list == null)
				return;
			foreach (var item in list)
			{
				var name = item.Name;
				var value = item.Value;
				// If message name not found then...
				if (!Items.Any(x => x.IsSame(name)) && TryParse(name, out var byName))
				{
					Items.Add(byName);
					if (OldItemsComplete)
						newItems.Add(byName);
				}
				// Skip if 'name' and 'value' values are the same.
				if (name != value)
					continue;
				// If message value not found then...
				if (!Items.Any(x => x.IsSame(value)) && TryParse(value, out var byValue))
				{
					Items.Add(byValue);
					if (OldItemsComplete)
						newItems.Add(byValue);
				}
			}
		}

		private bool TryParse(string text, out MessageItem item)
		{
			// By default it will be plain message.
			item = new MessageItem();
			item.Message = text;
			item.MessageType = MessageType.None;
			// If null then no need to parse further.
			if (string.IsNullOrEmpty(text))
				return true;
			byte[] bytes;
			// Try base 64 first.
			try
			{
				bytes = Security.FromBase64(text);
			}
			catch
			{
				// No need to parse further.
				return true;
			}
			if (bytes == null)
				return true;
			// Try parse as text message.
			try
			{
				var textBytes = Security.Decrypt(bytes);
				item.MessageType = MessageType.YourMessage;
				item.DecryptedMessage = System.Text.Encoding.UTF8.GetString(textBytes);
				// Try to find item from the past.
				var messageItem = Items.FirstOrDefault(x => x.Message == text);
				item.MessageType = messageItem == null
					? MessageType.OtherMessage
					: MessageType.YourMessage;
				return true;
			}
			catch { }
			// Try parse as public key.
			try
			{
				var key = CngKey.Import(bytes, CngKeyBlobFormat.EccPublicBlob);
				// Check against your public key.
				if (string.IsNullOrEmpty(Global.AppSettings.YourPublicKey))
				{
					var keyBytes = Security.FromBase64(Global.AppSettings.YourPublicKey);
					if (Enumerable.SequenceEqual(keyBytes, bytes))
					{
						item.MessageType = MessageType.YourPublicKey;
						return true;
					}
				}
				// Check against other public key.
				if (string.IsNullOrEmpty(Global.AppSettings.OtherPublicKey))
				{
					var keyBytes = Security.FromBase64(Global.AppSettings.OtherPublicKey);
					if (Enumerable.SequenceEqual(keyBytes, bytes))
					{
						item.MessageType = MessageType.OtherPublicKey;
						return true;
					}
				}
				// Try to find most recent item from the past.
				var keyItem = Items
					.OrderByDescending(x => x.Created)
					.FirstOrDefault(x => x.Message == text);
				item.MessageType = keyItem == null
					// Assume this is the key from the other side.
					? MessageType.OtherPublicKey
					: keyItem.MessageType;
			}
			catch { }
			return true;
		}

		/// <summary>
		///  Will be used to skip old items on chat window.
		/// </summary>
		public bool OldItemsComplete = false;

		public void Reset()
		{
			OldItemsComplete = false;
			Items.Clear();
		}

	}
}
