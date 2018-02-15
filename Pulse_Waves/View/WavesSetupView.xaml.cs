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

namespace RTI
{
    /// <summary>
    /// Interaction logic for DvlSetupView.xaml
    /// </summary>
    public partial class WavesSetupView : UserControl
    {
        /// <summary>
        /// Initialize the view.
        /// </summary>
        public WavesSetupView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Workaround for the IsDefault on the Send button.  When the user presses enter, the
        /// Send Command is called for the Send button, but the
        /// property for the combobox not have the PropertyChanged called.  So the latest command
        /// is not sent and if it is the first command entered in, no command is sent.  
        /// 
        /// This will not monitor for any key presses.  If the key press is an ENTER, it will update
        /// the combobox property.  The command will then send whatever is entered in the combobox.
        /// 
        /// http://www.tigraine.at/2010/09/13/beware-of-button-isdefaulttrue-in-wpfsilverlight/
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            ((ComboBox)sender).GetBindingExpression(ComboBox.TextProperty).UpdateSource();
        }

        /// <summary>
        /// Autoscroll to the bottom.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = sender as ScrollViewer;
            //if (e.ExtentHeightChange != 0 && Math.Abs(sv.VerticalOffset - sv.ScrollableHeight) < 20)
            if (sv.ScrollableHeight - sv.VerticalOffset < 20)
            {
                sv.ScrollToEnd();
            }
        }
    }
}
