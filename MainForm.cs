using LeetCodeInputFormatter.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeetCodeInputFormatter
{
    public partial class MainForm : Form
    {
        #region 剪贴板相关

        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern IntPtr SetClipboardViewer(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern IntPtr ChangeClipboardChain(IntPtr hwnd, IntPtr hWndNext);

        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        private const int WM_DRAWCLIPBOARD = 0x308;
        private const int WM_CHANGECBCHAIN = 0x30D;
        private IntPtr nextClipHwnd;

        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        private readonly Status status = new Status();

        // 支持的语言列表
        private static readonly string[] languages = new string[] {"C/C++", "Python", "Go"};

        private string lastData = "";

        public MainForm()
        {
            InitializeComponent();

            nextClipHwnd = SetClipboardViewer(Handle);
            Icon = Resources.icon;
            notifyIcon.Icon = Resources.icon;
            // 自定义渲染器，符合 Windows 10 风格
            contextMenuStrip.Renderer = new CustomContextMenuStripRenderer();
            foreach (string language in languages)
            {
                languageToolStripMenuItem.DropDownItems.Add(language, null, new EventHandler(OnLanguageChanged));
            }

            languageToolStripMenuItem.Text = string.Format("语言（{0}）", languages[0]);

            UpdateStatusLabel();
        }

        private string GoFormat(string input)
        {
            string t = input.Replace('[', '{').Replace(']', '}').Replace('\n', ',');
            bool hasDot = t.Contains('.');
            if (hasDot)
            {
                return "[]float64" + t;
            }

            int idx = -1;
            int offset = 0;

            for (var i = 0; i < t.Length; i++)
            {
                if (t[i] == '\"')
                {
                    if (idx == -1)
                    {
                        idx = i;
                    }
                    else
                    {
                        offset = i - idx;
                        break;
                    }
                }
            }

            if (idx != -1)
            {
                return "[]" + (offset == 1 ? "byte" + t.Replace('\"', '\'') : "string" + t);
            }

            return "[]int" + t;
        }

        // https://www.jianshu.com/p/b60b77fcb2a3
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    SendMessage(nextClipHwnd, m.Msg, m.WParam, m.LParam);
                    if (m.WParam.ToInt32() == 0)
                    {
                        break;
                    }

                    Debug.WriteLine(m);
                    string type = ClipboardProcesser.GetDataTypeFromClipboard();
                    object data = ClipboardProcesser.GetDataFromClipboardByType(type);

                    if (type == ClipboardDataFormat.TEXT)
                    {
                        if (status.enable)
                        {
                            string input = (string) data;
                            if (input == lastData)
                            {
                                break;
                            }

                            string t = "";
                            if (new string[] {"C/C++"}.Contains(status.language))
                            {
                                t = input.Replace('[', '{').Replace(']', '}').Replace('\n', ',');
                            }
                            else if (new string[] {"Python"}.Contains(status.language))
                            {
                                t = input.Replace('\n', ',');
                            }
                            else if (new string[] {"Go"}.Contains(status.language))
                            {
                                t = GoFormat(input);
                            }

                            if (!t.Equals(input))
                            {
                                lastData = t;
                                ClipboardProcesser.SetDataToClipboard(t, ClipboardDataFormat.TEXT);
                                status.count++;
                            }
                        }

                        UpdateStatusLabel();
                    }

                    break;
                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipHwnd)
                        nextClipHwnd = m.LParam;
                    else
                        SendMessage(nextClipHwnd, m.Msg, m.WParam, m.LParam);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem clickedItem = (ToolStripMenuItem) sender;
            status.language = clickedItem.Text;
            UpdateStatusLabel();

            languageToolStripMenuItem.Text = string.Format("语言（{0}）", clickedItem.Text);
        }

        private void UpdateStatusLabel()
        {
            if (status.enable)
            {
                notifyIcon.Icon = Resources.icon;

                statuLabel.Text = "已启用";
                statuLabel.ForeColor = Color.FromArgb(75, 173, 145);
            }
            else
            {
                notifyIcon.Icon = Resources.iconDisabled;
                statuLabel.Text = "未启用";
                statuLabel.ForeColor = Color.FromArgb(175, 64, 52);
            }

            languageLabel.Text = status.language;
            countLabel.Text = status.count.ToString();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 最小化窗口
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                Hide();
            }
        }

        private void statuLabel_Click(object sender, EventArgs e)
        {
            status.enable = !status.enable;
            UpdateStatusLabel();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
            Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        private void toggleEnableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleEnableToolStripMenuItem.Checked = !toggleEnableToolStripMenuItem.Checked;
            status.enable = toggleEnableToolStripMenuItem.Checked;
            UpdateStatusLabel();
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void showMainFormToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            //从观察链中删除本观察窗口
            ChangeClipboardChain(Handle, nextClipHwnd);
            //将变动消息WM_CHANGECBCHAIN消息传递到下一个观察链中的窗口
            SendMessage(nextClipHwnd, WM_CHANGECBCHAIN, Handle, nextClipHwnd);
        }

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs) e).Button == MouseButtons.Left)
            {
                toggleEnableToolStripMenuItem.Checked = !toggleEnableToolStripMenuItem.Checked;
                status.enable = toggleEnableToolStripMenuItem.Checked;
                UpdateStatusLabel();
            }
        }
    }

    // 自定义类不能放在最开始，否则设计器不能正确识别
    class Status
    {
        public bool enable = true;
        public string language = "C/C++";
        public int count = 0;
    }

    // https://stackoverflow.com/questions/32786250/windows-10-styled-contextmenustrip
    public class CustomContextMenuStripColorTable : ProfessionalColorTable
    {
        public override Color MenuItemBorder
        {
            get { return Color.FromArgb(145, 201, 247); }
        }

        public override Color MenuItemSelected
        {
            get { return Color.FromArgb(145, 201, 247); }
        }

        public override Color ToolStripDropDownBackground
        {
            get { return Color.FromArgb(240, 240, 240); }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Color.FromArgb(240, 240, 240); }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Color.FromArgb(240, 240, 240); }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Color.FromArgb(240, 240, 240); }
        }
    }

    public class CustomContextMenuStripRenderer : ToolStripProfessionalRenderer
    {
        public CustomContextMenuStripRenderer()
            : base(new CustomContextMenuStripColorTable())
        {
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(e.ArrowRectangle.Location, e.ArrowRectangle.Size);
            r.Inflate(-2, -6);
            e.Graphics.DrawLines(Pens.Black, new Point[]
            {
                new Point(r.Left, r.Top),
                new Point(r.Right, r.Top + r.Height / 2),
                new Point(r.Left, r.Top + r.Height)
            });
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(e.ImageRectangle.Location, e.ImageRectangle.Size);
            r.Inflate(-4, -6);
            e.Graphics.DrawLines(Pens.Black, new Point[]
            {
                new Point(r.Left, r.Bottom - r.Height / 2),
                new Point(r.Left + r.Width / 3, r.Bottom),
                new Point(r.Right, r.Top)
            });
        }
    }
}