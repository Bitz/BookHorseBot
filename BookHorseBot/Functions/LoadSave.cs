using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using BookHorseBot.Models;

namespace BookHorseBot.Functions
{
    class Load
    {
        /// <summary>
        /// Load the contents of the Configuration.xml file in the .exe's current directory.
        /// </summary>
        /// <returns></returns>
        public static Config Config()
        {

            string fileName = Configuration.FileName;
            string path = !Get.IsMono() ?
                $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\{fileName}" :
                $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/{fileName}";
            XmlDocument config = new XmlDocument();
            config.Load(path);
            XmlNode node = config.DocumentElement;
            Config settings = XmlToClass<Config>(node);
            return settings;
        }

        private static T XmlToClass<T>(XmlNode node) where T : class
        {
            MemoryStream stm = new MemoryStream();
            StreamWriter stw = new StreamWriter(stm);
            stw.Write(node.OuterXml);
            stw.Flush();
            stm.Position = 0;
            XmlSerializer ser = new XmlSerializer(typeof(T));
            return ser.Deserialize(stm) as T;
        }
    }


    class Save
    {
        public static bool Config()
        {

            string fileName = Configuration.FileName;
            XmlDocument config = new XmlDocument();
            string path = !Get.IsMono() ?
                $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\{fileName}" :
                $@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/{fileName}";
            XmlSerializer xs = new XmlSerializer(typeof(Config));
            using (MemoryStream stream = new MemoryStream())
            {
                xs.Serialize(stream, Configuration.C);
                stream.Position = 0;
                config.Load(stream);
                config.Save(path);
                stream.Close();
            }
            return true;
        }
    }
}
