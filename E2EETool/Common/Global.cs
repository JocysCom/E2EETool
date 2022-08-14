using System.Linq;

namespace JocysCom.Tools.E2EETool
{
	public static class Global
	{

		public static AppData AppSettings =>
			AppData.Items.FirstOrDefault();

		public static ClassLibrary.Configuration.SettingsData<AppData> AppData =
			new ClassLibrary.Configuration.SettingsData<AppData>(null, null, null, null);

	}
}
