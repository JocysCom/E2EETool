using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using MSAA;
using System.Collections.Generic;
using System.Diagnostics;
using JocysCom.ClassLibrary.Processes;
using JocysCom.ClassLibrary.Collections;
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
			xmlParseTimer.Interval = 500;
			xmlParseTimer.AutoReset = false;
			xmlParseTimer.Elapsed += XmlParseTimer_Elapsed;
			xmlParseTimer.Start();

		}

		System.Timers.Timer xmlParseTimer;

		MessageParser messageParser;

		private void XmlParseTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			// If chat is not selected then skip.
			if (!MainWindow.Current.IsChatSelected)
			{
				xmlParseTimer.Start();
				return;
			}
			// If programs list is empty then...
			if (_AllWindowMsaaItems.Count == 0)
			{
				// Try to refresh list.
				var isBusy = false;
				ControlsHelper.Invoke(() =>
				{
					isBusy = InfoPanel.Tasks.Count > 0;
				});
				if (!isBusy)
					RefreshWindowList();
			}
			if (MainWindow.IsClosing)
				return;
			var selectedWindow = SelectedWindowMsaaItem;
			// If chat is not selected or not connected or program not selected then skip.
			if (!MainWindow.Current.IsChatSelected || !IsConnected || selectedWindow == null)
			{
				xmlParseTimer.Start();
				return;
			}
			// Make timer refresh slower.
			if (xmlParseTimer.Interval != 2000)
			{
				// Call Close() to prevent timer auto-restart when Interval changed.
				xmlParseTimer.Close();
				xmlParseTimer.Interval = 2000;
			}
			try
			{
				//ControlsHelper.Invoke(() => InfoPanel.AddTask(RefreshAutomationTreeTaskName));
				// Get window XML and all relevant MSAA items
				_SelectedWindowAllMsaaItems = MsaaSerializer.GetList(selectedWindow);
				List<MessageItem> newItems;
				messageParser.ParseMessages(_SelectedWindowAllMsaaItems, out newItems);
				foreach (var item in newItems)
				{
					ControlsHelper.Invoke(() =>
					{
						DataListPanel.AddMessage(item.MessageType, item.Message, item.DecryptedMessage);
					});
				}

			}
			catch (System.Exception)
			{
			}
			ControlsHelper.Invoke(() =>
			{
				// If just connected and send public key requested.
				if (SendPublicKey)
				{
					SendPublicKey = false;
					var keyBytes = Security.FromBase64(Global.AppSettings.YourPublicKey);
					var keyBase64 = Security.ToBase64(keyBytes,
						Global.AppSettings.ChatAddBase64KeyHeaders ? Base64HeaderType.PublicKey : Base64HeaderType.None);
					messageParser.AddMessage(keyBase64, MessageType.YourPublicKey);
					//DataListPanel.AddMessage("Out", "Your Public Key was send.");
					SendMessage(keyBase64, MessageType.YourPublicKey);
				}
				UpdateControlButtons();
				//InfoPanel.RemoveTask(RefreshAutomationTreeTaskName);
			});
			xmlParseTimer.Start();
		}

		private void Tasks_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
		{
			UpdateControlButtons();
		}

		InfoControl InfoPanel
			=> MainWindow.Current.InfoPanel;

		private void SendButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				SendMessage(DataTextBox.Text, MessageType.YourMessage);
			});
		}

		private void DataTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
			{
				if (!string.IsNullOrEmpty(DataTextBox.Text))
				{
					var message = DataTextBox.Text;
					SendMessage(message, MessageType.YourMessage);
					DataTextBox.Text = string.Empty;
					ControlsHelper.AutoScroll(this);
				}
				e.Handled = true;
			}
		}

		public string Connect = "Connect";
		public string Disconnect = "Disconnect";
		public bool IsConnected;
		public bool SendPublicKey;

		private void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (ConnectButton.Content as string == Connect)
			{
				messageParser.Reset();
				// If your keys are missing then generate new ones.
				if (Global.AppSettings.YourPublicKey == null || Global.AppSettings.YourPrivateKey == null)
					Security.GenerateKeys();
				SendPublicKey = true;
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
		}

		private void ProgramListRefreshButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			Task.Run(() =>
			{
				RefreshWindowList();
			});
		}

		const string RefreshProgramsTaskName = nameof(RefreshProgramsTaskName);
		const string RefreshAutomationTreeTaskName = nameof(RefreshAutomationTreeTaskName);
		const string SendingMessage = nameof(SendingMessage);

		Process _CurrentProcess { get; } = Process.GetCurrentProcess();

		// List of all windows - MsaaItems their processes.
		List<MsaaItem> _AllWindowMsaaItems = new List<MsaaItem>();
		List<Process> _AllWindowProcesses = new List<Process>();

		// Currently selected MsaaItem windows and their processes.
		MsaaItem SelectedWindowMsaaItem
		{
			get => _SelectedWindowMsaaItem;
			set { _SelectedWindowMsaaItem = value; UpdateControlButtons(); }
		}
		MsaaItem _SelectedWindowMsaaItem { get; set; }

		Process _SelectedWindowProcess;

		List<MsaaItem> _SelectedWindowAllMsaaItems = new List<MsaaItem>();

		void RefreshWindowList()
		{
			string processTitle = null;
			// Exit if refreshing already.
			var allWindowMsaaItems = new List<MsaaItem>();
			var allWindowProcesses = new List<Process>();
			List<MsaaItem> windows = null;
			ControlsHelper.Invoke(() =>
			{
				var selection = ProgramsComboBox.SelectedItem as ComboBoxItem;
				processTitle = selection?.Content as string;
				InfoPanel.AddTask(RefreshProgramsTaskName);
			});
			try
			{
				// Get all Accessibility items.
				windows = Msaa
					.GetWindows(null, MsaaRole.Window)
					.Select(x => { var item = new MsaaItem(); item.Load(x); return item; })
					.Where(x => !string.IsNullOrEmpty(x.Name))
					.Where(x => x.IsVisible && x.IsEnabled)
					.ToList();
				foreach (var window in windows)
				{
					MsaaWin32.GetWindowThreadProcessId(new IntPtr(window.Handle), out uint processId);
					var process = Process.GetProcessById((int)processId);
					if (process != null)
					{
						allWindowMsaaItems.Add(window);
						allWindowProcesses.Add(process);
					}
				}
			}
			catch (System.Exception)
			{
			}
			// Update combobox from results.
			ControlsHelper.Invoke(() =>
			{
				CollectionsHelper.Synchronize(allWindowMsaaItems, _AllWindowMsaaItems);
				CollectionsHelper.Synchronize(allWindowProcesses, _AllWindowProcesses);
				// Attach title and index to combobox.
				var all = allWindowMsaaItems
					.Select((x, index) => new ComboBoxItem() { Content = x.Name, Tag = index })
					.OrderBy(x => x.Content)
					.ToArray();
				ProgramsComboBox.ItemsSource = all;
				if (processTitle != null)
				{
					var item = all.FirstOrDefault(x => Equals(x.Content, processTitle));
					if (item != null)
						item.IsSelected = true;
				}
				InfoPanel.RemoveTask(RefreshProgramsTaskName);
			});
		}

		public AppAutoSettings AutoSettings { get => _AutoSettings; set { _AutoSettings = value; UpdateControlButtons(); } }
		AppAutoSettings _AutoSettings;

		void UpdateControlButtons()
		{
			ControlsHelper.Invoke(() =>
			{
				var isBusy = InfoPanel.Tasks.Count > 0;
				ControlsHelper.SetEnabled(ShowProgramXmlButton, _SelectedWindowMsaaItem != null);
				ControlsHelper.SetEnabled(ShowChatXmlButton, _SelectedWindowMsaaItem != null);
			});
		}

		private void ProgramsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = ProgramsComboBox.SelectedItem as ComboBoxItem;
			if (item == null)
				return;
			var title = (string)item.Content;
			var index = (int)item.Tag;
			_SelectedWindowProcess = _AllWindowProcesses[index];
			SelectedWindowMsaaItem = _AllWindowMsaaItems[index];
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

		private void ShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var item = SelectedWindowMsaaItem;
			if (item == null)
				return;

			var xml = JocysCom.ClassLibrary.Runtime.Serializer.SerializeToXmlString(item);
			ShowMessage(xml.ToString());
			/*
			List<MsaaItem> list = null;
			// Get window XML and all relevant MSAA items
			var xml = ToXml(item, ref list);
			if (xml == null)
				return;
			ShowMessage(xml.ToString());
			*/
		}

		private void ChatPathShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			var chatPath = AutoSettings.ChatPath;
			_SelectedWindowAllMsaaItems = MsaaSerializer.GetList(SelectedWindowMsaaItem);
			var items = MsaaSerializer.XPathSelectElements(SelectedWindowMsaaItem, chatPath, _SelectedWindowAllMsaaItems);

			//var xml = null;
			//if (xml == null)
			//	return;
			//var xmlText = string.Join("\r\n", xml);
			//ShowMessage(xmlText.ToString());
		}


		private void SendMessage(string message, MessageType messageType)
		{
			var messageToSend = message;
			// It this is your message then...
			if (messageType == MessageType.YourMessage)
			{
				// ...message must be encrypted.
				messageToSend = Security.Encrypt(message,
					Global.AppSettings.ChatAddBase64MessageHeaders ? Base64HeaderType.Message : Base64HeaderType.None);
				DataListPanel.AddMessage(MessageType.YourMessage, message);
			}
			else if (messageType == MessageType.YourPublicKey)
			{
				DataListPanel.AddMessage(MessageType.YourPublicKey);
			}
			else
			{
				throw new NotSupportedException();
			}
			// Add message to parser so that it will be skipped as old.
			messageParser.AddMessage(messageToSend, messageType);
			// Begin adding message to chat program window.
			Task.Run(() =>
			{
				// Type message into chat program window.
				var focusHandle = GetFocusHandle();
				var success = KeyboardHelper.SendTextMessage(
					messageToSend, true, true,
					_SelectedWindowProcess.MainWindowHandle,
					focusHandle,
					_CurrentProcess.MainWindowHandle
				);
			});
		}

		/// <summary>
		/// Get chat window edit box handle.
		/// </summary>
		public IntPtr GetFocusHandle()
		{
			var focusHandle = IntPtr.Zero;
			var editItem = _SelectedWindowAllMsaaItems.FirstOrDefault(x => x.Role == MsaaRole.Text);
			if (editItem != null)
			{
				editItem.Focus();
				focusHandle = new IntPtr(editItem.Handle);
			}
			return focusHandle;
		}

		private void DataTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			SendButton.Opacity = string.IsNullOrEmpty(DataTextBox.Text) ? 0.5 : 1.0;
		}
	}

}

