using ElaCommon;
using ElaSoftwareCommon.Error;
using ElaWirepas;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ElaWirepas.ElaWirepasPublicService;

namespace WirepasMicroservice_Sample.Controller
{
    /** \brief delegate fot data received */
    public delegate void WirepasDataReceivedEventHandler(Object sender, String brokerhostname, ElaWirepasDataPacket data);

    /**
     * \class ElaWirepasBase
     * \brief Ela Wirepas Base implementation
     */
    public class ElaWirepasBase : ElaGrpcClientBase
    {
        /** \brief wirepas client definition */
        private ElaWirepasPublicServiceClient WirepasClient { get => m_GrpcClient as ElaWirepasPublicServiceClient; }

        /** \brief event for new wirepas data received */
        public event WirepasDataReceivedEventHandler evWirepasDataReceived = null;

        /** \brief streaming cancellation token **/
        private CancellationTokenSource CancellationToken;

        /**
         * \fn CreateGrpcClient
         * \brief abstract function declaration to create a new instante of grpc client
         */
        protected override void CreateGrpcClient(uint channelTestResult)
        {
            if (channelTestResult == ErrorServiceHandlerBase.ERR_OK)
                m_GrpcClient = new ElaWirepasPublicServiceClient(m_Channel);
            else
            {
                m_strLastErrorMessage = ErrorServiceHandlerBase.getErrorMessage(channelTestResult);
                m_GrpcClient = null;
            }
        }

        #region implement IWirepasBase interface
        public ElaWirepasMicroserviceStatus GetServiceStatus()
        {
            try
            {
                ElaWirepasMicroserviceStatus status = new ElaWirepasMicroserviceStatus();
                if (null == this.WirepasClient)
                {
                    status.LastError = new ElaError();
                    status.LastError.Error = ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED;
                    status.LastError.LastErrorMessage = ErrorServiceHandlerBase.getErrorMessage(status.LastError.Error);
                    return status;
                }
                //
                ElaInputBaseRequest request = getRequest();
                status = this.WirepasClient.GetServiceStatus(request);
                //
                return status;
            }
            catch (Exception ex)
            {
                uint errorCode = HandleClientException(ex);

                return new ElaWirepasMicroserviceStatus()
                {
                    LastError = new ElaError()
                    {
                        LastErrorMessage = ex.Message,
                        LastExceptionMessage = (null == ex.InnerException) ? String.Empty : ex.InnerException.Message,
                        Error = errorCode
                    }
                };
            }
        }

        public async Task<ElaWirepasMicroserviceStatus> GetServiceStatusAsync()
        {
            try
            {
                ElaWirepasMicroserviceStatus status = new ElaWirepasMicroserviceStatus();
                if (null == this.WirepasClient)
                {
                    status.LastError = new ElaError();
                    status.LastError.Error = ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED;
                    status.LastError.LastErrorMessage = ErrorServiceHandlerBase.getErrorMessage(status.LastError.Error);
                    return status;
                }
                //
                ElaInputBaseRequest request = getRequest();
                status = await this.WirepasClient.GetServiceStatusAsync(request);
                //
                return status;
            }
            catch (Exception ex)
            {
                uint errorCode = HandleClientException(ex);

                return new ElaWirepasMicroserviceStatus()
                {
                    LastError = new ElaError()
                    {
                        LastErrorMessage = ex.Message,
                        LastExceptionMessage = (null == ex.InnerException) ? String.Empty : ex.InnerException.Message,
                        Error = errorCode
                    }
                };
            }
        }

        public Tuple<uint, String> SendElaWirepasCommand(string brokerHostname, uint brokerPort, uint networkId, uint destinationAddress, uint sourceEndpoint, uint destinationEndpoint, string command)
        {
            if (null == this.WirepasClient) return new Tuple<uint, String>(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED, String.Empty);
            //
            uint errorCode = ErrorServiceHandlerBase.ERR_OK;
            string resultFromTag = String.Empty;
            //
            try
            {
                ElaWirepasSendCommandRequest sendCommandRequest = new ElaWirepasSendCommandRequest()
                {
                    Request = getRequest(),
                    BrokerAddress = brokerHostname,
                    BrokerPort = (int)brokerPort,
                    DestinationAddress = destinationAddress,
                    NetworkId = networkId,
                    SourceEndpoint = sourceEndpoint,
                    DestinationEndpoint = destinationEndpoint,
                    Qos = 0,
                    Command = command,
                };

                ElaError result = this.WirepasClient.SendElaWirepasCommand(sendCommandRequest);
                resultFromTag = ErrorServiceHandlerBase.getErrorMessage(result.Error);
            }
            catch (Exception ex)
            {
                errorCode = HandleClientException(ex);
            }

            return new Tuple<uint, String>(errorCode, resultFromTag);
        }

        public async Task<Tuple<uint, String>> SendElaWirepasCommandAsync(string brokerHostname, uint brokerPort, uint networkId, uint destinationAddress, uint sourceEndpoint, uint destinationEndpoint, string command)
        {
            if (null == this.WirepasClient) return new Tuple<uint, String>(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED, String.Empty);
            //
            uint errorCode = ErrorServiceHandlerBase.ERR_OK;
            string resultFromTag = String.Empty;
            //
            try
            {
                ElaWirepasSendCommandRequest sendCommandRequest = new ElaWirepasSendCommandRequest()
                {
                    Request = getRequest(),
                    BrokerAddress = brokerHostname,
                    BrokerPort = (int)brokerPort,
                    DestinationAddress = destinationAddress,
                    NetworkId = networkId,
                    SourceEndpoint = sourceEndpoint,
                    DestinationEndpoint = destinationEndpoint,
                    Qos = 0,
                    Command = command,
                };

                ElaError result = await this.WirepasClient.SendElaWirepasCommandAsync(sendCommandRequest);
                resultFromTag = ErrorServiceHandlerBase.getErrorMessage(result.Error);
            }
            catch (Exception ex)
            {
                errorCode = HandleClientException(ex);
            }

            return new Tuple<uint, String>(errorCode, resultFromTag);
        }

