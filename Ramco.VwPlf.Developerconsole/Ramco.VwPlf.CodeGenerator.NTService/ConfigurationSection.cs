using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ramco.VwPlf.CodeGenerator.NTService.Custom
{
    public class ModelConfiguration : ConfigurationSection
    {
        [ConfigurationProperty("models", Options = ConfigurationPropertyOptions.IsRequired)]
        public ModelCollection Models
        {
            get
            {
                return (ModelCollection)this["models"];
            }
        }
    }

    [ConfigurationCollection(typeof(ModelInfo), AddItemName = "modelInfo")]
    public class ModelCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ModelInfo();
        }

        protected override object GetElementKey(ConfigurationElement model)
        {
            if (model == null)
            {
                throw new ArgumentNullException("modelInfo");
            }
            return ((ModelInfo)model).Url;
        }
    }

    public class ModelInfo : ConfigurationElement
    {
        [ConfigurationProperty("url", IsRequired = true, IsKey = true)]
        public String Url { get { return (string)base["url"]; } }

        [ConfigurationProperty("username", IsRequired = true)]
        public String UserName { get { return (string)base["username"]; } }

        [ConfigurationProperty("password", IsRequired = true)]
        public String Password { get { return (string)base["password"]; } }

        [ConfigurationProperty("maxinstance", IsRequired = true)]
        public String MaxInstance { get { return (string)base["maxinstance"]; } }
    }
}
