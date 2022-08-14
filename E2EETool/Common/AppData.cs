using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.Tools.E2EETool
{
	public class AppData : JocysCom.ClassLibrary.Configuration.ISettingsItem, INotifyPropertyChanged
	{
		public bool Enabled { get; set; }

		public bool IsEmpty => false;

		#region Whois

		public bool AlwaysOnTop { get => _AlwaysOnTop; set => SetProperty(ref _AlwaysOnTop, value); }
		private bool _AlwaysOnTop;

		public bool ShowPrivateKey { get => _ShowPrivateKey; set => SetProperty(ref _ShowPrivateKey, value); }
		private bool _ShowPrivateKey;

		[DefaultValue(true)]
		public bool GenerateKeysOnApplicationStart { get => _GenerateKeysOnApplicationStart; set => SetProperty(ref _GenerateKeysOnApplicationStart, value); }
		private bool _GenerateKeysOnApplicationStart = true;

		public bool SaveKeysOnApplicationClose { get => _SaveKeysOnApplicationClose; set => SetProperty(ref _SaveKeysOnApplicationClose, value); }
		private bool _SaveKeysOnApplicationClose;

		public string YourPublicKey { get => _YourPublicKey; set => SetProperty(ref _YourPublicKey, value); }
		private string _YourPublicKey;
		public string YourPrivateKey { get => _YourPrivateKey; set => SetProperty(ref _YourPrivateKey, value); }
		private string _YourPrivateKey;

		public string OtherPublicKey { get => _OtherPublicKey; set => SetProperty(ref _OtherPublicKey, value); }
		private string _OtherPublicKey;

		public string FilePath { get => _FilePath; set => SetProperty(ref _FilePath, value); }
		private string _FilePath;

		#endregion

		#region ■ INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
		{
			property = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
