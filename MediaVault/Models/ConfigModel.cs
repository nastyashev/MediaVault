using System.Xml.Serialization;

namespace MediaVault.Models
{
    public class ConfigModel
    {
        public string Theme { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string MediaFolderPath { get; set; } = string.Empty;
    }
}