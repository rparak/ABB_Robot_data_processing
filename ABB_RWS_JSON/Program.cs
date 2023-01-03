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
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Diagnostics;

namespace ABB_RWS_Data_Processing_JSON
{
    public static class ABB_Stream_Data
    {
        // IP Port Number and IP Address
        public static string ip_address;
        //  The target of reading the data: jointtarget / robtarget
        public static string json_target = "";
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
            ABB_Stream_Data.json_target = "robtarget";
            //  Communication speed (ms)
            ABB_Stream_Data.time_step = 12;

            // Start Stream {Universal Robots TCP/IP}
            ABB_Stream ABB_Stream_Robot_JSON = new ABB_Stream();
            ABB_Stream_Robot_JSON.Start();

            Console.WriteLine("[INFO] Stop (y):");
            // Stop communication
            string stop_rs = Convert.ToString(Console.ReadLine());

            if (stop_rs == "y")
            {
                if (ABB_Stream_Data.json_target == "jointtarget")
                {
                    Console.WriteLine("Joint Space: Orientation (radian)");
                    Console.WriteLine("J1: {0} | J2: {1} | J3: {2} | J4: {3} | J5: {4} | J6: {5}",
                                       ABB_Stream_Data.J_Orientation[0], ABB_Stream_Data.J_Orientation[1], ABB_Stream_Data.J_Orientation[2],
                                       ABB_Stream_Data.J_Orientation[3], ABB_Stream_Data.J_Orientation[4], ABB_Stream_Data.J_Orientation[5]);
                }
                else if (ABB_Stream_Data.json_target == "robtarget")
                {
                    Console.WriteLine("Cartesian Space: Position (metres), Orientation (radian):");
                    Console.WriteLine("X: {0} | Y: {1} | Z: {2} | Q1: {3} | Q2: {4} | Q3: {5} | Q4: {6}",
                                       ABB_Stream_Data.C_Position[0], ABB_Stream_Data.C_Position[1], ABB_Stream_Data.C_Position[2],
                                       ABB_Stream_Data.C_Orientation[0], ABB_Stream_Data.C_Orientation[1], ABB_Stream_Data.C_Orientation[2], ABB_Stream_Data.C_Orientation[3]);
                }

                // Destroy ABB {Stream}
                ABB_Stream_Robot_JSON.Destroy();

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

        async void ABB_Stream_Thread()
        {
            var handler = new HttpClientHandler { Credentials = new NetworkCredential("Default User", "robotics") };
            // disable the proxy, the controller is connected on same subnet as the PC 
            handler.Proxy = null;
            handler.UseProxy = false;

            try
            {
                // Send a request continue when complete
                using (HttpClient client = new HttpClient(handler))
                {
                    // Initialization timer
                    var t = new Stopwatch();

                    while (exit_thread == false)
                    {
                        // t_{0}: Timer start.
                        t.Start();

                        // Current data streaming from the source page
                        using (HttpResponseMessage response = await client.GetAsync("http://" + ABB_Stream_Data.ip_address + "/rw/rapid/tasks/T_ROB1/motion?resource=" + ABB_Stream_Data.json_target + "&json=1"))
                        {
                            using (HttpContent content = response.Content)
                            {
                                try
                                {
                                    // Check that response was successful or throw exception
                                    response.EnsureSuccessStatusCode();
                                    // Get HTTP response from completed task.
                                    string result = await content.ReadAsStringAsync();
                                    // Deserialize the returned json string
                                    dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

                                    // Display controller name, version and version name
                                    var service = obj._embedded._state[0];

                                    if (ABB_Stream_Data.json_target == "jointtarget")
                                    {
                                        // Joint {1 .. 6} -> Read RWS JSON
                                        ABB_Stream_Data.J_Orientation[0] = (double)service.j1;
                                        ABB_Stream_Data.J_Orientation[1] = (double)service.j2;
                                        ABB_Stream_Data.J_Orientation[2] = (double)service.j3;
                                        ABB_Stream_Data.J_Orientation[3] = (double)service.j4;
                                        ABB_Stream_Data.J_Orientation[4] = (double)service.j5;
                                        ABB_Stream_Data.J_Orientation[5] = (double)service.j6;
                                    }
                                    else if (ABB_Stream_Data.json_target == "robtarget")
                                    {
                                        // TCP {X, Y, Z} -> Read RWS JSON
                                        ABB_Stream_Data.C_Position[0] = (double)service.x;
                                        ABB_Stream_Data.C_Position[1] = (double)service.y;
                                        ABB_Stream_Data.C_Position[2] = (double)service.z;
                                        // Quaternion {q1 .. q4} -> Read RWS JSON
                                        ABB_Stream_Data.C_Orientation[0] = (double)service.q1;
                                        ABB_Stream_Data.C_Orientation[1] = (double)service.q2;
                                        ABB_Stream_Data.C_Orientation[2] = (double)service.q3;
                                        ABB_Stream_Data.C_Orientation[3] = (double)service.q4;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                        }

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
            }
            catch (Exception e)
            {
                Console.WriteLine("Communication Problem: {0}", e);
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
