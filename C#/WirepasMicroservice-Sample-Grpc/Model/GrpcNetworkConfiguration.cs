using System;
using System.Collections.Generic;
using System.Text;

/**
 * \namsespace ElaBleGui.Model
 * \brief namespace associated to the all the model to represent data through the User Interface
 */
namespace WirepasMicroservice_Sample.Model
{
    /**
     * \class GrpcNetworkConfiguration
     * \brief display configuration for a grpc service
     */
    public class GrpcNetworkConfiguration
    {
        /** \brief grpc display name */
        public String GrpcServiceName { get; set; }

        /** \brief grpc host name */
        public String GrpcHostName { get; set; }

        /** \brief grpc communication port */
        public int GrpcPort { get; set; }

        /** \brief is the current user allowed */
        public bool UserAllowed { get; set; }

        /** \brief constructor */
        public GrpcNetworkConfiguration()
        {

        }

        /** \brief copy constructor */
        public GrpcNetworkConfiguration(GrpcNetworkConfiguration config)
        {
            this.GrpcServiceName = config.GrpcServiceName;
            this.GrpcHostName = config.GrpcHostName;
            this.GrpcPort = config.GrpcPort;
            this.UserAllowed = config.UserAllowed;
        }
    }
}
