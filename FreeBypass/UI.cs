using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Memory;
using Microsoft.Win32;
using static Memory.Mem;
using Helper;
using System.Runtime.InteropServices;

namespace FreeBypass
{
    public partial class UI : Form
    {
        KeyHelper kh = new KeyHelper();
        bool shift;
        public UI()
        {
            InitializeComponent();

            kh.KeyDown += Kh_KeyDown;
            kh.KeyUp += Kh_KeyUp;
        }
        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        [DllImport("KERNEL32.DLL")]
        public static extern IntPtr CreateToolhelp32SnaPSPShot(uint flags, uint processid);
        [DllImport("KERNEL32.DLL")]
        public static extern int Process32First(IntPtr handle, ref ProcessEntry32 pe);
        [DllImport("KERNEL32.DLL")]
        public static extern int Process32Next(IntPtr handle, ref ProcessEntry32 pe);
        [DllImport("KERNEL32.DLL")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        private async Task PutTaskDelay(int Time)
        {
            await Task.Delay(Time);
        }
        private async Task<bool> ApplyAimChange(string originalHex, string replacementHex)
        {
            try
            {
                ChangeMem(originalHex, replacementHex);
                await Task.Delay(500); // Add a short delay for stability
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int GetMainProcessId()
        {
            string[] targetExeNames = { "HD-Player", "HD-Player.exe", "HD-Players.exe", "AndroidProcess",
                                  "LdVBoxHeadless", "MEmuHeadless", "NoxVMHandle", "aow_exe" };

            int processId = GetProcessIdByExeName(targetExeNames);

            if (processId == 0)
            {
                PSPS.Text = "No target processes found.";
                PSPS.ForeColor = Color.DarkRed;
            }
            else
            {
                PSPS.Text = "Found target process.";
                PSPS.ForeColor = Color.Green;
            }

            return processId;
        }

        private int GetProcessIdByExeName(string[] targetExeNames)
        {
            throw new NotImplementedException();
        }

        public string PrivateChange(int index)
        {
            string processId = string.Empty;

            if (index == 1 || index == 0)
            {
                int mainProcessId = GetMainProcessId();
                processId = mainProcessId.ToString();
                PID.Text = processId;

                if (mainProcessId == 0)
                {
                    PSPS.Text = "No target processes found.";
                    PSPS.ForeColor = Color.DarkRed;
                }
                else
                {
                    PSPS.Text = "Found target process.";
                    PSPS.ForeColor = Color.Green;
                }
            }

            return processId;
        }


        public static string infile(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        private int x;
        public Mem MemLib = new Mem();


        public struct ProcessEntry32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;


        public bool hide = true;
        private WebClient webClient = new WebClient();

        public long enumerable = new long();

        private void SuspendProcess()
        {
            var process = Process.GetProcessById(Convert.ToInt32(PID.Text)); // throws exception if process does not exist
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;
                SuspendThread(pOpenThread);
                CloseHandle(pOpenThread);
            }
        }
        public void ResumeProcess()
        {
            var process = Process.GetProcessById(Convert.ToInt32(PID.Text));
            if (process.ProcessName == string.Empty)
                return;
            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                    continue;
                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);
                CloseHandle(pOpenThread);
            }
        }
        public async void ChangeMem(string original, string replace)
        {
            try
            {
                this.MemLib.OpenProcess(Convert.ToInt32(PID.Text));

                IEnumerable<long> scanmem = await this.MemLib.AoBScan(0L, 140737488355327L, original, true, true);
                long firstScan = scanmem.FirstOrDefault();

                if (firstScan == 0)
                {
                    PSPS.Text = "Failed To Apply, Try Again";
                    PSPS.ForeColor = Color.DarkRed;
                    return; // Exit the method early if the scan failed
                }

                foreach (long address in scanmem)
                {
                    this.MemLib.ChangeProtection(address.ToString("X"), Mem.MemoryProtection.ReadWrite, out Mem.MemoryProtection _);
                    this.MemLib.WriteMemory(address.ToString("X"), "bytes", replace);
                }

                PSPS.Text = "Applied";
            }
            catch (Exception ex)
            {
                // Handle exceptions here
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        private void UI_Load(object sender, EventArgs e)
        {

        }
        private void Kh_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey) shift = false;

        }

        private void Kh_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey) shift = true;
            if (e.KeyCode == Keys.Delete)
            {
                Environment.Exit(0);
            }
            if (e.KeyCode == Keys.Home)
            {
                if (keyhide == false)
                {
                    this.Hide();
                    keyhide = true;
                }
                else
                {
                    this.Show();
                    keyhide = false;
                }
            }
            if (e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey) shift = true;
            if (shift == true && e.KeyCode == Keys.Delete)
            {
                Environment.Exit(0);
            }

