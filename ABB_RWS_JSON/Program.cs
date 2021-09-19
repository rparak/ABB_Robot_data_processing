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
using System.Net;
using System.Net.Http;
using System.Threading;

namespace ABB_RWS_Data_Processing_JSON
{
    class Program
    {
        // -------------------- Thread -------------------- //
        static Thread rws_read_Thread;
        // -------------------- String -------------------- //
        static string ip_address;
        static string json_address;
        static string json_address_joint, json_address_cartesian;
        // -------------------- Double -------------------- //
        static double[] robotBaseRotLink_ABB_c = { 0f, 0f, 0f, 0f, 0f, 0f, 0f };
        static double[] robotBaseRotLink_ABB_j = { 0f, 0f, 0f, 0f, 0f, 0f };
        // -------------------- Bool -------------------- //
        static bool rws_r_while;
        static bool communication_read_isOk;
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
            // Robot - Joint address {Web}
            json_address_joint = "/rw/rapid/tasks/T_ROB1/motion?resource=jointtarget&json=1";
            // Robot - Cartesian address {Web}
            json_address_cartesian = "/rw/rapid/tasks/T_ROB1/motion?resource=robtarget&json=1";
            // Read state {0 -> Joint, 1 -> Cartesian}
            read_state_rws = 1;

            // Set Json address
            if (read_state_rws == 0)
            {
                json_address = json_address_joint;
            }
            else if (read_state_rws == 1)
            {
                json_address = json_address_cartesian;
            }

            // ------------------------ Threading Block { RWS - Read Data } ------------------------//
            rws_r_while = true;
            rws_read_Thread = new Thread(() => RWS_Service_read_thread_function(read_state_rws, "http://" + ip_address, json_address));
            rws_read_Thread.IsBackground = true;
            rws_read_Thread.Start();

            // ------------------------ Main Block { Read data from the Robot (ABB) } ------------------------//
            try
            {
                // -------------------- Main Cycle {While} -------------------- //
                while (true)
                {
                    // -------------------- Communication {Check} -------------------- //
                    if (communication_read_isOk == true)
                    {
                        // -------------------- Read State {Check} -------------------- //
                        if (read_state_rws == 0)
                        {
                            // Read Joint data (1 - 6)
                            Console.WriteLine("J1: {0} | J2: {1} | J3: {2} | J4: {3} | J5: {4} | J6: {5}",
                                              robotBaseRotLink_ABB_j[0], robotBaseRotLink_ABB_j[1], robotBaseRotLink_ABB_j[2],
                                              robotBaseRotLink_ABB_j[3], robotBaseRotLink_ABB_j[4], robotBaseRotLink_ABB_j[5]);
                        }
                        else if (read_state_rws == 1)
                        {
                            // Read Cartesian data (X,Y,Z, Quaternion {q1 - q4})
                            Console.WriteLine("X: {0} | Y: {1} | Z: {2} | Q1: {3} | Q2: {4} | Q3: {5} | Q4: {6}",
                                              robotBaseRotLink_ABB_c[0], robotBaseRotLink_ABB_c[1], robotBaseRotLink_ABB_c[2],
                                              robotBaseRotLink_ABB_c[3], robotBaseRotLink_ABB_c[4], robotBaseRotLink_ABB_c[5],
                                              robotBaseRotLink_ABB_c[6]);
                        }
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
        static async void RWS_Service_read_thread_function(int read_state, string ip_adr, string json_adr)
        {
            var handler = new HttpClientHandler { Credentials = new NetworkCredential("Default User", "robotics") };
            // disable the proxy, the controller is connected on same subnet as the PC 
            handler.Proxy = null;
            handler.UseProxy = false;

            // Send a request continue when complete
            using (HttpClient client = new HttpClient(handler))
            {
                // while - reading {data Joint, Cartesian}
                while (rws_r_while)
                {
                    using (HttpResponseMessage response = await client.GetAsync(ip_adr + json_adr))
                    {
                        using (HttpContent content = response.Content)
                        {
                            try
                            {
                                // set variable {communation is ok}
                                communication_read_isOk = true;
                                // Check that response was successful or throw exception
                                response.EnsureSuccessStatusCode();
                                // Get HTTP response from completed task.
                                string result = await content.ReadAsStringAsync();
                                // Deserialize the returned json string
                                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

                                // Display controller name, version and version name
                                var service = obj._embedded._state[0];

                                if (read_state == 0)
                                {
                                    // Joint {1 .. 6} -> Read RWS JSON
                                    robotBaseRotLink_ABB_j[0] = Math.Round((double)service.j1, 2);
                                    robotBaseRotLink_ABB_j[1] = Math.Round((double)service.j2, 2);
                                    robotBaseRotLink_ABB_j[2] = Math.Round((double)service.j3, 2);
                                    robotBaseRotLink_ABB_j[3] = Math.Round((double)service.j4, 2);
                                    robotBaseRotLink_ABB_j[4] = Math.Round((double)service.j5, 2);
                                    robotBaseRotLink_ABB_j[5] = Math.Round((double)service.j6, 2);
                                }
                                else if (read_state == 1)
                                {
                                    // TCP {X, Y, Z} -> Read RWS JSON
                                    robotBaseRotLink_ABB_c[0] = Math.Round((double)service.x, 2);
                                    robotBaseRotLink_ABB_c[1] = Math.Round((double)service.y, 2);
                                    robotBaseRotLink_ABB_c[2] = Math.Round((double)service.z, 2);
                                    // Quaternion {q1 .. q4} -> Read RWS JSON
                                    robotBaseRotLink_ABB_c[3] = Math.Round((double)service.q1, 6);
                                    robotBaseRotLink_ABB_c[4] = Math.Round((double)service.q2, 6);
                                    robotBaseRotLink_ABB_c[5] = Math.Round((double)service.q3, 6);
                                    robotBaseRotLink_ABB_c[6] = Math.Round((double)service.q4, 6);
                                }

                                // Thread Sleep {200 ms}
                                Thread.Sleep(200);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                            finally
                            {
                                content.Dispose();
                            }
                        }
                    }
                }
            }
        }
    }
}
