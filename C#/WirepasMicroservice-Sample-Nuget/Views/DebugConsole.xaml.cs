using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WirepasMicroservice_Sample.Configuration;

namespace WirepasMicroservice_Sample.Views
{
    /// <summary>
    /// Logique d'interaction pour DebugConsole.xaml
    /// </summary>
    public partial class DebugConsole : UserControl
    {
        /** \brief constructor */
        public DebugConsole()
        {
            InitializeComponent();
            initializeInternalComponents();
            localizeComponents();
        }

        #region implement IUserControlBase interface
        public void initializeInternalComponents()
        {
            ElaMicroConfiguration.getInstance().Logger.evNewLogItemAdded += Logger_evNewLogItemAdded;
        }

        public void localizeComponents()
        {

        }
        #endregion

        #region event implementation
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            this.listDebugItems.Items.Clear();
        }

        private void Logger_evNewLogItemAdded(object sender, ElaSoftwareCommon.Log.LogItem item)
        {
            //
            // dispatch animation event
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                this.listDebugItems.Items.Add(item.getLine());
            }));
        }
        #endregion
    }
}
