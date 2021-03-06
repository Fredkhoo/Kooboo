﻿using System;
using System.IO;
namespace Kooboo.Data.Log
{
    public class LogWriter
    { 
        private string Folder { get; set; }

        public LogWriter(string FolderName)
        {
            this.Folder = FolderName;
            _date = DateTime.UtcNow.Date;
            _writer = CreateWriter(_date);
        }

        private StreamWriter _writer;
        private DateTime _date;
        private object _createLock = new object();


        public void Write(string line)
        {
            Writer.WriteLine(line);
        }

        public void WriteObj(object JsonObject)
        { 
            var text = Lib.Helper.JsonHelper.Serialize(JsonObject);
            Write(text); 
        }

        public void WriteException(Exception ex)
        {
            string text = ex.Message + ex.Source + ex.StackTrace;
            Write(text); 
        }

        private StreamWriter Writer
        {
            get
            {
                if (_date < DateTime.UtcNow.Date)
                {
                    lock (_createLock)
                    {
                        if (_date < DateTime.UtcNow.Date)
                        {
                            _date = DateTime.UtcNow.Date;
                            _writer = CreateWriter(_date);
                        }
                    }
                }

                if (_writer == null)
                {
                    lock (_createLock)
                    {
                        if (_writer == null)
                        {
                            _date = DateTime.UtcNow.Date;
                            _writer = CreateWriter(_date);
                        }
                    }
                }

                return _writer;
            }

        }

        private StreamWriter CreateWriter(DateTime utcdate)
        {
            string path = System.IO.Path.Combine(AppSettings.Global.LogPath, this.Folder);

            string filename = System.IO.Path.Combine(path, utcdate.ToString("yy-MM-dd") + ".log");
            Lib.Helper.IOHelper.EnsureFileDirectoryExists(filename);
            var wr = new StreamWriter(new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
            wr.AutoFlush = true;
            return wr;
        }

    }  

}




