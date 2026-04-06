using JocysCom.ClassLibrary.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace JocysCom.Tools.E2EETool
{
	public class AppData : SettingsItem, INotifyPropertyChanged
	{

		#region Main Properties

		public bool AlwaysOnTop { get => _AlwaysOnTop; set => SetProperty(ref _AlwaysOnTop, value); }
		private bool _AlwaysOnTop;

		public bool ShowPrivateKey { get => _ShowPrivateKey; set => SetProperty(ref _ShowPrivateKey, value); }
		private bool _ShowPrivateKey;
		
		public bool ShowMessageButtons { get => _ShowMessageButtons; set => SetProperty(ref _ShowMessageButtons, value); }
		private bool _ShowMessageButtons;

		[DefaultValue(true)]
		public bool GenerateKeysOnApplicationStart { get => _GenerateKeysOnApplicationStart; set => SetProperty(ref _GenerateKeysOnApplicationStart, value); }
		private bool _GenerateKeysOnApplicationStart = false;

		[DefaultValue(true)]
		public bool SaveKeysOnApplicationClose { get => _SaveKeysOnApplicationClose; set => SetProperty(ref _SaveKeysOnApplicationClose, value); }
		private bool _SaveKeysOnApplicationClose = true;

		[XmlElement(DataType =  "normalizedString")]
		public string YourPublicKey { get => _YourPublicKey; set => SetProperty(ref _YourPublicKey, value); }
		private string _YourPublicKey;
		public string YourPrivateKey { get => _YourPrivateKey; set => SetProperty(ref _YourPrivateKey, value); }
		private string _YourPrivateKey;

		public string OtherPublicKey { get => _OtherPublicKey; set => SetProperty(ref _OtherPublicKey, value); }
		private string _OtherPublicKey;

		public string FilePath { get => _FilePath; set => SetProperty(ref _FilePath, value); }
		private string _FilePath;

		[DefaultValue(true)]
		public bool AddBase64KeyHeaders { get => _AddBase64KeyHeaders; set => SetProperty(ref _AddBase64KeyHeaders, value); }
		private bool _AddBase64KeyHeaders = true;

		[DefaultValue(true)]
		public bool AddBase64MessageHeaders { get => _AddBase64MessageHeaders; set => SetProperty(ref _AddBase64MessageHeaders, value); }
		private bool _AddBase64MessageHeaders = true;

		[DefaultValue(true)]
		public bool AddBase64FileHeaders { get => _AddBase64FileHeaders; set => SetProperty(ref _AddBase64FileHeaders, value); }
		private bool _AddBase64FileHeaders = true;


		[DefaultValue(false)]
		public bool ChatAddBase64KeyHeaders { get => _ChatAddBase64KeyHeaders; set => SetProperty(ref _ChatAddBase64KeyHeaders, value); }
		private bool _ChatAddBase64KeyHeaders;

		[DefaultValue(false)]
		public bool ChatAddBase64MessageHeaders { get => _ChatAddBase64MessageHeaders; set => SetProperty(ref _ChatAddBase64MessageHeaders, value); }
		private bool _ChatAddBase64MessageHeaders;

		#endregion

		public List<AppAutoSettings> AutoSettings { get => _AutoSettings; set => SetProperty(ref _AutoSettings, value); }
		private List<AppAutoSettings> _AutoSettings;

	}
}
