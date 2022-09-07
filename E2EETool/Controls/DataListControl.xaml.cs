using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.Tools.E2EETool.Controls
{
	/// <summary>
	/// Interaction logic for DataListControl.xaml
	/// </summary>
	public partial class DataListControl : UserControl
	{
		public DataListControl()
		{
			InitializeComponent();
			if (ControlsHelper.IsDesignMode(this))
				return;
			SetDataItems(new SortableBindingList<DataItem>());
		}

		public void SetDataItems(SortableBindingList<DataItem> dataItems)
		{
			if (DataItems != null)
				DataItems.ListChanged -= DataItems_ListChanged;
			DataItems = dataItems;
			DataItems.ListChanged += DataItems_ListChanged;
			MainDataGrid.ItemsSource = dataItems;
			if (DataItems.Count > 0)
				MainDataGrid.SelectedIndex = 0;
		}

		private void Tasks_ListChanged(object sender, ListChangedEventArgs e)
			=> UpdateUpdateButton();

		private void DataItems_ListChanged(object sender, ListChangedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
			});
		}

		public SortableBindingList<DataItem> DataItems { get; set; }

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> UpdateUpdateButton();

		void UpdateUpdateButton()
		{
			MainDataGrid.SelectedItem = null;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
		}

		private void AddMessage(string user, string body, MessageType style = MessageType.None)
		{
			DataItems.Add(new DataItem()
			{
				User = user,
				Body = body,
				Style = (Style)Resources[style.ToString()],
			});
		}

		public void AddMessage(MessageType messageType, string message = null, string decryptedMessage = null)
		{
			switch (messageType)
			{
				case MessageType.None:
					break;
				case MessageType.YourPublicKey:
					AddMessage("Out", "Your Public Key", MessageType.YourPublicKey);
					break;
				case MessageType.YourMessage:
					AddMessage("Out", message, MessageType.YourPublicKey);
					break;
				case MessageType.OtherPublicKey:
					AddMessage("In", "Other Public Key", MessageType.OtherPublicKey);
					// Update public key.
					Global.AppSettings.OtherPublicKey = message;
					break;
				case MessageType.OtherMessage:
					AddMessage("In", decryptedMessage, MessageType.OtherMessage);
					break;
				default:
					break;
			}

		}

	}
}