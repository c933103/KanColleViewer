using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grabacr07.KanColleViewer.Models;
using Grabacr07.KanColleViewer.ViewModels.Messages;
using Grabacr07.KanColleWrapper;
using Livet;
using Livet.EventListeners;
using Livet.Messaging.Windows;
using MetroRadiance;
using Grabacr07.KanColleViewer.ViewModels.Contents;
using Grabacr07.KanColleViewer.ViewModels.Contents.Fleets;
using System.Collections.ObjectModel;

namespace Grabacr07.KanColleViewer.ViewModels
{
	public class MainWindowViewModel : WindowViewModel
	{
		private Mode currentMode;

		public SettingsViewModel Settings { get; private set; }

		#region Mode 変更通知プロパティ

		public Mode Mode
		{
			get { return this.currentMode; }
			set
			{
				this.currentMode = value;
				switch (value)
				{
					case Mode.NotStarted:
                        this.StatusBar = null;
						StatusService.Current.Set(Properties.Resources.StatusBar_NotStarted);
						ThemeService.Current.ChangeAccent(Accent.Purple);
                        _browser.ShowNavigator = true;
                        break;
					case Mode.Started:
                        DispatcherHelper.UIDispatcher.BeginInvoke(new Action(() => {
                            if (SelectedItem == StartContentViewModel.Instance)
                                SelectedItem = TabItems.FirstOrDefault(x => x != StartContentViewModel.Instance);
                            TabItems.Remove(StartContentViewModel.Instance);
                        }));
						StatusService.Current.Set(Properties.Resources.StatusBar_Ready);
						ThemeService.Current.ChangeAccent(Accent.Blue);
                        _browser.ShowNavigator = false;
                        break;
					case Mode.InSortie:
						ThemeService.Current.ChangeAccent(Accent.Orange);
                        _browser.ShowNavigator = false;
                        break;
				}

				this.RaisePropertyChanged();
			}
		}

		#endregion

		#region StatusMessage 変更通知プロパティ

		private string _StatusMessage;

