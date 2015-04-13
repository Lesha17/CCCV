using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Disk.SDK;
using System.IO;
using System.Collections.Specialized;

namespace CCCV
{
    [Serializable]
    public class Item
    {
        public enum ContentType { Text, FileList, Audio, Image };

        object data;
        ContentType type;

        public Item(object data, ContentType type)
        {
            this.data = data;
            this.type = type;
        }


        public object Data
        {
            get { return data; }
            set { this.data = value; }
        }
        public ContentType Type
        {
            get { return type; }
            set { this.type = value; }
        }

        public static DiskItemInfo LastInList(IEnumerable<DiskItemInfo> items)
        {
            if (items != null && items.Count() > 0)
            {
                DiskItemInfo last = items.First();
                foreach (DiskItemInfo dii in items)
                {
                    if (dii.CreationDate > last.CreationDate && !dii.IsDirectory)
                    {
                        last = dii;
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

    public class CCCV_FileList
    {
        private List<string> allDirs;
        private List<string> allFiles;
        private string disk_path;
        private string local_path;
        private long size;

        public CCCV_FileList()
        {

        }

        public CCCV_FileList(StringCollection selected, string disk_path)
        {
            this.disk_path = disk_path;
            this.allDirs = new List<string>();
            this.allFiles = new List<string>();
            this.size = 0;
            process(selected);
        }

        public void process(StringCollection selectedFiles)
        {
            foreach (string file_p in selectedFiles)
            {
                FileInfo fi = new FileInfo(file_p);
                local_path = fi.DirectoryName + "\\";
                int l = local_path.Length;
                if ((fi.Attributes & FileAttributes.Directory) != 0)
                {
                    allDirs.Add(file_p.Substring(l));
                    inDir(file_p, l);
                }
                else
                {
                    allFiles.Add(file_p.Substring(l));
                    size += fi.Length;
                }
            }
        }

        private void inDir(string d_p, int l)
        {
            foreach (string dir_p in Directory.GetDirectories(d_p))
            {
                allDirs.Add(dir_p.Substring(l));
                inDir(dir_p, l);
            }
            foreach (string file_p in Directory.GetFiles(d_p))
            {
                allFiles.Add(file_p.Substring(l));
                size += new FileInfo(file_p).Length;
            }
        }

        public string DiskPath
        {
            get { return disk_path; }
            set { this.disk_path = value; }
        }

        public string LocalPath
        {
            get { return local_path; }
            set { this.local_path = value; }
        }

        public long Size
        {
            get { return size; }
            set { this.size = value; }
        }

        public List<string> AllFiles
        {
            get { return allFiles; }
            set { this.allFiles = value; }
        }

        public List<string> AllDirs
        {
            get { return allDirs; }
            set { this.allDirs = value; }
        }
    }


    public partial class MainWindow : Window
    {
        private IEnumerable<DiskItemInfo> elements_in_DataFolder;
    }
}
