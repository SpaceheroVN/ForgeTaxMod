// Điểm khởi chạy ForgeTax Mod
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace khong_gian_mod
{
    internal static class chuong_trinh
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        // Khởi động ứng dụng WinForms x64
        [STAThread]
        public static void Main(string[] doi_so)
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool che_do_thu = (doi_so != null && doi_so.Length > 0 && doi_so[0] == "--tu-kiem-tra");
            using (var cua_so = new cua_so_chinh(che_do_thu))
            {
                if (che_do_thu)
                {
                    var hen_gio = new Timer();
                    hen_gio.Interval = 250;
                    hen_gio.Tick += delegate { hen_gio.Stop(); cua_so.Close(); };
                    hen_gio.Start();
                }
                Application.Run(cua_so);
            }
        }
    }
}
