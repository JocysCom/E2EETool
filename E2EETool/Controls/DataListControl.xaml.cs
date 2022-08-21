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
			AddMessage("In", "Message In", true);
			AddMessage("Out", "Message Out");
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

		public void AddMessage(string user, string body, bool incomming = false)
		{
			DataItems.Add(new DataItem()
			{
				User = user,
				Body = body,
				Style = (Style)Resources[ incomming ? "MessageIn" : "MessageOut"]
			});
		}

	}
}