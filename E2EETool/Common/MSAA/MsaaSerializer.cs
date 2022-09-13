using JocysCom.ClassLibrary.Runtime;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MSAA
{
	public static class MsaaSerializer
	{

		public static List<MsaaItem> GetList(MsaaItem item)
		{
			var list = new List<MsaaItem>();
			FillList(item, ref list);
			for (int i = 0; i < list.Count; i++)
				list[i].Id = i;
			return list;
		}

		public static void FillList(MsaaItem item, ref List<MsaaItem> items)
		{
			items.Add(item);
			// Loop trough children.
			foreach (var child in item.Children)
				if (child.IsVisible)
					FillList(child, ref items);
		}

		public static MsaaItem[] XPathSelectElements(MsaaItem item, string xpath, List<MsaaItem> source)
		{
			var list = new List<MsaaItem>();
			var xml = Serializer.SerializeToXmlString(item);
			var element = XElement.Parse(xml);
			var elements = element.XPathSelectElements(xpath);
			foreach (var el in elements)
			{
				var elItem = Serializer.DeserializeFromXmlString<MsaaItem>(el.ToString());
				list.Add(elItem);
			}
			RestoreItems(source, list);
			return list.ToArray();
		}

		/// <summary>
		/// Set back original accessible item.
		/// </summary>
		private static void RestoreItems(List<MsaaItem> source, IEnumerable<MsaaItem> target)
		{
			foreach (var item in target)
			{
				var children = GetList(item);
				foreach (var child in children)
					child.Accessible = source[child.Id].Accessible;
			}
		}

	}
}
