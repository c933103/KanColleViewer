using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ShipInfo = LynLogger.Models.Battling.BattleProcess.ShipInfo;

namespace LynLogger.Views.Contents
{
    /// <summary>
    /// FleetInfoPresenter.xaml 的交互逻辑
    /// </summary>
    public partial class FleetInfoPresenter : UserControl, INotifyPropertyChanged
    {
        public FleetInfoPresenter()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty dpShips = DependencyProperty.Register(nameof(Ships), typeof(IEnumerable<ShipInfo>), typeof(FleetInfoPresenter), new PropertyMetadata(
            (o, e) => {
                var handler = ((FleetInfoPresenter)o).PropertyChanged;
                if (handler != null) handler(o, new PropertyChangedEventArgs(e.Property.Name));
            }
        ));

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<ShipInfo> Ships
        {
            get { return (IEnumerable<ShipInfo>)GetValue(dpShips); }
            set { SetValue(dpShips, value); }
        }

        private bool _dispEquiptDetail;
        public bool DisplayEquiptDetail
        {
            get { return _dispEquiptDetail; }
            set
            {
                if (_dispEquiptDetail == value) return;
                _dispEquiptDetail = value;
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(nameof(DisplayEquiptDetail)));
            }
        }
    }
}
