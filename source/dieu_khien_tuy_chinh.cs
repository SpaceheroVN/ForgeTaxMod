// Điều khiển giao diện tối vẽ tĩnh siêu nhẹ
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace khong_gian_mod
{
    // === Bảng màu Obsidian & Molten Forge ===
    public static class bang_mau
    {
        public static readonly Color nen_chinh = Color.FromArgb(13, 15, 20);
        public static readonly Color nen_the = Color.FromArgb(22, 25, 34);
        public static readonly Color vien_the = Color.FromArgb(38, 43, 58);

        public static readonly Color nen_nut = Color.FromArgb(27, 31, 43);
        public static readonly Color nen_nhap = Color.FromArgb(13, 15, 20);
        public static readonly Color vien_nhap = Color.FromArgb(46, 52, 70);
        public static readonly Color vien_nhap_focus = Color.FromArgb(217, 119, 6);

        public static readonly Color nen_huy_hieu = Color.FromArgb(33, 37, 52);
        public static readonly Color vien_huy_hieu = Color.FromArgb(50, 56, 77);
        public static readonly Color nen_huy_hieu_bat = Color.FromArgb(51, 37, 18);
        public static readonly Color vien_huy_hieu_bat = Color.FromArgb(180, 83, 9);
        public static readonly Color chu_huy_hieu_bat = Color.FromArgb(252, 211, 77);

        public static readonly Color gat_tat = Color.FromArgb(40, 44, 58);
        public static readonly Color vien_gat_tat = Color.FromArgb(60, 66, 87);
        public static readonly Color num_gat_tat = Color.FromArgb(220, 225, 236);
        public static readonly Color gat_bat_dau = Color.FromArgb(245, 158, 11);
        public static readonly Color num_gat_bat = Color.FromArgb(255, 255, 255);

        public static readonly Color vang_chinh = Color.FromArgb(251, 191, 36);
        public static readonly Color vang_cam = Color.FromArgb(245, 158, 11);
        public static readonly Color vang_hover = Color.FromArgb(252, 211, 77);
        public static readonly Color xanh_ket_noi = Color.FromArgb(52, 211, 153);

        public static readonly Color chu_chinh = Color.FromArgb(243, 244, 246);
        public static readonly Color chu_phu = Color.FromArgb(148, 163, 184);
        public static readonly Color chu_toi_cta = Color.FromArgb(17, 19, 24);

        public static readonly Color nen_popup = Color.FromArgb(20, 23, 32);
        public static readonly Color vien_popup = Color.FromArgb(74, 58, 34);
        public static readonly Color hover_popup = Color.FromArgb(37, 42, 56);
        public static readonly Color chon_popup = Color.FromArgb(36, 30, 20);
        public static readonly Color vien_nut_nn = Color.FromArgb(52, 57, 74);

        // Tạo đường bo góc hình chữ nhật
        public static GraphicsPath tao_duong_bo_goc(RectangleF vung, float ban_kinh)
        {
            var dg = new GraphicsPath();
            float r = Math.Min(ban_kinh, Math.Min(vung.Width, vung.Height) / 2f);
            float d = r * 2f;
            if (d <= 0.5f)
            {
                dg.AddRectangle(vung);
                return dg;
            }
            dg.AddArc(vung.X, vung.Y, d, d, 180, 90);
            dg.AddArc(vung.Right - d, vung.Y, d, d, 270, 90);
            dg.AddArc(vung.Right - d, vung.Bottom - d, d, d, 0, 90);
            dg.AddArc(vung.X, vung.Bottom - d, d, d, 90, 90);
            dg.CloseFigure();
            return dg;
        }
    }

    // === Khung bo góc vẽ tĩnh ===
    public class khung_bo_goc : Panel
    {
        private Color mau_nen_ngoai_val = bang_mau.nen_chinh;
        private Color mau_nen_hien_tai = bang_mau.nen_the;
        private Color mau_vien_hien_tai = bang_mau.vien_the;
        private int ban_kinh_goc = 10;

        public khung_bo_goc()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            BackColor = bang_mau.nen_the;
        }

        public Color mau_nen_ngoai
        {
            get { return mau_nen_ngoai_val; }
            set { mau_nen_ngoai_val = value; Invalidate(); }
        }

        public Color mau_nen_khung
        {
            get { return mau_nen_hien_tai; }
            set
            {
                if (mau_nen_hien_tai != value)
                {
                    mau_nen_hien_tai = value;
                    BackColor = value;
                    Invalidate();
                }
            }
        }

        public Color mau_vien
        {
            get { return mau_vien_hien_tai; }
            set
            {
                if (mau_vien_hien_tai != value)
                {
                    mau_vien_hien_tai = value;
                    Invalidate();
                }
            }
        }

        public int ban_kinh
        {
            get { return ban_kinh_goc; }
            set { ban_kinh_goc = value; Invalidate(); }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        // Vẽ khung bo góc bằng cọ đặc
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(mau_nen_ngoai_val);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var vung = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath dg = bang_mau.tao_duong_bo_goc(vung, ban_kinh_goc))
            using (var br = new SolidBrush(mau_nen_hien_tai))
            using (var pen = new Pen(mau_vien_hien_tai, 1f))
            {
                g.FillPath(br, dg);
                g.DrawPath(pen, dg);
            }
        }
    }

    // === Công tắc gạt bật/tắt tức thì ===
    public sealed class nut_gat : Control
    {
        private bool trang_thai_bat;
        private Color mau_nen_ngoai_val = bang_mau.nen_the;

        public nut_gat()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            BackColor = bang_mau.nen_the;
            Size = new Size(52, 26);
            Cursor = Cursors.Hand;
        }

        public Color mau_nen_ngoai
        {
            get { return mau_nen_ngoai_val; }
            set
            {
                if (mau_nen_ngoai_val != value)
                {
                    mau_nen_ngoai_val = value;
                    BackColor = value;
                    Invalidate();
                }
            }
        }

        public bool da_bat
        {
            get { return trang_thai_bat; }
            set
            {
                if (trang_thai_bat != value)
                {
                    trang_thai_bat = value;
                    Invalidate();
                }
            }
        }

        // Đặt trạng thái tức thì
        public void dat_trang_thai_ngay(bool bat)
        {
            trang_thai_bat = bat;
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        // Vẽ rãnh bo tròn và núm gạt
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(mau_nen_ngoai_val);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var vung_ranh = new RectangleF(1f, 1f, Width - 2.5f, Height - 2.5f);
            float ban_kinh_ranh = vung_ranh.Height / 2f;
            Color mau_ranh = trang_thai_bat ? bang_mau.gat_bat_dau : bang_mau.gat_tat;
            Color mau_vien = trang_thai_bat ? bang_mau.vang_chinh : bang_mau.vien_gat_tat;

            using (GraphicsPath dg = bang_mau.tao_duong_bo_goc(vung_ranh, ban_kinh_ranh))
            using (var br_ranh = new SolidBrush(mau_ranh))
            using (var pen_vien = new Pen(mau_vien, 1f))
            {
                g.FillPath(br_ranh, dg);
                g.DrawPath(pen_vien, dg);
            }

            float duong_kinh = 18f;
            float x_num = trang_thai_bat ? (Width - duong_kinh - 4f) : 4f;
            float y_num = (Height - duong_kinh) / 2f;

            using (var br_bong = new SolidBrush(Color.FromArgb(75, 0, 0, 0)))
            {
                g.FillEllipse(br_bong, x_num - 0.5f, y_num + 1.2f, duong_kinh + 1f, duong_kinh + 1f);
            }
            using (var br_num = new SolidBrush(trang_thai_bat ? bang_mau.num_gat_bat : bang_mau.num_gat_tat))
            {
                g.FillEllipse(br_num, x_num, y_num, duong_kinh, duong_kinh);
            }
        }
    }

    // === Hàng công tắc F1..F4 ===
    public sealed class hang_cong_tac : khung_bo_goc
    {
        private static readonly Font font_phim = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly Font font_chu = new Font("Segoe UI", 10f, FontStyle.Bold);
        private readonly string phim_tat;
        private readonly nut_gat cong_tac;
        private string tieu_de_chu = "";
        public event EventHandler su_kien_bam;

        public hang_cong_tac(string phim)
        {
            phim_tat = phim;
            ban_kinh = 10;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.StandardDoubleClick, false);

            cong_tac = new nut_gat();
            cong_tac.mau_nen_ngoai = bang_mau.nen_the;
            Controls.Add(cong_tac);
            cong_tac.MouseDown += xu_ly_nhan_chuot;
            MouseDown += xu_ly_nhan_chuot;
        }

        public string noi_dung
        {
            get { return tieu_de_chu; }
            set
            {
                string moi = value ?? "";
                if (tieu_de_chu != moi)
                {
                    tieu_de_chu = moi;
                    Invalidate();
                }
            }
        }

        public bool da_bat
        {
            get { return cong_tac.da_bat; }
            set
            {
                bool doi = (cong_tac.da_bat != value);
                cong_tac.da_bat = value;
                if (doi) Invalidate();
            }
        }

        // Đặt trạng thái cho kiểm thử
        public void dat_trang_thai_ngay(bool bat)
        {
            cong_tac.dat_trang_thai_ngay(bat);
            Invalidate();
        }

        // Bắt chuột trái ngay để nhận click nhanh
        public void kich_hoat_nhan_chuot_trai()
        {
            if (su_kien_bam != null) su_kien_bam(this, EventArgs.Empty);
        }

        private void xu_ly_nhan_chuot(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && su_kien_bam != null)
            {
                su_kien_bam(this, EventArgs.Empty);
            }
        }

        // Căn nút gạt bên phải khi đổi cỡ
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            cong_tac.Location = new Point(Width - cong_tac.Width - 14, (Height - cong_tac.Height) / 2);
        }

        // Vẽ phím tắt và tên chức năng
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            bool dang_bat = cong_tac.da_bat;
            var vung_huy_hieu = new RectangleF(12f, (Height - 24f) / 2f, 36f, 24f);
            Color nen_hh = dang_bat ? bang_mau.nen_huy_hieu_bat : bang_mau.nen_huy_hieu;
            Color vien_hh = dang_bat ? bang_mau.vien_huy_hieu_bat : bang_mau.vien_huy_hieu;
            Color chu_hh = dang_bat ? bang_mau.chu_huy_hieu_bat : bang_mau.vang_cam;

            using (GraphicsPath dg = bang_mau.tao_duong_bo_goc(vung_huy_hieu, 6f))
            using (var br = new SolidBrush(nen_hh))
            using (var pen = new Pen(vien_hh, 1f))
            {
                g.FillPath(br, dg);
                g.DrawPath(pen, dg);
            }
            TextRenderer.DrawText(
                g,
                phim_tat,
                font_phim,
                Rectangle.Round(vung_huy_hieu),
                chu_hh,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
            );
            var vung_chu = new Rectangle(60, 0, Width - 140, Height);
            TextRenderer.DrawText(
                g,
                tieu_de_chu,
                font_chu,
                vung_chu,
                bang_mau.chu_chinh,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
            );
        }
    }

    // === Nút bấm phẳng ===
    public sealed class nut_hien_dai : Control
    {
        private static readonly Font font_nut_chinh = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        private static readonly Font font_nut_nho = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly Font font_huy_hieu = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        private Color mau_nen_ngoai_val = bang_mau.nen_chinh;
        private Color mau_nen_thuong = bang_mau.nen_nut;
        private Color mau_chu_val = bang_mau.chu_chinh;
        private float co_chu_val = 9.5f;
        private int goc_bo_val = 9;
        private string huy_hieu_phim_val = "";
        private bool la_nut_chinh_val;

        public nut_hien_dai()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            BackColor = bang_mau.nen_chinh;
            Cursor = Cursors.Hand;
        }

        public Color mau_nen_ngoai
        {
            get { return mau_nen_ngoai_val; }
            set { mau_nen_ngoai_val = value; BackColor = value; Invalidate(); }
        }

        public Color mau_nen_nut
        {
            get { return mau_nen_thuong; }
            set { mau_nen_thuong = value; Invalidate(); }
        }

        public Color mau_chu
        {
            get { return mau_chu_val; }
            set { mau_chu_val = value; Invalidate(); }
        }

        public float co_chu
        {
            get { return co_chu_val; }
            set { co_chu_val = value; Invalidate(); }
        }

        public int goc_bo
        {
            get { return goc_bo_val; }
            set { goc_bo_val = value; Invalidate(); }
        }

        public string huy_hieu_phim
        {
            get { return huy_hieu_phim_val; }
            set { huy_hieu_phim_val = value ?? ""; Invalidate(); }
        }

        public bool la_nut_chinh
        {
            get { return la_nut_chinh_val; }
            set { la_nut_chinh_val = value; Invalidate(); }
        }

        // Vẽ lại ngay khi đổi chữ theo ngôn ngữ
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        // Vẽ nền, viền và nhãn nút bấm
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(mau_nen_ngoai_val);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var vung = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color nen = la_nut_chinh_val ? bang_mau.gat_bat_dau : mau_nen_thuong;
            Color vien = la_nut_chinh_val ? bang_mau.vang_chinh : bang_mau.vien_the;
            Color mau_chu_hien_tai = la_nut_chinh_val ? bang_mau.chu_toi_cta : mau_chu_val;

            using (GraphicsPath dg = bang_mau.tao_duong_bo_goc(vung, goc_bo_val))
            using (var br = new SolidBrush(nen))
            using (var pen = new Pen(vien, 1f))
            {
                g.FillPath(br, dg);
                g.DrawPath(pen, dg);
            }

            Font font_ve = (co_chu_val < 9f) ? font_nut_nho : font_nut_chinh;
            if (!string.IsNullOrEmpty(huy_hieu_phim_val))
            {
                var vung_hh = new RectangleF(10f, (Height - 22f) / 2f, 34f, 22f);
                using (GraphicsPath dg_hh = bang_mau.tao_duong_bo_goc(vung_hh, 6f))
                using (var br_hh = new SolidBrush(bang_mau.nen_huy_hieu))
                using (var pen_hh = new Pen(bang_mau.vien_huy_hieu, 1f))
                {
                    g.FillPath(br_hh, dg_hh);
                    g.DrawPath(pen_hh, dg_hh);
                }
                TextRenderer.DrawText(
                    g,
                    huy_hieu_phim_val,
                    font_huy_hieu,
                    Rectangle.Round(vung_hh),
                    bang_mau.vang_chinh,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
                );
                var vung_chu = new Rectangle(52, 0, Width - 60, Height);
                TextRenderer.DrawText(
                    g,
                    Text ?? "",
                    font_ve,
                    vung_chu,
                    mau_chu_hien_tai,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
                );
                return;
            }

            TextRenderer.DrawText(
                g,
                Text ?? "",
                font_ve,
                ClientRectangle,
                mau_chu_hien_tai,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
            );
        }
    }

    // === Hộp chọn ngôn ngữ ===
    public sealed class hop_chon_ngon_ngu : Control
    {
        private static readonly Font font_nhan = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly string[] danh_sach_muc = new string[] { "Tiếng Việt", "English" };

        private Color mau_nen_ngoai_val = bang_mau.nen_the;
        private int chi_so_val;
        private bang_chon_ngon_ngu bang_xo;
        public event EventHandler su_kien_doi;

        public hop_chon_ngon_ngu()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            BackColor = bang_mau.nen_the;
            Cursor = Cursors.Hand;
        }

        public Color mau_nen_ngoai
        {
            get { return mau_nen_ngoai_val; }
            set { mau_nen_ngoai_val = value; BackColor = value; Invalidate(); }
        }

        public int chi_so_chon
        {
            get { return chi_so_val; }
            set
            {
                int moi = (value == 1) ? 1 : 0;
                if (chi_so_val != moi)
                {
                    chi_so_val = moi;
                    Invalidate();
                    if (bang_xo != null) bang_xo.Invalidate();
                }
            }
        }

        public bool dang_mo
        {
            get { return bang_xo != null && bang_xo.Visible; }
        }

        // Gắn bảng xổ lên cửa sổ chính
        public void gan_bang_xo(Form cha, Point vi_tri_tren_form)
        {
            if (bang_xo == null)
            {
                bang_xo = new bang_chon_ngon_ngu(this);
            }
            bang_xo.Location = vi_tri_tren_form;
            bang_xo.Size = new Size(Width, 68);
            bang_xo.Visible = false;
            if (!cha.Controls.Contains(bang_xo))
            {
                cha.Controls.Add(bang_xo);
            }
            bang_xo.BringToFront();
        }

        // Đóng bảng xổ ngôn ngữ
        public void dong_bang_xo()
        {
            if (bang_xo != null && bang_xo.Visible)
            {
                bang_xo.Visible = false;
                Invalidate();
            }
        }

        // Mở bảng xổ phục vụ kiểm thử
        public void dat_mo_popup_kiem_thu(bool mo, int muc_hover)
        {
            if (bang_xo == null) return;
            bang_xo.dat_hover_kiem_thu(muc_hover);
            bang_xo.Visible = mo;
            if (mo) bang_xo.BringToFront();
            Invalidate();
        }

        internal void chon_tu_bang(int chi_so)
        {
            bool thay_doi = (chi_so_val != chi_so);
            chi_so_val = (chi_so == 1) ? 1 : 0;
            dong_bang_xo();
            Invalidate();
            if (thay_doi && su_kien_doi != null)
            {
                su_kien_doi(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || bang_xo == null) return;
            bang_xo.Visible = !bang_xo.Visible;
            if (bang_xo.Visible)
            {
                bang_xo.dat_hover_kiem_thu(-1);
                bang_xo.BringToFront();
            }
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        // Vẽ nút ngôn ngữ và mũi tên vector
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(mau_nen_ngoai_val);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            bool mo = dang_mo;
            var vung = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Color vien = mo ? bang_mau.vang_cam : bang_mau.vien_nut_nn;

            using (GraphicsPath dg = bang_mau.tao_duong_bo_goc(vung, 7f))
            using (var br = new SolidBrush(bang_mau.nen_nhap))
            using (var pen = new Pen(vien, 1f))
            {
                g.FillPath(br, dg);
                g.DrawPath(pen, dg);
            }

            var vung_chu = new Rectangle(10, 0, Width - 28, Height);
            TextRenderer.DrawText(
                g,
                danh_sach_muc[chi_so_val],
                font_nhan,
                vung_chu,
                bang_mau.chu_chinh,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
            );

            float cx = Width - 15f;
            float cy = Height / 2f;
            Color mau_mui_ten = mo ? bang_mau.vang_chinh : bang_mau.chu_phu;
            using (var pen_mt = new Pen(mau_mui_ten, 1.8f))
            {
                pen_mt.StartCap = LineCap.Round;
                pen_mt.EndCap = LineCap.Round;
                pen_mt.LineJoin = LineJoin.Round;
                if (mo)
                {
                    g.DrawLines(pen_mt, new PointF[]
                    {
                        new PointF(cx - 4f, cy + 2f),
                        new PointF(cx, cy - 2.2f),
                        new PointF(cx + 4f, cy + 2f)
                    });
                }
                else
                {
                    g.DrawLines(pen_mt, new PointF[]
                    {
                        new PointF(cx - 4f, cy - 2f),
                        new PointF(cx, cy + 2.2f),
                        new PointF(cx + 4f, cy - 2f)
                    });
                }
            }
        }
    }

    // === Bảng xổ chọn ngôn ngữ ===
    public sealed class bang_chon_ngon_ngu : Control
    {
        private static readonly Font font_muc = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        private static readonly Font font_muc_chon = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        private static readonly string[] danh_sach_muc = new string[] { "Tiếng Việt", "English" };

        private readonly hop_chon_ngon_ngu hop_cha;
        private int muc_hover = -1;

        public bang_chon_ngon_ngu(hop_chon_ngon_ngu cha)
        {
            hop_cha = cha;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            BackColor = bang_mau.nen_popup;
            Cursor = Cursors.Hand;
        }

        // Đặt mục hover phục vụ kiểm thử
        internal void dat_hover_kiem_thu(int muc)
        {
            muc_hover = muc;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Width > 2 && Height > 2)
            {
                using (GraphicsPath gp = bang_mau.tao_duong_bo_goc(new RectangleF(0f, 0f, Width, Height), 8f))
                {
                    Region cu = Region;
                    Region = new Region(gp);
                    if (cu != null) cu.Dispose();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Region != null)
            {
                Region cu = Region;
                Region = null;
                cu.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int moi = -1;
            if (e.Y >= 4 && e.Y < 34) moi = 0;
            else if (e.Y >= 34 && e.Y < 64) moi = 1;
            if (moi != muc_hover)
            {
                muc_hover = moi;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (muc_hover != -1)
            {
                muc_hover = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int idx = (e.Y < 34) ? 0 : 1;
            hop_cha.chon_tu_bang(idx);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        // Vẽ danh sách chọn ngôn ngữ
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(bang_mau.nen_popup);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var vung_khung = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath dg = bang_mau.tao_duong_bo_goc(vung_khung, 8f))
            using (var pen = new Pen(bang_mau.vien_popup, 1f))
            {
                g.DrawPath(pen, dg);
            }

            for (int i = 0; i < 2; i++)
            {
                var vung_muc = new RectangleF(4f, 4f + i * 30f, Width - 8f, 30f);
                bool dang_chon = (hop_cha.chi_so_chon == i);
                bool dang_re = (muc_hover == i);

                if (dang_chon || dang_re)
                {
                    Color nen_muc = dang_re ? bang_mau.hover_popup : bang_mau.chon_popup;
                    using (GraphicsPath dg_muc = bang_mau.tao_duong_bo_goc(vung_muc, 5f))
                    using (var br_muc = new SolidBrush(nen_muc))
                    {
                        g.FillPath(br_muc, dg_muc);
                    }
                }

                if (dang_chon)
                {
                    using (var br_cham = new SolidBrush(bang_mau.vang_cam))
                    {
                        g.FillEllipse(br_cham, 11f, vung_muc.Y + 12f, 6f, 6f);
                    }
                }

                var vung_chu = new Rectangle(22, (int)vung_muc.Y, Width - 26, 30);
                Color mau_chu_muc = dang_chon ? bang_mau.vang_hover : (dang_re ? Color.White : bang_mau.chu_chinh);
                TextRenderer.DrawText(
                    g,
                    danh_sach_muc[i],
                    dang_chon ? font_muc_chon : font_muc,
                    vung_chu,
                    mau_chu_muc,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix
                );
            }
        }
    }

    // === Ô nhập tối bo tròn ===
    public sealed class o_nhap_toi : khung_bo_goc
    {
        private static readonly Font font_nhap = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        private readonly TextBox hop_nhap;
        private string goi_y = "";
        private bool dang_hien_goi_y;
        public event EventHandler su_kien_enter;
        public event EventHandler su_kien_focus;

        public o_nhap_toi()
        {
            mau_nen_ngoai = bang_mau.nen_the;
            mau_nen_khung = bang_mau.nen_nhap;
            mau_vien = bang_mau.vien_nhap;
            ban_kinh = 7;

            hop_nhap = new TextBox();
            hop_nhap.BorderStyle = BorderStyle.None;
            hop_nhap.BackColor = bang_mau.nen_nhap;
            hop_nhap.ForeColor = bang_mau.chu_phu;
            hop_nhap.Font = font_nhap;
            Controls.Add(hop_nhap);

            hop_nhap.GotFocus += xu_ly_focus_vao;
            hop_nhap.LostFocus += xu_ly_focus_ra;
            hop_nhap.KeyDown += xu_ly_phim;
            Click += delegate { hop_nhap.Focus(); };
        }

        // Cập nhật chữ gợi ý
        public void dat_goi_y(string chuoi)
        {
            goi_y = chuoi ?? "";
            if (dang_hien_goi_y || string.IsNullOrEmpty(hop_nhap.Text))
            {
                dang_hien_goi_y = true;
                hop_nhap.Text = goi_y;
                hop_nhap.ForeColor = bang_mau.chu_phu;
            }
        }

        // Lấy nội dung người dùng nhập
        public string lay_gia_tri()
        {
            if (dang_hien_goi_y) return "";
            return hop_nhap.Text;
        }

        private void xu_ly_focus_vao(object sender, EventArgs e)
        {
            mau_vien = bang_mau.vien_nhap_focus;
            if (su_kien_focus != null) su_kien_focus(this, EventArgs.Empty);
            if (dang_hien_goi_y)
            {
                dang_hien_goi_y = false;
                hop_nhap.Text = "";
                hop_nhap.ForeColor = bang_mau.chu_chinh;
            }
        }

        private void xu_ly_focus_ra(object sender, EventArgs e)
        {
            mau_vien = bang_mau.vien_nhap;
            if (string.IsNullOrWhiteSpace(hop_nhap.Text))
            {
                dang_hien_goi_y = true;
                hop_nhap.Text = goi_y;
                hop_nhap.ForeColor = bang_mau.chu_phu;
            }
        }

        private void xu_ly_phim(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && su_kien_enter != null)
            {
                e.SuppressKeyPress = true;
                su_kien_enter(this, EventArgs.Empty);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            hop_nhap.Location = new Point(10, (Height - hop_nhap.Height) / 2);
            hop_nhap.Width = Math.Max(20, Width - 20);
        }
    }
}
