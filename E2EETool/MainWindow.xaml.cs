using JocysCom.ClassLibrary.Controls;
using System.Windows;
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace JocysCom.Tools.E2EETool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{

		public MainWindow()
		{
			Current = this;
			ControlsHelper.InitInvokeContext();
			if (ControlsHelper.IsDesignMode(this))
				return;
			Global.AppData.Load();
			if (Global.AppData.Items.Count == 0)
			{
				Global.AppData.Items.Add(new AppData());
				Global.AppData.Save();
			}
			if (Global.AppSettings.AutoSettings == null)
				Global.AppSettings.AutoSettings = new List<AppAutoSettings>();
			if (Global.AppSettings.AutoSettings.Count == 0)
			{
				var autoSettings = new AppAutoSettings()
				{
					Title = "Skype",
					ChatPath = "//Document[@Name=\"Skype\"]//Pane",
				};
				Global.AppSettings.AutoSettings.Add(autoSettings);
			}

			Topmost = Global.AppSettings.AlwaysOnTop;
			Global.AppSettings.PropertyChanged += AppSettings_PropertyChanged;
			// Initialize.
			InitializeComponent();
			LoadHelpAndInfo();
		}

		public static MainWindow Current;

		void LoadHelpAndInfo()
		{
			var ai = new ClassLibrary.Configuration.AssemblyInfo();
			Title = ai.GetTitle(true, false, true, false, false);
		}

		public InfoControl HMan;

		public static bool IsClosing;

		private void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AppData.AlwaysOnTop))
			{
				Topmost = Global.AppSettings.AlwaysOnTop;
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (!Global.AppSettings.SaveKeysOnApplicationClose)
			{
				Global.AppSettings.YourPrivateKey = null;
				Global.AppSettings.YourPublicKey = null;
				Global.AppSettings.OtherPublicKey = null;
			}
			Global.AppData.Save();
		}

		public bool IsChatSelected =>
			MainTab.SelectedItem == ChatTabItem;

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			IsClosing = true;
		}

	}

}
