using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Grabacr07.KanColleWrapper;
using Livet;

namespace Grabacr07.KanColleViewer.Models
{
	[Serializable]
	public class ProxySettings : NotificationObject, IProxySettings
	{
		#region IsEnabled 変更通知プロパティ

		private bool _IsEnabled;

		public bool IsEnabled
		{
			get { return this._IsEnabled; }
			set
			{
				if (this._IsEnabled != value)
				{
					this._IsEnabled = value;
					this.RaisePropertyChanged();
                }
                UpdateRule();
            }
		}

		#endregion
        
		#region Host 変更通知プロパティ

		private string _Host;

		[XmlElement(ElementName = "ProxyHost")]
		public string Host
		{
			get { return this._Host; }
			set
			{
				if (this._Host != value)
				{
					this._Host = value;
					this.RaisePropertyChanged();
                }
                UpdateRule();
            }
		}

		#endregion

		#region Port 変更通知プロパティ

		private ushort _Port;

		public ushort Port
		{
			get { return this._Port; }
			set
			{
				if (this._Port != value)
				{
					this._Port = value;
					this.RaisePropertyChanged();
                }
                UpdateRule();
            }
		}

		#endregion

		#region IsAuthRequired 変更通知プロパティ

		private bool _IsAuthRequired;

		public bool IsAuthRequired
		{
			get { return this._IsAuthRequired; }
			set
			{
				if (this._IsAuthRequired != value)
				{
					this._IsAuthRequired = value;
					this.RaisePropertyChanged();
                }
                UpdateRule();
            }
		}

		#endregion

		#region Username 変更通知プロパティ

		private string _Username;

		public string Username
		{
			get { return this._Username; }
			set
			{
				if (this._Username != value)
				{
					this._Username = value;
					this.RaisePropertyChanged();
                }
                UpdateRule();
            }
		}

		#endregion

        #region ProtectedPassword 変更通知プロパティ

        private string _Password;

        public string Password
        {
			get { return this._Password; }
			set
			{
				if (this._Password != value)
				{
					this._Password = value;
					this.RaisePropertyChanged();
				}
                UpdateRule();
            }
		}

        #endregion

        private void UpdateRule()
        {
            Rules[int.MaxValue] = new ProxyRule() {
                Enabled = IsEnabled,
                Type = ProxyRule.MatchType.Any,
                Action = (Host != null && IsEnabled) ? ProxyRule.MatchAction.Proxy : ProxyRule.MatchAction.SystemProxy,
                ActionString = Host?.Contains(':') == true ? string.Format("[{0}]:{1}", Host, Port) : string.Format("{0}:{1}", Host, Port),
            };
            Rules[int.MaxValue - 1] = new ProxyRule() {
                Enabled = IsAuthRequired && Username != null && Password != null,
                Type = ProxyRule.MatchType.Any,
                Action = ProxyRule.MatchAction.SetProxyAuth,
                ActionString = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Format("{0}:{1}", Username, Password))),
            };
            RaisePropertyChanged(nameof(Rules));
            ((IProxySettings)this).CompiledRules = null;
        }

        IReadOnlyDictionary<int, ProxyRule> IProxySettings.Rules => Rules;
        ProxyRule[] IProxySettings.CompiledRules { get; set; }
        
        public Data.Xml.XmlSerializableDictionary<int, ProxyRule> Rules { get; set; } = new Data.Xml.XmlSerializableDictionary<int, ProxyRule>();
    }
}
