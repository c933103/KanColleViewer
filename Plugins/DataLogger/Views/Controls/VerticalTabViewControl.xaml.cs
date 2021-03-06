﻿using System;
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

namespace LynLogger.Views.Controls
{
    /// <summary>
    /// VerticalTabViewControl.xaml 的交互逻辑
    /// </summary>
    public partial class VerticalTabViewControl : UserControl, INotifyPropertyChanged
    {
        public VerticalTabViewControl()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty dpTabViewItems = DependencyProperty.Register(nameof(TabViewItems), typeof(IList<TabViewItem>), typeof(VerticalTabViewControl), new PropertyMetadata((o, e) => ((VerticalTabViewControl)o).RaisePropertyChanged(e.Property.Name)));
        public static readonly DependencyProperty dpTabViewSelected = DependencyProperty.Register(nameof(TabViewSelected), typeof(TabViewItem), typeof(VerticalTabViewControl), new PropertyMetadata((o, e) => ((VerticalTabViewControl)o).RaisePropertyChanged(e.Property.Name)));

        public IList<TabViewItem> TabViewItems
        {
            get { return (IList<TabViewItem>)GetValue(dpTabViewItems); }
            set { SetValue(dpTabViewItems, value); }
        }

        public TabViewItem TabViewSelected
        {
            get { return (TabViewItem)GetValue(dpTabViewSelected); }
            set { SetValue(dpTabViewSelected, value); }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TabViewSelected = (TabViewItem)((Border)sender).DataContext;
        }

        private void RaisePropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