		public string StatusMessage
		{
			get { return this._StatusMessage; }
			set
			{
				if (this._StatusMessage != value)
				{
					this._StatusMessage = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		#region StatusBar 変更通知プロパティ

		private ViewModel _StatusBar;

		public ViewModel StatusBar
		{
			get { return this._StatusBar; }
			set
			{
				if (this._StatusBar != value)
				{
					this._StatusBar = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

        private bool _downloadActivity = false;
        public bool DownloadActive { get { return _downloadActivity = !_downloadActivity; } set { RaisePropertyChanged(); } }
        private bool _uploadActivity = false;
        public bool UploadActive { get { return _uploadActivity = !_uploadActivity; } set { RaisePropertyChanged(); } }

        private volatile int _outstandingRequests = 0;
        public int OutstandingRequests { get { return _outstandingRequests; } }

        public AdmiralViewModel Admiral { get; private set; }
        public MaterialsViewModel Materials { get; private set; }
        public ShipsViewModel Ships { get; private set; }
        public SlotItemsViewModel SlotItems { get; private set; }

        public FleetsViewModel Fleets { get; private set; }
        public ShipyardViewModel Shipyard { get; private set; }
        public QuestsViewModel Quests { get; private set; }
        public ExpeditionsViewModel Expeditions { get; private set; }

        private readonly bool _mini;
        private readonly BrowserViewModel _browser;
        public BrowserViewModel Browser => _mini ? null : _browser;

        public IList<TabItemViewModel> TabItems { get; set; }

        #region SelectedItem 変更通知プロパティ

        private TabItemViewModel _SelectedItem;

        public TabItemViewModel SelectedItem
        {
            get { return this._SelectedItem; }
            set
            {
                if (this._SelectedItem != value) {
                    if (this._SelectedItem != null) _SelectedItem.IsSelected = false;
                    this._SelectedItem = value;
                    if (value != null) value.IsSelected = true;

                    this.RaisePropertyChanged();
                    StatusBar = value;
                }
            }
        }

        #endregion

        public MainWindowViewModel()
		{
			this.Title = AppProductInfo.Title;
			this.Settings = new SettingsViewModel();

			this.CompositeDisposable.Add(new PropertyChangedEventListener(StatusService.Current)
			{
				{ () => StatusService.Current.Message, (sender, args) => this.StatusMessage = StatusService.Current.Message },
			});
			this.CompositeDisposable.Add(new PropertyChangedEventListener(KanColleClient.Current)
			{
				{ () => KanColleClient.Current.IsStarted, (sender, args) => this.UpdateMode() },
				{ () => KanColleClient.Current.IsInSortie, (sender, args) => this.UpdateMode() },
			});

            this._browser = new BrowserViewModel();

            this.UpdateMode();
            Models.Settings.Current.PropertyChanged += (_, __) => this.UpdateLayout(Models.Settings.Current.LRSplit);

            Fiddler.FiddlerApplication.OnReadResponseBuffer += (_, e) => DownloadActive = true;
            Fiddler.FiddlerApplication.OnReadRequestBuffer += (_, e) => UploadActive = true;
            Fiddler.FiddlerApplication.BeforeRequest += _ => { System.Threading.Interlocked.Increment(ref _outstandingRequests); RaisePropertyChanged(nameof(OutstandingRequests)); };
            Fiddler.FiddlerApplication.BeforeResponse += _ => { System.Threading.Interlocked.Decrement(ref _outstandingRequests); RaisePropertyChanged(nameof(OutstandingRequests)); };
            Fiddler.FiddlerApplication.BeforeReturningError += _ => { if (_.responseCode == 408) return; System.Threading.Interlocked.Decrement(ref _outstandingRequests); RaisePropertyChanged(nameof(OutstandingRequests)); };

            this.Admiral = new AdmiralViewModel();
            this.Materials = new MaterialsViewModel();
            this.Ships = new ShipsViewModel();
            this.SlotItems = new SlotItemsViewModel();

            this.Fleets = new FleetsViewModel();
            this.Shipyard = new ShipyardViewModel();
            this.Quests = new QuestsViewModel();
            this.Expeditions = new ExpeditionsViewModel(this.Fleets);

            this.TabItems = new ObservableCollection<TabItemViewModel>
            {
                StartContentViewModel.Instance,
                new OverviewViewModel(this),
                this.Fleets,
                this.Shipyard,
                this.Quests,
                this.Expeditions,
                new ToolsViewModel(),
                new SettingsViewModel(),
                #region DEBUG
#if false
				new DebugTabViewModel(),
#endif
                #endregion
            };
            if(_mini = Models.Settings.Current.MiniLayout) {
                TabItems.Insert(0, _browser);
            }
            this.SelectedItem = this.TabItems.FirstOrDefault();
        }
        
		/// <summary>
		/// メイン ウィンドウをアクティブ化することを試みます。
		/// </summary>
		public void Activate()
		{
			this.Messenger.Raise(new WindowActionMessage(WindowAction.Active, "Window/Activate"));
		}


		private void UpdateMode()
		{
			this.Mode = KanColleClient.Current.IsStarted
				? KanColleClient.Current.IsInSortie
					? Mode.InSortie
					: Mode.Started
				: Mode.NotStarted;
		}

        internal void UpdateLayout(bool LR)
        {
            if(LR) {
                ((Views.MainWindow)App.Instance.MainWindow).Row0Height.Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                ((Views.MainWindow)App.Instance.MainWindow).Row1Height.Height = System.Windows.GridLength.Auto;
                ((Views.MainWindow)App.Instance.MainWindow).Col0Width.Width = System.Windows.GridLength.Auto;
                ((Views.MainWindow)App.Instance.MainWindow).Col1Width.Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
            } else {
                ((Views.MainWindow)App.Instance.MainWindow).Row0Height.Height = System.Windows.GridLength.Auto;
                ((Views.MainWindow)App.Instance.MainWindow).Row1Height.Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                ((Views.MainWindow)App.Instance.MainWindow).Col0Width.Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star);
                ((Views.MainWindow)App.Instance.MainWindow).Col1Width.Width = System.Windows.GridLength.Auto;
            }
        }
	}
}
