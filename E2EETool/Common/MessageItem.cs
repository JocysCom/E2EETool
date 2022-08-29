using System;
namespace JocysCom.Tools.E2EETool
{
	public class MessageItem
	{
		public MessageType MessageType { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;
		public string Message { get; set; }
	}
}
