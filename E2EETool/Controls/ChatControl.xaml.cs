using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using MSAA;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Diagnostics;
using JocysCom.ClassLibrary.Processes;

namespace JocysCom.Tools.E2EETool.Controls
{
	/// <summary>
	/// Interaction logic for ChatControl.xaml
	/// </summary>
	public partial class ChatControl : UserControl
	{
		public ChatControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			UpdateControlButtons();
			DataTextBox_TextChanged(null, null);
			InfoPanel.Tasks.ListChanged += Tasks_ListChanged;
			MainWindow.Current.MainTab.SelectionChanged += MainTab_SelectionChanged;
		}

		private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainWindow.Current.IsChatSelected)
			{
				var isBusy = InfoPanel.Tasks.Count > 0;
				// Refresh programs combobox if program is not busy.
				if (!isBusy)
					RefreshPrograms();
			}
		}

		private void Tasks_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
		{
			UpdateControlButtons();
		}

		InfoControl InfoPanel
			=> MainWindow.Current.InfoPanel;

		private void DataTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && !System.Windows.Input.Keyboard.IsKeyDown(Key.LeftShift) && !System.Windows.Input.Keyboard.IsKeyDown(Key.RightShift))
			{
				if (!string.IsNullOrEmpty(DataTextBox.Text))
				{
					var message = DataTextBox.Text.TrimEnd('\r', '\n');
					DataListPanel.AddMessage("Out", message);
					SendMessage(message);
					DataTextBox.Text = string.Empty;
					ControlsHelper.AutoScroll(this);
				}
				e.Handled = true;
			}
		}

		private void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// If your keys are missing then generate new ones.
			if (Global.AppSettings.YourPublicKey == null || Global.AppSettings.YourPrivateKey == null)
				Security.GenerateKeys();
			SendMessage(Global.AppSettings.YourPublicKey);
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Commented future feature.
			//RefreshPrograms();
		}

		private void ProgramListRefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			RefreshPrograms();
		}

		const string RefreshProgramsTaskName = nameof(RefreshProgramsTaskName);
		const string RefreshAutomationTreeTaskName = nameof(RefreshAutomationTreeTaskName);

		Task RefreshProgramsTask;

		void RefreshPrograms()
		{
			var selection = ProgramsComboBox.SelectedItem as ComboBoxItem;
			List<MsaaItem> windows = null;
			InfoPanel.AddTask(RefreshProgramsTaskName);
			RefreshProgramsTask = Task.Run(() =>
			{
				windows = Msaa
					.GetWindows(null, MsaaRole.Window)
					.Select(x => new MsaaItem(x))
					.Where(x => !string.IsNullOrEmpty(x.Name))
					.Where(x => x.IsVisible && x.IsEnabled)
					.ToList();

			}).ContinueWith((task) =>
			{
				// Update control from results.
				ControlsHelper.BeginInvoke(() =>
				{
					var all = windows
						.Select(x => new ComboBoxItem() { Content = x.Name, Tag = x })
						.OrderBy(x => x.Content)
						.ToArray();
					ProgramsComboBox.ItemsSource = all;
					if (selection != null)
					{
						var item = all.FirstOrDefault(x => Equals(x.Content, selection.Content));
						if (item != null)
							item.IsSelected = true;
					}
					InfoPanel.RemoveTask(RefreshProgramsTaskName);
				});
			});
		}

		public AppAutoSettings AutoSettings { get => _AutoSettings; set { _AutoSettings = value; UpdateControlButtons(); } }
		AppAutoSettings _AutoSettings;
		Task RefreshAutomationTreeTask;

		void UpdateControlButtons()
		{
			ControlsHelper.Invoke(() =>
			{
				var isBusy = InfoPanel.Tasks.Count > 0;
				ProgramListRefreshButton.IsEnabled = !isBusy;
				ShowXmlButton.IsEnabled = AutoSettings != null && !isBusy;
			});
		}

		private void ProgramsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = ProgramsComboBox.SelectedItem as ComboBoxItem;
			if (item == null)
				return;
			var title = (string)item.Content;
			_Process = Process.GetProcesses().Where(x => x.MainWindowTitle == title).FirstOrDefault();
			// Try to load automation settings.
			AutoSettings = Global.AppSettings.AutoSettings.FirstOrDefault(x => x.Title == title);
			AutoSettingsGrid.DataContext = AutoSettings;
			// If Automation settings not found then exit.
			if (AutoSettings == null)
				return;
		}

		private void ShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			InfoPanel.AddTask(RefreshAutomationTreeTaskName);
			RefreshAutomationTreeTask = Task.Run(RefreshTree)
				.ContinueWith((task) =>
				{
					ControlsHelper.BeginInvoke(() =>
					{
						InfoPanel.RemoveTask(RefreshAutomationTreeTaskName);
						RefreshAutomationTreeTask = null;
						ShowMessage(_WindowXml.ToString());
					});
				});
		}

		private void RefreshTree()
		{
			var window = default(MsaaItem);
			ControlsHelper.Invoke(() =>
			{
				var item = ProgramsComboBox.SelectedItem as ComboBoxItem;
				if (item == null)
					return;
				window = (MsaaItem)item.Tag;
			});

			// Pane 'Skype'\ Window 'Skype'\ Document 'Skype'\ Edit [AriaRole='textbox'
			var name = window.Name;

			var all = Msaa
				.GetWindows(null)
				.Select(x => new MsaaItem(x))
				.OrderBy(x => x.Role).ThenBy(x => x.Name)
				.ToArray();

			//var allItems = GetAll(window, false, 0, ref log, 11);

			_WindowItems.Clear();
			_WindowXml = ToXml(window, ref _WindowItems);
		}

		Process _Process;
		Process _CurrentProcess => Process.GetCurrentProcess();

		/// <summary>
		/// All Program window elements as XML.
		/// </summary>
		XElement _WindowXml;

		/// <summary>
		/// All Program window elements as MsaaItem.
		/// List is used to find original MsaaItem by "Id".
		/// </summary>
		List<MsaaItem> _WindowItems = new List<MsaaItem>();

		IEnumerable<XElement> _ChatXml;

		public void ShowMessage(string message)
		{
			ControlsHelper.Invoke(() =>
			{
				var box = new MessageBoxWindow();
				box.SetSize(800, 600);
				box.ShowPrompt(message, "XML Text", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
			});
		}

		public static XElement ToXml(MsaaItem item, ref List<MsaaItem> items)
		{
			var root = new XElement(item.Role.ToString());
			if (!string.IsNullOrEmpty(item.Name))
				root.SetAttributeValue(nameof(item.Name), item.Name);
			if (!string.IsNullOrEmpty(item.Value))
				root.SetAttributeValue(nameof(item.Value), item.Value);
			if (!string.IsNullOrEmpty(item.DefaultAction))
				root.SetAttributeValue(nameof(item.DefaultAction), item.DefaultAction);
			if (!item.IsVisible)
				root.SetAttributeValue(nameof(item.IsVisible), item.IsVisible);
			if (!item.IsEnabled)
				root.SetAttributeValue(nameof(item.IsEnabled), item.IsEnabled);
			// Id which will be used to find original MsaaItem.
			item.Id = items.Count;
			items.Add(item);
			root.SetAttributeValue(nameof(item.Id), item.Id);
			// Loop trough children.
			foreach (var child in item.Children)
				if (child.IsVisible)
					root.Add(ToXml(child, ref items));
			return root;
		}

		private void ChatPathShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var xml = "";
			try
			{
				_ChatXml = _WindowXml.XPathSelectElements(ChatPathTextBox.Text);
				xml = string.Join("\r\n", _ChatXml);
			}
			catch (System.Exception ex)
			{
				xml = ex.Message;
			}
			ShowMessage(xml);
		}

		private void SendMessage(string message)
		{
			Task.Run(() =>
			{
				KeyboardHelper.SendTextMessage(
					message, true,
					_Process.MainWindowHandle,
					_CurrentProcess.MainWindowHandle
				);
			});
		}

		private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{

		}

		private void DataTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			SendButton.Opacity = string.IsNullOrEmpty(DataTextBox.Text) ? 0.5 : 1.0;
		}
	}

}

