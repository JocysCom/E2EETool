using System;
namespace JocysCom.Tools.E2EETool
{
	public class MessageItem
	{
		public MessageType MessageType { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;
		public string Message { get; set; }

		public string DecryptedMessage { get; set; }
		public bool IsOld { get; set; }

		// Make relaxed comparison.
		public bool IsSame(string value)
		{
			return (Message ?? "").Replace("\r", "").Trim() == (value ?? "").Replace("\r", "").Trim();
		}

	}
}
