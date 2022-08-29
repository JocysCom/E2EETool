﻿using MSAA;
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

		public void AddMessage(string message, MessageType type)
		{
			var item = new MessageItem();
			item.Message = message;
			item.MessageType = type;
			Items.Add(item);
		}

		public void ParseMessages(IEnumerable<MsaaItem> list, out List<MessageItem> newItems)
		{
			newItems = new List<MessageItem>();
			if (list == null)
				return;
			foreach (var item in list)
			{
				var name = item.Name;
				var value = item.Value;
				// If message name not found then...
				if (!Items.Any(x => x.Message == name) && TryParse(name, out var byName))
				{
					Items.Add(byName);
					newItems.Add(byName);
				}
				// If message value not found then...
				if (!Items.Any(x => x.Message == value) && TryParse(value, out var byValue))
				{
					Items.Add(byValue);
					newItems.Add(byValue);
				}
			}
		}

		public bool TryParse(string text, out MessageItem item)
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
				// Try to find item from the past.
				var messageItem = Items.FirstOrDefault(x => x.Message == text);
				item.MessageType = messageItem == null
					? MessageType.OtherMessage
					: MessageType.YourMessage;
			}
			catch { }
			// Try parse as public key.
			try
			{
				var key = CngKey.Import(bytes, CngKeyBlobFormat.EccPublicBlob);
				// Try to find item from the past.
				var keyItem = Items.FirstOrDefault(x => x.Message == text);
				item.MessageType = keyItem == null
					? MessageType.OtherPublicKey
					: MessageType.YourPublicKey;
				// Check against your public key.
				if (string.IsNullOrEmpty(Global.AppSettings.YourPublicKey))
				{
					var keyBytes = Security.FromBase64(Global.AppSettings.YourPublicKey);
					if (Enumerable.SequenceEqual(keyBytes, bytes))
						item.MessageType = MessageType.YourPublicKey;
				}
				// Check against other public key.
				if (string.IsNullOrEmpty(Global.AppSettings.OtherPublicKey))
				{
					var keyBytes = Security.FromBase64(Global.AppSettings.OtherPublicKey);
					if (Enumerable.SequenceEqual(keyBytes, bytes))
						item.MessageType = MessageType.OtherPublicKey;
				}
			}
			catch { }
			return true;
		}

	}
}
