/****************************************************************************
MIT License
Copyright(c) 2022 Roman Parak
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*****************************************************************************
Author   : Roman Parak
Email    : Roman.Parak @outlook.com
Github   : https://github.com/rparak
File Name: Program.cs
****************************************************************************/

// System Lib.
using System.Net;
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ABB_RWS_Data_Processing_XML
{
    public static class ABB_Data
    {
        // IP Port Number and IP Address
        public static string ip_address;
        //  The target of reading the data: jointtarget / robtarget
        public static string xml_target = "";
        // Comunication Speed (ms)
        public static int time_step;
        // Joint Space:
        //  Orientation {J1 .. J6} (°)
        public static string J_Orientation;
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Initialization {Robot Web Services ABB}
            //  Stream Data:
            ABB_Data.ip_address = "127.0.0.1";
            //  The target of reading the data: jointtarget / robtarget
            ABB_Data.xml_target = "robtarget";
            //  Communication speed (ms)
            ABB_Data.time_step = 12;
            //  Joint Targets
            ABB_Data.J_Orientation = "value=[" +
                                     "[[0,0,0,0,0,0],[0,0,0,0,0,0]]," +
                                     "[[0,0,0,0,90,0],[0,0,0,0,0,0]]," +
                                     "[[20.0,-20.0,20.0,-20.0,20.0,-20.0],[0,0,0,0,0,0]]," +
                                     "[[-20.0,20.0,-20.0,20.0,-20.0,20.0],[0,0,0,0,0,0]]," +
                                     "[[0,0,0,0,0,0],[0,0,0,0,0,0]]" +
                                     "]";

            // Start Stream {ABB Robot Web Services - XML}
            ABB_Stream ABB_Stream_Robot_XML = new ABB_Stream();
            ABB_Stream_Robot_XML.Start();

            Console.WriteLine("[INFO] Stop (y):");
            // Stop communication
            string stop_rs = Convert.ToString(Console.ReadLine());

            if (stop_rs == "y")
            {
                // Destroy ABB {Stream}
                ABB_Stream_Robot_XML.Destroy();

                // Application quit
                Environment.Exit(0);
            }
        }

    }
    class ABB_Stream
    {
        // Initialization of Class variables
        //  Thread
        private Thread robot_thread = null;
        private bool exit_thread = false;
        // Robot Web Services (RWS): XML Communication
        private CookieContainer c_cookie = new CookieContainer();
        private NetworkCredential n_credential = new NetworkCredential("Default User", "robotics");

        // Control state
        private int main_state = 0;

        public void ABB_Stream_Thread()
        {
            try
            {
                // Initialization timer
                var t = new Stopwatch();

                while (exit_thread == false)
                {
                    // t_{0}: Timer start.
                    t.Start();

                    switch (main_state)
                    {
                        case 0:
                            {
                                // State: Reset PP to main

                                // Create data to send
                                string post_data = "";

                                // Control data: Sending data to the robot controller
                                Stream result = Control_Data(ABB_Data.ip_address, "execution?action=resetpp", post_data);

                                main_state = 1;
                            }
                            break;

                        case 1:
                            {
                                // State: Set Joint Targets

                                // Control data: Sending data to the robot controller
                                Stream result = Control_Data(ABB_Data.ip_address, "symbol/data/RAPID/T_ROB1/J_Orientation_Target?action=set", ABB_Data.J_Orientation);

                                main_state = 2;
                            }
                            break;

                        case 2:
                            {
                                // State: Start Rapid

                                // Create data to send
                                string post_data = "regain=continue&execmode=continue&cycle=forever&condition=none&stopatbp=disabled&alltaskbytsp=false";

                                // Control data: Sending data to the robot controller
                                Stream result = Control_Data(ABB_Data.ip_address, "execution?action=start", post_data);

                                main_state = 3;
                            }
                            break;

                        case 3:
                            {
                                // State: Wait

                                // Get the system resource
                                Stream source_data = Get_System_Resource(ABB_Data.ip_address, "in_position");
                                // Current data streaming from the source page
                                string value = Stream_Data(source_data);

                                if(value == "1")
                                {
                                    main_state = 4;
                                }
                            }
                            break;

                        case 4:
                            {
                                // State: Stop

                                // Create data to send
                                string post_data = "stopmode=stop&usetsp=normal";

                                // Control data: Sending data to the robot controller
                                Control_Data(ABB_Data.ip_address, "execution?action=stop", post_data);

                                main_state = 5;
                            }
                            break;

                        case 5:
                            {
                                // State: Empty
                            }
                            break;
                    }

                    Console.WriteLine("Current State: {0}", main_state);

                    // t_{1}: Timer stop.
                    t.Stop();

                    // Recalculate the time: t = t_{1} - t_{0} -> Elapsed Time in milliseconds
                    if (t.ElapsedMilliseconds < ABB_Data.time_step)
                    {
                        Thread.Sleep(ABB_Data.time_step - (int)t.ElapsedMilliseconds);
                    }

                    // Reset (Restart) timer.
                    t.Restart();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Communication Problem: {0}", e);
            }
        }

        Stream Control_Data(string host, string target, string value)
        {
            // http:// + ip address + xml address + target
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("http://" + host + "/rw/rapid/" + target));

            // Login: Default User; Password: robotics
            request.Credentials = n_credential;
            // don't use proxy, it's aussumed that the RC/VC is reachable without going via proxy 
            request.Proxy = null;
            request.Method = "POST";
            request.PreAuthenticate = true;
            // re-use http session between requests 
            request.CookieContainer = c_cookie;

            // Create data to send (Byte)
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] value_byte = encoding.GetBytes(value);

            // set the length of the post data
            request.ContentLength = value_byte.Length;

            // Use form data when sending update etc to controller
            request.ContentType = "application/x-www-form-urlencoded";

            using (var stream = request.GetRequestStream())
            {
                stream.Write(value_byte, 0, value_byte.Length);
                stream.Close();
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }

        Stream Get_System_Resource(string host, string target)
        {
            // http:// + ip address + xml address + target
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("http://" + host + "/rw/rapid/symbol/data/RAPID/T_ROB1/" + target));
            // Login: Default User; Password: robotics
            request.Credentials = n_credential;
            // don't use proxy, it's aussumed that the RC/VC is reachable without going via proxy 
            request.Proxy = null;
            request.Method = "GET";
            // re-use http session between requests 
            request.CookieContainer = c_cookie;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }
        string Stream_Data(Stream source_data)
        {
            // Xml Node: Initialization Document
            XmlDocument xml_doc = new XmlDocument();
            // Load XML data
            xml_doc.Load(source_data);

            // Create an XmlNamespaceManager for resolving namespaces.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml_doc.NameTable);

            nsmgr.AddNamespace("ns", "http://www.w3.org/1999/xhtml");

            // Get collection of nodes
            XmlNodeList xml_node = xml_doc.SelectNodes("//ns:li[@class='rap-data']", nsmgr);

            // Reading a value from a node and converting it to a string
            return xml_node[0].SelectSingleNode("ns:span[@class='value']", nsmgr).InnerText.ToString();
        }

        public void Start()
        {
            exit_thread = false;
            // Start a thread to stream ABB Robot
            robot_thread = new Thread(new ThreadStart(ABB_Stream_Thread));
            robot_thread.IsBackground = true;
            robot_thread.Start();
        }
        public void Stop()
        {
            exit_thread = true;
            // Stop a thread
            Thread.Sleep(100);
        }
        public void Destroy()
        {
            // Stop a thread (Robot Web Services communication)
            Stop();
            Thread.Sleep(100);
        }
    }
}