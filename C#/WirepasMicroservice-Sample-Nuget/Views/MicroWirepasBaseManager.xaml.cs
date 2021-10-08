using elaMicroserviceClient.Core.Wirepas.Single.Client;
using ElaSoftwareCommon.Error;
using ElaSoftwareCommon.Log;
using ElaWirepas;
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
using WirepasMicroservice_Sample.Configuration;

namespace WirepasMicroservice_Sample.Views
{
    /// <summary>
    /// Logique d'interaction pour MicroWirepasBaseManager.xaml
    /// </summary>
    public partial class MicroWirepasBaseManager : UserControl
    {
        /** \brief target wirepas base client */
        private ElaWirepasBase m_client = new ElaWirepasBase();

        /** \brief constructor */
        public MicroWirepasBaseManager()
        {
            InitializeComponent();
            initializeInternalComponents();
        }

        private void initializeInternalComponents()
        {
        }

        #region event implementation
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                uint errorCode = this.m_client.connect(ElaMicroConfiguration.getInstance().Settings.WirepasConfiguration.GrpcHostName, ElaMicroConfiguration.getInstance().Settings.WirepasConfiguration.GrpcPort);
                if (0 != errorCode)
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"An error occur during connection : error code {errorCode} ({m_client.LastErrorMessage})"));
                }
                else ElaMicroConfiguration.getInstance().Logger.add(new LogItem("Connection Established"));
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void btnConnectAuthen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                uint errorCode = this.m_client.login(this.tbUserName.Text, this.pwdUser.Password);
                if (0 != errorCode)
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Authentication Connect failed : error code {errorCode} ({m_client.LastErrorMessage})"));
                }
                else ElaMicroConfiguration.getInstance().Logger.add(new LogItem("Authenticate Connect Success"));
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void btnDisconnectAuthent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                uint errorCode = this.m_client.logout();
                if (0 != errorCode)
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Authentication Disconnect failed : error code {errorCode} ({m_client.LastErrorMessage})"));
                }
                else ElaMicroConfiguration.getInstance().Logger.add(new LogItem("Authenticate Disconnection Success"));
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void btnStartWirepasFlow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int port = 0;
                string hostname = tbBrokerHostnameValue.Text;
                if (int.TryParse(tbBrokerWirepasPortValue.Text, out port))
                {
                    uint errorCode = m_client.StartWirepasDataFlow(hostname, port);
                    if (0 != errorCode)
                    {
                        ElaMicroConfiguration.getInstance().Logger.add(new LogItem(String.Format("An error occur during StartWirepasDataFlow : error code {0} ({1})", errorCode, ErrorServiceHandlerBase.getErrorMessage(errorCode))));
                    }
                    else
                    {
                        ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"StartWirepasDataFlow on hostname = {hostname} / port = {port} SUCCESS !"));
                        m_client.evWirepasDataReceived += M_client_evWirepasDataReceived;
                    }
                }
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void M_client_evWirepasDataReceived(object sender, string brokerhostname, ElaWirepas.ElaWirepasDataPacket data)
        {
            try
            {
                if (data.CloseStreamingRequest != null && data.CloseStreamingRequest.CloseSteamingRequiered)
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Error: {data}", EnuLogLevel.Info));
                    m_client.evWirepasDataReceived -= M_client_evWirepasDataReceived;
                }
                else ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Sensor Data Received : {data}", EnuLogLevel.Info));
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void btnStopWirepasFlow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //int port = 0;
                //string hostname = this.tbBrokerHostnameValue.Text;
                //if (int.TryParse(this.tbBrokerWirepasPortValue.Text, out port))
                //{
                uint errorCode = this.m_client.StopWirepasDataFlow();
                if (0 != errorCode)
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem(String.Format("An error occur during StopWirepasDataFlow : error code {0} ({1})", errorCode, ErrorServiceHandlerBase.getErrorMessage(errorCode))));
                }
                else
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"StopWirepasDataFlow SUCCESS"));
                }
                this.m_client.evWirepasDataReceived -= M_client_evWirepasDataReceived;
                //}
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private async void btnSendWirepasCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (false == this.tbCommandValue.Equals(String.Empty)
                    && false == this.tbTagDestinationEndpointValue.Equals(String.Empty)
                    && false == this.tbTagSourceEndpointValue.Equals(String.Empty)
                    && false == this.tbTagAddressDecimalValue.Equals(String.Empty)
                    && false == this.tbTagNetworkIdDecimalValue.Equals(String.Empty))
                {
                    uint uiAddress = 0, destinationEndpoint = 0, sourceEndpoint = 0;
                    if (uint.TryParse(this.tbTagDestinationEndpointValue.Text, out destinationEndpoint)
                        && uint.TryParse(this.tbTagNetworkIdDecimalValue.Text, out var networkId)
                        && uint.TryParse(this.tbTagSourceEndpointValue.Text, out sourceEndpoint)
                        && uint.TryParse(this.tbTagAddressDecimalValue.Text, out uiAddress)
                        && uint.TryParse(this.tbBrokerWirepasPortValue.Text, out var brokerPort))
                    {
                        Tuple<uint, String> response = await this.m_client.SendElaWirepasCommandAsync(this.tbBrokerHostnameValue.Text, brokerPort, networkId, uiAddress, sourceEndpoint, destinationEndpoint, this.tbCommandValue.Text);
                        if (ErrorServiceHandlerBase.ERR_OK != response.Item1)
                        {
                            ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"An error occurs in Send Command : {ErrorServiceHandlerBase.getErrorMessage(response.Item1)}", EnuLogLevel.Error));
                        }
                        else ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Reponse from tag with SUCCESS", EnuLogLevel.Info));
                    }
                }
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void btnGetStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ElaWirepasMicroserviceStatus status = this.m_client.GetServiceStatus();
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem("**************************************************"));
                if (null != status.MicroserviceInfos) ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Version Service : {status.MicroserviceInfos.ServiceVersion.ToString()}"));
                if (null != status.MicroserviceInfos) ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Version Proto : {status.MicroserviceInfos.ProtoVersion.ToString()}"));
                if (null != status.MicroserviceInfos) ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Service Name : {status.MicroserviceInfos.ServiceName.ToString()}"));
                if (null != status.MicroserviceInfos) ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Target OS : {status.MicroserviceInfos.TargetOS.ToString()}"));
                if (null != status.MicroserviceInfos) ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Connector Name : {status.MicroserviceInfos.ConnectorName.ToString()}"));
                //
                if (null != status.StatisticsSummary) ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Stats Functions : {status.StatisticsSummary.StatsFunctionCall.ToString()}"));
                if (null != status.StatisticsSummary) ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Stats Timing : {status.StatisticsSummary.StatsTiming.ToString()}"));
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }
        #endregion

        private void btnStartListening_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int port = 0;
                string hostname = tbBrokerHostnameValue.Text;
                if (int.TryParse(tbBrokerWirepasPortValue.Text, out port))
                {
                    uint errorCode = m_client.StartListening(hostname, port);
                    if (0 != errorCode)
                    {
                        ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"An error occur during StartListening : error code {errorCode} ({ErrorServiceHandlerBase.getErrorMessage(errorCode)}, {m_client.LastErrorMessage})"));
                    }
                    else
                    {
                        ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"StartListening on hostname = {hostname} / port = {port} SUCCESS !"));
                    }
                }
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void btnGetPackets_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int packetCount = -1;
                if (!string.IsNullOrEmpty(tbPacketCount.Text))
                {
                    int.TryParse(tbPacketCount.Text, out packetCount);
                }

                var packets = m_client.GetPackets(packetCount);

                if (packets != null)
                {
                    foreach (ElaWirepasDataPacket packet in packets)
                    {
                        ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"{packet}"));
                    }
                }
                else
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Error: {m_client.LastErrorMessage}"));
                }


            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }

        private void btnStopListening_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = m_client.StopListening();
                if (result == ErrorServiceHandlerBase.ERR_OK)
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Stopped listening with success"));
                }
                else
                {
                    ElaMicroConfiguration.getInstance().Logger.add(new LogItem($"Failed to stop listening: error code {result} ({m_client.LastErrorMessage})"));
                }
            }
            catch (Exception ex)
            {
                ElaMicroConfiguration.getInstance().Logger.add(new LogItem(ex));
            }
        }
    }
}
