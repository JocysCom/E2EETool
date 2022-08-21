﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.Tools.E2EETool
{
	public class AppAutoSettings : JocysCom.ClassLibrary.Configuration.ISettingsItem, INotifyPropertyChanged
	{
		public bool IsEmpty => false;

		public string Title { get => _Title; set => SetProperty(ref _Title, value); }
		private string _Title;

		public string MainPath { get => _MainPath; set => SetProperty(ref _MainPath, value); }
		private string _MainPath;

		public string ChatPath { get => _ChatPath; set => SetProperty(ref _ChatPath, value); }
		private string _ChatPath;
		
		public string EditPath { get => _EditPath; set => SetProperty(ref _EditPath, value); }
		private string _EditPath;

		#region ISettingsItem

		public bool Enabled { get; set; }

		public bool Empty => false;

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