            if (e.KeyCode == Keys.F1)
            {

            }

            if (e.KeyCode == Keys.Delete)
            {
                Environment.Exit(0);
            }
            if (e.KeyCode == Keys.PageUp)
            {
                if (keyhide == false)
                {
                    const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
                    SetWindowDisplayAffinity(this.Handle, WDA_EXCLUDEFROMCAPTURE);
                    keyhide = true;
                }
                else
                {
                    const uint WDA_NONE = 0;
                    SetWindowDisplayAffinity(this.Handle, WDA_NONE);
                    keyhide = false;
                }
            }
            if (e.KeyCode == Keys.End)
            {
                if (keyhide == false)
                {
                    PrivateChange(0);
                    SuspendProcess();
                    PSPS.Text = "Emulator Pause";
                    keyhide = true;
                }
                else
                {
                    PSPS.Text = "Emulator Resume";
                    PrivateChange(0);
                    ResumeProcess();
                    keyhide = false;
                }
            }
            if (e.KeyCode == Keys.PageDown)
            {
                if (keyhide == false)
                {
                    this.ShowInTaskbar = false;
                    this.FormBorderStyle = FormBorderStyle.None;
                    keyhide = true;
                }
                else
                {
                    this.ShowInTaskbar = true;
                    this.FormBorderStyle = FormBorderStyle.None;
                    keyhide = false;
                }
            }

        }
        public bool keyhide = true;
        public static bool IsAlreadyRunning = false;

