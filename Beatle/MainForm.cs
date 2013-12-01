﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Beatle
{
    public partial class MainForm : MetroFramework.Forms.MetroForm
    {
        public delegate void StringStringDelegate(string msg, string writer);
        private bool isShiftPressed = false;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        Thread listen;

        //                                                             127.0.0.1     10.0.0.146
        private IPEndPoint partnerEndPoint = new IPEndPoint(IPAddress.Parse("10.0.0.146"), 1998);



        public MainForm()
        {
            InitializeComponent();
            ChatTextBox.Select();

            InComing listener = new InComing();
            listen = new Thread(listener.Listen);
            listen.Start();
            InComing.messegeAddedEvent += WriteToChat;

            OutGoing.parent = this;

            timer.Interval = 15;
            timer.Tick += timer_Tick;
            timer.Start();

            this.Text += "Noam Gal"; // = GetFirstName() + GetLastName();

            Chat.Font = MetroFramework.MetroFonts.Label(MetroFramework.MetroLabelSize.Small, MetroFramework.MetroLabelWeight.Regular);
        }

        private void ChatTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar.Equals((char)13) && !isShiftPressed)
            {
                e.Handled = true;
                SendMessage(null, null);
            }
        }

        private void SendMessage(object sender, EventArgs e)
        {
            string msg = ChatTextBox.Text;

            WriteToChat(msg, "Me");

            ChatTextBox.Select();
            ChatTextBox.Text = "";
            ChatTextBox.Refresh();


            if (!string.IsNullOrWhiteSpace(msg))
                (new Thread(() => (new OutGoing()).SendMessage(msg, partnerEndPoint))).Start();
        }

        public void WriteToChat(string msg, string writer)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                if (InvokeRequired)
                {
                    this.BeginInvoke(new StringStringDelegate(WriteToChat), new object[] { msg, writer });
                    return;
                }

                Chat.AppendText(writer + ":\t" + msg + Environment.NewLine);
            }
        }

        public void WriteRecievedSymboleToChat()
        {
            if (ThreadSafety(new Action(WriteRecievedSymboleToChat), new object[] { }))
                return;

            Chat.AppendText("\t[R]\r\n\r\n");
        }

        public void WriteErrorToChat()
        {
            if (ThreadSafety(new Action(WriteErrorToChat), new object[] { }))
                return;
            if (Chat != null)
                Chat.AppendText("Error Connecting To Partner\r\n\r\n");
        }


        private bool ThreadSafety(Action action, params object[] args)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(action, args);
                return true;
            }
            return false;
        }

        private bool ThreadSafety(Delegate a, params object[] args)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(a, args);
                return true;
            }
            return false;
        }






        private void ChatTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.ShiftKey))
                isShiftPressed = false;
        }

        private void ChatTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.ShiftKey))
                isShiftPressed = true;
        }

        private void Chat_Enter(object sender, EventArgs e)
        {
            timer.Start();
            HideCaret(Chat.Handle);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            HideCaret(Chat.Handle);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        private void Chat_Leave(object sender, EventArgs e)
        {
            timer.Stop();
        }

        private void metroTabControl2_CustomPaint(object sender, MetroFramework.Drawing.MetroPaintEventArgs e)
        {
            Point location = new Point();
            location.X = Profile.Location.X;
            location.Y = Profile.Location.Y - 29;

            Size size = new Size();
            size.Width = Profile.Size.Width + 1;
            size.Height = Profile.Size.Height + 30;

            e.Graphics.DrawRectangle(new Pen(SystemColors.ControlLight, 2), new Rectangle(location, size));
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            OutGoing.isExiting = true;

            InComing.isExiting = true;
            Socket selfish = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            selfish.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1998));
            selfish.Send(Encoding.ASCII.GetBytes("FLYING MONKEYS ARE WAY BETTER THAN A JAR OF PICKLES"), SocketFlags.None);

            System.Windows.Forms.Application.Exit();
        }
    }
}
