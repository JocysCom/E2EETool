using JocysCom.ClassLibrary.Configuration;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Serialization;

namespace JocysCom.Tools.E2EETool
{
	public class DataItem : ISettingsItem, INotifyPropertyChanged
	{
		public string User { get => _User; set => SetProperty(ref _User, value); }
		string _User;

		public string Body { get => _Body; set => SetProperty(ref _Body, value); }
		string _Body;

		public DateTime Date { get => _Date; set => SetProperty(ref _Date, value); }
		DateTime _Date = DateTime.Now;

		public Style Style { get => _Style; set => SetProperty(ref _Style, value); }
		Style _Style;

		public bool IsIncomming { get => _IsIncomming; set => SetProperty(ref _IsIncomming, value); }
		bool _IsIncomming;

		public bool IsEnabled { get => _IsEnabled; set => SetProperty(ref _IsEnabled, value); }
		bool _IsEnabled;

		[XmlIgnore]
		public object Tag;

		#region ■ ISettingsItem
		bool ISettingsItem.Enabled { get => IsEnabled; set => IsEnabled = value; }

		public bool IsEmpty =>
			string.IsNullOrEmpty(User) &&
			string.IsNullOrEmpty(Body);

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
