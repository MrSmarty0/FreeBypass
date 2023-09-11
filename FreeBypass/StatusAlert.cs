using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FreeBypass
{
    public partial class StatusAlert : Form
    {
        public StatusAlert()
        {
            InitializeComponent();
        }
        [DllImport("user32.dll")]
        public static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        public void showAlert(string msg)
        {
            this.Opacity = 0.0;
            this.StartPosition = FormStartPosition.Manual;
            string fname;

            for (int i = 1; i < 10; i++)
            {
                fname = "alert" + i.ToString();
                StatusAlert frm = (StatusAlert)Application.OpenForms[fname];

                if (frm == null)
                {
                    this.Name = fname;
                    this.x = Screen.PrimaryScreen.WorkingArea.Width - this.Width + 15;
                    this.y = Screen.PrimaryScreen.WorkingArea.Height - this.Height * i - 5 * i;
                    this.Location = new Point(this.x, this.y);
                    break;

                }

            }
            this.x = Screen.PrimaryScreen.WorkingArea.Width - base.Width - 5;
            this.state.Text = msg;

            this.Show();
            this.action = OiAction.start;
            this.timer1.Interval = 1;
            this.timer1.Start();
        }
        public enum OiAction
        {
            wait,
            start,
            close
        }

        public enum enmType
        {
            Normal,
            Sucess,
            Erro,
        }
        private OiAction action;

        private int x, y;

        private void StatusAlert_Load(object sender, EventArgs e)
        {
            const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
            SetWindowDisplayAffinity(this.Handle, WDA_EXCLUDEFROMCAPTURE);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            switch (this.action)
            {

                case OiAction.wait:
                    timer1.Interval = 1000;
                    action = OiAction.close;
                    break;
                case OiAction.start:
                    this.timer1.Interval = 1;
                    this.Opacity += 0.1;
                    if (this.x < this.Location.X)
                    {
                        this.Left--;
                    }
                    else
                    {
                        if (this.Opacity == 1.0)
                        {
                            action = OiAction.wait;
                        }
                    }
                    break;
                case OiAction.close:
                    timer1.Interval = 1;
                    this.Opacity -= 0.1;

                    this.Left -= 3;
                    if (base.Opacity == 0.0)
                    {
                        base.Close();
                    }
                    break;
            }
        }
    }
}
