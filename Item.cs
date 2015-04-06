using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Disk.SDK;

namespace CCCV
{
    [Serializable]
    class Item
    {
        object data;
        string path;
        DateTime time;
        string type;
        long size;
        bool uploaded;

        public Item(object data, string path, DateTime time, string type, long size)
        {
            this.data = data;
            this.path = path;
            this.time = time;
            this.type = type;
            this.size = size;
            this.uploaded = false;
        }


        public object Data
        {
            get { return data; }
            set { this.data = value; }
        }
        public string Path
        {
            get { return path; }
        }
        public DateTime Time
        {
            get { return time; }
        }
        public string Type
        {
            get { return type; }
        }
        public long Size
        {
            get { return size; }
        }
        public bool Uploaded
        {
            get { return uploaded; }
            set { uploaded = value; }
        }

        public static DiskItemInfo LastInList(IEnumerable<DiskItemInfo> items)
        {
            if (items != null && items.Count() > 0)
            {
                DiskItemInfo last = items.First();
                foreach (DiskItemInfo dii in items)
                {
                    long diival = 0;
                    if (long.TryParse(dii.DisplayName, out diival))
                    {
                        long lastval = 0;
                        if (long.TryParse(last.DisplayName, out lastval))
                        {
                            if (DateTime.FromBinary(diival) > DateTime.FromBinary(lastval))
                                last = dii;
                        }
                    }
                }
                return last;
            }
            else return null;
        }
        public static string CreateInfoFileName(string s)
        {
            return s + "_info";
        }
    }
    public partial class MainWindow : Window, IProgress
    {
        private IEnumerable<DiskItemInfo> elements_in_DataFolder;
        private List<Item> list_of_items;
    }
}
