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
using WirepasMicroservice_Sample.Configuration;
using WirepasMicroservice_Sample.Views;

namespace WirepasMicroservice_Sample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /** \brief constructor */
        public MainWindow()
        {
            InitializeComponent();
            initializeInternalComponents();
            localizeComponents();
        }

        #region implement IuserControlBase interface
        public void initializeInternalComponents()
        {
            TabItem settingsTab = new TabItem();
            //
            if (true == ElaMicroConfiguration.getInstance().Settings?.WirepasConfiguration?.UserAllowed)
            {
                TabItem blueTab = new TabItem();
                blueTab.Header = ElaMicroConfiguration.getInstance().Settings?.WirepasConfiguration.GrpcServiceName;
                blueTab.Content = new MicroWirepasBaseManager() { Tag = ElaMicroConfiguration.getInstance().Settings?.WirepasConfiguration.GrpcServiceName };
                this.tabMain.Items.Add(blueTab);
            }
            //
        }

        public void localizeComponents()
        {

        }
        #endregion
    }
}
