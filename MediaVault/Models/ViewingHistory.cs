using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace MediaVault.Models
{
    [Serializable]
    public class ViewingHistoryRecord
    {
        public int RecordId { get; set; }
        public string FileId { get; set; } = string.Empty; // file path
        public string FileName { get; set; } = string.Empty;
        public DateTime ViewDate { get; set; }
        public int Duration { get; set; } // seconds
        public int EndTime { get; set; } // seconds
        public string Status { get; set; } = string.Empty;
    }

    [Serializable]
    [XmlRoot("ViewingHistoryLog")]
    public class ViewingHistoryLog
    {
        private static readonly string FilePath = "history.xml";

        [XmlElement("Record")]
        public List<ViewingHistoryRecord> Records { get; set; } = new List<ViewingHistoryRecord>();

        public static ViewingHistoryLog Load()
        {
            if (!File.Exists(FilePath))
            {
                var log = new ViewingHistoryLog();
                log.Save(); // Створити порожній файл одразу
                return log;
            }

            try
            {
                using var stream = File.OpenRead(FilePath);
                var serializer = new XmlSerializer(typeof(ViewingHistoryLog));
                return (ViewingHistoryLog)serializer.Deserialize(stream);
            }
            catch
            {
                return new ViewingHistoryLog();
            }
        }

        public void Save()
        {
            // Ensure directory exists if FilePath contains directories
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var stream = File.Create(FilePath);
            var serializer = new XmlSerializer(typeof(ViewingHistoryLog));
            serializer.Serialize(stream, this);
        }

        public void AddRecord(ViewingHistoryRecord record)
        {
            Records.Add(record);
            Save();
        }

        public int GetNextRecordId()
        {
            return Records.Count == 0 ? 1 : Records.Max(r => r.RecordId) + 1;
        }
    }
}
