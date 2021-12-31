using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace YerraAgent
{
    class Program
    {
        protected Agent user;
        public System.Threading.Timer actionTimer;
        public System.Threading.Timer checkAppStateTimer;
        public List<IntPtr> preProcesses;                                   //previous process list to determine if the process list is changed.
        public string baseURL = "https://localhost:44398";                  //server URL.
        public string companyName = "company-one";                                //company name of the agent.
        public string companyID = "9720-5079-8501";
        static string baseDir = @"C:/yerra";                                //directory to save agent app.
        static int actionDuration = 15000;                                  //duration for hide/unhide action timer(agent app send the request of current process list once per this period)
        static int checkStateDuration = 6000;                               //duration to check stop/start/uninstall status of agent app on the server.
        public HttpClient _client;                                          //instance of REST API to communicate with the server.
        private NotifyIcon trayIcon;                                        //instance of Tray icon to show agent ID.

        static void Main(string[] args)
        {
            try
            {
                new Program();
                Application.Run();
            }
            catch (Exception evt)
            {
                logger(evt.Message.ToString());
            }
        }
        
        public Program()
        {
            try
            {
                //initialize of tray icon instance.
                Stream st;
                System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
                st = a.GetManifestResourceStream("YerraAgent.logo.ico");
                this.trayIcon = new NotifyIcon();
                this.trayIcon.Icon = new System.Drawing.Icon(st);
                this.trayIcon.Text = "Yerra Agent";
                this.trayIcon.Click += new EventHandler(m_notifyIcon_Click);
                this.trayIcon.Visible = true;

                //initialize of HTTP request instance.
                _client = new HttpClient();
                _client.BaseAddress = new Uri(baseURL);
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                List<string> paths = new List<string>();
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            paths.Add(subkey.GetValue("InstallLocation").ToString());
                        }
                    }
                }

                generateAccount();
            }
            catch (Exception evt)
            {
                logger(evt.Message.ToString() + "---------started");
            }
        }

        //Show a dialog to display agent ID when click the tray icon.
        void m_notifyIcon_Click(object sender, EventArgs e)
        {
            var agentWindow = new AgentWindow();
            agentWindow.setAgentID(this.user.Id);
            agentWindow.Show();
        }

        ~Program()
        {
            actionTimer.Dispose();
            checkAppStateTimer.Dispose();
            turnOff();
        }

        //Record all error list of agent app in logger.txt.
        static void logger(string log)
        {
            try
            {
                string path = $"{baseDir}/log.txt";
                if (!File.Exists(path))
                {
                    if (!Directory.Exists(baseDir))
                    {
                        Directory.CreateDirectory(baseDir);
                    }
                    using (StreamWriter sw = File.CreateText(path))
                    {
                        sw.WriteLine(log);
                        sw.Close();
                    }

                    return;
                }
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(log);
                    sw.Close();
                }
            }
            catch(Exception evt)
            {

            }
            
        }
       
        //execute the hide/unhide action using process name.
        public void action(string command, string processName)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.UseShellExecute = false;
                info.FileName = $"{baseDir}/action.exe";
                info.Arguments = command + " " + processName;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.CreateNoWindow = true;

                Process process = Process.Start(info);
                process.WaitForExit();
                logger(info.FileName + " " + info.Arguments);
            }
            catch (Exception evt)
            {
                logger(evt.Message.ToString());
            }
        }
        
        //call once per variable actionDuration.
        //send current process list and execute hide/unhide action.
        public async void sendRequest()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("AgentAppInformation", true);
                if (key == null) return;
                var isInstalled = key.GetValue("IsInstalled").ToString();
                if (Int32.Parse(isInstalled) != 1) return;

                var newProcesses = Process.GetProcesses().Select(p => p.MainWindowHandle).Distinct();
                var strDiffProcessNames = newProcesses.Except(preProcesses);
                var sendProcessList = new List<ProcessInfo>();
                Dictionary<string, bool> processStatus = new Dictionary<string, bool>();

                string path = $"{baseDir}/process-infos.json";
                if (!File.Exists(path))
                {
                    var strProcessStatus = JsonConvert.SerializeObject(processStatus);

                    File.WriteAllText(path, strProcessStatus);
                }

                Dictionary<string, bool> storedProcessStatus = JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(path));

                if (strDiffProcessNames.Count() > 0)
                {

                    preProcesses = newProcesses.ToList();
                    sendProcessList = newProcesses.Select(p =>
                    {
                        uint procId = 0;
                        NativeImports.GetWindowThreadProcessId(p, out procId);
                        var proc = Process.GetProcessById((int)procId);
                        string strProName = $"{proc.ProcessName}.exe";
                        
                        return new ProcessInfo(this.user, strProName, Process.GetProcesses().First(pr => pr.MainWindowHandle == p).ProcessName);
                    }).Where(p => !excludesProcesses.Any(ep => ep == p.Name)).ToList();
                }

                foreach(ProcessInfo pro in sendProcessList)
                {
                    if (!storedProcessStatus.Keys.Any(k => k.Equals(pro.Name)))
                    {
                        storedProcessStatus.Add(pro.Name, false);
                    }
                }

                var res = await _client.PostAsJsonAsync($"api/agent/processes/{this.user.Id}", sendProcessList);
                res.EnsureSuccessStatusCode();
                var readTask = res.Content.ReadAsAsync<List<ActionResult>>();
                List<ActionResult> response = readTask.Result;
                 
                foreach(ActionResult p in response)
                {
                    if (storedProcessStatus.Keys.Any(k => k.Equals(p.ProcessName)))
                    {

                        if (storedProcessStatus[p.ProcessName] != p.Action && p.Action == true)
                        {
                            //foreach (var process in Process.GetProcessesByName("TeamViewer"))
                            //{
                            //    process.Kill();
                            //}
                            action(p.Action ? "-h" : "-u", p.ProcessName);
                        }
                    }
                    processStatus[p.ProcessName] = p.Action;
                }

                var strProcessStatusResult = JsonConvert.SerializeObject(processStatus);

                File.WriteAllText(path, strProcessStatusResult);

            }
            catch (Exception evt)
            {
                logger(evt.Message.ToString() + "--------send request");
            }
        }

        //get all system informations and send these to server.
        public async void generateAccount()
        {
            try
            {
                this.user = new Agent();
                this.user.CompanyName = this.companyName;
                this.user.CompanyId = this.companyID;
                this.user.SystemInfo.HostName = Dns.GetHostName().ToString();
                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        this.user.IpAddress = ip.ToString();
                    }
                }

                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (ManagementObject os in searcher.Get())
                {
                    
                    this.user.SystemInfo.OSName = os["Caption"].ToString();
                    break;
                }

               

                this.user.SystemInfo.OSVersion = Environment.OSVersion.ToString();


                List<string> installedProgs = new List<string>();

                string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            //installedProgs = subkey.GetSubKeyNames().ToList();
                            //installedProgs.Add(subkey.GetValue("DisplayName").ToString());
                        }
                    }
                }

                this.user.SystemInfo.RegisteredOwner = Environment.UserName.ToString();

                this.user.MachineID = (
                            from nic in NetworkInterface.GetAllNetworkInterfaces()
                            where nic.OperationalStatus == OperationalStatus.Up
                            select nic.GetPhysicalAddress().ToString()
                        ).FirstOrDefault();

                Guid guid = Guid.NewGuid();

                string[] splitedIds = { "", "", "" };
                for(int i = 0; i<this.user.MachineID.Length; i++)
                {
                    splitedIds[i/4] += this.user.MachineID[i];
                    
                }

                this.user.Id = $"{this.user.CompanyName}-{splitedIds[0]}-{splitedIds[1]}-{splitedIds[2]}";

                var res = await _client.PostAsJsonAsync("api/agent", this.user);
                res.EnsureSuccessStatusCode();

                var readTask = res.Content.ReadAsAsync<Agent>();
                this.user = readTask.Result;

                if (this.user == null) return;

                setAppState(0);

                preProcesses = new List<IntPtr>();

                actionTimer = new System.Threading.Timer((Object param) =>
                {
                    sendRequest();

                }, null, 5000, actionDuration);

                checkAppStateTimer = new System.Threading.Timer((Object param) =>
                {
                    checkAppState();

                }, null, 5000, checkStateDuration);

            }
            catch (Exception evt)
            {
                logger(evt.Message.ToString() + "--------generate account");
            }
        }

        //check agent app status(stop/start/uninstall)
        async public void checkAppState()
        {
            try
            {
                var res = await _client.GetAsync($"api/agent/checkstate/{this.user.Id}");
                res.EnsureSuccessStatusCode();
                var readTask = res.Content.ReadAsAsync<int>();
                int state = readTask.Result;
                switch (state)
                {
                    case 0:
                        setAppState(0);
                        break;
                    case 1:
                        setAppState(1);
                        break;
                    case 2:
                        setAppState(0);
                        break;
                    case 3:
                        setAppState(2);
                        var process = new Process();
                        var startInfo = new ProcessStartInfo();
                        startInfo.WorkingDirectory = @"C:\Windows\System32";
                        startInfo.UseShellExecute = true;
                        startInfo.CreateNoWindow = true;
                        startInfo.FileName = "cmd.exe";
                        string killservice = "/c D: & installer uninstall";
                        startInfo.Arguments = killservice;
                        startInfo.Verb = "runas";
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();

                        break;
                }
            }catch(Exception evt)
            {
                logger($"{evt.Message.ToString()}------check state issues");
            }
            
        }

        //send a request when turn off agent app.
        async public void turnOff()
        {
            await _client.GetAsync($"api/agent/turnoff/{this.user.Id}");
        }

        //modify agent app status base on server status.
        public void setAppState(int state)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("AgentAppInformation", true);
            if (key == null)
            {
                key = Registry.CurrentUser.CreateSubKey("AgentAppInformation");
                key.SetValue("IsInstalled", state);
                return;
            }
            key.SetValue("IsInstalled", state);
        }

        public string[] excludesProcesses = { "Idle.exe", "SystemSettings.exe", "TextInputHost.exe", "ApplicationFrameHost.exe", "smBootTime.exe", "Microsoft.Photos.exe", "Monitor.exe", "ScriptedSandbox64.exe", "explorer.exe", "cmd.exe" };

    }
}
