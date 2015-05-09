// https://codehosting.net/blog/BlogEngine/post/Simple-C-Web-Server.aspx
//
// The MIT License (MIT)
//
// Copyright (c) 2013 David's Blog (www.codehosting.net) 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
// associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, 
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or 
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR 
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR 
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

// TODO: Combine licenses

using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace ArchiveServer
{
    public class ArchiveContentServer
    {
        public ArchiveContentServer() { }
         
        private readonly HttpListener                               _listener   = new HttpListener();
        private readonly List<KeyValuePair<string, ArchiveContent>> _precedence = new List<KeyValuePair<string, ArchiveContent>>();
        private readonly Dictionary<string, ArchiveContent>         _archives   = new Dictionary<string,ArchiveContent>();

        public Dictionary<string, ArchiveContent> Archives { get { return _archives; } }
        public List<KeyValuePair<string, ArchiveContent>> Precedence { get { return _precedence; } }

        public ArchiveContentServer(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Requires Windows XP SP2, Server 2003 or later.");
 
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("At least one prefix must be specified.");
 
            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);
 
            _listener.Start();
        }

        private void Serve(HttpListenerContext context)
        {
            bool found = false;
            foreach ( var archive in _precedence )
            {
                found = context.Request.RawUrl.StartsWith(archive.Key);
                if (found)
                {
                    archive.Value.ServeRequest(context);
                    break;
                }
            }
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var context = c as HttpListenerContext;
                            try
                            {
                                if ( context != null )
                                {
                                    Serve(context);
                                }
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                context.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }
 
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
