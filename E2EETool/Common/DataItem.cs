using JocysCom.ClassLibrary.Configuration;
using System;
using System.Windows;
using System.Xml.Serialization;

namespace JocysCom.Tools.E2EETool
{
	public class DataItem : SettingsItem
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

		[XmlIgnore]
		public object Tag;

	}
}
