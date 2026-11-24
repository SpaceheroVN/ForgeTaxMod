// Giao diện chính ForgeTax Mod
using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace khong_gian_mod
{
    // === Cửa sổ chính ForgeTax Mod ===
    public sealed class cua_so_chinh : Form
    {
        private const int wm_hotkey = 0x0312;
        private const uint co_khong_lap = 0x4000;
        private const int vk_f1 = 0x70;
        private const int vk_f2 = 0x71;
        private const int vk_f3 = 0x72;
        private const int vk_f4 = 0x73;
        private const int vk_f5 = 0x74;
        private const int vk_f6 = 0x75;
        private const int vk_f7 = 0x76;
        private const int vk_f8 = 0x77;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private readonly dong_co_bo_nho dong_co = new dong_co_bo_nho();
        private readonly AutoResetEvent su_kien_danh_thuc = new AutoResetEvent(false);
        private readonly bool che_do_thu;

        private ma_ngon_ngu ngon_ngu_hien_tai = ma_ngon_ngu.tieng_viet;
        private ban_ghi_trang_thai trang_thai_cuoi = new ban_ghi_trang_thai();
        private string ma_loi_gan = "mo_tro_choi";
        private long phien_cong_tac;

        private Thread luong_nen;
        private volatile bool dang_chay = true;
        private volatile bool yeu_cau_gan_ngay;

        private readonly System.Windows.Forms.Timer bo_dem_giu_nut = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer bo_dem_giu_phim = new System.Windows.Forms.Timer();
        private int ket_qua_nut_dang_giu;
        private long moc_bat_dau_giu_nut;
        private int phim_dang_giu;
        private long moc_bat_dau_giu_phim;
        private long lan_lap_phim_cuoi;

        // Điều khiển giao diện
        private khung_bo_goc khung_tieu_de;
        private Label nhan_thuong_hieu;
        private hop_chon_ngon_ngu hop_ngon_ngu;
        private Label nhan_pid;
        private Label nhan_man_choi;
        private nut_hien_dai nut_ket_noi_lai;

        private hang_cong_tac[] cac_cong_tac;
        private nut_hien_dai nut_f5;
        private nut_hien_dai nut_f6;
        private nut_hien_dai nut_f7;
        private nut_hien_dai nut_f8;

        private khung_bo_goc khung_tang_tien;
        private Label nhan_tang_tien;
        private o_nhap_toi o_nhap_vang;
        private o_nhap_toi o_nhap_credits;
        private nut_hien_dai nut_nhan;

        // Khởi tạo giao diện và bộ đếm giữ nút
        public cua_so_chinh(bool kiem_thu)
        {
            che_do_thu = kiem_thu;
            Text = "ForgeTax Mod";
            ClientSize = new Size(468, 540);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = bang_mau.nen_chinh;
            DoubleBuffered = true;

            try
            {
                Icon ic = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (ic != null) Icon = ic;
            }
            catch
            {
            }

            tao_giao_dien();
            ap_dung_ngon_ngu();

            bo_dem_giu_nut.Interval = 75;
            bo_dem_giu_nut.Tick += xu_ly_giu_nut;
            bo_dem_giu_phim.Interval = 55;
            bo_dem_giu_phim.Tick += xu_ly_giu_phim;
        }

        // Đóng bảng xổ khi nhấn Esc
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && hop_ngon_ngu != null && hop_ngon_ngu.dang_mo)
            {
                hop_ngon_ngu.dong_bang_xo();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Bật tiêu đề tối và đăng ký phím tắt
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int gia_tri = 1;
            DwmSetWindowAttribute(Handle, 20, ref gia_tri, 4);
            DwmSetWindowAttribute(Handle, 19, ref gia_tri, 4);

            if (!che_do_thu)
            {
                for (int i = 0; i < 8; i++)
                {
                    RegisterHotKey(Handle, 100 + i, co_khong_lap, (uint)(vk_f1 + i));
                }
                luong_nen = new Thread(vong_lap_nen);
                luong_nen.IsBackground = true;
                luong_nen.Start();
            }
        }

        // Dựng bố cục giao diện chính
        private void tao_giao_dien()
        {
            EventHandler dong_nn = delegate
            {
                if (hop_ngon_ngu != null) hop_ngon_ngu.dong_bang_xo();
            };
            Click += dong_nn;

            // --- Khung tiêu đề 3 dòng ---
            khung_tieu_de = new khung_bo_goc();
            khung_tieu_de.SetBounds(14, 14, 440, 104);
            khung_tieu_de.ban_kinh = 10;
            khung_tieu_de.mau_nen_ngoai = bang_mau.nen_chinh;
            khung_tieu_de.Click += dong_nn;
            Controls.Add(khung_tieu_de);

            nhan_thuong_hieu = new Label();
            nhan_thuong_hieu.Text = "FORGETAX MOD";
            nhan_thuong_hieu.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            nhan_thuong_hieu.ForeColor = bang_mau.vang_chinh;
            nhan_thuong_hieu.BackColor = bang_mau.nen_the;
            nhan_thuong_hieu.SetBounds(14, 11, 240, 24);
            nhan_thuong_hieu.Click += dong_nn;
            khung_tieu_de.Controls.Add(nhan_thuong_hieu);

            nhan_pid = new Label();
            nhan_pid.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            nhan_pid.ForeColor = bang_mau.vang_chinh;
            nhan_pid.BackColor = bang_mau.nen_the;
            nhan_pid.TextAlign = ContentAlignment.MiddleLeft;
            nhan_pid.UseMnemonic = false;
            nhan_pid.SetBounds(14, 41, 292, 24);
            nhan_pid.Click += dong_nn;
            khung_tieu_de.Controls.Add(nhan_pid);

            nut_ket_noi_lai = new nut_hien_dai();
            nut_ket_noi_lai.mau_nen_ngoai = bang_mau.nen_the;
            nut_ket_noi_lai.SetBounds(312, 40, 114, 26);
            nut_ket_noi_lai.co_chu = 8.2f;
            nut_ket_noi_lai.goc_bo = 6;
            nut_ket_noi_lai.Click += delegate
            {
                hop_ngon_ngu.dong_bang_xo();
                yeu_cau_gan_ngay = true;
                su_kien_danh_thuc.Set();
                SystemSounds.Beep.Play();
            };
            khung_tieu_de.Controls.Add(nut_ket_noi_lai);

            nhan_man_choi = new Label();
            nhan_man_choi.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            nhan_man_choi.ForeColor = bang_mau.chu_phu;
            nhan_man_choi.BackColor = bang_mau.nen_the;
            nhan_man_choi.TextAlign = ContentAlignment.MiddleLeft;
            nhan_man_choi.AutoEllipsis = true;
            nhan_man_choi.UseMnemonic = false;
            nhan_man_choi.SetBounds(14, 72, 292, 24);
            nhan_man_choi.Click += dong_nn;
            khung_tieu_de.Controls.Add(nhan_man_choi);

            hop_ngon_ngu = new hop_chon_ngon_ngu();
            hop_ngon_ngu.mau_nen_ngoai = bang_mau.nen_the;
            hop_ngon_ngu.SetBounds(312, 70, 114, 26);
            hop_ngon_ngu.gan_bang_xo(this, new Point(326, 114));
            hop_ngon_ngu.su_kien_doi += delegate
            {
                ngon_ngu_hien_tai = (hop_ngon_ngu.chi_so_chon == 1) ? ma_ngon_ngu.tieng_anh : ma_ngon_ngu.tieng_viet;
                ap_dung_ngon_ngu();
                cap_nhat_hien_thi_trang_thai();
            };
            khung_tieu_de.Controls.Add(hop_ngon_ngu);

            // --- 4 công tắc F1..F4 ---
            string[] phim = new string[] { "F1", "F2", "F3", "F4" };
            string[] ma_phim = new string[] { "f1", "f2", "f3", "f4" };
            cac_cong_tac = new hang_cong_tac[4];
            int y = 128;
            for (int i = 0; i < 4; i++)
            {
                var hang = new hang_cong_tac(phim[i]);
                hang.mau_nen_ngoai = bang_mau.nen_chinh;
                hang.SetBounds(14, y, 440, 46);
                string k = ma_phim[i];
                hang.su_kien_bam += delegate
                {
                    hop_ngon_ngu.dong_bang_xo();
                    bam_cong_tac(k);
                };
                Controls.Add(hang);
                cac_cong_tac[i] = hang;
                y += 54;
            }

            // --- 4 nút thao tác F5..F8 ---
            nut_f5 = new nut_hien_dai();
            nut_f5.mau_nen_ngoai = bang_mau.nen_chinh;
            nut_f5.SetBounds(14, 348, 215, 42);
            nut_f5.huy_hieu_phim = "F5";
            nut_f5.goc_bo = 9;
            nut_f5.Click += delegate
            {
                hop_ngon_ngu.dong_bang_xo();
                dung_trieu_hoi();
            };
            Controls.Add(nut_f5);

            nut_f6 = new nut_hien_dai();
            nut_f6.mau_nen_ngoai = bang_mau.nen_chinh;
            nut_f6.SetBounds(239, 348, 215, 42);
            nut_f6.huy_hieu_phim = "F6";
            nut_f6.goc_bo = 9;
            nut_f6.Click += delegate
            {
                hop_ngon_ngu.dong_bang_xo();
                dung_hoi_to();
            };
            Controls.Add(nut_f6);

            nut_f7 = new nut_hien_dai();
            nut_f7.mau_nen_ngoai = bang_mau.nen_chinh;
            nut_f7.SetBounds(14, 398, 215, 42);
            nut_f7.huy_hieu_phim = "F7";
            nut_f7.goc_bo = 9;
            nut_f7.MouseDown += delegate(object s, MouseEventArgs me)
            {
                hop_ngon_ngu.dong_bang_xo();
                if (me.Button == MouseButtons.Left) bat_dau_giu_nut(3);
            };
            nut_f7.MouseUp += delegate { dung_giu_nut(); };
            nut_f7.MouseLeave += delegate { dung_giu_nut(); };
            Controls.Add(nut_f7);

            nut_f8 = new nut_hien_dai();
            nut_f8.mau_nen_ngoai = bang_mau.nen_chinh;
            nut_f8.SetBounds(239, 398, 215, 42);
            nut_f8.huy_hieu_phim = "F8";
            nut_f8.goc_bo = 9;
            nut_f8.MouseDown += delegate(object s, MouseEventArgs me)
            {
                hop_ngon_ngu.dong_bang_xo();
                if (me.Button == MouseButtons.Left) bat_dau_giu_nut(4);
            };
            nut_f8.MouseUp += delegate { dung_giu_nut(); };
            nut_f8.MouseLeave += delegate { dung_giu_nut(); };
            Controls.Add(nut_f8);

            // --- Thanh tặng Vàng & Credits ---
            khung_tang_tien = new khung_bo_goc();
            khung_tang_tien.mau_nen_ngoai = bang_mau.nen_chinh;
            khung_tang_tien.SetBounds(14, 452, 440, 74);
            khung_tang_tien.ban_kinh = 10;
            khung_tang_tien.Click += dong_nn;
            Controls.Add(khung_tang_tien);

            nhan_tang_tien = new Label();
            nhan_tang_tien.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            nhan_tang_tien.ForeColor = bang_mau.vang_chinh;
            nhan_tang_tien.BackColor = bang_mau.nen_the;
            nhan_tang_tien.UseMnemonic = false;
            nhan_tang_tien.SetBounds(14, 9, 280, 19);
            nhan_tang_tien.Click += dong_nn;
            khung_tang_tien.Controls.Add(nhan_tang_tien);

            o_nhap_vang = new o_nhap_toi();
            o_nhap_vang.mau_nen_ngoai = bang_mau.nen_the;
            o_nhap_vang.SetBounds(12, 33, 158, 30);
            o_nhap_vang.su_kien_focus += dong_nn;
            o_nhap_vang.su_kien_enter += delegate { bam_nhan_tien(); };
            khung_tang_tien.Controls.Add(o_nhap_vang);

            o_nhap_credits = new o_nhap_toi();
            o_nhap_credits.mau_nen_ngoai = bang_mau.nen_the;
            o_nhap_credits.SetBounds(178, 33, 158, 30);
            o_nhap_credits.su_kien_focus += dong_nn;
            o_nhap_credits.su_kien_enter += delegate { bam_nhan_tien(); };
            khung_tang_tien.Controls.Add(o_nhap_credits);

            nut_nhan = new nut_hien_dai();
            nut_nhan.mau_nen_ngoai = bang_mau.nen_the;
            nut_nhan.SetBounds(344, 33, 84, 30);
            nut_nhan.la_nut_chinh = true;
            nut_nhan.goc_bo = 7;
            nut_nhan.Click += delegate
            {
                hop_ngon_ngu.dong_bang_xo();
                bam_nhan_tien();
            };
            khung_tang_tien.Controls.Add(nut_nhan);
        }

        // Đặt trạng thái mẫu để kiểm thử
        public void dat_mau_kiem_thu(int pid, bool f1, bool f3, bool vao_man, bool mo_popup, int hover_popup)
        {
            trang_thai_cuoi = new ban_ghi_trang_thai();
            trang_thai_cuoi.da_ket_noi = pid > 0;
            trang_thai_cuoi.da_moc = pid > 0;
            trang_thai_cuoi.ma_tien_trinh = pid;
            trang_thai_cuoi.o_man_hinh_chinh = !vao_man;
            trang_thai_cuoi.trong_man_choi = vao_man;
            trang_thai_cuoi.khong_pha_huy = f1;
            trang_thai_cuoi.khong_tinh_tien = f3;
            trang_thai_cuoi.co_kiem_de = vao_man;
            trang_thai_cuoi.cap_kiem_de = 12;
            trang_thai_cuoi.vang = 100000;
            trang_thai_cuoi.luot_credits = 50;
            cap_nhat_hien_thi_trang_thai();
            cac_cong_tac[0].dat_trang_thai_ngay(f1);
            cac_cong_tac[1].dat_trang_thai_ngay(false);
            cac_cong_tac[2].dat_trang_thai_ngay(f3);
            cac_cong_tac[3].dat_trang_thai_ngay(false);
            hop_ngon_ngu.dat_mo_popup_kiem_thu(mo_popup, hover_popup);
        }

        // Cập nhật chữ theo ngôn ngữ
        private void ap_dung_ngon_ngu()
        {
            Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "tieu_de");
            nut_ket_noi_lai.Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "ket_noi_lai");
            cac_cong_tac[0].noi_dung = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f1");
            cac_cong_tac[1].noi_dung = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f2");
            cac_cong_tac[2].noi_dung = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f3");
            cac_cong_tac[3].noi_dung = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f4");
            nut_f5.Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f5");
            nut_f6.Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f6");
            nut_f7.Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f7");
            nut_f8.Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "f8");
            nhan_tang_tien.Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "tang_tien");
            o_nhap_vang.dat_goi_y(ngon_ngu.lay_chu(ngon_ngu_hien_tai, "vang"));
            o_nhap_credits.dat_goi_y(ngon_ngu.lay_chu(ngon_ngu_hien_tai, "credits"));
            nut_nhan.Text = ngon_ngu.lay_chu(ngon_ngu_hien_tai, "nhan");
        }

        // So sánh trạng thái tránh vẽ thừa
        private static bool giong_trang_thai(ban_ghi_trang_thai a, ban_ghi_trang_thai b)
        {
            if (a == null || b == null) return false;
            return a.da_ket_noi == b.da_ket_noi
                && a.ma_tien_trinh == b.ma_tien_trinh
                && a.da_moc == b.da_moc
                && a.o_man_hinh_chinh == b.o_man_hinh_chinh
                && a.trong_man_choi == b.trong_man_choi
                && a.khong_pha_huy == b.khong_pha_huy
                && a.khong_hu_ton == b.khong_hu_ton
                && a.khong_tinh_tien == b.khong_tinh_tien
                && a.mien_cong_nap == b.mien_cong_nap
                && a.co_kiem_de == b.co_kiem_de
                && a.cap_kiem_de == b.cap_kiem_de
                && a.vang == b.vang
                && a.luot_credits == b.luot_credits;
        }

        // Sao chép trạng thái dùng giữa 2 luồng
        private static ban_ghi_trang_thai sao_chep_trang_thai(ban_ghi_trang_thai nguon)
        {
            var kq = new ban_ghi_trang_thai();
            kq.da_ket_noi = nguon.da_ket_noi;
            kq.ma_tien_trinh = nguon.ma_tien_trinh;
            kq.da_moc = nguon.da_moc;
            kq.o_man_hinh_chinh = nguon.o_man_hinh_chinh;
            kq.trong_man_choi = nguon.trong_man_choi;
            kq.khong_pha_huy = nguon.khong_pha_huy;
            kq.khong_hu_ton = nguon.khong_hu_ton;
            kq.khong_tinh_tien = nguon.khong_tinh_tien;
            kq.mien_cong_nap = nguon.mien_cong_nap;
            kq.co_kiem_de = nguon.co_kiem_de;
            kq.cap_kiem_de = nguon.cap_kiem_de;
            kq.vang = nguon.vang;
            kq.luot_credits = nguon.luot_credits;
            kq.thieu_quyen_admin = nguon.thieu_quyen_admin;
            return kq;
        }

        // Luồng nền tự gắn và đọc RAM
        private void vong_lap_nen()
        {
            ban_ghi_trang_thai da_gui_tt = null;
            string da_gui_loi = null;
            int so_lan_thu_loi = 0;

            while (dang_chay)
            {
                bool can_ket_noi = yeu_cau_gan_ngay;
                yeu_cau_gan_ngay = false;
                if (can_ket_noi)
                {
                    so_lan_thu_loi = 0;
                    ket_qua_thao_tac kq = dong_co.ket_noi_lai();
                    ma_loi_gan = kq.ma_thong_bao;
                    if (!kq.thanh_cong) so_lan_thu_loi++;
                }
                else if (!dong_co.dang_da_moc)
                {
                    ket_qua_thao_tac kq = dong_co.dam_bao_da_moc();
                    ma_loi_gan = kq.ma_thong_bao;
                    if (kq.thanh_cong) so_lan_thu_loi = 0;
                    else so_lan_thu_loi = Math.Min(so_lan_thu_loi + 1, 5);
                }

                long phien_truoc = Interlocked.Read(ref phien_cong_tac);
                ban_ghi_trang_thai tt = dong_co.doc_trang_thai();
                if (tt.da_moc) so_lan_thu_loi = 0;

                if (dang_chay && IsHandleCreated && !IsDisposed)
                {
                    if (!giong_trang_thai(tt, da_gui_tt) || ma_loi_gan != da_gui_loi)
                    {
                        da_gui_tt = tt;
                        da_gui_loi = ma_loi_gan;
                        ban_ghi_trang_thai ban_ui = sao_chep_trang_thai(tt);
                        try
                        {
                            BeginInvoke((MethodInvoker)delegate
                            {
                                if (Interlocked.Read(ref phien_cong_tac) != phien_truoc)
                                {
                                    ban_ui.khong_pha_huy = trang_thai_cuoi.khong_pha_huy;
                                    ban_ui.khong_hu_ton = trang_thai_cuoi.khong_hu_ton;
                                    ban_ui.khong_tinh_tien = trang_thai_cuoi.khong_tinh_tien;
                                    ban_ui.mien_cong_nap = trang_thai_cuoi.mien_cong_nap;
                                }
                                trang_thai_cuoi = ban_ui;
                                cap_nhat_hien_thi_trang_thai();
                            });
                        }
                        catch
                        {
                        }
                    }
                }

                int cho_ms = tt.da_moc ? 500 : ((so_lan_thu_loi <= 1) ? 900 : (so_lan_thu_loi == 2 ? 2000 : 3500));
                su_kien_danh_thuc.WaitOne(cho_ms);
            }
        }

        // Cập nhật nhãn nếu nội dung thay đổi
        private static void dat_nhan_neu_doi(Label lbl, string chu_moi, Color mau_moi)
        {
            if (lbl.Text != chu_moi) lbl.Text = chu_moi;
            if (lbl.ForeColor != mau_moi) lbl.ForeColor = mau_moi;
        }

        // Đồng bộ tiêu đề và công tắc từ RAM
        private void cap_nhat_hien_thi_trang_thai()
        {
            if (!trang_thai_cuoi.da_ket_noi || !trang_thai_cuoi.da_moc)
            {
                string khoa_pid = (ma_loi_gan == "thieu_admin") ? "thieu_admin" : "chua_ket_noi";
                dat_nhan_neu_doi(nhan_pid, ngon_ngu.lay_chu(ngon_ngu_hien_tai, khoa_pid), bang_mau.vang_chinh);

                string khoa_man = (ma_loi_gan == "dang_tai") ? "dang_tai" : "mo_tro_choi";
                dat_nhan_neu_doi(nhan_man_choi, ngon_ngu.lay_chu(ngon_ngu_hien_tai, khoa_man), bang_mau.chu_phu);

                for (int i = 0; i < 4; i++) cac_cong_tac[i].da_bat = false;
                return;
            }

            string chu_pid = string.Format(ngon_ngu.lay_chu(ngon_ngu_hien_tai, "da_ket_noi"), trang_thai_cuoi.ma_tien_trinh);
            dat_nhan_neu_doi(nhan_pid, chu_pid, bang_mau.xanh_ket_noi);

            cac_cong_tac[0].da_bat = trang_thai_cuoi.khong_pha_huy;
            cac_cong_tac[1].da_bat = trang_thai_cuoi.khong_hu_ton;
            cac_cong_tac[2].da_bat = trang_thai_cuoi.khong_tinh_tien;
            cac_cong_tac[3].da_bat = trang_thai_cuoi.mien_cong_nap;

            if (trang_thai_cuoi.trong_man_choi)
            {
                string cap_str = trang_thai_cuoi.co_kiem_de ? ("+" + trang_thai_cuoi.cap_kiem_de) : "--";
                string vang_str = ngon_ngu.dinh_dang_so(trang_thai_cuoi.vang);
                string cred_str = ngon_ngu.dinh_dang_so(trang_thai_cuoi.luot_credits);
                string chu_ts = string.Format(ngon_ngu.lay_chu(ngon_ngu_hien_tai, "thong_so"), cap_str, vang_str, cred_str);
                dat_nhan_neu_doi(nhan_man_choi, chu_ts, bang_mau.chu_chinh);
            }
            else if (trang_thai_cuoi.o_man_hinh_chinh)
            {
                dat_nhan_neu_doi(nhan_man_choi, ngon_ngu.lay_chu(ngon_ngu_hien_tai, "vao_man_choi"), bang_mau.vang_chinh);
            }
            else
            {
                dat_nhan_neu_doi(nhan_man_choi, ngon_ngu.lay_chu(ngon_ngu_hien_tai, "dang_tai"), bang_mau.chu_phu);
            }
        }

        // Bật tắt công tắc tức thì và ghi 4 byte
        private void bam_cong_tac(string khoa)
        {
            if (!trang_thai_cuoi.da_moc && !dong_co.dang_da_moc)
            {
                yeu_cau_gan_ngay = true;
                su_kien_danh_thuc.Set();
                return;
            }
            Interlocked.Increment(ref phien_cong_tac);
            int chi_so = (khoa == "f1") ? 0 : (khoa == "f2") ? 1 : (khoa == "f3") ? 2 : 3;
            bool trang_thai_moi = !cac_cong_tac[chi_so].da_bat;
            cac_cong_tac[chi_so].da_bat = trang_thai_moi;
            if (chi_so == 0) trang_thai_cuoi.khong_pha_huy = trang_thai_moi;
            else if (chi_so == 1) trang_thai_cuoi.khong_hu_ton = trang_thai_moi;
            else if (chi_so == 2) trang_thai_cuoi.khong_tinh_tien = trang_thai_moi;
            else trang_thai_cuoi.mien_cong_nap = trang_thai_moi;

            if (!dong_co.dat_cong_tac_nhanh(khoa, trang_thai_moi))
            {
                yeu_cau_gan_ngay = true;
                su_kien_danh_thuc.Set();
            }
        }

        // Kích hoạt F5 triệu hồi kiếm +0
        private void dung_trieu_hoi()
        {
            dong_co.kich_hoat_f5_trieu_hoi();
        }

        // Kích hoạt F6 hồi tố thời gian
        private void dung_hoi_to()
        {
            dong_co.kich_hoat_f6_hoi_to();
        }

        // Bắt đầu giữ nút F7/F8
        private void bat_dau_giu_nut(int ket_qua)
        {
            dong_co.kich_hoat_nang_cap(ket_qua);
            ket_qua_nut_dang_giu = ket_qua;
            moc_bat_dau_giu_nut = Environment.TickCount;
            bo_dem_giu_nut.Start();
        }

        // Dừng giữ nút F7/F8
        private void dung_giu_nut()
        {
            ket_qua_nut_dang_giu = 0;
            bo_dem_giu_nut.Stop();
        }

        // Lặp nâng cấp khi giữ chuột F7/F8
        private void xu_ly_giu_nut(object sender, EventArgs e)
        {
            if (ket_qua_nut_dang_giu == 0)
            {
                bo_dem_giu_nut.Stop();
                return;
            }
            if ((Environment.TickCount - moc_bat_dau_giu_nut) >= 240)
            {
                dong_co.kich_hoat_nang_cap(ket_qua_nut_dang_giu);
            }
        }

        // Lặp nâng cấp khi giữ phím F7/F8
        private void xu_ly_giu_phim(object sender, EventArgs e)
        {
            if (phim_dang_giu == 0)
            {
                bo_dem_giu_phim.Stop();
                return;
            }
            bool van_giu = (GetAsyncKeyState(phim_dang_giu) & 0x8000) != 0;
            if (!van_giu)
            {
                phim_dang_giu = 0;
                bo_dem_giu_phim.Stop();
                return;
            }
            long bay_gio = Environment.TickCount;
            if ((bay_gio - moc_bat_dau_giu_phim) >= 240 && (bay_gio - lan_lap_phim_cuoi) >= 85)
            {
                lan_lap_phim_cuoi = bay_gio;
                dong_co.kich_hoat_nang_cap(phim_dang_giu == vk_f8 ? 4 : 3);
            }
        }

        // Xử lý tặng Vàng & Credits
        private void bam_nhan_tien()
        {
            double vang;
            double credits;
            if (!ngon_ngu.phan_tich_so(o_nhap_vang.lay_gia_tri(), out vang)) return;
            if (!ngon_ngu.phan_tich_so(o_nhap_credits.lay_gia_tri(), out credits)) return;
            if (vang <= 0 && credits <= 0) return;
            dong_co.kich_hoat_tang_tien(vang, credits);
        }

        // Nhận phím tắt toàn cục F1..F8
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == wm_hotkey)
            {
                int id = m.WParam.ToInt32();
                if (id == 100) bam_cong_tac("f1");
                else if (id == 101) bam_cong_tac("f2");
                else if (id == 102) bam_cong_tac("f3");
                else if (id == 103) bam_cong_tac("f4");
                else if (id == 104) dung_trieu_hoi();
                else if (id == 105) dung_hoi_to();
                else if (id == 106)
                {
                    dong_co.kich_hoat_nang_cap(3);
                    phim_dang_giu = vk_f7;
                    moc_bat_dau_giu_phim = Environment.TickCount;
                    lan_lap_phim_cuoi = moc_bat_dau_giu_phim;
                    bo_dem_giu_phim.Start();
                }
                else if (id == 107)
                {
                    dong_co.kich_hoat_nang_cap(4);
                    phim_dang_giu = vk_f8;
                    moc_bat_dau_giu_phim = Environment.TickCount;
                    lan_lap_phim_cuoi = moc_bat_dau_giu_phim;
                    bo_dem_giu_phim.Start();
                }
            }
            base.WndProc(ref m);
        }

        // Thoát êm: gỡ hook và hoàn nguyên game
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            dang_chay = false;
            su_kien_danh_thuc.Set();
            bo_dem_giu_nut.Stop();
            bo_dem_giu_phim.Stop();
            for (int i = 0; i < 8; i++)
            {
                UnregisterHotKey(Handle, 100 + i);
            }
            dong_co.go_bo_moc_em_dep();
            base.OnFormClosing(e);
        }
    }
}
