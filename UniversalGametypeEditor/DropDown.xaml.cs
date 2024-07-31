using System;
using System.Collections.Generic;
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

namespace UniversalGametypeEditor
{
    /// <summary>
    /// Interaction logic for DropDown.xaml
    /// </summary>
    public partial class DropDown : UserControl
    {
        public DropDown()
        {
            InitializeComponent();
            Scroller.Loaded += Scroller_Loaded;
        }

        private void Scroller_Loaded(object sender, RoutedEventArgs e)
        {
            Scroller.ScrollChanged += Scroller_ScrollChanged;
        }

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Show or hide the top scroll indicator
            TopIndicator.Visibility = Scroller.VerticalOffset > 0 ? Visibility.Visible : Visibility.Collapsed;
            double height = Scroller.ScrollableHeight;
            height -= 10;
            // Show or hide the bottom scroll indicator
            BottomIndicator.Visibility = Scroller.VerticalOffset < height ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            ((Expander)sender).BringIntoView();
        }
    }
}
