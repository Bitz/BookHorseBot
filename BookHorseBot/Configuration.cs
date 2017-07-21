using System.Collections.Generic;
using System.Xml.Serialization;
using BookHorseBot.Functions;

namespace BookHorseBot
{
    public static class Configuration
    {
        public static Models.Config C;
        public static string FileName = "Configuration.xml";

        static Configuration()
        {
            if (C == null)
            {
                C = Load.Config();
            }
        }

    }
}

namespace BookHorseBot.Models
{
    [XmlRoot(ElementName = "FimFiction")]
    public class FimFiction
    {
        [XmlElement(ElementName = "Token")]
        public string Token { get; set; }
        [XmlElement(ElementName = "ClientId")]
        public string ClientId { get; set; }
        [XmlElement(ElementName = "ClientSecret")]
        public string ClientSecret { get; set; }
    }

    [XmlRoot(ElementName = "Reddit")]
    public class Reddit
    {
        [XmlElement(ElementName = "Username")]
        public string Username { get; set; }
        [XmlElement(ElementName = "Password")]
        public string Password { get; set; }
        [XmlElement(ElementName = "ClientId")]
        public string ClientId { get; set; }
        [XmlElement(ElementName = "ClientSecret")]
        public string ClientSecret { get; set; }
    }

    [XmlRoot(ElementName = "Ignored")]
    public class Ignored
    {
        [XmlElement(ElementName = "User")]
        public List<string> User { get; set; }
    }

    [XmlRoot(ElementName = "Config")]
    public class Config
    {
        [XmlElement(ElementName = "FimFiction")]
        public FimFiction FimFiction { get; set; }
        [XmlElement(ElementName = "Reddit")]
        public Reddit Reddit { get; set; }
        [XmlElement(ElementName = "Ignored")]
        public Ignored Ignored { get; set; }
    }

}