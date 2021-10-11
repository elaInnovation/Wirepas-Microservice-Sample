using ElaAuthentication;
using ElaCommon;
using ElaSoftwareCommon.Error;
using ElaSoftwareCommon.Log;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WirepasMicroservice_Sample.Configuration;
using static ElaAuthentication.ElaAuthenticationPublicService;
using static ElaWirepas.ElaWirepasPublicService;

/**
 * \namespace WirepasMicroservice_Sample.Controller
 * \brief namespace dedicated to the implementation of all object usefull to connect or use the communicaiton
 */
namespace WirepasMicroservice_Sample.Controller
{
    /**
     * \class ElaGrpcClientBase
     * \brief common grpc object developped to reach each ELA microservice
     */
    public abstract class ElaGrpcClientBase
    {
        private const int CONNECTION_TIMEOUT_SECONDS = 21;

        /** \brief absolute counter for request id */
        private static uint g_uiAbsoluteCounter = 0;

        /** \brief internal client ID*/
        protected string m_strClientInternalId = string.Empty;

        /** \brief internal client IP*/
        protected string m_strClientIP = string.Empty;

        /** \brief grpc channel */
        protected Channel m_Channel;

        /** \brief last error message */
        protected string m_strLastErrorMessage = string.Empty;

        /** \brief internal sessionId definition */
        protected string m_strInternalSessionId = string.Empty;

        /** \brief generic gRPC client for common procedures */
        protected object m_GrpcClient;

        /** \brief describes service configuration (hostname, port) **/
        protected CfgService m_ServiceConfig = new CfgService();

        /** \brief client is currently streaming **/
        protected bool m_IsStreaming = false;

        /** \brief client is currently listening **/
        protected bool m_IsListening = false;

        #region accessors
        public string Hostname { get => m_ServiceConfig.host; }
        public int Port { get => m_ServiceConfig.port; }
        public string LastErrorMessage { get => m_strLastErrorMessage; }
        public string Id { get => m_strClientInternalId; }
        #endregion

        /** \brief constructor */
        public ElaGrpcClientBase()
        {
            this.m_strClientInternalId = Guid.NewGuid().ToString();
            this.m_strClientIP = GetLocalIPAddress();
        }

        #region connection functions
        /** 
         * \fn connect 
         * \brief connection with grpc framework : connect with the initial configuration
         * \retur error code
         */
        public uint reconnect()
        {
            return connect(Hostname, Port);
        }

        /** 
         * \fn connect 
         * \brief connection function to 
         * \param [in] host : hostname of the service to connect
         * \param [in] port : port to ensure the microservice connection
         * \return error code
         */
        public uint connect(string host, int port)
        {
            try
            {
                if (string.IsNullOrEmpty(host)) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_HOST_ADDRESS);
                if (port < 0) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_PORT_VALUE);

                var connectionTestResult = CreateChannel(host, port);
                CreateGrpcClient(connectionTestResult);

                return connectionTestResult;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }

        /**
         * \fn CreateGrpcClient
         * \brief abstract function declaration to create a new instante of grpc client
         */
        protected abstract void CreateGrpcClient(uint channelTestResult);

