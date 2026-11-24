// Từ điển song ngữ và định dạng số
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace khong_gian_mod
{
    public enum ma_ngon_ngu
    {
        tieng_viet = 0,
        tieng_anh = 1
    }

    // === Chuỗi giao diện và định dạng số ===
    public static class ngon_ngu
    {
        private static readonly Dictionary<string, string> bang_vi = tao_bang_vi();
        private static readonly Dictionary<string, string> bang_en = tao_bang_en();
        private static readonly Regex mau_cham = new Regex(@"^\d{1,3}(\.\d{3})+$", RegexOptions.Compiled);
        private static readonly Regex mau_phay = new Regex(@"^\d{1,3}(,\d{3})+$", RegexOptions.Compiled);

        // Từ điển tiếng Việt
        private static Dictionary<string, string> tao_bang_vi()
        {
            var d = new Dictionary<string, string>();
            d["tieu_de"] = "ForgeTax Mod";
            d["chua_ket_noi"] = "Chưa kết nối";
            d["da_ket_noi"] = "PID {0} • Đã kết nối";
            d["thieu_admin"] = "Cần chạy quyền Admin";
            d["mo_tro_choi"] = "Mở game để kích hoạt.";
            d["vao_man_choi"] = "Hãy vào màn chơi.";
            d["dang_tai"] = "Đang kết nối...";
            d["thong_so"] = "Màn chơi • Đe: {0} • Vàng: {1} • Credits: {2}";
            d["ket_noi_lai"] = "Kết nối lại";
            d["f1"] = "0% Phá Hủy";
            d["f2"] = "0% Hư Tổn";
            d["f3"] = "Không Tính Tiền";
            d["f4"] = "Miễn Cống Nạp";
            d["f5"] = "Triệu Hồi Kiếm";
            d["f6"] = "Hồi Tố Thời Gian";
            d["f7"] = "Nâng Cấp (+1)";
            d["f8"] = "Siêu Nâng Cấp (+2)";
            d["tang_tien"] = "Tặng Vàng & Credits";
            d["vang"] = "Vàng (vd: 1E20)";
            d["credits"] = "Credits (vd: 1E6)";
            d["nhan"] = "Nhận";
            return d;
        }

        // Từ điển tiếng Anh
        private static Dictionary<string, string> tao_bang_en()
        {
            var d = new Dictionary<string, string>();
            d["tieu_de"] = "ForgeTax Mod";
            d["chua_ket_noi"] = "Disconnected";
            d["da_ket_noi"] = "PID {0} • Connected";
            d["thieu_admin"] = "Run as Administrator";
            d["mo_tro_choi"] = "Launch game to activate.";
            d["vao_man_choi"] = "Please enter a run.";
            d["dang_tai"] = "Connecting...";
            d["thong_so"] = "In-game • Anvil: {0} • Gold: {1} • Credits: {2}";
            d["ket_noi_lai"] = "Reconnect";
            d["f1"] = "0% Break";
            d["f2"] = "0% Damage";
            d["f3"] = "Free Shop & Reroll";
            d["f4"] = "Free Tribute";
            d["f5"] = "Summon Sword";
            d["f6"] = "Time Rewind";
            d["f7"] = "Upgrade (+1)";
            d["f8"] = "Super Upgrade (+2)";
            d["tang_tien"] = "Grant Gold & Credits";
            d["vang"] = "Gold (e.g. 1E20)";
            d["credits"] = "Credits (e.g. 1E6)";
            d["nhan"] = "Grant";
            return d;
        }

        // Lấy chuỗi theo ngôn ngữ hiện tại
        public static string lay_chu(ma_ngon_ngu ma, string khoa)
        {
            Dictionary<string, string> nguon = (ma == ma_ngon_ngu.tieng_anh) ? bang_en : bang_vi;
            string gia_tri;
            if (nguon.TryGetValue(khoa, out gia_tri))
            {
                return gia_tri;
            }
            return khoa;
        }

        // Định dạng số (chấm dưới 1E6, E từ 1E6)
        public static string dinh_dang_so(double gia_tri)
        {
            if (double.IsNaN(gia_tri) || double.IsInfinity(gia_tri))
            {
                return "0";
            }
            bool am = gia_tri < 0;
            double tuyet_doi = Math.Abs(gia_tri);
            if (tuyet_doi < 0.5)
            {
                return "0";
            }
            if (tuyet_doi < 999999.5)
            {
                long nguyen = (long)Math.Round(tuyet_doi, MidpointRounding.AwayFromZero);
                string chu_so = nguyen.ToString(CultureInfo.InvariantCulture);
                var sb = new StringBuilder();
                int dem = 0;
                for (int i = chu_so.Length - 1; i >= 0; i--)
                {
                    if (dem > 0 && (dem % 3) == 0)
                    {
                        sb.Insert(0, '.');
                    }
                    sb.Insert(0, chu_so[i]);
                    dem++;
                }
                return am ? ("-" + sb.ToString()) : sb.ToString();
            }

            int mu = (int)Math.Floor(Math.Log10(tuyet_doi));
            double co_so = tuyet_doi / Math.Pow(10.0, mu);
            co_so = Math.Round(co_so, 2, MidpointRounding.AwayFromZero);
            if (co_so >= 10.0)
            {
                co_so = 1.0;
                mu += 1;
            }
            string chuoi_co_so = co_so.ToString("0.##", CultureInfo.InvariantCulture);
            return (am ? "-" : "") + chuoi_co_so + "E" + mu.ToString(CultureInfo.InvariantCulture);
        }

        // Phân tích số (1E20, 100.000, 100,000)
        public static bool phan_tich_so(string tho, out double ket_qua)
        {
            ket_qua = 0;
            if (tho == null)
            {
                return true;
            }
            string s = tho.Trim().Replace(" ", "").Replace("_", "");
            if (s.Length == 0)
            {
                ket_qua = 0;
                return true;
            }
            if (s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0)
            {
                string chuan = s.Replace(',', '.');
                double v;
                if (!double.TryParse(chuan, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                {
                    return false;
                }
                if (double.IsNaN(v) || double.IsInfinity(v) || v < 0 || v > 1e300)
                {
                    return false;
                }
                ket_qua = v;
                return true;
            }
            if (mau_cham.IsMatch(s) || mau_phay.IsMatch(s))
            {
                string bo_ngan = s.Replace(".", "").Replace(",", "");
                double v;
                if (!double.TryParse(bo_ngan, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                {
                    return false;
                }
                if (double.IsNaN(v) || double.IsInfinity(v) || v < 0 || v > 1e300)
                {
                    return false;
                }
                ket_qua = v;
                return true;
            }
            string chung = s.Replace(',', '.');
            double kq;
            if (!double.TryParse(chung, NumberStyles.Float, CultureInfo.InvariantCulture, out kq))
            {
                return false;
            }
            if (double.IsNaN(kq) || double.IsInfinity(kq) || kq < 0 || kq > 1e300)
            {
                return false;
            }
            ket_qua = kq;
            return true;
        }

        // Tự kiểm tra định dạng và phân tích số
        public static void tu_kiem_tra()
        {
            if (dinh_dang_so(0) != "0") throw new Exception("Loi 0");
            if (dinh_dang_so(50) != "50") throw new Exception("Loi 50");
            if (dinh_dang_so(1000) != "1.000") throw new Exception("Loi 1000");
            if (dinh_dang_so(100000) != "100.000") throw new Exception("Loi 100000");
            if (dinh_dang_so(999999) != "999.999") throw new Exception("Loi 999999");
            if (dinh_dang_so(1000000) != "1E6") throw new Exception("Loi 1E6");
            if (dinh_dang_so(1500000) != "1.5E6") throw new Exception("Loi 1.5E6");
            if (dinh_dang_so(1e20) != "1E20") throw new Exception("Loi 1E20");

            double v;
            if (!phan_tich_so("1E20", out v) || Math.Abs(v - 1e20) > 1e15) throw new Exception("Loi parse 1E20");
            if (!phan_tich_so("2.5e6", out v) || Math.Abs(v - 2.5e6) > 1) throw new Exception("Loi parse 2.5e6");
            if (!phan_tich_so("100.000", out v) || Math.Abs(v - 100000) > 0.1) throw new Exception("Loi parse 100.000");
            if (!phan_tich_so("1,000,000", out v) || Math.Abs(v - 1000000) > 0.1) throw new Exception("Loi parse 1,000,000");
            if (phan_tich_so("-5", out v)) throw new Exception("Loi reject am");
            if (phan_tich_so("abc", out v)) throw new Exception("Loi reject chu");
        }
    }
}
