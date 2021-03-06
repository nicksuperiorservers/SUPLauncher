﻿using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using Microsoft.VisualBasic;
using DiscordRPC;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using CefSharp;
using System.Drawing.Text;


namespace SUPLauncher
{



    public partial class frmLauncher : Form
    {
        int refresh = 0;
        bool appStarted = false;
        public static string dupePath = "";
        string playerServer;
        public static readonly SteamBridge steam = new SteamBridge();
        public static string forumSteamIDLookup = "";
        bool isTopPanelDragged = false;
        Point offset;
        Size _normalWindowSize;
        Point _normalWindowLocation = Point.Empty;
        private Image refresh_img;
        Image original_refreshimg;
        KeyboardHook hook = new KeyboardHook();
        public static Overlay overlay = new Overlay();
        public static Bans banPage = null;


        private void rotateInThread(Bitmap bm, float angle)
        {
                if (InvokeRequired)
                {
                    this.Invoke(new Action<Bitmap, float>(rotateInThread), new object[] { bm, angle });
                   
                }
           refresh_img = RotateBitmap(bm, angle);
        }


        private void GetPointBounds(PointF[] points, out float xmin, out float xmax, out float ymin, out float ymax)
        {
            xmin = points[0].X;
            xmax = xmin;
            ymin = points[0].Y;
            ymax = ymin;
            foreach (PointF point in points)
            {
                if (xmin > point.X) xmin = point.X;
                if (xmax < point.X) xmax = point.X;
                if (ymin > point.Y) ymin = point.Y;
                if (ymax < point.Y) ymax = point.Y;
            }
        }

