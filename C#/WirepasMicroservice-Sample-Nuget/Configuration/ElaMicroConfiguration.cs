using ElaSoftwareCommon.Log;
using ElaSoftwareCommon.Tools;
using System;
using System.Collections.Generic;
using System.Text;
using WirepasMicroservice_Sample.Model;

namespace WirepasMicroservice_Sample.Configuration
{
    public class ElaMicroConfiguration
    {
        private static ElaMicroConfiguration instance = null;

        /** \breif internal logger definition */
        private Logger m_logger = new Logger();

        /** \brief default user management */
        private String m_currentUser = ELAFileSystem.KEY_DEFAULT_USER;

        /** \brief default BLE service configuration */
        private UserSettings m_settings = new UserSettings();

        #region accessor
        public Logger Logger { get => m_logger; }
        public UserSettings Settings { get => m_settings; }
        #endregion


        /** \brief constructor */
        private ElaMicroConfiguration()
        {
        }

        /** \brief singleton */
        public static ElaMicroConfiguration getInstance()
        {
            if (null == instance)
            {
                instance = new ElaMicroConfiguration();
            }
            return instance;
        }
    }
}
