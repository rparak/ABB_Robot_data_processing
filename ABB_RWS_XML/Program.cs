/****************************************************************************
MIT License
Copyright(c) 2020 Roman Parak
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

namespace ABB_RWS_Data_Processing_XML
{
    public static class ABB_Stream_Data
    {
        // IP Port Number and IP Address
        public static string ip_address;
        //  The target of reading the data: jointtarget / robtarget
        public static string xml_target = "";
        // Comunication Speed (ms)
        public static int time_step;
        // Joint Space:
        //  Orientation {J1 .. J6} (°)
        public static double[] J_Orientation = new double[6];
        // Cartesian Space:
        //  Position {X, Y, Z} (mm)
        public static double[] C_Position = new double[3];
        //  Orientation {Quaternion} (-):
        public static double[] C_Orientation = new double[4];
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Initialization {Robot Web Services ABB}
            //  Stream Data:
            ABB_Stream_Data.ip_address = "127.0.0.1";
            //  The target of reading the data: jointtarget / robtarget
            ABB_Stream_Data.xml_target = "robtarget";
            //  Communication speed (ms)
            ABB_Stream_Data.time_step = 2;

            // Start Stream {ABB Robot Web Services - XML}
            ABB_Stream ABB_Stream_Robot_XML = new ABB_Stream();
            ABB_Stream_Robot_XML.Start();

            Console.WriteLine("[INFO] Stop (y):");
            // Stop communication
            string stop_rs = Convert.ToString(Console.ReadLine());

            if (stop_rs == "y")
            {
                if (ABB_Stream_Data.xml_target == "jointtarget")
                {
                    Console.WriteLine("Joint Space: Orientation (radian)");
                    Console.WriteLine("J1: {0} | J2: {1} | J3: {2} | J4: {3} | J5: {4} | J6: {5}",
                                       ABB_Stream_Data.J_Orientation[0], ABB_Stream_Data.J_Orientation[1], ABB_Stream_Data.J_Orientation[2],
                                       ABB_Stream_Data.J_Orientation[3], ABB_Stream_Data.J_Orientation[4], ABB_Stream_Data.J_Orientation[5]);
                }
                else if (ABB_Stream_Data.xml_target == "robtarget")
                {
                    Console.WriteLine("Cartesian Space: Position (metres), Orientation (radian):");
                    Console.WriteLine("X: {0} | Y: {1} | Z: {2} | Q1: {3} | Q2: {4} | Q3: {5} | Q4: {6}",
                                       ABB_Stream_Data.C_Position[0], ABB_Stream_Data.C_Position[1], ABB_Stream_Data.C_Position[2],
                                       ABB_Stream_Data.C_Orientation[0], ABB_Stream_Data.C_Orientation[1], ABB_Stream_Data.C_Orientation[2], ABB_Stream_Data.C_Orientation[3]);
                }

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

                    // Get the system resource
                    Stream source_data = Get_System_Resource(ABB_Stream_Data.ip_address, ABB_Stream_Data.xml_target);
                    // Current data streaming from the source page
                    Stream_Data(source_data);

                    // t_{1}: Timer stop.
                    t.Stop();

                    // Recalculate the time: t = t_{1} - t_{0} -> Elapsed Time in milliseconds
                    if (t.ElapsedMilliseconds < ABB_Stream_Data.time_step)
                    {
                        Thread.Sleep(ABB_Stream_Data.time_step - (int)t.ElapsedMilliseconds);
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

        Stream Get_System_Resource(string host, string target)
        {
            // http:// + ip address + xml address + target {joint, cartesian}
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri("http://" + host + "/rw/rapid/tasks/T_ROB1/motion?resource=" + target));
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

        void Stream_Data(Stream source_data)
        {
            // Xml Node: Initialization Document
            XmlDocument xml_doc = new XmlDocument();
            // Load XML data
            xml_doc.Load(source_data);

            // Create an XmlNamespaceManager for resolving namespaces.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml_doc.NameTable);

            nsmgr.AddNamespace("ns", "http://www.w3.org/1999/xhtml");

            if (ABB_Stream_Data.xml_target == "jointtarget")
            {
                // -------------------- Read State {Joint (1 - 6)} -------------------- //
                XmlNodeList xml_node = xml_doc.SelectNodes("//ns:li[@class='rapid-jointtarget']", nsmgr);

                // Joint (1 - 6) -> Read RWS XML
                ABB_Stream_Data.J_Orientation[0] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='j1']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.J_Orientation[1] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='j2']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.J_Orientation[2] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='j3']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.J_Orientation[3] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='j4']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.J_Orientation[4] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='j5']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.J_Orientation[5] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='j6']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);

            }
            else if (ABB_Stream_Data.xml_target == "robtarget")
            {
                // -------------------- Read State {Cartesian (X,Y,Z, Quaternion {q1 - q4})} -------------------- //
                XmlNodeList xml_node = xml_doc.SelectNodes("//ns:li[@class='rapid-robtarget']", nsmgr);

                // x, y, z {Target positions} -> Read RWS XML
                ABB_Stream_Data.C_Position[0] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='x']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.C_Position[1] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='y']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.C_Position[2] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='z']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                // q1, q2, q3, q4 {Orientation} -> Read RWS XML
                ABB_Stream_Data.C_Orientation[0] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='q1']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.C_Orientation[1] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='q2']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.C_Orientation[2] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='q3']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                ABB_Stream_Data.C_Orientation[3] = double.Parse(xml_node[0].SelectSingleNode("ns:span[@class='q4']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
            }
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
            // Start a thread
            if (robot_thread.IsAlive == true)
            {
                Thread.Sleep(100);
            }
        }
        public void Destroy()
        {
            // Stop a thread (Robot Web Services communication)
            Stop();
            Thread.Sleep(100);
        }
    }
}
