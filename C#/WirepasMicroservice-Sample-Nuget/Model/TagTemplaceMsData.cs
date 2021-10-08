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
     * \class TagTemplaceMsData
     * \brief class dedicated to all information related to the tag information used to manage funcitonnalities
     */
    public class TagTemplaceMsData
    {
        /** \brief target tag name */
        public String name { get; set; }
        
        /** \brief target tag mac address */
        public String mac { get; set; }
        
        /** \brief tag bluetooth password */
        public String password { get; set; }
        
        /** \brief taregt command */
        public String command { get; set; }
        
        /** target arguments */
        public String arguments { get; set; }
        
        /** \brief target confugration  file */
        public String configurationfile { get; set; }

        /** \brief constructor */
        public TagTemplaceMsData() { }
        
        /** 
         * \brief copy constructor 
         */
        public TagTemplaceMsData(TagTemplaceMsData t)
        {

            this.name = t.name;
            this.mac = t.mac;
            this.password = t.password;
            this.command = t.command;
            this.arguments = t.arguments;
            this.configurationfile = t.configurationfile;
        }
    }
}
