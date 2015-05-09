using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SharpCompress;
using SharpCompress.Archive;
using SharpCompress.Common;
using System.Net;

namespace ArchiveServer
{
    public class ArchiveContent
    {
        private IArchive Archive = null;
        private Dictionary<string,IArchiveEntry> Entries = new Dictionary<string,IArchiveEntry>();

        public string Root     { get; set; }
        public string Location { get; private set; }
        public bool   IsOpen   { get; private set; }   

        public ArchiveContent()
        {
            Root     = String.Empty;
            Location = String.Empty;
            IsOpen   = false;
        }

        public ArchiveContent(string location) : this( location, "/" ) { }

        public ArchiveContent(string location, string root)
        {
            Root     = root;
            Location = location;
            Open(location);
        }

        public bool Open( string archivePath )
        {
            try
            {
                Archive = ArchiveFactory.Open(archivePath);
                Entries.Clear();
                foreach (var entry in Archive.Entries)
                {
                    if (!Entries.ContainsKey(entry.FilePath))
                    {
                        Entries.Add(entry.FilePath, entry);
                    }
                    else
                    {
                        Console.WriteLine("Duplicate entry found: " + entry.FilePath);
                    }
                }
                IsOpen = true;
                return true;
            }
            catch
            {
                Archive = null;
                Entries.Clear();
                IsOpen = false;
                return false;
            }
        }

        public void ServeRequest( HttpListenerContext context )
        {
            if (context.Request.IsLocal)
            {
                string key = context.Request.RawUrl;
                key = key.TrimStart('/');

                if (Entries.ContainsKey(key))
                {
                    Entries[key].WriteTo(context.Response.OutputStream);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    byte[] buf = ResponseFromString(Properties.Resources.Error);
                    context.Response.ContentLength64 = buf.Length;
                    context.Response.OutputStream.Write(buf, 0, buf.Length);
                }
            }
            else
            {
                context.Response.StatusCode = 403;
                byte[] buf = ResponseFromString(Properties.Resources.Forbidden);
                context.Response.ContentLength64 = buf.Length;
                context.Response.OutputStream.Write(buf, 0, buf.Length);
            }
        }

        public byte[] ResponseFromString(string response)
        {
            return Encoding.UTF8.GetBytes(response);
        }
    }
}