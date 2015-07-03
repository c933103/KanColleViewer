using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Grabacr07.KanColleViewer.Models
{
	public static class AppProductInfo
    {
        private const string Major = "3.8.2.1";
        private const string Mod = "2.0";
        private const string Revision = "1";
        private const string Train = "M";

        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
		private static string _Title;
		private static string _Description;
		private static string _Company;
		private static string _Product;
		private static string _Copyright;
		private static string _Trademark;
		private static Version _Version;
		private static string _VersionString;
		private static IReadOnlyCollection<Library> _Libraries;

		public static string Title
		{
			get { return _Title ?? (_Title = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute))).Title); }
		}

		public static string Description
		{
			get { return _Description ?? (_Description = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute))).Description); }
		}

		public static string Company
		{
			get { return _Company ?? (_Company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute))).Company); }
		}

		public static string Product
		{
			get { return _Product ?? (_Product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product); }
		}

		public static string Copyright
		{
			get { return _Copyright ?? (_Copyright = ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute))).Copyright); }
		}

		public static string Trademark
		{
			get { return _Trademark ?? (_Trademark = ((AssemblyTrademarkAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTrademarkAttribute))).Trademark); }
		}

		public static Version Version
		{
			get { return _Version ?? (_Version = assembly.GetName().Version); }
		}

		public static string VersionString
		{
			get { return _VersionString ?? (_VersionString = string.Format("{0}{1}{2}", Version.ToString(3), IsBetaRelease ? " β" : "", Version.Revision == 0 ? "" : " rev." + Version.Revision)); }
		}

        public static string ModRelease
        {
            get
            {
                return Major + "-" + Mod + "(" + Train + Revision + ")"
#if DEBUG
                     + "d"
#endif
                     ;
            }
        }

		public static bool IsBetaRelease
		{
#if BETA
			get { return true; }
#else
			get { return false; }
#endif
		}

		public static bool IsDebug
		{
#if DEBUG
			get { return true; }
#else
			get { return false; }
#endif
		}

		public static IReadOnlyCollection<Library> Libraries
		{
			get
			{
				return _Libraries ?? (_Libraries = new List<Library>
				{
					new Library("Reactive Extensions", new Uri("http://rx.codeplex.com/")),
					new Library("Interactive Extensions", new Uri("http://rx.codeplex.com/")),
					new Library("Windows API Code Pack", new Uri("http://archive.msdn.microsoft.com/WindowsAPICodePack")),
					new Library("Livet", new Uri("http://ugaya40.net/livet")),
					new Library("DynamicJson", new Uri("http://dynamicjson.codeplex.com/")),
					new Library("FiddlerCore", new Uri("http://fiddler2.com/fiddlercore")),
				});
			}
		}
	}

	public class Library
	{
		public string Name { get; private set; }
		public Uri Url { get; private set; }

		public Library(string name, Uri url)
		{
			this.Name = name;
			this.Url = url;
		}
	}
}