        /**
         * \fn streamWirepasDataFlow
         * \brief thread function associated for the streaming data coming from wirepas service
         * \param [in] data : input object, used only ElaWirepasDataRequest
         */
        private async void streamWirepasDataFlow(object data)
        {
            try
            {
                if (data is ElaWirepasDataRequest request)
                {
                    using (var serverStream = this.WirepasClient.StartWirepasDataFlow(request))
                    {
                        while (await serverStream.ResponseStream.MoveNext(CancellationToken.Token))
                        {
                            ElaWirepasDataPacket dataPacket = serverStream.ResponseStream.Current;
                            if (null != dataPacket)
                            {
                                if (dataPacket.CloseStreamingRequest != null && dataPacket.CloseStreamingRequest.CloseSteamingRequiered)
                                {
                                    m_strLastErrorMessage =
                                        !string.IsNullOrEmpty(dataPacket.CloseStreamingRequest.Error.LastErrorMessage) ?
                                        dataPacket.CloseStreamingRequest.Error.LastErrorMessage :
                                        dataPacket.CloseStreamingRequest.Error.LastExceptionMessage;

                                    StopWirepasDataFlow();
                                }
                                evWirepasDataReceived?.Invoke(this, request.BrokerAddress, dataPacket);
                            }

                            Thread.Sleep(10);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.m_strLastErrorMessage = ex.Message;
            }
            finally
            {
                if (CancellationToken != null)
                {
                    CancellationToken.Dispose();
                    CancellationToken = null;
                }
                m_IsStreaming = false;
            }
        }

        /**
         * \fn StartWirepasDataFlow
         * \brief start a wirepas data flow on the wirepas microservice
         * \param [in] hostname : target hostname for the broker mqtt
         * \param [in] port : target port for the mqtt broker
         * \return error code
         */
        public uint StartWirepasDataFlow(string brokerAddress, int brokerPort)
        {
            try
            {
                if (null == this.WirepasClient) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED);
                if (String.IsNullOrEmpty(brokerAddress)) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_HOST_ADDRESS);
                if (brokerPort < 0) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_PORT_VALUE);
                if (m_IsStreaming) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_STREAMING_THREAD_ALREADY_STARTED);
                //
                ElaWirepasDataRequest request = new ElaWirepasDataRequest()
                {
                    Request = getRequest(),
                    BrokerAddress = brokerAddress,
                    BrokerPort = brokerPort
                };
                //
                CancellationToken = new CancellationTokenSource();
                m_IsStreaming = true;
                Thread thWirepasFlow = new Thread(streamWirepasDataFlow);
                thWirepasFlow.Start(request);
                //
                return ErrorServiceHandlerBase.ERR_OK;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }

        /**
         * \fn StopWirepasDataFlow
         * \brief stop wirepas data flow for the target broker
         * \param [in] hostname : target hostname for the broker mqtt
         * \param [in] port : target port for the mqtt broker
         * \return error code
         */
        public uint StopWirepasDataFlow()
        {
            try
            {
                if (null == this.WirepasClient) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED);
                if (CancellationToken == null) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_WIREPAS_NO_CANCELLATION_TOKEN);

                CancellationToken.Cancel();
                return ErrorServiceHandlerBase.ERR_OK;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }

        public uint StartListening(string brokerHostname, int brokerPort)
        {
            try
            {
                if (null == this.WirepasClient) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED);
                if (String.IsNullOrEmpty(brokerHostname)) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_HOST_ADDRESS);
                if (brokerPort < 0) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_PORT_VALUE);
                //
                ElaWirepasDataRequest request = new ElaWirepasDataRequest()
                {
                    Request = getRequest(),
                    BrokerAddress = brokerHostname,
                    BrokerPort = brokerPort
                };
                //
                var result = WirepasClient.StartListening(request);
                if (result.Error != ErrorServiceHandlerBase.ERR_OK)
                {
                    m_strLastErrorMessage = !string.IsNullOrEmpty(result.LastErrorMessage) ? result.LastErrorMessage : result.LastExceptionMessage;
                }
                //
                return result.Error;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }

        public List<ElaWirepasDataPacket> GetPackets(int number = -1)
        {
            try
            {
                if (null == this.WirepasClient) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED);

                ElaWirepasDataRequest request = new ElaWirepasDataRequest()
                {
                    Request = getRequest(),
                    PacketsCount = number
                };
                var result = WirepasClient.GetPackets(request);

                if (result.Error.Error == ErrorServiceHandlerBase.ERR_OK)
                {
                    return new List<ElaWirepasDataPacket>(result.PacketsList);
                }
                else
                {
                    m_strLastErrorMessage = !string.IsNullOrEmpty(result.Error.LastErrorMessage) ? result.Error.LastErrorMessage : result.Error.LastExceptionMessage;
                    return null;
                }
            }
            catch (Exception ex)
            {
                m_strLastErrorMessage = $"Unknown exception: {ex.Message}";
                return null;
            }
        }

        public uint StopListening()
        {
            try
            {
                if (null == this.WirepasClient) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED);

                var result = WirepasClient.StopListening(getRequest());
                if (result.Error != ErrorServiceHandlerBase.ERR_OK)
                {
                    m_strLastErrorMessage = !string.IsNullOrEmpty(result.LastErrorMessage) ? result.LastErrorMessage : result.LastExceptionMessage;
                }
                return result.Error;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }
        #endregion
    }

}
