using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using MSAA;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Diagnostics;
using JocysCom.ClassLibrary.Processes;
using JocysCom.ClassLibrary.Collections;
using System.Xml.XPath;
using System;

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
			messageParser = new MessageParser();
			UpdateControlButtons();
			DataTextBox_TextChanged(null, null);
			InfoPanel.Tasks.ListChanged += Tasks_ListChanged;
			xmlParseTimer = new System.Timers.Timer();
			xmlParseTimer.Interval = 2000;
			xmlParseTimer.Elapsed += XmlParseTimer_Elapsed;
			xmlParseTimer.AutoReset = false;
		}

		System.Timers.Timer xmlParseTimer;

		MessageParser messageParser;

		private void XmlParseTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (MainWindow.IsClosing)
				return;
			// Process nothing if chat is not selected
			if (!MainWindow.Current.IsChatSelected)
			{
				xmlParseTimer.Start();
				return;
			}
			var isBusy = false;
			ControlsHelper.Invoke(() =>
			{
				isBusy = InfoPanel.Tasks.Count > 0;
			});
			// If list is empty then try to refresh list.
			if (_AllMsaaItemWindowList.Count == 0)
			{
				if (!isBusy)
					RefreshPrograms();
			}
			var selectedWindow = _SelectedMsaaItemWindow;
			if (selectedWindow == null || !IsConnected)
			{
				if (!MainWindow.IsClosing)
					xmlParseTimer.Start();
				return;
			}
			try
			{
				//ControlsHelper.Invoke(() => InfoPanel.AddTask(RefreshAutomationTreeTaskName));
				// Get window XML and all relevant MSAA items
				_ProgramMsaaItems.Clear();
				_ProgramXml = ToXml(selectedWindow, ref _ProgramMsaaItems);
				var chatPath = AutoSettings.ChatPath;
				_ChatXml = string.IsNullOrEmpty(chatPath)
					? null
					: _ProgramXml?.XPathSelectElements(chatPath);
				List<MessageItem> newItems;
				messageParser.ParseMessages(_ProgramMsaaItems, out newItems);
			}
			catch (System.Exception)
			{
			}
			finally
			{
				ControlsHelper.Invoke(() =>
				{
					UpdateControlButtons();
					//InfoPanel.RemoveTask(RefreshAutomationTreeTaskName);
				});
				if (!MainWindow.IsClosing)
					xmlParseTimer.Start();
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

		public string Connect = "Connect";
		public string Disconnect = "Disconnect";
		public bool IsConnected;

		private void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (ConnectButton.Content as string == Connect)
			{
				// If your keys are missing then generate new ones.
				if (Global.AppSettings.YourPublicKey == null || Global.AppSettings.YourPrivateKey == null)
					Security.GenerateKeys();
				messageParser.AddMessage(Global.AppSettings.YourPublicKey, MessageType.YourPublicKey);
				DataListPanel.AddMessage("Out", "Your Public Key was send.");
				ControlsHelper.BeginInvoke(() =>
				{
					SendMessage(Global.AppSettings.YourPublicKey);
				});
				ConnectButton.Content = Disconnect;
				IsConnected = true;
			}
			else
			{
				ConnectButton.Content = Connect;
				IsConnected = false;
			}
		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
			// Commented future feature.
			//RefreshPrograms();
			xmlParseTimer.Start();
		}

		private void ProgramListRefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			RefreshPrograms();
		}

		const string RefreshProgramsTaskName = nameof(RefreshProgramsTaskName);
		const string RefreshAutomationTreeTaskName = nameof(RefreshAutomationTreeTaskName);
		const string SendingMessage = nameof(SendingMessage);

		Process _CurrentProcess { get; } = Process.GetCurrentProcess();

		// List of MsaaItem windows and their processes.
		List<MsaaItem> _AllMsaaItemWindowList = new List<MsaaItem>();
		List<Process> _AllProcessWindowList = new List<Process>();

		// Currently selected MsaaItem windows and their processes.
		MsaaItem _SelectedMsaaItemWindow;
		Process _SelectedProcessWindow;

		/// <summary>
		/// All Program window elements as XML.
		/// </summary>
		XElement _ProgramXml;
		IEnumerable<XElement> _ChatXml;
		List<MsaaItem> _ProgramMsaaItems = new List<MsaaItem>();

		void RefreshPrograms()
		{
			string programTitle = null;
			// Exit if refreshing already.
			var allMsaaItemWindowList = new List<MsaaItem>();
			var allProcessWindowList = new List<Process>();
			List<MsaaItem> windows = null;
			Task.Run(() =>
			{
				ControlsHelper.Invoke(() =>
				{
					var selection = ProgramsComboBox.SelectedItem as ComboBoxItem;
					programTitle = selection?.Content as string;
					InfoPanel.AddTask(RefreshProgramsTaskName);
				});
				try
				{
					// Get all Accessibility items.
					windows = Msaa
						.GetWindows(null, MsaaRole.Window)
						.Select(x => new MsaaItem(x))
						.Where(x => !string.IsNullOrEmpty(x.Name))
						.Where(x => x.IsVisible && x.IsEnabled)
						.ToList();
					foreach (var window in windows)
					{
						MsaaWin32.GetWindowThreadProcessId(window.Handle, out uint processId);
						var process = Process.GetProcessById((int)processId);
						if (process != null)
						{
							allMsaaItemWindowList.Add(window);
							allProcessWindowList.Add(process);
						}
					}
				}
				catch (System.Exception)
				{
				}
				// Update combobox from results.
				ControlsHelper.Invoke(() =>
				{
					CollectionsHelper.Synchronize(allMsaaItemWindowList, _AllMsaaItemWindowList);
					CollectionsHelper.Synchronize(allProcessWindowList, _AllProcessWindowList);
					// Attach title and index to combobox.
					var all = allMsaaItemWindowList
						.Select((x, index) => new ComboBoxItem() { Content = x.Name, Tag = index })
						.OrderBy(x => x.Content)
						.ToArray();
					ProgramsComboBox.ItemsSource = all;
					if (programTitle != null)
					{
						var item = all.FirstOrDefault(x => Equals(x.Content, programTitle));
						if (item != null)
							item.IsSelected = true;
					}
					InfoPanel.RemoveTask(RefreshProgramsTaskName);
				});
			});
		}

		public AppAutoSettings AutoSettings { get => _AutoSettings; set { _AutoSettings = value; UpdateControlButtons(); } }
		AppAutoSettings _AutoSettings;

		void UpdateControlButtons()
		{
			ControlsHelper.Invoke(() =>
			{
				var isBusy = InfoPanel.Tasks.Count > 0;
				ControlsHelper.SetEnabled(ShowProgramXmlButton, _ProgramXml != null);
				ControlsHelper.SetEnabled(ShowChatXmlButton, _ChatXml != null);
			});
		}

		private void ProgramsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = ProgramsComboBox.SelectedItem as ComboBoxItem;
			if (item == null)
				return;
			var title = (string)item.Content;
			var index = (int)item.Tag;
			_SelectedProcessWindow = _AllProcessWindowList[index];
			_SelectedMsaaItemWindow = _AllMsaaItemWindowList[index];
			// Try to load automation settings.
			var autoSettings = Global.AppSettings.AutoSettings.FirstOrDefault(x => x.Title == title);
			if (autoSettings == null)
			{
				autoSettings = new AppAutoSettings() { Title = title };
				Global.AppSettings.AutoSettings.Add(autoSettings);
			}
			AutoSettings = autoSettings;
			AutoSettingsGrid.DataContext = autoSettings;
		}

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
			if (items != null)
			{
				item.Id = items.Count;
				items.Add(item);
			}
			root.SetAttributeValue(nameof(item.Id), item.Id);
			// Loop trough children.
			foreach (var child in item.Children)
				if (child.IsVisible)
					root.Add(ToXml(child, ref items));
			return root;
		}

		private void ShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var xml = _ProgramXml;
			if (xml == null)
				return;
			ShowMessage(xml.ToString());
		}

		private void ChatPathShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var xml = _ChatXml;
			if (xml == null)
				return;
			var xmlText = string.Join("\r\n", xml);
			ShowMessage(xmlText.ToString());
		}

		private void SendMessage(string message)
		{
			Task.Run(() =>
			{
				messageParser.AddMessage(message, MessageType.YourMessage);
				var editHandle = IntPtr.Zero;
				var editItem = _ProgramMsaaItems.FirstOrDefault(x => x.Role == MsaaRole.Text);
				if (editItem != null)
				{
					editItem.Focus();
					editHandle = editItem.Handle;
				}
				//ControlsHelper.Invoke(() => InfoPanel.AddTask(SendingMessage));
				var success = KeyboardHelper.SendTextMessage(
					message, true,
					_SelectedProcessWindow.MainWindowHandle,
					editHandle,
					_CurrentProcess.MainWindowHandle
				);
				ControlsHelper.Invoke(() =>
				{
					//InfoPanel.RemoveTask(SendingMessage);
					//TargetProgramLabel.Content = success
					//	? "Target Program: OK"
					//	: "Target Program: Fail";

				});
			});
		}

		private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				SendMessage(DataTextBox.Text);
			});
		}

		private void DataTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			SendButton.Opacity = string.IsNullOrEmpty(DataTextBox.Text) ? 0.5 : 1.0;
		}
	}

}