        /**
         * \fn CreateChannel
         * \brief create a new GRPC hannel for the client context
         * \param [in] host : hostname of the service to connect
         * \param [in] port : port to ensure the microservice connection
         * \return error code
         */
        private uint CreateChannel(string host, int port)
        {
            try
            {
                if (m_Channel != null)
                {
                    if (host != Hostname || port != Port || (m_Channel.State != ChannelState.Ready))
                    {
                        Task.Run(() => m_Channel.ShutdownAsync());
                        ObserveState(m_Channel, c => c.State == ChannelState.Shutdown, CONNECTION_TIMEOUT_SECONDS * 1000);
                        m_Channel = null;
                    }
                    else
                    {
                        return ErrorServiceHandlerBase.ERR_OK;
                    }
                }

                m_ServiceConfig.host = host;
                m_ServiceConfig.port = port;

                m_Channel = new Channel(Hostname, Port, ChannelCredentials.Insecure);
                m_Channel.ConnectAsync(DateTime.UtcNow.AddSeconds(CONNECTION_TIMEOUT_SECONDS)).Wait();
                //
                return m_Channel.State == ChannelState.Ready ? ErrorServiceHandlerBase.ERR_OK : ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED;
            }
            catch (AggregateException ex)
            {
                var rootException = ex.GetBaseException();
                if (rootException?.GetType() == typeof(TaskCanceledException)) return ErrorServiceHandlerClient.ERR_TIME_OUT;
                return ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }

        /**
         * \fn disconnect
         * \brief ensure the disconnection from the current channel created
         * \return error code
         */
        public uint disconnect()
        {
            var result = m_Channel.ShutdownAsync().Wait(CONNECTION_TIMEOUT_SECONDS * 1000);
            return result ? ErrorServiceHandlerBase.ERR_OK : ErrorServiceHandlerBase.ERR_KO;
        }

        /**
         * \fn isMicroserviceOnline
         * \brief provded the connection status from the microservice
         * \return status online (true) or offline (false)
         */
        public bool isMicroserviceOnline()
        {
            return m_Channel != null && m_Channel.State == ChannelState.Ready;
        }

        /**
         * \fn ObserveState
         * \brief function to watch the current grpc channel
         */
        private bool ObserveState(Channel grpcChannel, Predicate<Channel> successCondition, int timeout = -1)
        {
            if (successCondition(grpcChannel)) return true;

            var startTime = DateTime.UtcNow;
            bool result;

            do
            {
                result = successCondition(grpcChannel);
                if (timeout > 0 && !result && DateTime.UtcNow - startTime > TimeSpan.FromMilliseconds(timeout)) return false;
            }
            while (!result);

            return result;
        }
        #endregion

        #region authentication functions
        /**
         * \fn login
         * \brief login function associated to the authentication process for ELA Microservices
         * \param [in] login : user login
         * \param [in] password : password associated to the user login
         * \return error code
         */
        public uint login(string login, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(login)) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_LOGIN);
                if (string.IsNullOrEmpty(password)) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_INVALID_PASSWORD);
                if (null == this.m_GrpcClient) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED);

                ElaAuthenticationRequest request = new ElaAuthenticationRequest()
                {
                    Login = login,
                    Certificate = password,
                    Request = getRequest()
                };
                ElaAuthenticationResponse response = null;

                if (m_GrpcClient is ElaWirepasPublicServiceClient wirepasClient)
                {
                    response = wirepasClient.Connect(request);
                }
                else if (m_GrpcClient is ElaAuthenticationPublicServiceClient authClient)
                {
                    response = authClient.Login(request);
                }
                else
                {
                    this.m_strLastErrorMessage = ErrorServiceHandlerBase.getErrorMessage(ErrorServiceHandlerBase.ERR_CLIENT_NOT_CONNECTED);
                    return ErrorServiceHandlerBase.ERR_CLIENT_NOT_CONNECTED;
                }

                if (ErrorServiceHandlerBase.ERR_OK == response.Error.Error)
                    this.m_strInternalSessionId = response.SessionId;
                else
                    this.m_strLastErrorMessage = ErrorServiceHandlerBase.getErrorMessage(response.Error.Error);

                return response.Error.Error;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }

        /**
         * \fn logout
         * \brief logout function associated to the authentication process for ELA Microservices
         * \return error code
         */
        public uint logout()
        {
            try
            {
                if (null == this.m_GrpcClient) throw new ElaSoftwareCommonException(ErrorServiceHandlerClient.ERR_CLIENT_NOT_CONNECTED);

                ElaInputBaseRequest request = getRequest();
                ElaError response = null;

                if (m_GrpcClient is ElaWirepasPublicServiceClient wirepasClient)
                {
                    response = wirepasClient.Disconnect(request);
                }
                else if (m_GrpcClient is ElaAuthenticationPublicServiceClient authClient)
                {
                    response = authClient.Logout(request);
                }
                else
                {
                    this.m_strLastErrorMessage = ErrorServiceHandlerBase.getErrorMessage(ErrorServiceHandlerBase.ERR_CLIENT_NOT_CONNECTED);
                    return ErrorServiceHandlerBase.ERR_CLIENT_NOT_CONNECTED;
                }

                if (ErrorServiceHandlerBase.ERR_OK != response.Error)
                    this.m_strLastErrorMessage = ErrorServiceHandlerBase.getErrorMessage(response.Error);

                return response.Error;
            }
            catch (Exception ex)
            {
                return HandleClientException(ex);
            }
        }
        #endregion

        #region main function to manage request and response from services
        /**
         * \fn getAbsoluteId
         * \brief getter on the absolute counter 
         * \return absolute counter value 
         */
        protected static UInt32 getAbsoluteId() { return g_uiAbsoluteCounter++; }

        /**
         * \fn getRequest
         * \brief get internal reuqest
         * \return internal initialized request
         */
        protected ElaInputBaseRequest getRequest()
        {
            ElaInputBaseRequest request = new ElaInputBaseRequest();
            request.RequestId = getAbsoluteId().ToString();
            request.ClientId = m_strClientInternalId;
            request.SessionId = m_strInternalSessionId;
            request.ClientIpAddress = m_strClientIP;
            return request;
        }

        /**
         * \fn getBleRequest
         * \brief getter on the ble base request
         * \return internal intialized base ble request
         */
        protected ElaInputBaseRequest getBleRequest()
        {
            ElaInputBaseRequest request = new ElaInputBaseRequest();
            request.RequestId = getAbsoluteId().ToString();
            request.ClientId = m_strClientInternalId;
            request.SessionId = m_strInternalSessionId;
            request.ClientIpAddress = m_strClientIP;
            return request;
        }

        /**
         * \fn HandleClientException
         * \brief handle the client excemtion, use ela sofware common error handling
         * \return the associated error code
         */
        protected uint HandleClientException(Exception ex)
        {
            LoggerHandler.getInstance().Logger.add(new LogItem(ex));
            this.m_strLastErrorMessage = ex.Message;
            if (ex.GetType() == typeof(ElaSoftwareCommonException))
                return ((ElaSoftwareCommonException)ex).ErrorCode;
            else
                return ErrorServiceHandlerClient.ERR_UNHANDLED_EXCEPTION;
        }

        /**
         * \fn GetLocalIPAddress
         * \brief getter on the local ip address
         * \return value of the local ip address
         */
        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                m_strLastErrorMessage = $"Error while computing IP address: {ex.Message}";
            }

            return string.Empty;
        }

        /**
         * \fn updateConfiguration
         * \brief update the current configuraiton of the object
         * \param [in] config : configuration associated to the object, update only if this one is an instance od CfgService
         */
        public void updateConfiguration(object config)
        {
            if (config is CfgService cfg) m_ServiceConfig = cfg;
        }
        #endregion
    }
}
