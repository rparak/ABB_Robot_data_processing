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

// ------------------------------------------------------------------------------------------------------------------------//
// ----------------------------------------------------- LIBRARIES --------------------------------------------------------//
// ------------------------------------------------------------------------------------------------------------------------//

// -------------------- System -------------------- //
using System;
using System.Threading;
using System.Net;
using System.IO;
using System.Xml;
using System.Globalization;

namespace ABB_RWS_XML_data_processing
{
    class Program
    {
        // -------------------- Thread -------------------- //
        static Thread rws_read_Thread;
        // -------------------- Cookie Container -------------------- //
        static CookieContainer c_cookie = new CookieContainer();
        // -------------------- Network Credential -------------------- //
        static NetworkCredential n_credential = new NetworkCredential("Default User", "robotics");
        // -------------------- String -------------------- //
        static string ip_address;
        static string xml_target;
        // -------------------- Bool -------------------- //
        static bool rws_r_while;
        // -------------------- Double -------------------- //
        static double[] robotBaseRotLink_ABB_c = { 0f, 0f, 0f, 0f, 0f, 0f, 0f };
        static double[] robotBaseRotLink_ABB_j = { 0f, 0f, 0f, 0f, 0f, 0f };
        // -------------------- Int -------------------- //
        static int read_state_rws;

        // ------------------------------------------------------------------------------------------------------------------------//
        // ------------------------------------------------ MAIN FUNCTION {Cyclic} ------------------------------------------------//
        // ------------------------------------------------------------------------------------------------------------------------//

        static void Main(string[] args)
        {
            // ------------------------ Initialization { RWS - Robot Web Services} ------------------------//
            // Robot IP Address
            ip_address = "127.0.0.1";
            // Read state {0 -> Joint, 1 -> Cartesian}
            read_state_rws = 0;
            // Set Json address
            if (read_state_rws == 0)
            {
                // Robot - Joint target {Web}
                xml_target = "jointtarget";
            }
            else if (read_state_rws == 1)
            {
                // Robot - Cartesian target {Web}
                xml_target = "robtarget";

            }

            // ------------------------ Threading Block { RWS - Read Data } ------------------------//
            rws_r_while = true;
            rws_read_Thread = new Thread(() => RWS_Service_read_thread_function("http://" + ip_address, xml_target));
            rws_read_Thread.IsBackground = true;
            rws_read_Thread.Start();

            // ------------------------ Main Block { Read data from the Robot (ABB) } ------------------------//
            try
            {
                // -------------------- Main Cycle {While} -------------------- //
                while (true)
                {
                    // -------------------- Read State {Check} -------------------- //
                    if (read_state_rws == 0)
                    {
                        // Read Joint data (1 - 6)
                        Console.WriteLine("Joint 1: {0}", robotBaseRotLink_ABB_j[1]);
                    }
                    else if (read_state_rws == 1)
                    {
                        // Read Cartesian data (X,Y,Z, Quaternion {q1 - q4})
                        Console.WriteLine("Position X: {0}", robotBaseRotLink_ABB_c[0]);
                    }
                    // Thread Sleep {100 ms}
                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // Quit function
                Application_Quit();
            }
        }

        // ------------------------------------------------------------------------------------------------------------------------//
        // -------------------------------------------------------- FUNCTIONS -----------------------------------------------------//
        // ------------------------------------------------------------------------------------------------------------------------//

        // -------------------- Abort Threading Blocks -------------------- //
        static void Application_Quit()
        {
            try
            {
                // Stop - threading while (XML)
                rws_r_while = false;

                // Abort threading block {RWS XML -> read data}
                if (rws_read_Thread.IsAlive == true)
                {
                    rws_read_Thread.Abort();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // ------------------------ Threading Block { RWS - Robot Web Services (READ) } ------------------------//
        static void RWS_Service_read_thread_function(string ip_adr, string target)
        {
            while (rws_r_while)
            {
                // get the system resource
                Stream xml_joint = get_system_resource(ip_adr, target);
                // display the system resource
                display_data(xml_joint, target);  

            }
        }

        // ------------------------ RWS aux. function { Get System Resource } ------------------------//
        static Stream get_system_resource(string host, string target)
        {
            // ip address + xml address + target {joint, cartesian}
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(host + "/rw/rapid/tasks/T_ROB1/motion?resource=" + target));
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

        // ------------------------ RWS aux. function { Read Data } ------------------------//
        static void display_data(Stream xmldata, string target)
        {
            // XmlNode -> Initialization Document
            XmlDocument doc = new XmlDocument();
            // Load XML data
            doc.Load(xmldata);

            // Create an XmlNamespaceManager for resolving namespaces.
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);

            nsmgr.AddNamespace("ns", "http://www.w3.org/1999/xhtml");

            if (target == "jointtarget")
            {
                // -------------------- Read State {Joint (1 - 6)} -------------------- //
                XmlNodeList optionNodes = doc.SelectNodes("//ns:li[@class='rapid-jointtarget']", nsmgr);
                foreach (XmlNode optNode in optionNodes)
                {
                    // Joint (1 - 6) -> Read RWS XML
                    // optNode.SelectSingleNode("ns:span[@class='j1']", nsmgr).InnerText.ToString()
                    robotBaseRotLink_ABB_j[0] = double.Parse(optNode.SelectSingleNode("ns:span[@class='j1']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_j[1] = double.Parse(optNode.SelectSingleNode("ns:span[@class='j2']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_j[2] = double.Parse(optNode.SelectSingleNode("ns:span[@class='j3']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_j[3] = double.Parse(optNode.SelectSingleNode("ns:span[@class='j4']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_j[4] = double.Parse(optNode.SelectSingleNode("ns:span[@class='j5']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_j[5] = double.Parse(optNode.SelectSingleNode("ns:span[@class='j6']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    
                    // Thread Sleep {2 ms}
                    Thread.Sleep(2);
                }
            }
            else if (target == "robtarget")
            {
                // -------------------- Read State {Cartesian (X,Y,Z, Quaternion {q1 - q4})} -------------------- //
                XmlNodeList optionNodes = doc.SelectNodes("//ns:li[@class='rapid-robtarget']", nsmgr);
                foreach (XmlNode optNode in optionNodes)
                {
                    // x, y, z {Target positions} -> Read RWS XML
                    // optNode.SelectSingleNode("ns:span[@class='x']", nsmgr).InnerText.ToString()
                    robotBaseRotLink_ABB_c[0] = double.Parse(optNode.SelectSingleNode("ns:span[@class='x']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_c[1] = double.Parse(optNode.SelectSingleNode("ns:span[@class='y']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_c[2] = double.Parse(optNode.SelectSingleNode("ns:span[@class='z']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    // q1, q2, q3, q4 {Orientation} -> Read RWS XML
                    // optNode.SelectSingleNode("ns:span[@class='q1']", nsmgr).InnerText.ToString()
                    robotBaseRotLink_ABB_c[3] = double.Parse(optNode.SelectSingleNode("ns:span[@class='q1']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_c[4] = double.Parse(optNode.SelectSingleNode("ns:span[@class='q2']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_c[5] = double.Parse(optNode.SelectSingleNode("ns:span[@class='q3']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);
                    robotBaseRotLink_ABB_c[6] = double.Parse(optNode.SelectSingleNode("ns:span[@class='q4']", nsmgr).InnerText.ToString(), CultureInfo.InvariantCulture.NumberFormat);

                    // Thread Sleep {2 ms}
                    Thread.Sleep(2);
                }
            }
        }
    }
}
