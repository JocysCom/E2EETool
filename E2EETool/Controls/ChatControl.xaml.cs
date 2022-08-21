using JocysCom.ClassLibrary.Controls;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using MSAA;
using System.Collections.Generic;

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
					DataListPanel.AddMessage("Out", DataTextBox.Text.TrimEnd('\r', '\n'));
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
			List<MsaaAccessible> windows = null;
			InfoPanel.AddTask(RefreshProgramsTaskName);
			RefreshProgramsTask = Task.Run(() =>
			{
				windows = Msaa
					.GetWindows(null, MsaaRole.Window)
					.Select(x => new MsaaAccessible(x))
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
				TestAutomationButton.IsEnabled = AutoSettings != null && !isBusy;
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

		private void TestAutomationButton_Click(object sender, System.Windows.RoutedEventArgs e)
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

		// IAutomation uiAutomation { get; } = new CUIAutomation8();

		private void RefreshTree()
		{
			var window = default(MsaaAccessible);
			ControlsHelper.Invoke(() =>
			{
				var item = ProgramsComboBox.SelectedItem as ComboBoxItem;
				if (item == null)
					return;
				window = (MsaaAccessible)item.Tag;
			});

			// Pane 'Skype'\ Window 'Skype'\ Document 'Skype'\ Edit [AriaRole='textbox'
			var name = window.Name;

			var log = "";

			var all = Msaa
				.GetWindows(null)
				.Select(x => new MsaaAccessible(x))
				.OrderBy(x => x.Role).ThenBy(x => x.Name)
				.ToArray();



			GetChildrenAll(window, ref log);

			//var root = uiAutomation.GetRootElement();
			//var processCondition = uiAutomation.CreatePropertyCondition(UIA_PropertyIds.UIA_ProcessIdPropertyId, _Process.Id);
			//var processElement = root.FindFirst(TreeScope.TreeScope_Element | TreeScope.TreeScope_Children, processCondition);

			//var processType = processElement.CurrentControlType;
			//var processName = processElement.CurrentName;

			/*
			var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, _Process.Id);
			var rootElement = AutomationElement.RootElement.FindFirst(TreeScope.Element | TreeScope.Children, condition);

			var rootText = AutomationHelper.GetAnyText(rootElement);

			AutomationElement parentAutomationElement = AutomationElement.FromHandle(_Process.MainWindowHandle);
			*/

			/*


			log = $"\r\n----- Process {_Process.Id}\r\n" + log;

			var mainTypes = AutomationHelper.ParsePath(AutoSettings.MainPath).Select(x => x.Id).ToArray();

			var chatTypes = AutomationHelper.ParsePath(AutoSettings.ChatPath);
			var chatTypesPath = chatTypes.Take(chatTypes.Count - 1).Select(x => x.Id).ToArray();
			var chatTypesName = chatTypes.Last();

			var editTypes = AutomationHelper.ParsePath(AutoSettings.EditPath);
			var editTypesPath = editTypes.Take(editTypes.Count - 1).Select(x => x.Id).ToArray();
			var editTypesName = editTypes.Last();


			var mainPanel = GetFirstByTypes(processElement, 1, ref log, mainTypes);

			var allSub = GetAllSubChildren(processElement, 1, ref log);

			var raw = uiAutomation.RawViewWalker;

			//IUIAutomation.RawViewWalker to get an IUIAutomationTreeWalker


			// Get panel which contains chat messages.
			var chatPanel = chatTypesPath.Length > 0
				? GetFirstByTypes(mainPanel, 1, ref log, chatTypesPath)
				: mainPanel;
			// Get panel which contains edit box for sending a message.
			var editPanel = editTypesPath.Length > 0
				? GetFirstByTypes(mainPanel, 1, ref log, editTypesPath)
				: mainPanel;

			GetChildrenAll(processElement, ref log);
			*/
			/*

			var mainPanel = GetFirstByTypes(rootElement, 1, ref log, mainTypes);
			// Get panel which contains chat messages.
			var chatPanel = chatTypesPath.Length > 0
				? GetFirstByTypes(mainPanel, 1, ref log, chatTypesPath)
				: mainPanel;
			// Get panel which contains edit box for sending a message.
			var editPanel = editTypesPath.Length > 0
				? GetFirstByTypes(mainPanel, 1, ref log, editTypesPath)
				: mainPanel;

			//log = "----- EditChildren\r\n" + log;
			//var editChildren = GetChildren(editPanel, 1, ref log, editTypesName);
			//log = "----- ChatChildren\r\n" + log;
			//var chatChildren = GetChildren(chatPanel, 1, ref log, chatTypesName);

			log = "----- GetCharChildren\r\n" + log;

			var walker = TreeWalker.ControlViewWalker;

			var chatChildren = GetChildren(chatPanel, 0, ref log, ControlType.Pane);
			foreach (var chatChild in chatChildren)
			{
				//var chatChildren2 = GetChildren(chatChild, 1, ref log);

				walker.GetLastChild(chatChild);
				//AutomationHelper.FindChildrenRaw(chatChild);
				log = ToString(chatChild, 2) + log;
				//foreach (var child2 in chatChildren2)
				//{
				//	//log = ToString(child2, 4) + log;
				//}
			}

			*/

		}


		// Experiment.
		void GetChildrenAll(MsaaAccessible ma, ref string log)
		{
			log = $"\r\n----- GetChildren\r\n" + log;

			var children1 = GetChildren(ma, 0, ref log);
			foreach (var child1 in children1)
			{
				var children2 = GetChildren(child1, 1, ref log);
				foreach (var child2 in children2)
				{
					var children3 = GetChildren(child2, 2, ref log);
					foreach (var child3 in children3)
					{
						var children4 = GetChildren(child3, 3, ref log);
						foreach (var child4 in children4)
						{
							var children5 = GetChildren(child4, 4, ref log);
							foreach (var child5 in children5)
							{
								var children6 = GetChildren(child5, 5, ref log);
								foreach (var child6 in children6)
								{
									var children7 = GetChildren(child6, 6, ref log);
									foreach (var child7 in children7)
									{
										var children8 = GetChildren(child7, 7, ref log);
										foreach (var child8 in children8)
										{
											var children9 = GetChildren(child8, 8, ref log);
											foreach (var child9 in children9)
											{
												var children10 = GetChildren(child9, 9, ref log);
												foreach (var child10 in children10)
												{
													var children11 = GetChildren(child10, 10, ref log);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}


		}

		public MsaaAccessible[] GetChildren(MsaaAccessible ma, int index, ref string log)
		{
			var children = Msaa.GetAccessibleChildren(ma)
				.ToArray();
			foreach (var child in children)
				log = ToString(child, index) + log;
			return children;
		}

		string ToString(MsaaAccessible e, int index)
		{
			return $"{new string(' ', index * 2)}{e.Role} '{e.Name}': {e.Error?.Message}\r\n";
		}

	}

}

