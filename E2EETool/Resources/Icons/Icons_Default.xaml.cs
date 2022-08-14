using System.Windows;

namespace JocysCom.Tools.E2EETool.Resources.Icons
{
	partial class Icons_Default : ResourceDictionary
	{
		public Icons_Default()
		{
			InitializeComponent();
		}

		public static Icons_Default Current => _Current = _Current ?? new Icons_Default();
		private static Icons_Default _Current;

		public const string Icon_add = nameof(Icon_add);
		public const string Icon_clipboard_paste = nameof(Icon_clipboard_paste);
		public const string Icon_copy = nameof(Icon_copy);
		public const string Icon_delete = nameof(Icon_delete);
		public const string Icon_gearwheel = nameof(Icon_gearwheel);
		public const string Icon_inbox_into = nameof(Icon_inbox_into);
		public const string Icon_inbox_out = nameof(Icon_inbox_out);
		public const string Icon_information = nameof(Icon_information);
		public const string Icon_key = nameof(Icon_key);
		public const string Icon_lock = nameof(Icon_lock);

	}
}