        private async void guna2CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);

            ChangeMem("10 00 00 00 62 00 6F 00 6E 00 65 00 5F 00 4C 00 65 00 66 00 74 00 5F 00 57 00 65 00 61 00 70 00 6F 00 6E 00",
                "10 00 00 00 62 00 6F 00 6E 00 65 00 5F 00 48 00 65 00 61 00 64 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00");

            PSPS.ForeColor = Color.LightCyan;
            PSPS.Text = "Wait For Applying Step - 1 ";
            Program.ShowNotification("Wait For Applying Step - 1 ");

            await Task.Delay(11000);
            ChangeMem("4C 7B 5A BD 0A 57 66 BB 1E 21 48 BA 2A C2 CF 3B 96 FB 28 3D E8 B1 17 BD E3 99 7F 3F 04 00 80 3F 01 00 80 3F FC FF 7F 3F ?? ?? ?? ?? 23 AA A6 B8 46 0A CD 70 00 00 00 00",
                "D1 0A C0 BE 16 DC 98 BD BB 82 97 B4 00 00 00 00 BF B2 2F 3F 43 32 73 36 66 03 7B 35 72 1C C7 3F 72 1C C7 3F 72 1C C7 3F ?? ?? ?? ?? 23 AA A6 B8 B2 F7 1F A4");

            PSPS.ForeColor = Color.LightCyan;
            PSPS.Text = "Wait For Applying Step - 2";
            Program.ShowNotification("Wait For Applying Step - 2 ");

            await Task.Delay(11000);

            Program.ShowNotification("Wait For Applying Step - last ");
            ChangeMem("47 7B 5A BD AE 57 66 BB 5C 1F 48 BA 1B C0 CF 3B 9C FB 28 3D A2 B1 17 BD E4 99 7F 3F 04 00 80 3F 00 00 80 3F FE FF 7F 3F",
                      "BF 87 BF BE 16 DC 98 BD BB 82 97 B4 00 00 00 00 BF B2 2F 3F 43 32 73 36 66 03 7B 35 72 1C C7 3F 72 1C C7 3F 72 1C C7 3F");
            PSPS.ForeColor = Color.LightCyan;
            PSPS.Text = "Wait For Applying Last Aimbot Trick";
        }

        private void guna2CheckBox5_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);
            ChangeMem(
"0A 00 A0 E3 4C 5A 06 EB 44 00 96 E5 10 01 90 E5",
"00 F0 20 E3");

            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Applying Antiban Defender ";
            Program.ShowNotification("Applying Antiban ");
        }

        private void guna2CheckBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (guna2CheckBox6.Checked == true)
            {

                PrivateChange(0);
                SuspendProcess();

            }
            else
            {

                PrivateChange(0);
                ResumeProcess();
            }
        }

        private async void guna2CheckBox4_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);

            ChangeMem(
           "43 68 65 61 74 69 6E 67 5F 41 69 6D 41 73 73 69 73 74",
           "43 68 65 40 74 69 6E 67 5F 41 69 6D 41 73 73 69 73 74");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Step 1 Anticheat Defender ";
            Program.ShowNotification("Step 1 Anticheat Defender ");

            await Task.Delay(11000);

            ChangeMem(
            "47 61 6D 65 56 61 72 44 65 66",
            "43 68 65 61 74 69 6E 67 5F 4D 65 6D 6F 72 79 48 61 63 6B");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 2 Anticheat Defender ";
            Program.ShowNotification("Step 2 Anticheat Defender ");
            await Task.Delay(11000);
            ChangeMem(
            "49 44 48 48 42 47 42 4E 48 4D 44",
             "00 00 00 00 00 00 00 00 00 00 00");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 3 Anticheat Defender ";
            Program.ShowNotification("Step 3 Anticheat Defender ");

        }

        private async void guna2CheckBox8_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);
            ChangeMem(
           "01 01 A0 E3 1E FF 2F E1",
           "01 00 A0 E3 6E 00 54 E3");
            PSPS.ForeColor = Color.Cyan;
            Program.ShowNotification("Applying AntiBlack");
            PSPS.Text = "Applying AntiBlack";

            await Task.Delay(11000);

            ChangeMem("7F 45 4C 46 01 01 01 00",
             "42 F9 20 E5 0E EE 0F E1");
            PSPS.ForeColor = Color.Cyan;
            Program.ShowNotification("Applying 2 AntiBlack");
            PSPS.Text = "Applying 2 AntiBlack";

        }

        private async void guna2CheckBox7_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);
            ChangeMem(

            "0A 00 A0 E3 ?? ?? ?? ?? ?? ?? ?? ?? 08",
            "00 F0 20 E3");

            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = " Step 1 Anti-Report ";

            await Task.Delay(11000);

            ChangeMem(

          "0A 00 A0 E3 ?? ?? ?? ?? ?? ?? ?? ?? 03",
          "00 F0 20 E3");

            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Last Step Anti-Report ";
        }

        private void guna2CheckBox9_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);
            ChangeMem(
               "7F 45 4C 46 01 01 01 00",
               "01 00 A0 E3 1E FF 2F E0");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Step 1 Lib Bypass Defender";
            Program.ShowNotification("Step 1 Lib Bypass Defender ");
        }

        private async void guna2CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);

            ChangeMem("62 6F 6E 65 5F 4E 65 63 6B 62 6F 6E 65 5F 53 70 69 6E 65 31 42 61 73 65 20 4C 61 79 65 72 2E 53 68 6F 77 46 69 73 74 42 61 73 65 20 4C 61 79 65 72 2E 53 74 61 6E 64 49 64 6C 65 42 61 73 65 20",
                "62 6F 6E 65 5F 4E 65 63 73 62 6F 6E 65 5F 53 70 69 6E 65 31 42 61 73 65 20 4C 61 79 65 72 2E 53 68 6F 77 46 69 73 74 42 61 73 65 20 4C 61 79 65 72 2E 53 74 61 6E 64 49 64 6C 65 42 61 73 65 20");


            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Wait For Applying Step 1 AimNeck";

            await Task.Delay(10000);

            ChangeMem("62 6F 6E 65 5F 48 69 70 73 62 6F 6E 65 5F 4C 65 66 74 54 6F 65 62 6F 6E 65 5F 52 69 67 68 74 54 6F 65 49 53 56 49 53 49 42 4C 45 5F 43 41 4D 45 52 41 20 20 20 49 53 56 49 53 49 42 4C 45 5F 56 45 48 49 43 4C 45",
             "62 6F 6E 65 5F 4E 65 63 6B 62 6F 6E 65 5F 4C 65 66 74 54 6F 65 62 6F 6E 65 5F 52 69 67 68 74 54 6F 65 49 53 56 49 53 49 42 4C 45 5F 43 41 4D 45 52 41 20 20 20 49 53 56 49 53 49 42 4C 45 5F 56 45 48 49 43 4C 45");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Wait For Applying Last Step  AimNeck";
        }

        private void guna2CheckBox10_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);
            ChangeMem(
                "10 4C 2D E9 08 B0 8D E2 0C 01 9F E5 00 00 8F E0",
                "01 00 A0 E3 1E FF 2F E1 0C 01 9F E5 00 00 8F E0");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Applying Guest Reset";
        }

        private async void guna2CheckBox3_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);
            ChangeMem(
                "62 00 6F 00 6E 00 65 00 5F 00 4E 00 65 00 63 00 6B",
                "62 00 6F 00 6E 00 65 00 5F 00 4E 00 65 00 63 00 73");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Applying Step-1 AimNeck Lobby";

            await Task.Delay(10000);

            ChangeMem(
    "62 00 6F 00 6E 00 65 00 5F 00 4E 00 65 00 63 00 73 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 62 00 6F 00 6E 00 65 00 5F 00 53 00 70 00 69 00 6E 00 65",
    "62 00 6F 00 6E 00 65 00 5F 00 4E 00 65 00 63 00 6B");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Applying Step-2 AimNeck Lobby";

            await Task.Delay(10000);

            ChangeMem(
    "62 00 6F 00 6E 00 65 00 5F 00 4E 00 65 00 63 00 73 00 00 00 68 00 65",
    "62 00 6F 00 6E 00 65 00 5F 00 4E 00 65 00 63 00 6B 00 00 00 68 00 65");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Applying Step-3 AimNeck Lobby";

            await Task.Delay(10000);

            ChangeMem(
    "62 00 6F 00 6E 00 65 00 5F 00 48 00 69 00 70 00 73 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 09 00 00 00 62 00 6F 00 6E 00 65 00 5F 00 48 00 65 00 61 00 64",
    "62 00 6F 00 6E 00 65 00 5F 00 4E 00 65 00 63 00 6B");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Applying Last Step AimNeck Lobby";
        }

        private void guna2ControlBox1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private async void guna2CheckBox11_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);

            ChangeMem(
           "49 44 48 48 42 47 42 4E 48 4D 44",
           "00 00 00 00 00 00 00 00 00 00 00");
            PSPS.ForeColor = Color.Cyan;
            PSPS.Text = "Step 1 Anticheat Defender ";
            Program.ShowNotification("Step 1 Anticheat Defender ");

            await Task.Delay(11000);

            ChangeMem(
            "47 61 6D 65 56 61 72 44 65 66",
            "43 68 65 61 74 69 6E 67 5F 4D 65 6D 6F 72 79 48 61 63 6B");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 2 Anticheat Defender ";
            Program.ShowNotification("Step 2 Anticheat Defender ");

            await Task.Delay(11000);

            ChangeMem(
            "50 4B 45 4A 42 4C 4E 42 41 48 48",
            "49 44 48 48 42 47 42 4E 48 4D 44");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 3 Anticheat Defender ";
            Program.ShowNotification("Step 3 Anticheat Defender ");

            await Task.Delay(11000);

            ChangeMem(
            "73 74 72 6F 6E 67 68 6F 6C 64",
            "73 74 72 6F 6E 67 64 69 73 61 62 6C 65");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 4 Anticheat Defender ";
            Program.ShowNotification("Step 4 Anticheat Defender ");

            await Task.Delay(11000);

            ChangeMem(
            "F6 0D ?? EA",
            "00 F0 20 E3");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 5 Anticheat Defender ";
            Program.ShowNotification("Step 5 Anticheat Defender ");

            await Task.Delay(11000);

            ChangeMem(
            "0A 00 A0 E3 09 10 A0 E1 1C D0 4B E2 F0 4F BD E8",
            "00 F0 20 E3");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 6 Anticheat Defender ";
            Program.ShowNotification("Step 6 Anticheat Defender ");

        }

        private async void guna2CheckBox12_CheckedChanged(object sender, EventArgs e)
        {
            PrivateChange(1);
            ChangeMem(
             "02 E0 D4 F8 A4 10 0D 18 66 68 5E F0 5D F8 29 46",
                 "00 20 70 40");
            PSPS.ForeColor = Color.Cyan;

            PSPS.Text = "Step 1 Anticheat Lobby Defender ";
            Program.ShowNotification("Step 1 Anticheat Defender ");

            await Task.Delay(11000);

            ChangeMem(
            "02 E0 D9 F8 A4 10 08 44 48 F2 21 7A 45 F6 12 08",
                 "00 20 70 40");
            PSPS.ForeColor = Color.Cyan;


            PSPS.Text = "Step 2 Anticheat Lobby Defender ";
            Program.ShowNotification("Step 6 Anticheat Defender ");
        }

        private async void guna2CheckBox13_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                PrivateChange(1);

                PSPS.ForeColor = Color.Cyan;
                PSPS.Text = "Applying Step 1 Aim Changes...";

                bool step1Success = await ApplyAimChange("62 6F 6E 65 5F 4E 65 63 6B 62 6F 6E 65 5F 53 70 69 6E 65 31 42 61 73 65 20 4C 61 79 65 72 2E 53 68 6F 77 46 69 73 74 42 61 73 65 20 4C 61 79 65 72 2E", "62 6F 6E 65 5F 4E 65 63 73 62 6F 6E 65 5F 53 70 69 6E 65 31 42 61 73 65 20 4C 61 79 65 72 2E 53 68 6F 77 46 69 73 74 42 61 73 65 20 4C 61 79 65 72 2E");

                if (step1Success)
                {
                    PSPS.ForeColor = Color.LightCyan;
                    PSPS.Text = "Step 1 Aim Changes Applied. Waiting for Step 2...";

                    await Task.Delay(10000); // Wait for 10 seconds

                    bool step2Success = await ApplyAimChange("62 6F 6E 65 5F 48 69 70 73 62 6F 6E 65 5F 4C 65 66 74 54 6F 65 62 6F 6E 65 5F 52 69 67 68 74 54 6F 65 49 53 56 49 53 49 42 4C 45 5F 43 41 4D 45 52 41", "62 6F 6E 65 5F 4E 65 63 6B 62 6F 6E 65 5F 4C 65 66 74 54 6F 65 62 6F 6E 65 5F 52 69 67 68 74 54 6F 65 49 53 56 49 53 49 42 4C 45 5F 43 41 4D 45 52 41");



                    if (step2Success)
                    {
                        PSPS.ForeColor = Color.Green;
                        PSPS.Text = "Aim Changes Applied Successfully!";


                    }


                    else
                    {
                        PSPS.ForeColor = Color.Red;
                        PSPS.Text = "Failed to Apply Step 1 Aim Changes.";
                    }

                }
            }
            catch (Exception ex)
            {
                PSPS.ForeColor = Color.Red;
                PSPS.Text = "An error occurred: " + ex.Message;
            }
        }
    }
    
}

