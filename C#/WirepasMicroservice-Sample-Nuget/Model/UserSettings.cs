using System;
using System.Collections.Generic;
using System.Text;

/**
 * \namsespace ElaBleGui.Model
 * \brief namespace associated to the all the model to represent data through the User Interface
 */
namespace WirepasMicroservice_Sample.Model
{
    public class UserSettings
    {
        // bluetooth configuration
        public GrpcNetworkConfiguration WirepasConfiguration { get; set; }

        /** \brief constructor */
        public UserSettings() {

            this.WirepasConfiguration = new GrpcNetworkConfiguration()
            {
                GrpcHostName = elaMicroservicesGrpc.Constant.ElaGrpcConstants.DEFAULT_LOCALHOST,
                GrpcPort = elaMicroservicesGrpc.Constant.ElaGrpcConstants.PORT_WIREPAS_REMOTE_API,
                GrpcServiceName = elaMicroservicesGrpc.Constant.ElaGrpcConstants.DEFAULT_WIREPAS_BASE_NAME,
                UserAllowed = true
            };
        }
    }
}
