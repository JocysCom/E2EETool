using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using MSAA;
using System.Collections.Generic;
using System;
using System.Xml.Linq;
using System.Xml.XPath;

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
			InfoPanel.Tasks.ListChanged += Tasks_ListChanged;
		}

		private void Tasks_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
		{
			UpdateControlButtons();
		}

		InfoControl InfoPanel
			=> MainWindow.Current.InfoPanel;

		private void DataTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
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

		}

		private void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
		{
			RefreshPrograms();
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
			// Try to load automation settings.
			AutoSettings = Global.AppSettings.AutoSettings.FirstOrDefault(x => (string)item.Content == x.Title);
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

			var log = "";

			var all = Msaa
				.GetWindows(null)
				.Select(x => new MsaaItem(x))
				.OrderBy(x => x.Role).ThenBy(x => x.Name)
				.ToArray();

			GetAll(window, false, 0, ref log, 11);

			_WindowXml = ToXml(window);

			ShowMessage(_WindowXml.ToString());

		}

		XElement _WindowXml;
		IEnumerable<XElement> _ChatXml;
		IEnumerable<XElement> _EditXml;

		public void ShowMessage(string message)
		{
			ControlsHelper.Invoke(() =>
			{
				var box = new MessageBoxWindow();
				box.SetSize(800, 600);
				box.ShowPrompt(message, "XML Text", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
			});
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public IEnumerable<MsaaItem> GetAll(MsaaItem control, bool includeTop, int level, ref string log, int levelMax = int.MaxValue)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			// Create new list.
			var controls = new List<MsaaItem>();
			// Add top control if required.
			if (includeTop && !controls.Contains(control))
				controls.Add(control);
			log += ToString(control, level);
			if (level >= levelMax)
				return controls;
			// If control contains children then...
			foreach (var child in control.Children)
			{
				var children = GetAll(child, true, level + 1, ref log, levelMax).Except(controls);
				controls.AddRange(children);
			}
			return controls;
		}

		public static XElement ToXml(MsaaItem item)
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
			foreach (var child in item.Children)
				if (child.IsVisible)
					root.Add(ToXml(child));
			return root;
		}

		string ToString(MsaaItem e, int index)
		{
			return $"{index}{new string(' ', index * 2)}{e.Role} '{e.Name}': {e.Error?.Message}\r\n";
		}

		private void ChatPathShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			_ChatXml = _WindowXml.XPathSelectElements(ChatPathTextBox.Text);
			var xml = string.Join("\r\n", _ChatXml);
			ShowMessage(xml);
		}

		private void EditPathShowXmlButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			_EditXml = _WindowXml.XPathSelectElements(EditPathTextBox.Text);
			var xml = string.Join("\r\n", _EditXml);
			ShowMessage(xml);
		}

		private void SendMessage(string message)
		{

		}

		private void CheckForNewMessages()
		{

		}

	}

}