        private Bitmap RotateBitmap(Bitmap bm, float angle)
        {
            // Make a Matrix to represent rotation
            // by this angle.
            Matrix rotate_at_origin = new Matrix();
            rotate_at_origin.Rotate(angle);

            // Rotate the image's corners to see how big
            // it will be after rotation.
            PointF[] points =
            {
                new PointF(0, 0),
                new PointF(bm.Width, 0),
                new PointF(bm.Width, bm.Height),
                new PointF(0, bm.Height),
            };
            rotate_at_origin.TransformPoints(points);
            float xmin, xmax, ymin, ymax;
            GetPointBounds(points, out xmin, out xmax,
                out ymin, out ymax);

            // Make a bitmap to hold the rotated result.
            int wid = (int)Math.Round(xmax - xmin);
            int hgt = (int)Math.Round(ymax - ymin);
            Bitmap result = new Bitmap(wid, hgt);

            // Create the real rotation transformation.
            Matrix rotate_at_center = new Matrix();
            rotate_at_center.RotateAt(angle,
                new PointF(wid / 2f, hgt / 2f));

            // Draw the image onto the new bitmap rotated.
            using (Graphics gr = Graphics.FromImage(result))
            {
                // Use smooth image interpolation.
                gr.InterpolationMode = InterpolationMode.High;

                // Clear with the color in the image's upper left corner.
                gr.Clear(bm.GetPixel(0, 0));

                //// For debugging. (It's easier to see the background.)
                //gr.Clear(Color.LightBlue);

                // Set up the transformation to rotate.
                gr.Transform = rotate_at_center;

                // Draw the image centered on the bitmap.
                int x = (wid - bm.Width) / 2;
                int y = (hgt - bm.Height) / 2;
                gr.DrawImage(bm, x, y);
            }

            // Return the result bitmap.
            return result;
        }

        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string ipClassName, string ipWindowName);

        
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont,
               IntPtr pdv, [System.Runtime.InteropServices.In] ref uint pcFonts);

        private PrivateFontCollection fonts = new PrivateFontCollection();
        public frmLauncher()
        {
            if (Process.GetProcessesByName("steam").Length == 0) // Check if steam is running (Thanks Red Means Recording)
            {
                MessageBox.Show("An error occurred. Please restart the program when steam is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Interaction.Shell("taskkill /pid " + Process.GetCurrentProcess().Id.ToString() + " /f");
            }
            Thread trd = new Thread(new ThreadStart(Run));
            trd.Start();
            InitializeComponent();

            GetCurrentServer(steam.GetSteamId().ToString(), true);
            Thread.Sleep(5000);
            trd.Abort();
            refresh_img = imgrefresh.Image;
            original_refreshimg = imgrefresh.Image;

            hook.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(Keyboard);
            
            

            hook.RegisterKeybind(Properties.Settings.Default.overlayModiferKey,
                Properties.Settings.Default.overlayKey);

            byte[] fontData = Properties.Resources.Prototype;
            IntPtr fontPtr = System.Runtime.InteropServices.Marshal.AllocCoTaskMem(fontData.Length);
            System.Runtime.InteropServices.Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
            uint dummy = 0;
            fonts.AddMemoryFont(fontPtr, Properties.Resources.Prototype.Length);
            AddFontMemResourceEx(fontPtr, (uint)Properties.Resources.Prototype.Length, IntPtr.Zero, ref dummy);
            System.Runtime.InteropServices.Marshal.FreeCoTaskMem(fontPtr);
            lblUsername.Font = new Font(fonts.Families[0], lblUsername.Font.Size);
            btnForums.Font = new Font(fonts.Families[0], btnForums.Font.Size);
            btnTS.Font = new Font(fonts.Families[0], btnTS.Font.Size);
            btnDRPRules.Font = new Font(fonts.Families[0], btnDRPRules.Font.Size);
            btnMilRPRules.Font = new Font(fonts.Families[0], btnMilRPRules.Font.Size);
            btnCWRPRules.Font = new Font(fonts.Families[0], btnCWRPRules.Font.Size);
            btnDupes.Font = new Font(fonts.Families[0], btnDupes.Font.Size);
            btnDanktown.Font = new Font(fonts.Families[0], btnDanktown.Font.Size);
            btnSundown.Font = new Font(fonts.Families[0], btnSundown.Font.Size);
            btnC18.Font = new Font(fonts.Families[0], btnC18.Font.Size);
            btnZombies.Font = new Font(fonts.Families[0], btnZombies.Font.Size);
            btnMilRP.Font = new Font(fonts.Families[0], btnMilRP.Font.Size);
            btnCW1.Font = new Font(fonts.Families[0], btnCW1.Font.Size);
            btnCW2.Font = new Font(fonts.Families[0], btnCW2.Font.Size);

            Opacity = 0;      //first the opacity is 0

            t1.Interval = 10;  //we'll increase the opacity every 10ms
            t1.Tick += new EventHandler(fadeIn);  //this calls the function that changes opacity 
            t1.Start();
        }
        System.Windows.Forms.Timer t1 = new System.Windows.Forms.Timer();
        #region Fade

        void fadeIn(object sender, EventArgs e)
        {
            if (Opacity >= 1)
                t1.Stop();   //this stops the timer if the form is completely displayed
            else
                Opacity += 0.05;
        }

        void fadeOut(object sender, EventArgs e)
        {
            if (Opacity <= 0)     //check if opacity is 0
            {
                t1.Stop();    //if it is, we stop the timer
                Close();   //and we try to close the form
            }
            else
                Opacity -= 0.05;
        }
        #endregion

        bool altdown = false;
        bool sdown = false;
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        private void Keyboard(object sender, KeyPressedEventArgs e)
        {


            if (chkOverlay.Checked)
            {

                try
                {
                    if (overlay.Visible)
                    {

                        overlay.Visible = false;
                        SetForegroundWindow(getGmodHandle());
                    }
                    else
                    {
                        SetForegroundWindow(getGmodHandle());
                        overlay.Visible = true;

                        SetForegroundWindow(getGmodHandle());
                    }
                }
                catch
                {
                    overlay.Visible = false;
                }
            }

        }

        public void Run()
        {
            Application.Run(new Splashscreen1());
        }

    


        /// <summary>
        /// Gets the gmod window name
        /// </summary>

        public static IntPtr getGmodHandle()
        {
            try
            {
                if (getGmodProcess() == null) return IntPtr.Zero; else if(getGmodProcess().ProcessName == "hl2") { return FindWindow(null, "Garry's Mod"); } else { return FindWindow(null, "Garry's Mod (x64)"); }
            }
            catch (Exception)
            {
                return IntPtr.Zero;
            }
            
        }

        /// <summary>
        /// Gets the gmod process, this works even if gmod is on another branch.
        /// </summary>

        public static Process getGmodProcess()
        {
                Process[] hl2 = Process.GetProcessesByName("hl2");
                Process[] gmod = Process.GetProcessesByName("gmod");
                if (hl2.Length > 0)
                {
                    return hl2[0];
                }
                else if (gmod.Length > 0)
                {
                    return gmod[0];
                }
                else
                {
                    return null;
                }
            
        }



        private void TopBar_MouseUp(object sender, MouseEventArgs e)
        {
            isTopPanelDragged = false;
            if (this.Location.Y <= 5)
            {
                
                    _normalWindowSize = this.Size;
                    _normalWindowLocation = this.Location;

                    Rectangle rect = Screen.PrimaryScreen.WorkingArea;
                    this.Location = new Point(0, 0);
                    this.Size = new System.Drawing.Size(rect.Width, rect.Height);


            }
        }

        private void TopBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (isTopPanelDragged)
            {
                Point newPoint = topBar.PointToScreen(new Point(e.X, e.Y));
                newPoint.Offset(offset);
                this.Location = newPoint;

                if (this.Location.X > 2 || this.Location.Y > 2)
                {
                    if (this.WindowState == FormWindowState.Maximized)
                    {
                        this.Location = _normalWindowLocation;
                        this.Size = _normalWindowSize;
                    }
                }
            }
        }

        private void TopBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isTopPanelDragged = true;
                Point pointStartPosition = this.PointToScreen(new Point(e.X, e.Y));
                offset = new Point
                {
                    X = this.Location.X - pointStartPosition.X,
                    Y = this.Location.Y - pointStartPosition.Y
                };
            }
            else
            {
                isTopPanelDragged = false;
            }
            if (e.Clicks == 2)
            {
                isTopPanelDragged = false;

            }
        }

        private readonly DiscordRpcClient discord = new DiscordRpcClient("594668399653814335");
   
        private void FrmLauncher_Load(object sender, EventArgs e)
        {



            if (ClientUpdater.checkForUpdates())
            {
                versionWarn.Visible = true;
                toolTip1.SetToolTip(versionWarn, "You are using an\noutdated version\nof SUPLauncher.\n\nClick to install the\nlatest version");
                toolTip1.SetToolTip(lblVersion, "You are using an\noutdated version\nof SUPLauncher.\n\nClick to install the\nlatest version");
            }

            // If a update is avaliable ask
            if (Properties.Settings.Default.updatePopup == false) // Check if user has already had a update popup
            {
                ClientUpdater.Update();
            }

            imgrefresh.SizeMode = PictureBoxSizeMode.StretchImage;
            imgrefresh.Refresh();

            discord.Initialize();
            GetUsername();
            //GetDiscordCheckStatus();
            chkDiscord.Checked = Properties.Settings.Default.discordStatus;
            GetCurrentServer(steam.GetSteamId().ToString(), true);
            GetDupes();
            try
            {
                if (chkDiscord.Checked)
                {
                    LblServer_TextChanged(this, new EventArgs());
                }
                lblVersion.Text = Application.ProductVersion;
                var client = new WebClient();
                client.Headers.Add("user-agent", "SUP Launcher"); // penguin is a fucking bitch for blocking the sup api
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=7875E26FC3C740C9901DDA4C6E74EB4E&steamids=" + steam.GetSteamId().ToString());
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream());
                string streamData = sr.ReadToEnd();
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(streamData);
                string avatarURL = json.response.players[0].avatarfull;
                byte[] avatardata = client.DownloadData(new Uri(avatarURL));
                using (var ms = new MemoryStream(avatardata))
                {
                    picImage.Image = Image.FromStream(ms);
                    client.Dispose();
                    ms.Close();
                }
                GetPlayerCountAllServers(true);
                Activate();
                tmrSteamQuery.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(null, ex.ToString(), "REPORT THIS TO NICK YOU DUMB FUCK", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //FrmLauncher_FormClosing(this, new FormClosingEventArgs(CloseReason.ApplicationExitCall, false));
            }

            chkOverlay.Checked = Properties.Settings.Default.overlayEnabled;

            loadOverlay();
            if (Properties.Settings.Default.lastUpdate != Application.ProductVersion)
            {
                new whatsNew().Show();
                Properties.Settings.Default.lastUpdate = Application.ProductVersion;
                Properties.Settings.Default.Save();
            }
            
            string keybind = "";
            if (Properties.Settings.Default.overlayModiferKey != 0)
            {
                keybind = getModiferKey(Properties.Settings.Default.overlayModiferKey) + " + " + ((Keys)Properties.Settings.Default.overlayKey).ToString();
            }
            else
            {
                keybind = ((Keys)Properties.Settings.Default.overlayKey).ToString();
            }
            lblALTS.Text = "(" + keybind + ")";
            Database db = new Database();
            db.Connect();
            db.Insert();
        }
        // "Process.Start("steam:");" is for focusing steam
        private void BtnDanktown_Click(object sender, EventArgs e)
        {
            AppStartCheck();
            if (chkAFK.Checked && appStarted == false)
            {
                Process.Start("steam:");
                WindowFocus.ActivateProcess(Process.GetProcessesByName("steam")[0].Id);
                ProcessStartInfo startInfo = new ProcessStartInfo("steam");
                Process.Start("steam://run/4000//-64bit -textmode -single_core -nojoy -low -nosound -sw -noshader -nopix -novid -nopreload -nopreloadmodels -multirun +connect rp.superiorservers.co");
                startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            }
            else
            {
                Process.Start("steam://connect/rp.superiorservers.co:27015");
            }
            appStarted = true;
        }

        //private void btnSundown_Click(object sender, EventArgs e)
        //{
        //    if (chkAFK.Checked && appStarted == false)
        //    {
        //        Process.Start("steam:");
        //        Process.Start("steam://run/4000//-64bit -textmode -single_core -nojoy -low -nosound -sw -noshader -nopix -novid -nopreload -nopreloadmodels -multirun +connect rp2.superiorservers.co");
        //    }
        //    else
        //    {
        //        Process.Start("steam://connect/rp2.superiorservers.co:27015");
        //    }
        //    appStarted = true;
        //}
        private void BtnC18_Click(object sender, EventArgs e)
        {
            AppStartCheck();
            if (chkAFK.Checked && appStarted == false)
            {
                Process.Start("steam:");
                WindowFocus.ActivateProcess(Process.GetProcessesByName("steam")[0].Id);
                Process.Start("steam://run/4000//-64bit -textmode -single_core -nojoy -low -nosound -sw -noshader -nopix -novid -nopreload -nopreloadmodels -multirun +connect rp2.superiorservers.co");
            }
            else
            {
                Process.Start("steam://connect/rp2.superiorservers.co:27015");
            }
            appStarted = true;
        }
        private void BtnZombies_Click(object sender, EventArgs e)
        {
            AppStartCheck();
            if (chkAFK.Checked && appStarted == false)
            {
                Process.Start("steam:");
                WindowFocus.ActivateProcess(Process.GetProcessesByName("steam")[0].Id);
                Process.Start("steam://run/4000//-64bit -textmode -single_core -nojoy -low -nosound -sw -noshader -nopix -novid -nopreload -nopreloadmodels -multirun +connect zrp.superiorservers.co");
            }
            else
            {
                Process.Start("steam://connect/zrp.superiorservers.co:27015");
            }
            appStarted = true;
        }
        private void BtnMilRP_Click(object sender, EventArgs e)
        {
            AppStartCheck();
            if (chkAFK.Checked && appStarted == false)
            {
                Process.Start("steam:");
                WindowFocus.ActivateProcess(Process.GetProcessesByName("steam")[0].Id);
                Process.Start("steam://run/4000//-64bit -textmode -single_core -nojoy -low -nosound -sw -noshader -nopix -novid -nopreload -nopreloadmodels -multirun +connect milrp.superiorservers.co");
            }
            else
            {
                Process.Start("steam://connect/milrp.superiorservers.co:27015");
            }
            appStarted = true;
        }
        private void BtnCW1_Click(object sender, EventArgs e)
        {
            AppStartCheck();
            if (chkAFK.Checked && appStarted == false)
            {
                Process.Start("steam:");
                WindowFocus.ActivateProcess(Process.GetProcessesByName("steam")[0].Id);
                Process.Start("steam://run/4000//-64bit -textmode -single_core -nojoy -low -nosound -sw -noshader -nopix -novid -nopreload -nopreloadmodels -multirun +connect cwrp.superiorservers.co");
            }
            else
            {
                Process.Start("steam://connect/cwrp.superiorservers.co:27015");
            }
            appStarted = true;
        }
        private void BtnCW2_Click(object sender, EventArgs e)
        {
            AppStartCheck();
            if (chkAFK.Checked && appStarted == false)
            {
                Process.Start("steam:");
                WindowFocus.ActivateProcess(Process.GetProcessesByName("steam")[0].Id);
                Process.Start("steam://run/4000//-64bit -textmode -single_core -nojoy -low -nosound -sw -noshader -nopix -novid -nopreload -nopreloadmodels -multirun +connect cwrp2.superiorservers.co");
            }
            else
            {
                Process.Start("steam://connect/cwrp2.superiorservers.co:27015");
            }
            appStarted = true;
        }
        private void Panel1_MouseClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("Keep in mind that this program is still being worked on and is not an official release of the SUP Launcher. In order to use this program, you must just simply click on a button and watch the magic happen. The credit for this idea goes to aStonedPenguin, and all new releases will available on the github (nickiscool1022/SUPLauncher). Thanks for using this nice little program I made, and have a fun time playing SuperiorServers." + Environment.NewLine + Environment.NewLine + "-Nick", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void BtnForums_Click(object sender, EventArgs e)
        {
            Process.Start("https://forum.superiorservers.co");
        }
        private void BtnTS_Click(object sender, EventArgs e)
        {
            Process.Start("ts3server://TS.SuperiorServers.co:9987");
        }
        private void FrmLauncher_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            try
            {
                t1 = new System.Windows.Forms.Timer();
                t1.Interval = 10;  //we'll increase the opacity every 10ms
                t1.Tick += new EventHandler(fadeOut);  //this calls the function that changes opacity 
                t1.Start();

                
                if (chkDiscord.Checked && File.Exists("1") == false)
                {
                    File.Create("1");
                    File.SetAttributes("1", FileAttributes.Hidden);
                }
                else
                    File.Delete("1");
                Cef.Shutdown();
                if (this.Opacity == 0)
                    e.Cancel = false;
                Interaction.Shell("taskkill /pid " + Process.GetCurrentProcess().Id.ToString() + " /f /t"); // Whoops
            }
            catch (Exception)
            {
                Interaction.Shell("taskkill /pid " + Process.GetCurrentProcess().Id.ToString() + " /f /t");
            }
            
        }
        private void ChkAFK_CheckedChanged(object sender, EventArgs e)
        {
            //notifyIcon1.Visible = true;
            if (chkAFK.Checked)
            {
                //notifyIcon1.ShowBalloonTip(5000, "AFK Mode", "You are now in AFK Mode.\n Press on a server from the list on the menu\n and confirm it in steam to begin AFKing on SUP!", ToolTipIcon.Info);
                Notification notif = new Notification("You are now in AFK Mode. \nPress on a server from the list on the menu \nand confirm it in steam to begin AFKing \non SUP!", "AFK MODE", false, 115);
                notif.Show();
            }
            else
            {
                //notifyIcon1.ShowBalloonTip(5000, "AFK Mode", "You are no longer in AFK Mode.\n Pressing on a server will launch the game normally through steam with regular graphics (not in command", ToolTipIcon.Info);
                Notification notif = new Notification("You are no longer in AFK Mode. \nPressing on a server will launch the game normally through steam with regular graphics.", "AFK MODE", false, 115);
                notif.Show();
            }
            try
            {
                Process.GetProcessesByName("hl2")[0].Kill();
            }
            catch (Exception)
            {
                // Does nothing if permission is denied...
                // As doing something may bring up usless errors
                // Even though gmod is already closed
            }
            try
            {
                Process.GetProcessesByName("gmod")[0].Kill();
            }
            catch (Exception)
            {
                // Do nothing if process does not exist
            }
            appStarted = false;
        }

        private void PicImage_Click(object sender, EventArgs e)
        {
            new Bans(steam.GetSteamId().ToString()).Show();
        }
        private void BtnDRPRules_Click(object sender, EventArgs e)
        {
            Process.Start("https://superiorservers.co/darkrp/rules");
        }
        private void BtnMilRPRules_Click(object sender, EventArgs e)
        {
            Process.Start("https://superiorservers.co/ssrp/milrp/rules");
        }

        private void BtnCWRPRules_Click(object sender, EventArgs e)
        {
            Process.Start("https://superiorservers.co/ssrp/cwrp/rules");
        }
        private void LblVersion_Click(object sender, EventArgs e)
        {
            ClientUpdater.Update();
        }
        void GetUsername()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // Secure security protocol for querying the steam API
            HttpWebRequest request = WebRequest.CreateHttp("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=7875E26FC3C740C9901DDA4C6E74EB4E&steamids=" + steam.GetSteamId());
            request.UserAgent = new Random().NextDouble().ToString();
            WebResponse response = null;
            try
            {
                response = request.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream()); // Create stream to access web data
                string data = sr.ReadToEnd(); // Read data from response stream
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(data);

                // old: //string raw = currentRecord.Substring(currentRecord.IndexOf("personaname") + "personaname".Length + 3, (currentRecord.IndexOf("lastlogoff") - (currentRecord.IndexOf("personaname") + "personaname".Length + 6)));
                /* new: */ lblUsername.Text = "SUP Launcher (" + json.response.players[0].personaname + ")";
            }
            catch (Exception)
            {
                lblUsername.Text = "SUP Launcher";
            }
        }
        //void GetDiscordCheckStatus()
        //{
        //    if (File.Exists("1"))
        //        chkDiscord.Checked = true;
        //    else
        //        chkDiscord.Checked = false;
        //}
        void AppStartCheck()
        {
            Process proc = getGmodProcess();

            if (proc == null || proc.Container == null)
                appStarted = false;
            else
                appStarted = true;
        }
        void GetDupes()
        {
            // GetValue() only returns X:\Program Files (x86)\Steam
            string SteamInstallPathDir;
            if (Environment.Is64BitOperatingSystem)
                SteamInstallPathDir = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", null).ToString();
            else
                SteamInstallPathDir = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null).ToString();
            if (Directory.Exists(SteamInstallPathDir + @"\steamapps\common\GarrysMod\garrysmod\data\advdupe2") == false)
            {
                var sr = new StreamReader(SteamInstallPathDir + @"\steamapps\libraryfolders.vdf");
                string raw;
                do
                {
                    raw = sr.ReadLine();
                } while (raw.Contains(@"""1""") == false);
                string refined = raw.Substring(raw.IndexOf("\t\t") + 3);
                for (int i = 0; i < refined.Length; i++)
                {
                    if (refined.Substring(i, 1) == "\\".ToString())
                    {
                        refined = refined.Remove(i, 1);
                    }
                }
                dupePath = refined.Substring(0, refined.Length - 1) + @"\steamapps\common\GarrysMod\garrysmod\data\advdupe2";
            }
            else
                dupePath = SteamInstallPathDir + @"\steamapps\common\GarrysMod\garrysmod\data\advdupe2";
        }
        /// <summary>
        /// Gets the server name and IP the provided steam user is on
        /// </summary>
        /// <param name="steamID">The steamid to use</param>
        /// <param name="normalState">Whether or not it is normally called via timer or not.</param>
        void GetCurrentServer(string steamID, bool normalState)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; // Secure security protocol for querying the steam API
                HttpWebRequest request = WebRequest.CreateHttp("http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=7875E26FC3C740C9901DDA4C6E74EB4E&steamids=" + steamID);
                request.UserAgent = "Nick";
                WebResponse response = null;
                response = request.GetResponse(); // Get Response from webrequest
                StreamReader sr = new StreamReader(response.GetResponseStream()); // Create stream to access web data
                var rawResults = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd());
                string ip = rawResults.response.players.First.gameserverip.ToString();
                string playerName = rawResults.response.players.First.personaname.ToString();
                switch (ip)
                {
                    case "208.103.169.12:27015":
                        if (normalState)
                        {
                            panDanktown.BackColor = Color.SpringGreen;
                            panC18.BackColor = Color.RoyalBlue;
                            panZombies.BackColor = Color.RoyalBlue;
                            panMilRP.BackColor = Color.RoyalBlue;
                            panCW1.BackColor = Color.RoyalBlue;
                            panCW2.BackColor = Color.RoyalBlue;
                            lblServer.Text = "Danktown";
                        }
                        else
                        {
                            playerServer = playerName + "(" + steamID + ") is on Danktown(208.103.169.12:27015)";
                        }
                        break;
                    case "208.103.169.13:27015":
                        if (normalState)
                        {
                            panDanktown.BackColor = Color.RoyalBlue;
                            panC18.BackColor = Color.SpringGreen;
                            panZombies.BackColor = Color.RoyalBlue;
                            panMilRP.BackColor = Color.RoyalBlue;
                            panCW1.BackColor = Color.RoyalBlue;
                            panCW2.BackColor = Color.RoyalBlue;
                            lblServer.Text = "C18";
                        }
                        else
                        {
                            playerServer = playerName + "(" + steamID + ") is on C18(208.103.169.13:27015)";
                        }
                        break;
                    case "208.103.169.14:27015":
                        if (normalState)
                        {
                            panDanktown.BackColor = Color.RoyalBlue;
                            panC18.BackColor = Color.RoyalBlue;
                            panZombies.BackColor = Color.SpringGreen;
                            panMilRP.BackColor = Color.RoyalBlue;
                            panCW1.BackColor = Color.RoyalBlue;
                            panCW2.BackColor = Color.RoyalBlue;
                            lblServer.Text = "ZRP";
                        }
                        else
                        {
                            playerServer = playerName + "(" + steamID + ") is on ZombieRP(208.103.169.14:27015)";
                        }
                        break;
                    case "208.103.169.18:27015":
                        if (normalState)
                        {
                            panDanktown.BackColor = Color.RoyalBlue;
                            panC18.BackColor = Color.RoyalBlue;
                            panZombies.BackColor = Color.RoyalBlue;
                            panMilRP.BackColor = Color.SpringGreen;
                            panCW1.BackColor = Color.RoyalBlue;
                            panCW2.BackColor = Color.RoyalBlue;
                            lblServer.Text = "MilRP";
                        }
                        else
                        {
                            playerServer = playerName + "(" + steamID + ") is on MilRP(208.103.169.18:27015)";
                        }
                        break;
                    case "208.103.169.16:27015":
                        if (normalState)
                        {
                            panDanktown.BackColor = Color.RoyalBlue;
                            panC18.BackColor = Color.RoyalBlue;
                            panZombies.BackColor = Color.RoyalBlue;
                            panMilRP.BackColor = Color.RoyalBlue;
                            panCW1.BackColor = Color.SpringGreen;
                            panCW2.BackColor = Color.RoyalBlue;
                            lblServer.Text = "CWRP #1";
                        }
                        else
                        {
                            playerServer = playerName + "(" + steamID + ") is on CWRP #1(208.103.169.16:27015)";
                        }
                        break;
                    case "208.103.169.17:27015":
                        if (normalState)
                        {
                            panDanktown.BackColor = Color.RoyalBlue;
                            panC18.BackColor = Color.RoyalBlue;
                            panZombies.BackColor = Color.RoyalBlue;
                            panMilRP.BackColor = Color.RoyalBlue;
                            panCW1.BackColor = Color.RoyalBlue;
                            panCW2.BackColor = Color.SpringGreen;
                            lblServer.Text = "CWRP #2";
                        }
                        else
                        {
                            playerServer = playerName + "(" + steamID + ") is on CWRP #2(208.103.169.17:27015)";
                        }
                        break;
                    default:
                        if (normalState)
                        {
                            panDanktown.BackColor = Color.RoyalBlue;
                            panC18.BackColor = Color.RoyalBlue;
                            panZombies.BackColor = Color.RoyalBlue;
                            panMilRP.BackColor = Color.RoyalBlue;
                            panCW1.BackColor = Color.RoyalBlue;
                            panCW2.BackColor = Color.RoyalBlue;
                            lblServer.Text = "";
                        }
                        else
                        {
                            playerServer = playerName + "(" + steamID + ") is on SUP at the moment.";
                        }
                        break;
                }
            }
            catch (Exception)
            {
                if (normalState)
                {
                    panDanktown.BackColor = Color.RoyalBlue;
                    panC18.BackColor = Color.RoyalBlue;
                    panZombies.BackColor = Color.RoyalBlue;
                    panMilRP.BackColor = Color.RoyalBlue;
                    panCW1.BackColor = Color.RoyalBlue;
                    panCW2.BackColor = Color.RoyalBlue;
                    lblServer.Text = "";
                }
                else
                {
                    playerServer = "This player is not playing on a server or has their steam profile private.";
                }
            }
        }

        private void TmrSteamQuery_Tick(object sender, EventArgs e)
        {
            GetCurrentServer(steam.GetSteamId().ToString(), true);
            if (lblServer.Text == "" && chkAFK.Checked)
            {
                BtnDanktown_Click(this, new EventArgs());
            }
        }

        private void ChkDiscord_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDiscord.Checked)
            {
                LblServer_TextChanged(this, new EventArgs());
            }
            else
            {
                discord.ClearPresence();
            }
        }

        private void LblServer_TextChanged(object sender, EventArgs e)
        {
            if (discord.IsInitialized && chkDiscord.Checked)
            {
                switch (lblServer.Text)
                {
                    case "Danktown":
                        discord.SetPresence(new RichPresence()
                        {
                            Details = "Playing on Danktown",
                            State = "",
                            Timestamps = Timestamps.Now,
                            Assets = new Assets()
                            {
                                LargeImageKey = "suplogo",
                                LargeImageText = "SuperiorServers.co"
                            }
                        });
                        break;
                    case "C18":
                        discord.SetPresence(new RichPresence()
                        {
                            Details = "Playing on C18",
                            State = "",
                            Timestamps = Timestamps.Now,
                            Assets = new Assets()
                            {
                                LargeImageKey = "suplogo",
                                LargeImageText = "SuperiorServers.co"
                            }
                        });
                        break;
                    case "ZRP":
                        discord.SetPresence(new RichPresence()
                        {
                            Details = "Playing on ZRP",
                            State = "",
                            Timestamps = Timestamps.Now,
                            Assets = new Assets()
                            {
                                LargeImageKey = "suplogo",
                                LargeImageText = "SuperiorServers.co"
                            }
                        });
                        break;
                    case "MilRP":
                        discord.SetPresence(new RichPresence()
                        {
                            Details = "Playing on MilRP",
                            State = "",
                            Timestamps = Timestamps.Now,
                            Assets = new Assets()
                            {
                                LargeImageKey = "suplogo",
                                LargeImageText = "SuperiorServers.co"
                            }
                        });
                        break;
                    case "CWRP #1":
                        discord.SetPresence(new RichPresence()
                        {
                            Details = "Playing on CWRP #1",
                            State = "",
                            Timestamps = Timestamps.Now,
                            Assets = new Assets()
                            {
                                LargeImageKey = "suplogo",
                                LargeImageText = "SuperiorServers.co"
                            }
                        });
                        break;
                    case "CWRP #2":
                        discord.SetPresence(new RichPresence()
                        {
                            Details = "Playing on CWRP #2",
                            State = "",
                            Timestamps = Timestamps.Now,
                            Assets = new Assets()
                            {
                                LargeImageKey = "suplogo",
                                LargeImageText = "SuperiorServers.co"
                            }
                        });
                        break;
                    case "":
                        discord.SetPresence(new RichPresence()
                        {
                            Details = "Waiting to join a server...",
                            State = "",
                            Timestamps = Timestamps.Now,
                            Assets = new Assets()
                            {
                                LargeImageKey = "suplogo",
                                LargeImageText = "SuperiorServers.co"
                            }
                        });
                        break;
                }
                
            }
        }
        private byte GetPlayerCount(string ip)
        {
            // DT: 208.103.169.12
            // SD: 208.103.169.13 
            // C18: 208.103.169.15
            // ZRP: 208.103.169.14 
            // MilRP: 208.103.169.18 
            // CWRP: 208.103.169.16 
            // CWRP #2: 208.103.169.17 

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            byte[] rawData = new byte[512];
            socket.Connect(ip, 27015);
            byte[] sendBytes = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54, 0x53, 0x6F, 0x75, 0x72, 0x63, 0x65, 0x20, 0x45, 0x6E, 0x67, 0x69, 0x6E, 0x65, 0x20, 0x51, 0x75, 0x65, 0x72, 0x79, 0x00 };
            socket.Send(sendBytes);

            socket.Receive(rawData);
            using (var ms = new MemoryStream(rawData))
            {
                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte();
                ms.ReadByte();

                ms.ReadByte();
                ms.ReadByte();

                ms.ReadTerminatedString(); 
                ms.ReadTerminatedString();
                ms.ReadTerminatedString();
                ms.ReadTerminatedString();

                ms.ReadByte();
                ms.ReadByte();

                return Convert.ToByte(ms.ReadByte());
            }
        }
        private void GetPlayerCountAllServers(bool startup)
        {
            if (refresh == 0 || startup)
            {

                ThreadHelperClass.SetText(this, lblDT, GetPlayerCount("rp.superiorservers.co").ToString() + "/128");
                //ThreadHelperClass.SetText(this, lblSD, GetPlayerCount("208.103.169.13").ToString() + "/128");
                ThreadHelperClass.SetText(this, lblC18, GetPlayerCount("rp2.superiorservers.co").ToString() + "/128");
                ThreadHelperClass.SetText(this, lblZRP, GetPlayerCount("zrp.superiorservers.co").ToString() + "/128");
                ThreadHelperClass.SetText(this, lblMRP, GetPlayerCount("milrp.superiorservers.co").ToString() + "/128");
                ThreadHelperClass.SetText(this, lblCW1, GetPlayerCount("cwrp.superiorservers.co").ToString() + "/128");
                ThreadHelperClass.SetText(this, lblCW2, GetPlayerCount("cwrp2.superiorservers.co").ToString() + "/128");
                refresh++;
                tmrRefresh.Start();
            }

        }

        private void BtnDupes_Click(object sender, EventArgs e)
        {
            new DupeManager().ShowDialog();
        }

        private void LblSERVERLookup_Click(object sender, EventArgs e)
        {
            bool IDAquired = false;
            bool dirty = false;
            string rawID = Interaction.InputBox("Enter steamid.", "Enter info.", " ");
            string refinedID = "";
            if (rawID == "")
                return;
            if (rawID.StartsWith("7") && rawID.Length == 76561197960265728.ToString().Length)
                IDAquired = true;
            if (IDAquired == false && (rawID.Contains("STEAM_0:0:") || rawID.Contains("STEAM_0:1:")))
            {
                try
                {
                    if (rawID.StartsWith("STEAM_0:0"))
                    {
                        refinedID = ((Convert.ToInt32(rawID.Substring(10, rawID.Length - 10)) * 2) + 76561197960265728).ToString();
                    }
                    else if (rawID.StartsWith("STEAM_0:1"))
                    {
                        refinedID = ((Convert.ToInt32(rawID.Substring(10, rawID.Length - 10)) * 2) + 76561197960265729).ToString();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Invalid STEAMID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    dirty = true;
                }
            }
            if (dirty == false)
            {
                if (IDAquired)
                {
                    //GetCurrentServer(rawID, false);
                }
                else
                {
                   // GetCurrentServer(refinedID, false);
                }
                MessageBox.Show(playerServer, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Invalid STEAMID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LblFORUMLookup_Click(object sender, EventArgs e)
        {
            string steamid = Interaction.InputBox("Enter steamid.", "Enter info.", " ");
            if ((steamid.Contains("STEAM_0:0:") || steamid.Contains("STEAM_0:1:")) || (steamid.StartsWith("7") && steamid.Length == 76561197960265728.ToString().Length))
            {
                //Process.Start("https://superiorservers.co/profile/" + steamid);
                forumSteamIDLookup = steamid;
                new Bans(steamid).Show();
                //wbForumbrowser.Url = new Uri("https://superiorservers.co/profile/" + steamid);
                //wbForumbrowser.Size = new Size(1280, 720);
                //wbForumbrowser.Visible = true;
            }
        }

        private void LblRefresh_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                int i = 0;
            while (i != 10)
            {
                i = i + 1;
                    Thread.Sleep(70);
                    rotateInThread(new Bitmap(refresh_img), 90);
                    imgrefresh.Image = refresh_img;
            }
                imgrefresh.Image = original_refreshimg;
                return;

            }).Start();
                GetPlayerCountAllServers(false);
        }

        private void TmrRefresh_Tick(object sender, EventArgs e)
        {
            if (refresh > 0 && refresh < 60)
            {
                refresh++;
            }
            else
            {
                refresh = 0;
            }
        }

        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox1.Text.Contains("STEAM_0:0:") || textBox1.Text.Contains("STEAM_0:1:") || (textBox1.Text.StartsWith("7") && textBox1.Text.Length == 76561197960265728.ToString().Length))
                {
                    //Process.Start("https://superiorservers.co/profile/" + steamid);
                    forumSteamIDLookup = textBox1.Text;
                    new Bans(textBox1.Text).Show();
                    //wbForumbrowser.Url = new Uri("https://superiorservers.co/profile/" + steamid);
                    //wbForumbrowser.Size = new Size(1280, 720);
                    //wbForumbrowser.Visible = true;
                } else
                {
                    MessageBox.Show("Invalid SteamID. Make sure you have the correct SteamID", "Error");
                }
            }

        }
        private void TextBox1_Leave(object sender, EventArgs e)
        {
            textBox1.Text = "STEAM_0:X:XXXXXXXXX";
        }
        private void TextBox1_Enter(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void FrmLauncher_Click(object sender, EventArgs e)
        {
            pictureBox1.Focus();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.discordStatus = chkDiscord.Checked;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void PictureBox2_Click(object sender, EventArgs e)
        {
            ClientUpdater.Update();
        }

        private void ToolTip1_Draw(object sender, DrawToolTipEventArgs e)
        {
        }

        private void ToolTip1_Popup(object sender, PopupEventArgs e)
        {
            if ((e.AssociatedControl.Name != lblVersion.Name) && (e.AssociatedControl.Name != versionWarn.Name))
            {
                toolTip1.ToolTipIcon = ToolTipIcon.Info;
            }
            else
            {
                toolTip1.ToolTipIcon = ToolTipIcon.Warning;
            }
            if (e.AssociatedControl == lblALTS)
                toolTip1.ToolTipTitle = "Overlay";
            else if (e.AssociatedControl == versionWarn)
                toolTip1.ToolTipTitle = lblVersion.Text;
            else if (e.AssociatedControl == picImage)
                toolTip1.ToolTipTitle = "Your avatar";
            else
                toolTip1.ToolTipTitle = e.AssociatedControl.Text;
        }
        private string getModiferKey(uint key)
        {
            if (key == (uint)SUPLauncher.ModifierKeys.Control)
            {
                return "CTRL";
            }
            else if(key == (uint)SUPLauncher.ModifierKeys.Alt)
            {
                return "ALT";
            } else if (key == (uint)SUPLauncher.ModifierKeys.Shift)
            {
                return "SHIFT";
            }
            return "";
        }

        private void chkOverlay_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.overlayEnabled = chkOverlay.Checked;
            Properties.Settings.Default.Save();
            Notification notif;
            if (!chkOverlay.Checked)
            {
                notif = new Notification("SUPLauncher overlay is disabled.", "NOTIFICATION", true);
                notif.Show();
            }
            else
            {
                string keybind = "";
                if (Properties.Settings.Default.overlayModiferKey != 0)
                {
                    keybind = getModiferKey(Properties.Settings.Default.overlayModiferKey) + " + " + ((Keys)Properties.Settings.Default.overlayKey).ToString();
                }
                else
                {
                    keybind = ((Keys)Properties.Settings.Default.overlayKey).ToString();
                }
                notif = new Notification("SUPLauncher overlay is enabled.\n(" + keybind + ")", "NOTIFICATION", true);
                notif.Show();
                loadOverlay();
            }
        }

        public void loadOverlay()
        {
            if (getGmodProcess() != null) {
                if (chkOverlay.Checked)
                {
                    if (overlay.IsDisposed)
                    {
                        overlay = new Overlay();
                    }
                    overlay.Visible = false;
                    //string keybind = "";
                    //if (Properties.Settings.Default.overlayModiferKey != 0)
                    //{
                    //    keybind = getModiferKey(Properties.Settings.Default.overlayModiferKey) + " + " + ((Keys)Properties.Settings.Default.overlayKey).ToString();
                    //}
                    //else
                    //{
                    //    keybind = ((Keys)Properties.Settings.Default.overlayKey).ToString();
                    //}
                    //Notification notification = new Notification("SUPLauncher overlay is enabled.\n(" + keybind + ")", "NOTIFICATION" , true);
                    //notification.Show();
                    SetForegroundWindow(getGmodHandle());
                }
                else
                {
                    if (overlay != null)
                    {
                        if (!overlay.IsDisposed)
                        {
                            overlay.Close();
                        }
                    }
                }
            }
        }
       
        private void lblALTS_Click(object sender, EventArgs e)
        {
            new keyBinder().Show();
        }

        private void frmLauncher_KeyPress(object sender, KeyPressEventArgs e)
        {
            MessageBox.Show("test");
        }
    }



    public static class MemoryStreamExtensions
    {
        
        public static string ReadTerminatedString(this MemoryStream ms)
        {
            List<byte> res = new List<byte>();

            byte last;
            while ((last = (byte)ms.ReadByte()) != 0x00)
            {
                res.Add(last);
            }

            return System.Text.Encoding.ASCII.GetString(res.ToArray());
        }
    }
    public static class ThreadHelperClass // Because fuck threads and me not allowing to just set text on a label like a normal person
    {
        delegate void SetTextCallback(Form f, Control ctrl, string text);
        /// <summary>
        /// Set text property of various controls
        /// </summary>
        /// <param name="form">The calling form</param>
        /// <param name="ctrl">The control being modified</param>
        /// <param name="text">The text to set</param>
        public static void SetText(Form form, Control ctrl, string text)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (ctrl.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                form.Invoke(d, new object[] { form, ctrl, text });
            }
            else
            {
                ctrl.Text = text;
            }
        }
    }
}
