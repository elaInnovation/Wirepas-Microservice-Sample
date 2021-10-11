using System;
using System.Collections.Generic;
using System.Text;

/**
 * \namespace WirepasMicroservice_Sample.Configuratio
 * \brief namespace dedicated to the configuration 
 */
namespace WirepasMicroservice_Sample.Configuration
{
    /**
     * \class CfgService
     * \brief configuration service 
     */
    public class CfgService
    {
        // constant definition
        public static readonly String KEY_SECTION_AUTHENTICATION = "authentication";
        public static readonly String KEY_SECTION_BLUETOOTH_BASE = "bluetoothBase";
        //
        public static readonly String KEY_ENABLE = "enable";
        public static readonly String KEY_HOST = "host";
        public static readonly String KEY_PORT = "port";
        public static readonly String KEY_LOGIN = "login";
        public static readonly String KEY_PASSWORD = "password";

        /** \brief enable or disable state */
        public bool enable = true;

        /** \brief default host configuration is localhost */
        public String host = elaMicroservicesGrpc.Constant.ElaGrpcConstants.DEFAULT_LOCALHOST;

        /** \brief default port configuration is authentication */
        public int port = elaMicroservicesGrpc.Constant.ElaGrpcConstants.PORT_AUTHENTICATION_REMOTE_API;

        /** \brief login is mandatory and only use for debug to bypass the authentication module */
        public String login { get; set; }

        /** \brief password is mandatory and only use for debug to bypass the authentication module */
        public String password { get; set; }

        /** \brief default service name */
        public String name { get; set; }

        /** \brief default identifiant */
        public String id { get; set; }

        /** \brief constructor */
        public CfgService() { }

        /** 
         * \brief copy constructor 
         * \param [in] config instance of configuration to clone
         */
        public CfgService(CfgService config)
        {
            this.enable = config.enable;
            this.host = config.host;
            this.port = config.port;
            this.login = config.login;
            this.password = config.password;
            this.name = config.name;
            this.id = config.id;
        }

        /**
         * \fn Equals
         * \brief override equals
         * \param [in] obj : input object to test
         * \return true for equality, false if not
         */
        public override bool Equals(object obj)
        {
            if (null == obj) return false;

            if (obj is CfgService input)
            {
                if (input.host != this.host) return false;
                if (input.port != this.port) return false;
                if (input.name != this.name) return false;
                if (input.id != this.id) return false;
                if (input.login != this.login) return false;
                if (input.password != this.password) return false;

                return true;
            }
            else return false;
        }

        /**
         * \fn GetHashCode
         * \brief override GetHashCode
         * \return hsoh code of the object
         */
        public override int GetHashCode()
        {
            return HashCode.Combine(enable, host, port, login, password, name, id);
        }

    }
}
