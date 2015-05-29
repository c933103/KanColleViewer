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

namespace LynLogger.Views
{
    /// <summary>
    /// Interaction logic for HorizontalTabViewControl.xaml
    /// </summary>
    public partial class HorizontalTabViewControl : UserControl, INotifyPropertyChanged
    {
        public HorizontalTabViewControl()
        {
            InitializeComponent();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty dpTabViewItems = DependencyProperty.Register("TabViewItems", typeof(IList<TabViewItem>), typeof(HorizontalTabViewControl));
        public static readonly DependencyProperty dpTabViewSelected = DependencyProperty.Register("TabViewSelected", typeof(TabViewItem), typeof(HorizontalTabViewControl));

        public IList<TabViewItem> TabViewItems
        {
            get { return (IList<TabViewItem>)GetValue(dpTabViewItems); }
            set
            {
                SetValue(dpTabViewItems, value);
                if(PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TabViewItems"));
            }
        }

        public TabViewItem TabViewSelected
        {
            get { return (TabViewItem)GetValue(dpTabViewSelected); }
            set
            {
                SetValue(dpTabViewSelected, value);
                if(PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("TabViewSelected"));
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TabViewSelected = (TabViewItem)((Border)sender).DataContext;
        }
    }
}