using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public string Genre { get; set; } = string.Empty; // додано жанр
    }

    [Serializable]
    [XmlRoot("ViewingHistoryLog")]
    public class ViewingHistoryLog
    {
        private static readonly string DataDirectory = "Data";
        private static readonly string FilePath = Path.Combine(DataDirectory, "history.xml");

        [XmlElement("Record")]
        public ObservableCollection<ViewingHistoryRecord> Records { get; set; } = new ObservableCollection<ViewingHistoryRecord>();

        public static ViewingHistoryLog Load()
        {
            // Ensure Data directory exists
            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);

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
            // Ensure Data directory exists
            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);

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
