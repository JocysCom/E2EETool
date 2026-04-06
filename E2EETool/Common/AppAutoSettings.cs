using JocysCom.ClassLibrary.Configuration;
using System.ComponentModel;

namespace JocysCom.Tools.E2EETool
{
	public class AppAutoSettings : SettingsItem, INotifyPropertyChanged
	{
		public string Title { get => _Title; set => SetProperty(ref _Title, value); }
		private string _Title;

		/// <summary>
		/// XML path where app can check for chat messages.
		/// </summary>
		public string ChatPath { get => _ChatPath; set => SetProperty(ref _ChatPath, value); }
		private string _ChatPath;
	
	}
}
