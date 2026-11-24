// Động cơ RAM x64, quét AOB và dựng cave
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace khong_gian_mod
{
    // === Trạng thái đọc từ bộ nhớ game ===
    public sealed class ban_ghi_trang_thai
    {
        public bool da_ket_noi;
        public int ma_tien_trinh;
        public bool da_moc;
        public bool o_man_hinh_chinh;
        public bool trong_man_choi;
        public bool khong_pha_huy;
        public bool khong_hu_ton;
        public bool khong_tinh_tien;
        public bool mien_cong_nap;
        public bool co_kiem_de;
        public int cap_kiem_de;
        public double vang;
        public int luot_credits;
        public bool thieu_quyen_admin;
    }

    // === Thông tin trả về khi thao tác ===
    public sealed class ket_qua_thao_tac
    {
        public bool thanh_cong;
        public string ma_thong_bao;
        public bool thieu_admin;

        public ket_qua_thao_tac(bool tc, string ma, bool admin)
        {
            thanh_cong = tc;
            ma_thong_bao = ma;
            thieu_admin = admin;
        }
    }

    // === Mẫu AOB và độ lệch mục tiêu ===
    internal sealed class mau_aob
    {
        public byte[] gia_tri;
        public bool[] mat_na;
        public int do_lech;

        public mau_aob(string chuoi_hex, int lech)
        {
            do_lech = lech;
            string[] phan = chuoi_hex.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            gia_tri = new byte[phan.Length];
            mat_na = new bool[phan.Length];
            for (int i = 0; i < phan.Length; i++)
            {
                if (phan[i] == "?" || phan[i] == "??")
                {
                    mat_na[i] = false;
                }
                else
                {
                    mat_na[i] = true;
                    gia_tri[i] = Convert.ToByte(phan[i], 16);
                }
            }
            if (gia_tri.Length < 2 || !mat_na[0] || !mat_na[1])
            {
                throw new InvalidOperationException("Mau AOB can 2 byte dau co dinh");
            }
        }
    }

    // === Mục AOB cho bảng nhảy 2 byte đầu ===
    internal sealed class mau_aob_muc
    {
        public string ten;
        public mau_aob mau;

        public mau_aob_muc(string ten_muc, mau_aob mau_aob)
        {
            ten = ten_muc;
            mau = mau_aob;
        }
    }

    // === Bộ sinh mã x64 hỗ trợ nhãn và RIP ===
    public sealed class bo_phat_ma_x64
    {
        private readonly int lech_ma;
        private readonly List<byte> bo_dem = new List<byte>(2560);
        private readonly Dictionary<string, int> bang_nhan = new Dictionary<string, int>();
        private readonly List<KeyValuePair<int, string>> sua_nhay_32 = new List<KeyValuePair<int, string>>();
        private readonly List<int[]> sua_rip = new List<int[]>();

        public bo_phat_ma_x64(int vi_tri_ma)
        {
            lech_ma = vi_tri_ma;
        }

        // Đánh dấu nhãn tại vị trí hiện tại
        public void dat_nhan(string ten)
        {
            if (bang_nhan.ContainsKey(ten))
            {
                throw new InvalidOperationException("Trung nhan: " + ten);
            }
            bang_nhan[ten] = bo_dem.Count;
        }

        // Lấy RVA hiện tại tính từ gốc cave
        public int lay_vi_tri(string nhan)
        {
            return lech_ma + bang_nhan[nhan];
        }

        // Ghi chuỗi byte từ hex
        public void ghi_hex(string chuoi_hex)
        {
            string[] phan = chuoi_hex.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < phan.Length; i++)
            {
                bo_dem.Add(Convert.ToByte(phan[i], 16));
            }
        }

        // Ghi mảng byte có sẵn
        public void ghi_mang(byte[] du_lieu)
        {
            bo_dem.AddRange(du_lieu);
        }

        // Ghi lệnh tham chiếu dữ liệu theo RIP
        public void ghi_rip(string tien_to_hex, int vi_tri_bien, params byte[] hau_to)
        {
            ghi_hex(tien_to_hex);
            int vi_tri_chenh = bo_dem.Count;
            bo_dem.Add(0);
            bo_dem.Add(0);
            bo_dem.Add(0);
            bo_dem.Add(0);
            if (hau_to != null && hau_to.Length > 0)
            {
                bo_dem.AddRange(hau_to);
            }
            int ket_thuc = bo_dem.Count;
            sua_rip.Add(new int[] { vi_tri_chenh, ket_thuc, vi_tri_bien });
        }

        // Ghi lệnh nhảy có điều kiện 0F xx rel32
        public void ghi_nhay_dk(byte ma_dk, string nhan_dich)
        {
            bo_dem.Add(0x0F);
            bo_dem.Add(ma_dk);
            int pos = bo_dem.Count;
            bo_dem.Add(0);
            bo_dem.Add(0);
            bo_dem.Add(0);
            bo_dem.Add(0);
            sua_nhay_32.Add(new KeyValuePair<int, string>(pos, nhan_dich));
        }

        // Ghi lệnh nhảy không điều kiện E9 rel32
        public void ghi_nhay(string nhan_dich)
        {
            bo_dem.Add(0xE9);
            int pos = bo_dem.Count;
            bo_dem.Add(0);
            bo_dem.Add(0);
            bo_dem.Add(0);
            bo_dem.Add(0);
            sua_nhay_32.Add(new KeyValuePair<int, string>(pos, nhan_dich));
        }

        // Căn lề 16 byte bằng NOP
        public void can_le_16()
        {
            while ((bo_dem.Count % 16) != 0)
            {
                bo_dem.Add(0x90);
            }
        }

        // Vá độ lệch nhãn, RIP rồi xuất mảng byte
        public byte[] hoan_tat()
        {
            byte[] kq = bo_dem.ToArray();
            for (int i = 0; i < sua_nhay_32.Count; i++)
            {
                int pos = sua_nhay_32[i].Key;
                string nhan = sua_nhay_32[i].Value;
                int dich = bang_nhan[nhan];
                int chenh = dich - (pos + 4);
                byte[] b = BitConverter.GetBytes(chenh);
                Buffer.BlockCopy(b, 0, kq, pos, 4);
            }
            for (int i = 0; i < sua_rip.Count; i++)
            {
                int[] item = sua_rip[i];
                int chenh = item[2] - (lech_ma + item[1]);
                byte[] b = BitConverter.GetBytes(chenh);
                Buffer.BlockCopy(b, 0, kq, item[0], 4);
            }
            return kq;
        }
    }

    // === Động cơ thao tác tiến trình game ===
    public sealed class dong_co_bo_nho : IDisposable
    {
        // --- Hằng số Win32 ---
        private const uint co_snap_tien_trinh = 0x00000002;
        private const uint co_snap_module = 0x00000008;
        private const uint co_snap_module_32 = 0x00000010;
        private const uint quyen_doc_ghi = 0x0010 | 0x0020 | 0x0008 | 0x0400;
        private const uint mem_commit = 0x1000;
        private const uint mem_reserve = 0x2000;
        private const uint mem_release = 0x8000;
        private const uint trang_rwx = 0x40;
        private const uint trang_noaccess = 0x01;
        private const uint trang_guard = 0x100;
        private const int loi_tu_choi_truy_cap = 5;
        private const uint ma_dang_chay = 259;

        // --- Khai báo struct Win32 ---
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct muc_tien_trinh
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct muc_module
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public IntPtr modBaseAddr;
            public uint modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct thong_tin_bo_nho
        {
            public UIntPtr BaseAddress;
            public UIntPtr AllocationBase;
            public uint AllocationProtect;
            public ushort PartitionId;
            public UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        // --- Nhập hàm kernel32.dll ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool Process32FirstW(IntPtr hSnapshot, ref muc_tien_trinh lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool Process32NextW(IntPtr hSnapshot, ref muc_tien_trinh lppe);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool Module32FirstW(IntPtr hSnapshot, ref muc_module lpme);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool Module32NextW(IntPtr hSnapshot, ref muc_module lpme);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr VirtualAllocEx(
            IntPtr hProcess, UIntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(
            IntPtr hProcess, UIntPtr lpAddress, UIntPtr dwSize, uint dwFreeType
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtectEx(
            IntPtr hProcess, UIntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr VirtualQueryEx(
            IntPtr hProcess, UIntPtr lpAddress, out thong_tin_bo_nho lpBuffer, UIntPtr dwLength
        );

        // --- Bố cục vùng dữ liệu chia sẻ v4 ---
        private static readonly byte[] dau_hieu_v4 = Encoding.ASCII.GetBytes("FORGETAX_MOD_V4\0");
        private static readonly byte[] dau_hieu_v3 = Encoding.ASCII.GetBytes("FORGETAX_MOD_V3\0");
        private static readonly byte[] mang_bat = new byte[] { 1, 0, 0, 0 };
        private static readonly byte[] mang_tat = new byte[] { 0, 0, 0, 0 };

        private const int o_dau_hieu = 0x000;
        private const int o_phong_ren = 0x010;
        private const int o_chien_dich = 0x018;
        private const int o_khong_pha_huy = 0x020;
        private const int o_khong_hu_ton = 0x024;
        private const int o_khong_tinh_tien = 0x028;
        private const int o_mien_cong_nap = 0x02C;
        private const int o_so_kiem_trieu_hoi = 0x030;
        private const int o_co_hoi_to = 0x034;
        private const int o_ket_qua_nang_cap = 0x038;
        private const int o_co_tang_tien = 0x03C;
        private const int o_them_vang = 0x040;
        private const int o_them_credits = 0x048;
        private const int o_co_lam_moi_hud = 0x04C;
        private const int o_tong_trieu_hoi = 0x050;
        private const int o_che_do_hoi_to_cuoi = 0x054;
        private const int o_trang_thai_nang_cap = 0x058;
        private const int o_khoa_ban = 0x05C;
        private const int o_hang_so_nua = 0x060;
        private const int o_toa_do_du_phong = 0x068;
        private const int o_nhip_khung_hinh = 0x074;
        private const int o_kiem_cuoi = 0x078;
        private const int o_ham_gia_double = 0x080;

        private const int o_moc_update_va = 0x0E0;
        private const int o_moc_cc_va = 0x0E8;
        private const int o_moc_zh_va = 0x0F0;
        private const int o_moc_af_va = 0x0F8;
        private const int o_moc_gf_va = 0x100;

        private static readonly string[] danh_sach_o_ham = new string[]
        {
            "fn_instantiate", "p_instantiate_mi_addr", "fn_cd", "fn_tr", "fn_wB",
            "fn_hf", "fn_TH", "fn_VO", "fn_ur", "p_list_add_mi_root_addr",
            "fn_list_add_resize", "fn_wbarrier", "fn_Bo", "fn_oE", "fn_Xd",
            "fn_Uc", "fn_pC", "fn_UB", "fn_zH", "p_m_del_cls_addr",
            "p_af_init_addr", "p_gf_init_addr", "fn_get_pos", "fn_set_pos",
            "fn_get_go", "fn_get_tr", "fn_set_active", "fn_init_meta", "fn_gia_co_vat",
            "ret_update_va", "ret_cc_va", "ret_zh_va", "ret_af_va", "ret_gf_va",
            "ret_zh_vo_va", "ret_lf_mua_ok_va", "ret_lf_mua_thieu_va",
            "ret_lf_tru1_va", "ret_lf_tru2_va", "ret_yf_mua_ok_va", "ret_yf_mua_thieu_va",
            "ret_zf_tru_va", "ret_ui_tn_va", "ret_ui_cv_va"
        };
        private static readonly Dictionary<string, int> bang_o_ham = tao_bang_o_ham();
        private const int vi_tri_ma_cave = 0x1000;
        private const int kich_thuoc_cave = 0x2000;

        private static readonly byte[] goc_update = tu_hex("48 8B C4 48 89 48 08 53 56 57 41 54 41 56 41 57");
        private static readonly byte[] goc_cc = tu_hex("0F 10 00 8B 40 10 0F 11 06 89 46 10 48 8B C6");
        private static readonly byte[] goc_zh = tu_hex("48 8B C4 4C 89 48 20 4C 89 40 18 89 50 10");
        private static readonly byte[] goc_af = tu_hex("40 53 48 83 EC 30 80 3D F8 AB 56 01 00 48 8B D9");
        private static readonly byte[] goc_gf = tu_hex("40 53 48 81 EC 80 00 00 00 80 3D D1 38 57 01 00");
        private static readonly byte[] goc_zh_vo = tu_hex("8B 85 58 01 00 00 41 C6 47 69 01 41 89 47 54 41 89 4F 5C");
        private static readonly byte[] goc_lf_mua = tu_hex("66 0F 6E 8E 0C 01 00 00 F2 0F 10 40 18 F3 0F E6 C9 66 0F 2F C1 77 2A");
        private static readonly byte[] goc_lf_tru1 = tu_hex("F2 0F 2C 40 18 29 86 0C 01 00 00 48 8B 45 18");
        private static readonly byte[] goc_lf_tru2 = tu_hex("F2 0F 2C 40 18 29 86 0C 01 00 00 48 8B 45 18");
        private static readonly byte[] goc_yf_mua = tu_hex("F2 0F 10 46 18 66 0F 2F 85 28 01 00 00 0F 86 1D 05 00 00");
        private static readonly byte[] goc_zf_tru = tu_hex("F2 0F 10 85 28 01 00 00 33 D2 F2 0F 5C 47 18 48 8B CF F2 0F 11 85 28 01 00 00");
        private static readonly byte[] goc_ui_tn = tu_hex("F2 0F 10 80 28 01 00 00 41 B6 01 66 0F 2F 46 18 0F 93 C0");
        private static readonly byte[] goc_ui_cv = tu_hex("66 0F 6E F6 45 33 C0 48 8B D3 48 8B C8 F3 0F E6 F6 E8 00 77 14 00 33 C0 66 0F 2F F0 0F 93 C0");

        private static readonly Dictionary<string, mau_aob> bang_aob = tao_bang_aob();
        private static readonly mau_aob_muc[][] bang_nhay_2byte = tao_bang_nhay_2byte();

        // Tạo bảng tra 2 byte đầu quét 37 AOB 1 lượt
        private static mau_aob_muc[][] tao_bang_nhay_2byte()
        {
            var tam = new List<mau_aob_muc>[65536];
            foreach (KeyValuePair<string, mau_aob> cap in bang_aob)
            {
                int khoa = cap.Value.gia_tri[0] | (cap.Value.gia_tri[1] << 8);
                if (tam[khoa] == null)
                {
                    tam[khoa] = new List<mau_aob_muc>(2);
                }
                tam[khoa].Add(new mau_aob_muc(cap.Key, cap.Value));
            }
            var kq = new mau_aob_muc[65536][];
            for (int i = 0; i < 65536; i++)
            {
                if (tam[i] != null)
                {
                    kq[i] = tam[i].ToArray();
                }
            }
            return kq;
        }

        private readonly object khoa_dong_bo = new object();
        private IntPtr tay_cam = IntPtr.Zero;
        private int ma_pid;
        private ulong goc_ga;
        private uint kich_thuoc_ga;
        private ulong goc_cave;
        private bool da_moc_flag;
        private bool tu_tao_cave;
        private int pid_da_quet;
        private ulong ga_da_quet;
        private Dictionary<string, ulong> aob_da_quet;
        private int pid_khong_tuong_thich;
        private readonly byte[] bo_dem_trang_thai = new byte[64];
        private Dictionary<string, ulong> dia_chi = new Dictionary<string, ulong>();

        // Kiểm tra nhanh đã gắn từ luồng nền
        public bool dang_da_moc
        {
            get { return da_moc_flag; }
        }

        // Khởi tạo bảng địa chỉ ô hàm
        private static Dictionary<string, int> tao_bang_o_ham()
        {
            var d = new Dictionary<string, int>();
            for (int i = 0; i < danh_sach_o_ham.Length; i++)
            {
                d[danh_sach_o_ham[i]] = 0x200 + i * 8;
            }
            return d;
        }

        // Khởi tạo 37 chữ ký AOB GameAssembly.dll
        private static Dictionary<string, mau_aob> tao_bang_aob()
        {
            var d = new Dictionary<string, mau_aob>();
            d["update_anchor"] = new mau_aob("48 81 EC A8 00 00 00 0F 29 70 B8 0F 29 78 A8 44 0F 29 40 98 48 8B F9 80", -0x10);
            d["cc_anchor"] = new mau_aob("0F 14 C1 66 49 0F 7E C1 48 C7 44 24 20 00 00 00 00 E8 12 98 1B 00", 0x16);
            d["zh_anchor"] = new mau_aob("48 89 48 08 55 53 56 57 41 54 41 55 41 56 41 57 48 8D A8 B8 FE FF FF 48", -0x0E);
            d["zh_break_ctx"] = new mau_aob("F2 45 0F 10 A5 88 01 00 00 4C 8B 05 A1 8F 42 01 4C", -0x13);
            d["af_anchor"] = new mau_aob("75 2B 48 8D 0D E7 39 47 01 E8 92 1D DD FF 48 8D 0D 43 3D 47 01 E8 86 1D", -0x10);
            d["gf_anchor"] = new mau_aob("48 8B D9 0F 29 74 24 70 0F 29 7C 24 60 44 0F 29 44 24 50 44 0F 29 4C 24", -0x10);
            d["lf_afford"] = new mau_aob("83 BA E4 00 00 00 00 75 0F 48 8B CA E8 15 DA DC FF 48 8B 15 3E F1 46 01", -0x17);
            d["lf_ded1"] = new mau_aob("48 85 C0 0F 84 91 17 00 00 C6 40 24 01 48 8B 55 18 48 85 D2 0F 84 80 17 00 00", -0x0F);
            d["lf_ded2"] = new mau_aob("48 85 C0 0F 84 FD 00 00 00 C6 40 24 01 48 8B 55 18 48 85 D2 0F 84 EC 00 00 00", -0x0F);
            d["Yf_afford"] = new mau_aob("48 8B 15 7B 4F 47 01 83 BA E4 00 00 00 00 75 0F 48 8B CA E8 3A 38 DD FF", -0x13);
            d["zf_deduct"] = new mau_aob("8B 5F 2C FF C3 89 5F 2C E8 D3 B6 07 00 3B D8 0F 9D C1 88 4F 20", -0x1A);
            d["ui_thuong_nhan"] = new mau_aob("83 7E 2C 00 89 45 40 7E 67 F2 0F 10 46 18 66 41 0F 2F C1 76 5B 33 D2 48 8B CE E8", -0x13);
            d["ui_co_vat"] = new mau_aob("89 45 40 33 D2 48 8B CB E8 4A 89 18 00 48 8B 0D AB 4A 5C 01 4C 8B E0", -0x1F);
            d["fn_gia_co_vat"] = new mau_aob("48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 80 3D C7 42 57 01 00 48 8B FA 48 8B F1", 0);
            d["xc_instantiate"] = new mau_aob("4C 8B 0D 2D 7C 5C 01 4C 8D 44 24 60 48 8D 54 24 50 48 8B CB E8 43 57 28 00", 0);
            d["xc_list_add"] = new mau_aob("48 8B 8F C8 03 00 00 48 85 C9 0F 84 0D 06 00 00 4C 8B 0D CE E3 5B 01 FF 41 1C 48 8B 51 10 48 85 D2", 0);
            d["m_delegate_ctx"] = new mau_aob("48 8B 05 D5 FA 50 01 83 B8 E4 00 00 00 00 75 0F 48 8B C8 E8 BC F0 E3 FF", 0);
            d["fn_cd"] = new mau_aob("48 89 4C 24 08 53 56 57 41 56 41 57 48 81 EC 80 00 00 00 4C 8B F9 80 3D", 0);
            d["fn_tr"] = new mau_aob("40 53 55 56 57 48 83 EC 28 80 3D A5 AC 65 01 00 49 8B F9 49 8B F0 8B EA", 0);
            d["fn_wB"] = new mau_aob("48 89 5C 24 08 48 89 74 24 10 57 48 83 EC 20 80 3D D3 2D 6B 01 00 48 8B", 0);
            d["fn_hf"] = new mau_aob("48 89 5C 24 08 57 48 83 EC 30 80 3D 61 95 56 01 00 48 8B DA 48 8B F9 75", 0);
            d["fn_TH"] = new mau_aob("40 53 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 78 80 3D D2 AA 52 01 00", 0);
            d["fn_VO"] = new mau_aob("48 8B C4 48 89 48 08 53 56 57 41 54 41 55 41 56 41 57 48 81 EC 20 02 00", 0);
            d["fn_ur"] = new mau_aob("4C 89 44 24 18 53 41 56 48 83 EC 78 80 3D 54 A9 65 01 00 44 8B F2 48 8B", 0);
            d["fn_wbarrier"] = new mau_aob("E9 5B 67 02 00 CC CC CC CC CC CC CC CC CC CC CC 48 8B CA E9 48 67 02 00", 0);
            d["fn_Bo"] = new mau_aob("48 89 5C 24 20 56 48 83 EC 20 80 3D 27 7F 69 01 00 48 8B DA 48 8B F1 75", 0);
            d["fn_oE"] = new mau_aob("48 89 5C 24 18 48 89 6C 24 20 41 56 48 83 EC 20 80 3D 50 3A 55 01 00 44", 0);
            d["fn_Xd"] = new mau_aob("48 89 4C 24 08 53 56 57 41 54 41 56 41 57 48 81 EC 98 00 00 00 0F 29 B4 24 80 00 00 00 48 8B F9", 0);
            d["fn_Uc"] = new mau_aob("48 8B C4 48 89 58 08 55 56 57 41 56 41 57 48 8D 6C 24 80 48 81 EC 80 01", 0);
            d["fn_pC"] = new mau_aob("40 53 48 83 EC 40 80 3D 00 73 6B 01 00 48 8B D9 75 2B 48 8D 0D 9F ED 5C", 0);
            d["fn_UB"] = new mau_aob("40 55 53 41 56 48 8D 6C 24 C0 48 81 EC 40 01 00 00 80 3D DF 44 6C 01 00", 0);
            d["fn_get_pos"] = new mau_aob("48 89 5C 24 10 57 48 83 EC 20 80 3D 37 E9 83 00 00 48 8B DA 48 8B F9 75", 0);
            d["fn_set_pos"] = new mau_aob("48 89 5C 24 10 56 48 83 EC 20 80 3D D8 E0 83 00 00 48 8B F2 48 8B D9 75", 0);
            d["fn_get_go"] = new mau_aob("40 53 48 83 EC 20 80 3D BA 80 88 00 00 48 8B D9 75 1F 48 8D 0D 8F D6 78", 0);
            d["fn_get_tr"] = new mau_aob("40 53 48 83 EC 20 80 3D D9 7F 88 00 00 48 8B D9 75 1F 48 8D 0D AF D5 78", 0);
            d["fn_set_active"] = new mau_aob("48 89 5C 24 10 56 48 83 EC 20 80 3D F7 44 88 00 00 0F B6 F2 48 8B D9 75", 0);
            d["fn_init_meta"] = new mau_aob("48 83 EC 28 E8 37 9F 03 00 F0 83 0C 24 00 48 83 C4 28 C3 CC CC CC CC CC", 0);
            return d;
        }

        // Chuyển chuỗi hex thành mảng byte
        private static byte[] tu_hex(string chuoi)
        {
            string[] phan = chuoi.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] kq = new byte[phan.Length];
            for (int i = 0; i < phan.Length; i++)
            {
                kq[i] = Convert.ToByte(phan[i], 16);
            }
            return kq;
        }

        // Tạo lệnh nhảy tuyệt đối >= 14 byte
        private static byte[] tao_nhay_tuyet_doi(ulong dich_va, int tong_do_dai)
        {
            byte[] kq = new byte[tong_do_dai];
            kq[0] = 0xFF;
            kq[1] = 0x25;
            byte[] d = BitConverter.GetBytes(dich_va);
            Buffer.BlockCopy(d, 0, kq, 6, 8);
            for (int i = 14; i < tong_do_dai; i++)
            {
                kq[i] = 0x90;
            }
            return kq;
        }

        // Kiểm tra quyền Administrator
        public static bool co_quyen_admin()
        {
            try
            {
                WindowsIdentity dinh_danh = WindowsIdentity.GetCurrent();
                WindowsPrincipal quyen = new WindowsPrincipal(dinh_danh);
                return quyen.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        // Dựng code cave x64 cho 13 hook v4
        public static byte[] dung_vung_ma(ulong nen_cave, out Dictionary<string, ulong> loi_vao)
        {
            var e = new bo_phat_ma_x64(vi_tri_ma_cave);

            // 0) Delegate giả trả về 0.5 cho random
            e.dat_nhan("ham_gia_double");
            e.ghi_rip("F2 0F 10 05", o_hang_so_nua);
            e.ghi_hex("C3");
            e.can_le_16();

            // 1) Hook $Cc chỉnh tỉ lệ vỡ/hư tổn
            e.dat_nhan("moc_cc_cuoi");
            e.ghi_hex("0F 10 00 8B 40 10 0F 11 06 89 46 10");
            e.ghi_rip("83 3D", o_khong_pha_huy, 0x00);
            e.ghi_nhay_dk(0x84, "cc_kiem_tra_hu_ton");
            e.ghi_hex("8B 0E 85 C9");
            e.ghi_nhay_dk(0x84, "cc_kiem_tra_hu_ton");
            e.ghi_hex("01 4E 08 C7 06 00 00 00 00");

            e.dat_nhan("cc_kiem_tra_hu_ton");
            e.ghi_rip("83 3D", o_khong_hu_ton, 0x00);
            e.ghi_nhay_dk(0x84, "cc_xong");
            e.ghi_hex("8B 4E 04 85 C9");
            e.ghi_nhay_dk(0x84, "cc_xong");
            e.ghi_hex("01 4E 08 C7 46 04 00 00 00 00");

            e.dat_nhan("cc_xong");
            e.ghi_hex("48 8B C6");
            e.ghi_rip("FF 25", bang_o_ham["ret_cc_va"]);
            e.can_le_16();

            // 2) Hook đầu $zH chặn vỡ hoặc tụt cấp
            e.dat_nhan("moc_zh_dau");
            e.ghi_hex("85 D2");
            e.ghi_nhay_dk(0x85, "zh_kiem_tra_hu_ton");
            e.ghi_rip("83 3D", o_khong_pha_huy, 0x00);
            e.ghi_nhay_dk(0x84, "zh_goc");
            e.ghi_hex("BA 02 00 00 00");
            e.ghi_nhay("zh_goc");

            e.dat_nhan("zh_kiem_tra_hu_ton");
            e.ghi_hex("83 FA 01");
            e.ghi_nhay_dk(0x85, "zh_goc");
            e.ghi_rip("83 3D", o_khong_hu_ton, 0x00);
            e.ghi_nhay_dk(0x84, "zh_goc");
            e.ghi_hex("BA 02 00 00 00");

            e.dat_nhan("zh_goc");
            e.ghi_mang(goc_zh);
            e.ghi_rip("FF 25", bang_o_ham["ret_zh_va"]);
            e.can_le_16();

            // 3) Hook $aF miễn phí reroll khi bật F3
            e.dat_nhan("moc_af_dau");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x84, "af_goc");
            e.ghi_hex("B0 01 C3");

            e.dat_nhan("af_goc");
            e.ghi_hex("40 53 48 83 EC 30 48 8B D9");
            e.ghi_rip("48 8B 05", bang_o_ham["p_af_init_addr"]);
            e.ghi_hex("80 38 00");
            e.ghi_rip("FF 25", bang_o_ham["ret_af_va"]);
            e.can_le_16();

            // 4) Hook $Gf trả mức cống nạp 0 khi bật F4
            e.dat_nhan("moc_gf_dau");
            e.ghi_rip("83 3D", o_mien_cong_nap, 0x00);
            e.ghi_nhay_dk(0x84, "gf_goc");
            e.ghi_hex("0F 57 C0 C3");

            e.dat_nhan("gf_goc");
            e.ghi_hex("40 53 48 81 EC 80 00 00 00");
            e.ghi_rip("48 8B 05", bang_o_ham["p_gf_init_addr"]);
            e.ghi_hex("80 38 00");
            e.ghi_rip("FF 25", bang_o_ham["ret_gf_va"]);
            e.can_le_16();

            // 4b) Hook F1 chống gãy trong khối vỡ kiếm
            e.dat_nhan("moc_zh_vo");
            e.ghi_hex("8B 85 58 01 00 00");
            e.ghi_rip("83 3D", o_khong_pha_huy, 0x00);
            e.ghi_nhay_dk(0x85, "zh_vo_khong_gay");
            e.ghi_hex("41 C6 47 69 01");
            e.ghi_nhay("zh_vo_xong");
            e.dat_nhan("zh_vo_khong_gay");
            e.ghi_hex("41 C6 47 69 00");
            e.dat_nhan("zh_vo_xong");
            e.ghi_hex("41 89 47 54 41 89 4F 5C");
            e.ghi_rip("FF 25", bang_o_ham["ret_zh_vo_va"]);
            e.can_le_16();

            // 4c) Hook F3 mua cổ vật miễn phí (Credits)
            e.dat_nhan("moc_lf_mua");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x85, "lf_mua_duoc");
            e.ghi_hex("66 0F 6E 8E 0C 01 00 00 F2 0F 10 40 18 F3 0F E6 C9 66 0F 2F C1");
            e.ghi_nhay_dk(0x87, "lf_mua_thieu");
            e.dat_nhan("lf_mua_duoc");
            e.ghi_rip("FF 25", bang_o_ham["ret_lf_mua_ok_va"]);
            e.dat_nhan("lf_mua_thieu");
            e.ghi_rip("FF 25", bang_o_ham["ret_lf_mua_thieu_va"]);
            e.can_le_16();

            e.dat_nhan("moc_lf_tru1");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x85, "lf_tru1_xong");
            e.ghi_hex("F2 0F 2C 40 18 29 86 0C 01 00 00");
            e.dat_nhan("lf_tru1_xong");
            e.ghi_hex("48 8B 45 18");
            e.ghi_rip("FF 25", bang_o_ham["ret_lf_tru1_va"]);
            e.can_le_16();

            e.dat_nhan("moc_lf_tru2");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x85, "lf_tru2_xong");
            e.ghi_hex("F2 0F 2C 40 18 29 86 0C 01 00 00");
            e.dat_nhan("lf_tru2_xong");
            e.ghi_hex("48 8B 45 18");
            e.ghi_rip("FF 25", bang_o_ham["ret_lf_tru2_va"]);
            e.can_le_16();

            // 4d) Hook F3 mua thương nhân miễn phí (Gold)
            e.dat_nhan("moc_yf_mua");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x85, "yf_mua_duoc");
            e.ghi_hex("F2 0F 10 46 18 66 0F 2F 85 28 01 00 00");
            e.ghi_nhay_dk(0x86, "yf_mua_duoc");
            e.ghi_rip("FF 25", bang_o_ham["ret_yf_mua_thieu_va"]);
            e.dat_nhan("yf_mua_duoc");
            e.ghi_rip("FF 25", bang_o_ham["ret_yf_mua_ok_va"]);
            e.can_le_16();

            e.dat_nhan("moc_zf_tru");
            e.ghi_hex("F2 0F 10 85 28 01 00 00 33 D2");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x85, "zf_tru_xong");
            e.ghi_hex("F2 0F 5C 47 18");
            e.dat_nhan("zf_tru_xong");
            e.ghi_hex("48 8B CF F2 0F 11 85 28 01 00 00");
            e.ghi_rip("FF 25", bang_o_ham["ret_zf_tru_va"]);
            e.can_le_16();

            // 4e) Hook UI luôn hiện đủ tiền mua khi bật F3
            e.dat_nhan("moc_ui_thuong_nhan");
            e.ghi_hex("41 B6 01");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x84, "ui_tn_goc");
            e.ghi_hex("B8 01 00 00 00");
            e.ghi_rip("FF 25", bang_o_ham["ret_ui_tn_va"]);
            e.dat_nhan("ui_tn_goc");
            e.ghi_hex("F2 0F 10 80 28 01 00 00 66 0F 2F 46 18 0F 93 C0");
            e.ghi_rip("FF 25", bang_o_ham["ret_ui_tn_va"]);
            e.can_le_16();

            e.dat_nhan("moc_ui_co_vat");
            e.ghi_rip("83 3D", o_khong_tinh_tien, 0x00);
            e.ghi_nhay_dk(0x84, "ui_cv_goc");
            e.ghi_hex("B8 01 00 00 00");
            e.ghi_rip("FF 25", bang_o_ham["ret_ui_cv_va"]);
            e.dat_nhan("ui_cv_goc");
            e.ghi_hex("66 0F 6E F6 45 33 C0 48 8B D3 48 8B C8 F3 0F E6 F6");
            e.ghi_rip("FF 15", bang_o_ham["fn_gia_co_vat"]);
            e.ghi_hex("33 C0 66 0F 2F F0 0F 93 C0");
            e.ghi_rip("FF 25", bang_o_ham["ret_ui_cv_va"]);
            e.can_le_16();

            // 5) Hook Update điều phối F5..F8 và tặng tiền
            e.dat_nhan("moc_update");
            e.ghi_rip("48 89 0D", o_phong_ren);
            e.ghi_rip("FF 05", o_nhip_khung_hinh);
            e.ghi_hex("48 85 C9");
            e.ghi_nhay_dk(0x84, "thoat_goc");
            e.ghi_hex("48 8B 81 F0 00 00 00");
            e.ghi_rip("48 89 05", o_chien_dich);
            e.ghi_hex("80 B9 90 04 00 00 00");
            e.ghi_nhay_dk(0x85, "thoat_goc");
            e.ghi_rip("83 3D", o_khoa_ban, 0x00);
            e.ghi_nhay_dk(0x85, "thoat_goc");

            e.ghi_rip("8B 05", o_so_kiem_trieu_hoi);
            e.ghi_rip("0B 05", o_co_hoi_to);
            e.ghi_rip("0B 05", o_ket_qua_nang_cap);
            e.ghi_rip("0B 05", o_co_tang_tien);
            e.ghi_rip("0B 05", o_co_lam_moi_hud);
            e.ghi_nhay_dk(0x84, "thoat_goc");

            e.ghi_rip("C7 05", o_khoa_ban, 0x01, 0x00, 0x00, 0x00);
            e.ghi_hex("53 56 57 55 41 54 41 55 41 56 41 57");
            e.ghi_hex("48 81 EC 88 00 00 00 48 8B F9");

            e.ghi_hex("48 83 BF F0 00 00 00 00");
            e.ghi_nhay_dk(0x84, "ket_thuc_viec");
            e.ghi_hex("48 83 BF B0 02 00 00 00");
            e.ghi_nhay_dk(0x84, "ket_thuc_viec");
            e.ghi_hex("48 83 BF 30 03 00 00 00");
            e.ghi_nhay_dk(0x84, "ket_thuc_viec");
            e.ghi_hex("48 83 BF C8 03 00 00 00");
            e.ghi_nhay_dk(0x84, "ket_thuc_viec");

            // --- F5: Triệu hồi 1 kiếm +0 ---
            e.ghi_rip("83 3D", o_so_kiem_trieu_hoi, 0x00);
            e.ghi_nhay_dk(0x8E, "kiem_tra_hoi_to");
            e.ghi_rip("F0 FF 0D", o_so_kiem_trieu_hoi);

            e.ghi_hex("48 8B 97 38 03 00 00 48 85 D2");
            e.ghi_nhay_dk(0x85, "sp_co_tr");
            e.ghi_hex("48 8B 97 40 03 00 00 48 85 D2");
            e.ghi_nhay_dk(0x84, "sp_toa_do_du_phong");
            e.dat_nhan("sp_co_tr");
            e.ghi_hex("48 8D 4C 24 50 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_get_pos"]);
            e.ghi_nhay("sp_dat_xoay");

            e.dat_nhan("sp_toa_do_du_phong");
            e.ghi_hex("C7 44 24 50 7B 14 2E 3E C7 44 24 54 33 33 93 3F C7 44 24 58 AE 47 61 3E");

            e.dat_nhan("sp_dat_xoay");
            e.ghi_hex("33 C0 89 44 24 60 89 44 24 64 89 44 24 68 C7 44 24 6C 00 00 80 3F");

            e.ghi_rip("48 8B 0D", bang_o_ham["p_instantiate_mi_addr"]);
            e.ghi_hex("4C 8B 09 4D 85 C9");
            e.ghi_nhay_dk(0x85, "sp_mi_ok");
            e.ghi_rip("FF 15", bang_o_ham["fn_init_meta"]);
            e.ghi_rip("48 8B 0D", bang_o_ham["p_instantiate_mi_addr"]);
            e.ghi_hex("4C 8B 09 4D 85 C9");
            e.ghi_nhay_dk(0x84, "kiem_tra_hoi_to");
            e.dat_nhan("sp_mi_ok");

            e.ghi_hex("48 8B 8F 30 03 00 00 48 8D 54 24 50 4C 8D 44 24 60");
            e.ghi_rip("FF 15", bang_o_ham["fn_instantiate"]);
            e.ghi_hex("48 8B D8 48 85 DB");
            e.ghi_nhay_dk(0x84, "kiem_tra_hoi_to");

            e.ghi_hex("48 8B CF 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_cd"]);
            e.ghi_hex("44 8B E8");

            e.ghi_hex("C7 44 24 20 FF FF FF FF");
            e.ghi_hex("48 C7 44 24 28 00 00 00 00 48 C7 44 24 30 00 00 00 00");
            e.ghi_hex("48 C7 44 24 38 00 00 00 00 48 C7 44 24 40 00 00 00 00");
            e.ghi_hex("48 C7 44 24 48 00 00 00 00");
            e.ghi_hex("48 8B CB 41 8B D5 4C 8B 87 F0 01 00 00 4C 8B 8F B0 02 00 00");
            e.ghi_rip("FF 15", bang_o_ham["fn_tr"]);

            e.ghi_hex("C7 43 50 00 00 00 00 48 8B CF 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_wB"]);
            e.ghi_hex("4C 8B F0 4D 85 F6");
            e.ghi_nhay_dk(0x84, "kiem_tra_hoi_to");

            e.ghi_hex("49 8B CE 48 8B D3 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_hf"]);

            e.ghi_hex("48 8B CB 49 8B D6 4C 8B 87 B0 02 00 00 45 33 C9");
            e.ghi_rip("FF 15", bang_o_ham["fn_TH"]);

            e.ghi_hex("48 8B 4B 40 48 85 C9");
            e.ghi_nhay_dk(0x84, "sp_bo_qua_vo");
            e.ghi_hex("33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_VO"]);
            e.dat_nhan("sp_bo_qua_vo");

            e.ghi_hex("48 8B CB 33 D2 45 33 C0 45 33 C9");
            e.ghi_rip("FF 15", bang_o_ham["fn_ur"]);

            e.ghi_hex("48 8B 8F C8 03 00 00 48 85 C9");
            e.ghi_nhay_dk(0x84, "sp_bo_qua_ds");
            e.ghi_hex("FF 41 1C 48 8B 51 10 48 85 D2");
            e.ghi_nhay_dk(0x84, "sp_ds_cham");
            e.ghi_hex("4C 63 41 18 44 3B 42 18");
            e.ghi_nhay_dk(0x83, "sp_ds_cham");
            e.ghi_hex("41 8D 40 01 89 41 18 4A 89 5C C2 20 4A 8D 4C C2 20 48 8B D3");
            e.ghi_rip("FF 15", bang_o_ham["fn_wbarrier"]);
            e.ghi_nhay("sp_bo_qua_ds");

            e.dat_nhan("sp_ds_cham");
            e.ghi_rip("4C 8B 0D", bang_o_ham["p_list_add_mi_root_addr"]);
            e.ghi_hex("49 8B 01 48 85 C0");
            e.ghi_nhay_dk(0x85, "sp_ds_mi_ok");
            e.ghi_hex("49 8B C9");
            e.ghi_rip("FF 15", bang_o_ham["fn_init_meta"]);
            e.ghi_rip("4C 8B 0D", bang_o_ham["p_list_add_mi_root_addr"]);
            e.ghi_hex("49 8B 01 48 85 C0");
            e.ghi_nhay_dk(0x84, "sp_bo_qua_ds");
            e.dat_nhan("sp_ds_mi_ok");
            e.ghi_hex("48 8B 40 20 4C 8B 80 A8 00 00 00 4D 8B 40 70 48 8B 8F C8 03 00 00 48 8B D3");
            e.ghi_rip("FF 15", bang_o_ham["fn_list_add_resize"]);
            e.dat_nhan("sp_bo_qua_ds");

            e.ghi_hex("49 8B 4E 10 48 85 C9");
            e.ghi_nhay_dk(0x84, "sp_bo_qua_bo");
            e.ghi_hex("48 8B D3 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_Bo"]);
            e.dat_nhan("sp_bo_qua_bo");

            e.ghi_hex("48 8B CB B2 01 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_oE"]);

            e.ghi_hex("48 8B CF 48 8B D3 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_Uc"]);

            e.ghi_rip("48 89 1D", o_kiem_cuoi);
            e.ghi_rip("FF 05", o_tong_trieu_hoi);
            e.ghi_rip("C7 05", o_co_lam_moi_hud, 0x01, 0x00, 0x00, 0x00);

            // --- F6: Hồi tố thời gian hoặc hồi sinh kiếm ---
            e.dat_nhan("kiem_tra_hoi_to");
            e.ghi_rip("83 3D", o_co_hoi_to, 0x00);
            e.ghi_nhay_dk(0x84, "kiem_tra_nang_cap");
            e.ghi_rip("C7 05", o_co_hoi_to, 0x00, 0x00, 0x00, 0x00);

            e.ghi_hex("48 8B CF 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_wB"]);
            e.ghi_hex("4C 8B F0 4D 85 F6");
            e.ghi_nhay_dk(0x85, "rw_co_chien_dich");
            e.ghi_rip("C7 05", o_che_do_hoi_to_cuoi, 0x03, 0x00, 0x00, 0x00);
            e.ghi_nhay("kiem_tra_nang_cap");

            e.dat_nhan("rw_co_chien_dich");
            e.ghi_hex("48 8B 87 C8 01 00 00 48 85 C0");
            e.ghi_nhay_dk(0x84, "rw_du_phong");

            e.ghi_hex("4C 8B A7 E8 01 00 00 C6 44 24 78 00 4D 85 E4");
            e.ghi_nhay_dk(0x84, "rw_bo_qua_focus_dat");
            e.ghi_hex("41 8A 44 24 50 88 44 24 78 41 C6 44 24 50 01");
            e.dat_nhan("rw_bo_qua_focus_dat");

            e.ghi_hex("41 FF 86 10 01 00 00 48 8B CF 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_pC"]);
            e.ghi_hex("8A D8 4D 85 E4");
            e.ghi_nhay_dk(0x84, "rw_bo_qua_focus_tra");
            e.ghi_hex("8A 44 24 78 41 88 44 24 50");
            e.dat_nhan("rw_bo_qua_focus_tra");

            e.ghi_hex("84 DB");
            e.ghi_nhay_dk(0x84, "rw_that_bai");
            e.ghi_rip("C7 05", o_che_do_hoi_to_cuoi, 0x01, 0x00, 0x00, 0x00);
            e.ghi_rip("C7 05", o_co_lam_moi_hud, 0x01, 0x00, 0x00, 0x00);
            e.ghi_nhay("kiem_tra_nang_cap");

            e.dat_nhan("rw_that_bai");
            e.ghi_hex("41 FF 8E 10 01 00 00");

            e.dat_nhan("rw_du_phong");
            e.ghi_hex("4C 89 74 24 70 48 8B 97 38 03 00 00 48 85 D2");
            e.ghi_nhay_dk(0x85, "rw_co_tr");
            e.ghi_hex("48 8B 97 40 03 00 00 48 85 D2");
            e.ghi_nhay_dk(0x84, "rw_toa_do_fb");
            e.dat_nhan("rw_co_tr");
            e.ghi_hex("48 8D 4C 24 50 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_get_pos"]);
            e.ghi_nhay("rw_khoi_tao_vong");

            e.dat_nhan("rw_toa_do_fb");
            e.ghi_hex("C7 44 24 50 7B 14 2E 3E C7 44 24 54 33 33 93 3F C7 44 24 58 AE 47 61 3E");

            e.dat_nhan("rw_khoi_tao_vong");
            e.ghi_hex("4C 8B A7 C8 03 00 00 45 8B 6C 24 18 45 33 F6 45 33 FF");

            e.dat_nhan("rw_vong_lap");
            e.ghi_hex("45 3B F5");
            e.ghi_nhay_dk(0x8D, "rw_du_phong_xong");
            e.ghi_hex("49 8B 44 24 10 48 85 C0");
            e.ghi_nhay_dk(0x84, "rw_du_phong_xong");
            e.ghi_hex("44 3B 70 18");
            e.ghi_nhay_dk(0x83, "rw_du_phong_xong");
            e.ghi_hex("4A 8B 5C F0 20 48 85 DB");
            e.ghi_nhay_dk(0x84, "rw_ke_tiep");
            e.ghi_hex("80 7B 69 00");
            e.ghi_nhay_dk(0x84, "rw_ke_tiep");
            e.ghi_hex("C6 43 69 00");

            e.ghi_hex("48 8B CB 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_get_go"]);
            e.ghi_hex("48 85 C0");
            e.ghi_nhay_dk(0x84, "rw_bo_qua_bat");
            e.ghi_hex("48 8B C8 B2 01 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_set_active"]);
            e.dat_nhan("rw_bo_qua_bat");

            e.ghi_hex("48 8B CB 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_get_tr"]);
            e.ghi_hex("48 85 C0");
            e.ghi_nhay_dk(0x84, "rw_bo_qua_dat");
            e.ghi_hex("48 8B C8 48 8D 54 24 50 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_set_pos"]);
            e.dat_nhan("rw_bo_qua_dat");

            e.ghi_hex("48 8B CB 48 8B 54 24 70 4C 8B 87 B0 02 00 00 45 33 C9");
            e.ghi_rip("FF 15", bang_o_ham["fn_TH"]);

            e.ghi_hex("48 8B 4B 40 48 85 C9");
            e.ghi_nhay_dk(0x84, "rw_bo_qua_vo");
            e.ghi_hex("33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_VO"]);
            e.dat_nhan("rw_bo_qua_vo");

            e.ghi_hex("48 8B CF 48 8B D3 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_Uc"]);
            e.ghi_hex("41 FF C7");

            e.dat_nhan("rw_ke_tiep");
            e.ghi_hex("41 FF C6");
            e.ghi_nhay("rw_vong_lap");

            e.dat_nhan("rw_du_phong_xong");
            e.ghi_hex("45 85 FF");
            e.ghi_nhay_dk(0x84, "rw_khong_co");
            e.ghi_rip("C7 05", o_che_do_hoi_to_cuoi, 0x02, 0x00, 0x00, 0x00);
            e.ghi_rip("C7 05", o_co_lam_moi_hud, 0x01, 0x00, 0x00, 0x00);
            e.ghi_nhay("kiem_tra_nang_cap");
            e.dat_nhan("rw_khong_co");
            e.ghi_rip("C7 05", o_che_do_hoi_to_cuoi, 0x03, 0x00, 0x00, 0x00);

            // --- F7 (+1) và F8 (+2) nâng cấp kiếm ---
            e.dat_nhan("kiem_tra_nang_cap");
            e.ghi_rip("8B 05", o_ket_qua_nang_cap);
            e.ghi_hex("85 C0");
            e.ghi_nhay_dk(0x84, "kiem_tra_tang_tien");
            e.ghi_hex("44 8B E8");
            e.ghi_rip("C7 05", o_ket_qua_nang_cap, 0x00, 0x00, 0x00, 0x00);

            e.ghi_hex("48 8B 9F B0 03 00 00 48 85 DB");
            e.ghi_nhay_dk(0x85, "upg_co_kiem");
            e.ghi_hex("48 8B 9F 98 03 00 00 48 85 DB");
            e.ghi_nhay_dk(0x84, "upg_that_bai");
            e.dat_nhan("upg_co_kiem");
            e.ghi_hex("80 7B 69 00");
            e.ghi_nhay_dk(0x85, "upg_that_bai");

            e.ghi_hex("48 8B CF 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_wB"]);
            e.ghi_hex("4C 8B F0 4D 85 F6");
            e.ghi_nhay_dk(0x84, "upg_that_bai");
            e.ghi_hex("4C 8B BF B0 02 00 00 4D 85 FF");
            e.ghi_nhay_dk(0x84, "upg_that_bai");

            e.ghi_rip("48 8B 0D", bang_o_ham["p_m_del_cls_addr"]);
            e.ghi_hex("48 85 C9");
            e.ghi_nhay_dk(0x84, "upg_dung_del_gia");
            e.ghi_hex("48 8B 09 48 85 C9");
            e.ghi_nhay_dk(0x84, "upg_dung_del_gia");
            e.ghi_hex("48 8B 89 A0 00 00 00 48 85 C9");
            e.ghi_nhay_dk(0x84, "upg_dung_del_gia");
            e.ghi_hex("48 8B 41 48 48 85 C0");
            e.ghi_nhay_dk(0x85, "upg_co_del");
            e.dat_nhan("upg_dung_del_gia");
            e.ghi_rip("48 8D 05", o_ham_gia_double);
            e.dat_nhan("upg_co_del");

            e.ghi_hex("48 89 44 24 20 48 C7 44 24 28 00 00 00 00 48 C7 44 24 30 00 00 00 00");
            e.ghi_hex("48 8B CB 41 8B D5 4D 8B C6 4D 8B CF");
            e.ghi_rip("FF 15", bang_o_ham["fn_zH"]);

            e.ghi_hex("48 8B CF 48 8B D3 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_UB"]);

            e.ghi_hex("49 8B 4E 10 48 85 C9");
            e.ghi_nhay_dk(0x84, "upg_bo_qua_bo");
            e.ghi_hex("48 8B D3 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_Bo"]);
            e.dat_nhan("upg_bo_qua_bo");

            e.ghi_hex("48 8B CB B2 01 45 33 C0");
            e.ghi_rip("FF 15", bang_o_ham["fn_oE"]);

            e.ghi_hex("48 8B 4B 40 48 85 C9");
            e.ghi_nhay_dk(0x84, "upg_bo_qua_vo");
            e.ghi_hex("33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_VO"]);
            e.dat_nhan("upg_bo_qua_vo");

            e.ghi_rip("C7 05", o_trang_thai_nang_cap, 0x01, 0x00, 0x00, 0x00);
            e.ghi_rip("C7 05", o_co_lam_moi_hud, 0x01, 0x00, 0x00, 0x00);
            e.ghi_nhay("kiem_tra_tang_tien");

            e.dat_nhan("upg_that_bai");
            e.ghi_rip("C7 05", o_trang_thai_nang_cap, 0x02, 0x00, 0x00, 0x00);

            // --- Cộng Vàng và Credits chống tràn 64-bit ---
            e.dat_nhan("kiem_tra_tang_tien");
            e.ghi_rip("83 3D", o_co_tang_tien, 0x00);
            e.ghi_nhay_dk(0x84, "kiem_tra_hud");
            e.ghi_rip("C7 05", o_co_tang_tien, 0x00, 0x00, 0x00, 0x00);

            e.ghi_hex("48 8B CF 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_wB"]);
            e.ghi_hex("48 85 C0");
            e.ghi_nhay_dk(0x84, "kiem_tra_hud");

            e.ghi_hex("F2 0F 10 80 28 01 00 00");
            e.ghi_rip("F2 0F 58 05", o_them_vang);
            e.ghi_hex("F2 0F 11 80 28 01 00 00");

            // Cộng Credits giới hạn 0..2.000.000.000
            e.ghi_hex("48 63 88 0C 01 00 00");
            e.ghi_rip("48 63 15", o_them_credits);
            e.ghi_hex("48 01 D1 BA 00 94 35 77 48 39 D1");
            e.ghi_nhay_dk(0x8E, "cre_khong_vuot");
            e.ghi_hex("48 89 D1");
            e.dat_nhan("cre_khong_vuot");
            e.ghi_hex("48 85 C9");
            e.ghi_nhay_dk(0x89, "cre_luu_gia_tri");
            e.ghi_hex("31 C9");
            e.dat_nhan("cre_luu_gia_tri");
            e.ghi_hex("89 88 0C 01 00 00");

            e.ghi_hex("33 C9 0F 57 C0");
            e.ghi_rip("F2 0F 11 05", o_them_vang);
            e.ghi_rip("89 0D", o_them_credits);
            e.ghi_rip("C7 05", o_co_lam_moi_hud, 0x01, 0x00, 0x00, 0x00);

            // --- Làm mới hiển thị tỉ lệ $Xd ---
            e.dat_nhan("kiem_tra_hud");
            e.ghi_rip("83 3D", o_co_lam_moi_hud, 0x00);
            e.ghi_nhay_dk(0x84, "ket_thuc_viec");
            e.ghi_rip("C7 05", o_co_lam_moi_hud, 0x00, 0x00, 0x00, 0x00);
            e.ghi_hex("48 8B CF 33 D2");
            e.ghi_rip("FF 15", bang_o_ham["fn_Xd"]);

            e.dat_nhan("ket_thuc_viec");
            e.ghi_hex("48 8B CF 48 81 C4 88 00 00 00 41 5F 41 5E 41 5D 41 5C 5D 5F 5E 5B");
            e.ghi_rip("C7 05", o_khoa_ban, 0x00, 0x00, 0x00, 0x00);

            e.dat_nhan("thoat_goc");
            e.ghi_mang(goc_update);
            e.ghi_rip("FF 25", bang_o_ham["ret_update_va"]);

            byte[] ma_x64 = e.hoan_tat();
            loi_vao = new Dictionary<string, ulong>();
            loi_vao["ham_gia_double"] = nen_cave + (ulong)e.lay_vi_tri("ham_gia_double");
            loi_vao["moc_cc_cuoi"] = nen_cave + (ulong)e.lay_vi_tri("moc_cc_cuoi");
            loi_vao["moc_zh_dau"] = nen_cave + (ulong)e.lay_vi_tri("moc_zh_dau");
            loi_vao["moc_af_dau"] = nen_cave + (ulong)e.lay_vi_tri("moc_af_dau");
            loi_vao["moc_gf_dau"] = nen_cave + (ulong)e.lay_vi_tri("moc_gf_dau");
            loi_vao["moc_zh_vo"] = nen_cave + (ulong)e.lay_vi_tri("moc_zh_vo");
            loi_vao["moc_lf_mua"] = nen_cave + (ulong)e.lay_vi_tri("moc_lf_mua");
            loi_vao["moc_lf_tru1"] = nen_cave + (ulong)e.lay_vi_tri("moc_lf_tru1");
            loi_vao["moc_lf_tru2"] = nen_cave + (ulong)e.lay_vi_tri("moc_lf_tru2");
            loi_vao["moc_yf_mua"] = nen_cave + (ulong)e.lay_vi_tri("moc_yf_mua");
            loi_vao["moc_zf_tru"] = nen_cave + (ulong)e.lay_vi_tri("moc_zf_tru");
            loi_vao["moc_ui_thuong_nhan"] = nen_cave + (ulong)e.lay_vi_tri("moc_ui_thuong_nhan");
            loi_vao["moc_ui_co_vat"] = nen_cave + (ulong)e.lay_vi_tri("moc_ui_co_vat");
            loi_vao["moc_update"] = nen_cave + (ulong)e.lay_vi_tri("moc_update");
            return ma_x64;
        }

        // Tìm PID đầu tiên theo tên tiến trình
        private static int tim_pid_theo_ten(string ten_exe)
        {
            IntPtr snap = CreateToolhelp32Snapshot(co_snap_tien_trinh, 0);
            if (snap == IntPtr.Zero || snap == new IntPtr(-1))
            {
                return 0;
            }
            try
            {
                var muc = new muc_tien_trinh();
                muc.dwSize = (uint)Marshal.SizeOf(typeof(muc_tien_trinh));
                if (!Process32FirstW(snap, ref muc))
                {
                    return 0;
                }
                string dich = ten_exe.ToLowerInvariant();
                do
                {
                    if (muc.szExeFile != null && muc.szExeFile.ToLowerInvariant() == dich)
                    {
                        return (int)muc.th32ProcessID;
                    }
                }
                while (Process32NextW(snap, ref muc));
                return 0;
            }
            finally
            {
                CloseHandle(snap);
            }
        }

        // Tìm địa chỉ nền và kích thước module
        private static bool tim_module(int pid, string ten_mod, out ulong nen, out uint kich_thuoc)
        {
            nen = 0;
            kich_thuoc = 0;
            IntPtr snap = CreateToolhelp32Snapshot(co_snap_module | co_snap_module_32, (uint)pid);
            if (snap == IntPtr.Zero || snap == new IntPtr(-1))
            {
                return false;
            }
            try
            {
                var me = new muc_module();
                me.dwSize = (uint)Marshal.SizeOf(typeof(muc_module));
                if (!Module32FirstW(snap, ref me))
                {
                    return false;
                }
                string dich = ten_mod.ToLowerInvariant();
                do
                {
                    if (me.szModule != null && me.szModule.ToLowerInvariant() == dich)
                    {
                        nen = (ulong)me.modBaseAddr.ToInt64();
                        kich_thuoc = me.modBaseSize;
                        return true;
                    }
                }
                while (Module32NextW(snap, ref me));
                return false;
            }
            finally
            {
                CloseHandle(snap);
            }
        }

        // Kiểm tra tiến trình còn sống qua ExitCode
        private bool tien_trinh_con_song()
        {
            if (tay_cam == IntPtr.Zero || ma_pid == 0)
            {
                return false;
            }
            uint ma_thoat;
            if (!GetExitCodeProcess(tay_cam, out ma_thoat))
            {
                return false;
            }
            return ma_thoat == ma_dang_chay;
        }

        // Đọc bộ nhớ tiến trình theo handle truyền vào
        private static byte[] doc_mang_tu(IntPtr h, ulong dc, int kich_thuoc)
        {
            byte[] buf = new byte[kich_thuoc];
            UIntPtr da_doc;
            if (!ReadProcessMemory(h, new UIntPtr(dc), buf, new UIntPtr((uint)kich_thuoc), out da_doc) ||
                (int)da_doc.ToUInt64() != kich_thuoc)
            {
                throw new IOException("ReadProcessMemory that bai tai 0x" + dc.ToString("X"));
            }
            return buf;
        }

        // Đọc mảng byte từ tiến trình hiện tại
        private byte[] doc_mang(ulong dc, int kich_thuoc)
        {
            return doc_mang_tu(tay_cam, dc, kich_thuoc);
        }

        // Ghi bộ nhớ tiến trình theo handle truyền vào
        private static void ghi_mang_tu(IntPtr h, ulong dc, byte[] du_lieu, bool doi_quyen_rwx)
        {
            uint quyen_cu = 0;
            if (doi_quyen_rwx)
            {
                if (!VirtualProtectEx(h, new UIntPtr(dc), new UIntPtr((uint)du_lieu.Length), trang_rwx, out quyen_cu))
                {
                    throw new IOException("VirtualProtectEx that bai tai 0x" + dc.ToString("X"));
                }
            }
            try
            {
                UIntPtr da_ghi;
                if (!WriteProcessMemory(h, new UIntPtr(dc), du_lieu, new UIntPtr((uint)du_lieu.Length), out da_ghi) ||
                    (int)da_ghi.ToUInt64() != du_lieu.Length)
                {
                    throw new IOException("WriteProcessMemory that bai tai 0x" + dc.ToString("X"));
                }
            }
            finally
            {
                if (doi_quyen_rwx)
                {
                    uint bo_qua;
                    VirtualProtectEx(h, new UIntPtr(dc), new UIntPtr((uint)du_lieu.Length), quyen_cu, out bo_qua);
                }
            }
        }

        // Ghi mảng byte vào tiến trình hiện tại
        private void ghi_mang(ulong dc, byte[] du_lieu, bool doi_quyen_rwx)
        {
            ghi_mang_tu(tay_cam, dc, du_lieu, doi_quyen_rwx);
        }

        private byte doc_u8(ulong dc) { return doc_mang(dc, 1)[0]; }
        private uint doc_u32(ulong dc) { return BitConverter.ToUInt32(doc_mang(dc, 4), 0); }
        private int doc_i32(ulong dc) { return BitConverter.ToInt32(doc_mang(dc, 4), 0); }
        private ulong doc_u64(ulong dc) { return BitConverter.ToUInt64(doc_mang(dc, 8), 0); }
        private double doc_f64(ulong dc) { return BitConverter.ToDouble(doc_mang(dc, 8), 0); }

        private void ghi_u8(ulong dc, byte gt, bool rwx) { ghi_mang(dc, new byte[] { gt }, rwx); }
        private void ghi_u32(ulong dc, uint gt) { ghi_mang(dc, BitConverter.GetBytes(gt), false); }
        private void ghi_i32(ulong dc, int gt) { ghi_mang(dc, BitConverter.GetBytes(gt), false); }
        private void ghi_f64(ulong dc, double gt) { ghi_mang(dc, BitConverter.GetBytes(gt), false); }

        // Đóng tay cầm tiến trình hiện tại
        private void dong_tay_cam_noi_bo()
        {
            if (tay_cam != IntPtr.Zero)
            {
                CloseHandle(tay_cam);
                tay_cam = IntPtr.Zero;
            }
            ma_pid = 0;
            goc_ga = 0;
            kich_thuoc_ga = 0;
            goc_cave = 0;
            da_moc_flag = false;
        }

        // Đọc vùng nhớ liên tục bỏ qua trang lỗi
        private static byte[] doc_vung_tu(IntPtr h, ulong goc, int kich_thuoc)
        {
            byte[] buf = new byte[kich_thuoc];
            ulong hien_tai = goc;
            ulong ket_thuc = goc + (ulong)(uint)kich_thuoc;
            UIntPtr kich_mbi = new UIntPtr((uint)Marshal.SizeOf(typeof(thong_tin_bo_nho)));
            while (hien_tai < ket_thuc)
            {
                thong_tin_bo_nho mbi;
                if (VirtualQueryEx(h, new UIntPtr(hien_tai), out mbi, kich_mbi) == UIntPtr.Zero)
                {
                    hien_tai += 0x1000;
                    continue;
                }
                ulong v_bd = mbi.BaseAddress.ToUInt64();
                ulong v_kt = v_bd + mbi.RegionSize.ToUInt64();
                ulong bat_dau = Math.Max(hien_tai, v_bd);
                ulong ket_doan = Math.Min(ket_thuc, v_kt);
                if (mbi.State == mem_commit && (mbi.Protect & (trang_noaccess | trang_guard)) == 0 && ket_doan > bat_dau)
                {
                    int do_dai = (int)(ket_doan - bat_dau);
                    byte[] doan = doc_mang_tu(h, bat_dau, do_dai);
                    Buffer.BlockCopy(doan, 0, buf, (int)(bat_dau - goc), do_dai);
                }
                hien_tai = Math.Max(hien_tai + 0x1000, ket_doan);
            }
            return buf;
        }

        // Đọc PE header lấy vùng mã thực thi
        private static List<int[]> lay_cac_phan_thuc_thi(byte[] dau_pe, uint tong_ga, bool theo_file_tho)
        {
            var ds = new List<int[]>();
            if (dau_pe == null || dau_pe.Length < 0x40 || dau_pe[0] != 0x4D || dau_pe[1] != 0x5A)
            {
                return ds;
            }
            int pe_off = BitConverter.ToInt32(dau_pe, 0x3C);
            if (pe_off <= 0 || pe_off + 24 >= dau_pe.Length)
            {
                return ds;
            }
            if (BitConverter.ToInt32(dau_pe, pe_off) != 0x00004550)
            {
                return ds;
            }
            int so_phan = BitConverter.ToUInt16(dau_pe, pe_off + 6);
            int kich_opt = BitConverter.ToUInt16(dau_pe, pe_off + 20);
            int bang_phan = pe_off + 24 + kich_opt;
            for (int i = 0; i < so_phan; i++)
            {
                int o = bang_phan + i * 40;
                if (o + 40 > dau_pe.Length)
                {
                    break;
                }
                uint dac_tinh = BitConverter.ToUInt32(dau_pe, o + 36);
                if ((dac_tinh & 0x20000000u) == 0)
                {
                    continue;
                }
                uint vsize = BitConverter.ToUInt32(dau_pe, o + 8);
                uint vaddr = BitConverter.ToUInt32(dau_pe, o + 12);
                uint rsize = BitConverter.ToUInt32(dau_pe, o + 16);
                uint rptr = BitConverter.ToUInt32(dau_pe, o + 20);
                uint vi_tri = theo_file_tho ? rptr : vaddr;
                uint do_lon = theo_file_tho ? Math.Min(rsize, vsize) : vsize;
                if (do_lon > 0 && (ulong)vi_tri + do_lon <= tong_ga)
                {
                    ds.Add(new int[] { (int)vi_tri, (int)do_lon, (int)vaddr });
                }
            }
            return ds;
        }

        // Quét 37 AOB trong 1 lượt qua vùng thực thi
        private static void quet_nhanh_34_aob(byte[] vung, int bat_dau, int len, int nen_dich, Dictionary<string, int> ket_qua, Dictionary<string, int> so_khop)
        {
            int gioi_han = bat_dau + len - 2;
            mau_aob_muc[][] nhay = bang_nhay_2byte;
            for (int i = bat_dau; i <= gioi_han; i++)
            {
                mau_aob_muc[] nhom = nhay[vung[i] | (vung[i + 1] << 8)];
                if (nhom == null)
                {
                    continue;
                }
                int con_lai = bat_dau + len - i;
                for (int k = 0; k < nhom.Length; k++)
                {
                    mau_aob_muc muc = nhom[k];
                    byte[] g = muc.mau.gia_tri;
                    if (g.Length > con_lai)
                    {
                        continue;
                    }
                    bool[] m = muc.mau.mat_na;
                    bool khop = true;
                    for (int j = 2; j < g.Length; j++)
                    {
                        if (m[j] && vung[i + j] != g[j])
                        {
                            khop = false;
                            break;
                        }
                    }
                    if (khop)
                    {
                        ket_qua[muc.ten] = nen_dich + (i - bat_dau);
                        int dem;
                        so_khop.TryGetValue(muc.ten, out dem);
                        so_khop[muc.ten] = dem + 1;
                    }
                }
            }
        }

        // Quét 37 AOB và giải mã địa chỉ mục tiêu
        private static Dictionary<string, ulong> quet_tat_ca_aob_tu(IntPtr h, ulong ga_base, uint ga_size)
        {
            byte[] dau_pe = doc_mang_tu(h, ga_base, (int)Math.Min(4096u, ga_size));
            List<int[]> phan_exec = lay_cac_phan_thuc_thi(dau_pe, ga_size, false);
            if (phan_exec.Count == 0)
            {
                phan_exec.Add(new int[] { 0, (int)ga_size, 0 });
            }

            var rva_tim_duoc = new Dictionary<string, int>();
            var so_khop = new Dictionary<string, int>();
            for (int p = 0; p < phan_exec.Count; p++)
            {
                int rva = phan_exec[p][0];
                int len = phan_exec[p][1];
                byte[] du_lieu = doc_vung_tu(h, ga_base + (ulong)(uint)rva, len);
                quet_nhanh_34_aob(du_lieu, 0, len, rva, rva_tim_duoc, so_khop);
            }

            var kq = new Dictionary<string, ulong>();
            foreach (KeyValuePair<string, mau_aob> cap in bang_aob)
            {
                int dem;
                so_khop.TryGetValue(cap.Key, out dem);
                if (dem != 1)
                {
                    throw new InvalidOperationException("AOB " + cap.Key + " khop " + dem + " vi tri");
                }
                kq[cap.Key] = (ulong)((long)ga_base + rva_tim_duoc[cap.Key] + cap.Value.do_lech);
            }

            ulong xc_inst = kq["xc_instantiate"];
            kq["p_instantiate_mi_addr"] = (ulong)((long)xc_inst + 7 + BitConverter.ToInt32(doc_mang_tu(h, xc_inst + 3, 4), 0));
            kq["fn_instantiate"] = (ulong)((long)xc_inst + 25 + BitConverter.ToInt32(doc_mang_tu(h, xc_inst + 21, 4), 0));

            ulong xc_list = kq["xc_list_add"];
            kq["p_list_add_mi_root_addr"] = (ulong)((long)xc_list + 23 + BitConverter.ToInt32(doc_mang_tu(h, xc_list + 19, 4), 0));
            kq["fn_list_add_resize"] = (ulong)((long)xc_list + 0x48 + BitConverter.ToInt32(doc_mang_tu(h, xc_list + 0x44, 4), 0));

            ulong m_del = kq["m_delegate_ctx"];
            kq["p_m_del_cls_addr"] = (ulong)((long)m_del + 7 + BitConverter.ToInt32(doc_mang_tu(h, m_del + 3, 4), 0));

            return kq;
        }

        // Mô tả điểm móc detour cố định một lần
        private sealed class muc_moc_co_dinh
        {
            public readonly string ten_aob;
            public readonly string ten_loi_vao;
            public readonly byte[] byte_goc;

            public muc_moc_co_dinh(string aob, string vao, byte[] goc)
            {
                ten_aob = aob;
                ten_loi_vao = vao;
                byte_goc = goc;
            }
        }

        private static readonly muc_moc_co_dinh[] danh_sach_moc = new muc_moc_co_dinh[]
        {
            new muc_moc_co_dinh("cc_anchor", "moc_cc_cuoi", goc_cc),
            new muc_moc_co_dinh("zh_anchor", "moc_zh_dau", goc_zh),
            new muc_moc_co_dinh("af_anchor", "moc_af_dau", goc_af),
            new muc_moc_co_dinh("gf_anchor", "moc_gf_dau", goc_gf),
            new muc_moc_co_dinh("zh_break_ctx", "moc_zh_vo", goc_zh_vo),
            new muc_moc_co_dinh("lf_afford", "moc_lf_mua", goc_lf_mua),
            new muc_moc_co_dinh("lf_ded1", "moc_lf_tru1", goc_lf_tru1),
            new muc_moc_co_dinh("lf_ded2", "moc_lf_tru2", goc_lf_tru2),
            new muc_moc_co_dinh("Yf_afford", "moc_yf_mua", goc_yf_mua),
            new muc_moc_co_dinh("zf_deduct", "moc_zf_tru", goc_zf_tru),
            new muc_moc_co_dinh("ui_thuong_nhan", "moc_ui_thuong_nhan", goc_ui_tn),
            new muc_moc_co_dinh("ui_co_vat", "moc_ui_co_vat", goc_ui_cv),
            new muc_moc_co_dinh("update_anchor", "moc_update", goc_update)
        };

        // Gỡ các vá mã v3 cũ để nâng cấp lên v4
        private static byte[] go_bo_v3_va_lay_co(IntPtr h, ulong cave_cu, Dictionary<string, ulong> kq_aob)
        {
            byte[] co_cu = doc_mang_tu(h, cave_cu + o_khong_pha_huy, 16);
            ghi_mang_tu(h, cave_cu + o_khong_pha_huy, new byte[16], false);
            ghi_mang_tu(h, kq_aob["zh_break_ctx"] + 10, new byte[] { 0x01 }, true);
            ghi_mang_tu(h, kq_aob["lf_afford"] + 21, new byte[] { 0x77, 0x2A }, true);
            byte[] sub_cred = new byte[] { 0x29, 0x86, 0x0C, 0x01, 0x00, 0x00 };
            ghi_mang_tu(h, kq_aob["lf_ded1"] + 5, sub_cred, true);
            ghi_mang_tu(h, kq_aob["lf_ded2"] + 5, sub_cred, true);
            ghi_mang_tu(h, kq_aob["Yf_afford"] + 13, new byte[] { 0x0F, 0x86, 0x1D, 0x05, 0x00, 0x00 }, true);
            ghi_mang_tu(h, kq_aob["zf_deduct"] + 10, new byte[] { 0xF2, 0x0F, 0x5C, 0x47, 0x18 }, true);

            ghi_mang_tu(h, kq_aob["update_anchor"], goc_update, true);
            ghi_mang_tu(h, kq_aob["cc_anchor"], goc_cc, true);
            ghi_mang_tu(h, kq_aob["zh_anchor"], goc_zh, true);
            ghi_mang_tu(h, kq_aob["af_anchor"], goc_af, true);
            ghi_mang_tu(h, kq_aob["gf_anchor"], goc_gf, true);
            Thread.Sleep(55);
            return co_cu;
        }

        // Gắn vào ForgeTax.exe và dựng hook v4
        public ket_qua_thao_tac dam_bao_da_moc()
        {
            lock (khoa_dong_bo)
            {
                if (da_moc_flag && tien_trinh_con_song())
                {
                    return new ket_qua_thao_tac(true, "ok", false);
                }
                if (tay_cam != IntPtr.Zero && !tien_trinh_con_song())
                {
                    dong_tay_cam_noi_bo();
                }
            }

            int pid = tim_pid_theo_ten("ForgeTax.exe");
            if (pid == 0)
            {
                return new ket_qua_thao_tac(false, "mo_tro_choi", false);
            }

            IntPtr h = OpenProcess(quyen_doc_ghi, false, (uint)pid);
            if (h == IntPtr.Zero)
            {
                int loi = Marshal.GetLastWin32Error();
                return new ket_qua_thao_tac(false, "thieu_admin", loi == loi_tu_choi_truy_cap);
            }

            ulong ga_base;
            uint ga_size;
            if (!tim_module(pid, "GameAssembly.dll", out ga_base, out ga_size))
            {
                CloseHandle(h);
                return new ket_qua_thao_tac(false, "dang_tai", false);
            }

            try
            {
                Dictionary<string, ulong> kq_aob;
                if (pid_da_quet == pid && ga_da_quet == ga_base && aob_da_quet != null)
                {
                    kq_aob = aob_da_quet;
                }
                else
                {
                    kq_aob = quet_tat_ca_aob_tu(h, ga_base, ga_size);
                    pid_da_quet = pid;
                    ga_da_quet = ga_base;
                    aob_da_quet = kq_aob;
                    pid_khong_tuong_thich = 0;
                }

                ulong va_upd = kq_aob["update_anchor"];
                byte[] cur_upd = doc_mang_tu(h, va_upd, 16);
                byte[] co_ke_thua = null;

                if (cur_upd[0] == 0xFF && cur_upd[1] == 0x25 && cur_upd[2] == 0x00 && cur_upd[3] == 0x00)
                {
                    ulong entry_va = BitConverter.ToUInt64(cur_upd, 6);
                    ulong trang = entry_va & ~0xFFFUL;
                    ulong[] ung_vien = new ulong[] { trang - 0x1000UL, trang };
                    for (int i = 0; i < ung_vien.Length; i++)
                    {
                        try
                        {
                            byte[] mg = doc_mang_tu(h, ung_vien[i] + o_dau_hieu, dau_hieu_v4.Length);
                            if (so_sanh_mang(mg, dau_hieu_v4))
                            {
                                lock (khoa_dong_bo)
                                {
                                    dong_tay_cam_noi_bo();
                                    tay_cam = h;
                                    ma_pid = pid;
                                    goc_ga = ga_base;
                                    kich_thuoc_ga = ga_size;
                                    dia_chi = kq_aob;
                                    goc_cave = ung_vien[i];
                                    tu_tao_cave = false;
                                    da_moc_flag = true;
                                }
                                pid_khong_tuong_thich = 0;
                                GC.Collect(2, GCCollectionMode.Optimized);
                                return new ket_qua_thao_tac(true, "ok", false);
                            }
                            if (so_sanh_mang(mg, dau_hieu_v3))
                            {
                                co_ke_thua = go_bo_v3_va_lay_co(h, ung_vien[i], kq_aob);
                                cur_upd = doc_mang_tu(h, va_upd, 16);
                                break;
                            }
                        }
                        catch
                        {
                        }
                    }
                    if (cur_upd[0] == 0xFF && cur_upd[1] == 0x25 && cur_upd[2] == 0x00 && cur_upd[3] == 0x00)
                    {
                        pid_khong_tuong_thich = pid;
                        CloseHandle(h);
                        return new ket_qua_thao_tac(false, "dang_tai", false);
                    }
                }

                if (!so_sanh_mang(cur_upd, goc_update))
                {
                    pid_khong_tuong_thich = pid;
                    CloseHandle(h);
                    return new ket_qua_thao_tac(false, "dang_tai", false);
                }

                pid_khong_tuong_thich = 0;
                lock (khoa_dong_bo)
                {
                    dong_tay_cam_noi_bo();
                    tay_cam = h;
                    ma_pid = pid;
                    goc_ga = ga_base;
                    kich_thuoc_ga = ga_size;
                    dia_chi = kq_aob;
                    cai_dat_moc_noi_bo();
                    if (co_ke_thua != null)
                    {
                        ghi_mang(goc_cave + o_khong_pha_huy, co_ke_thua, false);
                        ghi_u32(goc_cave + o_co_lam_moi_hud, 1u);
                    }
                }
                GC.Collect(2, GCCollectionMode.Optimized);
                return new ket_qua_thao_tac(true, "ok", false);
            }
            catch
            {
                CloseHandle(h);
                return new ket_qua_thao_tac(false, "dang_tai", false);
            }
        }

        // Cấp phát cave v4 và gắn 13 detour cố định
        private void cai_dat_moc_noi_bo()
        {
            for (int i = 0; i < danh_sach_moc.Length; i++)
            {
                muc_moc_co_dinh moc = danh_sach_moc[i];
                byte[] cur = doc_mang(dia_chi[moc.ten_aob], moc.byte_goc.Length);
                if (!so_sanh_mang(cur, moc.byte_goc))
                {
                    throw new InvalidOperationException("Byte goc khong khop tai " + moc.ten_aob);
                }
            }

            ulong va_upd = dia_chi["update_anchor"];
            ulong va_cc = dia_chi["cc_anchor"];
            ulong va_zh = dia_chi["zh_anchor"];
            ulong va_af = dia_chi["af_anchor"];
            ulong va_gf = dia_chi["gf_anchor"];
            ulong va_zh_vo = dia_chi["zh_break_ctx"];
            ulong va_lf_mua = dia_chi["lf_afford"];
            ulong va_lf_tru1 = dia_chi["lf_ded1"];
            ulong va_lf_tru2 = dia_chi["lf_ded2"];
            ulong va_yf_mua = dia_chi["Yf_afford"];
            ulong va_zf_tru = dia_chi["zf_deduct"];
            ulong va_ui_tn = dia_chi["ui_thuong_nhan"];
            ulong va_ui_cv = dia_chi["ui_co_vat"];

            ulong p_af_init = (ulong)((long)va_af + 13 + doc_i32(va_af + 8));
            ulong p_gf_init = (ulong)((long)va_gf + 16 + doc_i32(va_gf + 11));

            UIntPtr ptr = VirtualAllocEx(tay_cam, UIntPtr.Zero, new UIntPtr(kich_thuoc_cave), mem_commit | mem_reserve, trang_rwx);
            if (ptr == UIntPtr.Zero)
            {
                throw new IOException("VirtualAllocEx that bai");
            }
            goc_cave = ptr.ToUInt64();
            tu_tao_cave = true;

            Dictionary<string, ulong> loi_vao;
            byte[] ma_x64 = dung_vung_ma(goc_cave, out loi_vao);

            byte[] trang_thai = new byte[vi_tri_ma_cave];
            Buffer.BlockCopy(dau_hieu_v4, 0, trang_thai, o_dau_hieu, dau_hieu_v4.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(0.5), 0, trang_thai, o_hang_so_nua, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(0.17f), 0, trang_thai, o_toa_do_du_phong, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(1.15f), 0, trang_thai, o_toa_do_du_phong + 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(0.22f), 0, trang_thai, o_toa_do_du_phong + 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(loi_vao["ham_gia_double"]), 0, trang_thai, o_ham_gia_double + 0x18, 8);

            ghi_qword_mang(trang_thai, o_moc_update_va, va_upd);
            ghi_qword_mang(trang_thai, o_moc_cc_va, va_cc);
            ghi_qword_mang(trang_thai, o_moc_zh_va, va_zh);
            ghi_qword_mang(trang_thai, o_moc_af_va, va_af);
            ghi_qword_mang(trang_thai, o_moc_gf_va, va_gf);

            var gia_tri_o = new Dictionary<string, ulong>();
            gia_tri_o["fn_instantiate"] = dia_chi["fn_instantiate"];
            gia_tri_o["p_instantiate_mi_addr"] = dia_chi["p_instantiate_mi_addr"];
            gia_tri_o["fn_cd"] = dia_chi["fn_cd"];
            gia_tri_o["fn_tr"] = dia_chi["fn_tr"];
            gia_tri_o["fn_wB"] = dia_chi["fn_wB"];
            gia_tri_o["fn_hf"] = dia_chi["fn_hf"];
            gia_tri_o["fn_TH"] = dia_chi["fn_TH"];
            gia_tri_o["fn_VO"] = dia_chi["fn_VO"];
            gia_tri_o["fn_ur"] = dia_chi["fn_ur"];
            gia_tri_o["p_list_add_mi_root_addr"] = dia_chi["p_list_add_mi_root_addr"];
            gia_tri_o["fn_list_add_resize"] = dia_chi["fn_list_add_resize"];
            gia_tri_o["fn_wbarrier"] = dia_chi["fn_wbarrier"];
            gia_tri_o["fn_Bo"] = dia_chi["fn_Bo"];
            gia_tri_o["fn_oE"] = dia_chi["fn_oE"];
            gia_tri_o["fn_Xd"] = dia_chi["fn_Xd"];
            gia_tri_o["fn_Uc"] = dia_chi["fn_Uc"];
            gia_tri_o["fn_pC"] = dia_chi["fn_pC"];
            gia_tri_o["fn_UB"] = dia_chi["fn_UB"];
            gia_tri_o["fn_zH"] = va_zh;
            gia_tri_o["p_m_del_cls_addr"] = dia_chi["p_m_del_cls_addr"];
            gia_tri_o["p_af_init_addr"] = p_af_init;
            gia_tri_o["p_gf_init_addr"] = p_gf_init;
            gia_tri_o["fn_get_pos"] = dia_chi["fn_get_pos"];
            gia_tri_o["fn_set_pos"] = dia_chi["fn_set_pos"];
            gia_tri_o["fn_get_go"] = dia_chi["fn_get_go"];
            gia_tri_o["fn_get_tr"] = dia_chi["fn_get_tr"];
            gia_tri_o["fn_set_active"] = dia_chi["fn_set_active"];
            gia_tri_o["fn_init_meta"] = dia_chi["fn_init_meta"];
            gia_tri_o["fn_gia_co_vat"] = dia_chi["fn_gia_co_vat"];
            gia_tri_o["ret_update_va"] = va_upd + 16;
            gia_tri_o["ret_cc_va"] = va_cc + 15;
            gia_tri_o["ret_zh_va"] = va_zh + 14;
            gia_tri_o["ret_af_va"] = va_af + 16;
            gia_tri_o["ret_gf_va"] = va_gf + 16;
            gia_tri_o["ret_zh_vo_va"] = va_zh_vo + 19;
            gia_tri_o["ret_lf_mua_ok_va"] = va_lf_mua + 23;
            gia_tri_o["ret_lf_mua_thieu_va"] = va_lf_mua + 23 + 0x2A;
            gia_tri_o["ret_lf_tru1_va"] = va_lf_tru1 + 15;
            gia_tri_o["ret_lf_tru2_va"] = va_lf_tru2 + 15;
            gia_tri_o["ret_yf_mua_thieu_va"] = va_yf_mua + 19;
            gia_tri_o["ret_yf_mua_ok_va"] = va_yf_mua + 19 + 0x51D;
            gia_tri_o["ret_zf_tru_va"] = va_zf_tru + 26;
            gia_tri_o["ret_ui_tn_va"] = va_ui_tn + 19;
            gia_tri_o["ret_ui_cv_va"] = va_ui_cv + 31;

            foreach (KeyValuePair<string, ulong> cap in gia_tri_o)
            {
                ghi_qword_mang(trang_thai, bang_o_ham[cap.Key], cap.Value);
            }

            ghi_mang(goc_cave, trang_thai, false);
            ghi_mang(goc_cave + vi_tri_ma_cave, ma_x64, false);

            for (int i = 0; i < danh_sach_moc.Length; i++)
            {
                muc_moc_co_dinh moc = danh_sach_moc[i];
                ghi_mang(dia_chi[moc.ten_aob], tao_nhay_tuyet_doi(loi_vao[moc.ten_loi_vao], moc.byte_goc.Length), true);
            }

            da_moc_flag = true;
        }

        // Thu hồi cờ F1..F4 và gỡ toàn bộ 13 detour
        public void go_bo_moc_em_dep()
        {
            lock (khoa_dong_bo)
            {
                if (tay_cam == IntPtr.Zero || goc_cave == 0 || !da_moc_flag)
                {
                    dong_tay_cam_noi_bo();
                    return;
                }
                if (!tien_trinh_con_song())
                {
                    dong_tay_cam_noi_bo();
                    return;
                }
                try
                {
                    ghi_u32(goc_cave + o_khong_pha_huy, 0);
                    ghi_u32(goc_cave + o_khong_hu_ton, 0);
                    ghi_u32(goc_cave + o_khong_tinh_tien, 0);
                    ghi_u32(goc_cave + o_mien_cong_nap, 0);

                    // Làm mới HUD đe và rồng trước khi gỡ hook
                    ghi_u32(goc_cave + o_co_lam_moi_hud, 1);
                    Thread.Sleep(55);

                    for (int i = danh_sach_moc.Length - 1; i >= 0; i--)
                    {
                        muc_moc_co_dinh moc = danh_sach_moc[i];
                        ulong va;
                        if (dia_chi.TryGetValue(moc.ten_aob, out va) && va != 0)
                        {
                            ghi_mang(va, moc.byte_goc, true);
                        }
                    }

                    if (tu_tao_cave)
                    {
                        Thread.Sleep(45);
                        VirtualFreeEx(tay_cam, new UIntPtr(goc_cave), UIntPtr.Zero, mem_release);
                    }
                }
                catch
                {
                }
                finally
                {
                    tu_tao_cave = false;
                    dong_tay_cam_noi_bo();
                }
            }
        }

        // Đặt lại kết nối khi bấm Kết nối lại
        public ket_qua_thao_tac ket_noi_lai()
        {
            lock (khoa_dong_bo)
            {
                dong_tay_cam_noi_bo();
            }
            pid_da_quet = 0;
            ga_da_quet = 0;
            aob_da_quet = null;
            pid_khong_tuong_thich = 0;
            return dam_bao_da_moc();
        }

        // Đọc ảnh chụp trạng thái cập nhật UI
        public ban_ghi_trang_thai doc_trang_thai()
        {
            var kq = new ban_ghi_trang_thai();
            lock (khoa_dong_bo)
            {
                if (!da_moc_flag || tay_cam == IntPtr.Zero || goc_cave == 0)
                {
                    return kq;
                }
                if (!tien_trinh_con_song())
                {
                    dong_tay_cam_noi_bo();
                    return kq;
                }
                kq.da_ket_noi = true;
                kq.ma_tien_trinh = ma_pid;
                kq.da_moc = true;
                try
                {
                    UIntPtr da_doc;
                    if (!ReadProcessMemory(tay_cam, new UIntPtr(goc_cave), bo_dem_trang_thai, new UIntPtr(64u), out da_doc) ||
                        (int)da_doc.ToUInt64() != 64)
                    {
                        throw new IOException("Doc trang thai cave that bai");
                    }
                    kq.khong_pha_huy = BitConverter.ToUInt32(bo_dem_trang_thai, o_khong_pha_huy) != 0;
                    kq.khong_hu_ton = BitConverter.ToUInt32(bo_dem_trang_thai, o_khong_hu_ton) != 0;
                    kq.khong_tinh_tien = BitConverter.ToUInt32(bo_dem_trang_thai, o_khong_tinh_tien) != 0;
                    kq.mien_cong_nap = BitConverter.ToUInt32(bo_dem_trang_thai, o_mien_cong_nap) != 0;

                    ulong phong = BitConverter.ToUInt64(bo_dem_trang_thai, o_phong_ren);
                    if (phong == 0)
                    {
                        kq.o_man_hinh_chinh = true;
                        return kq;
                    }
                    bool dang_title = doc_u8(phong + 0x490) != 0;
                    kq.o_man_hinh_chinh = dang_title;
                    ulong chien_dich = doc_u64(phong + 0x0F0);
                    ulong ds_kiem = doc_u64(phong + 0x3C8);
                    kq.trong_man_choi = (chien_dich != 0 && ds_kiem != 0 && !dang_title);
                    if (chien_dich != 0)
                    {
                        kq.vang = doc_f64(chien_dich + 0x128);
                        kq.luot_credits = doc_i32(chien_dich + 0x10C);
                    }
                    ulong kiem_de = doc_u64(phong + 0x3B0);
                    ulong kiem_tay = doc_u64(phong + 0x398);
                    ulong sw = kiem_de != 0 ? kiem_de : kiem_tay;
                    if (sw != 0)
                    {
                        kq.co_kiem_de = true;
                        kq.cap_kiem_de = doc_i32(sw + 0x50);
                    }
                }
                catch
                {
                    dong_tay_cam_noi_bo();
                    return new ban_ghi_trang_thai();
                }
            }
            return kq;
        }

        // Quy đổi tên công tắc sang độ lệch cờ
        private static int lay_do_lech_cong_tac(string ten)
        {
            if (ten == "f1") return o_khong_pha_huy;
            if (ten == "f2") return o_khong_hu_ton;
            if (ten == "f3") return o_khong_tinh_tien;
            if (ten == "f4") return o_mien_cong_nap;
            return -1;
        }

        // Ghi thẳng 4 byte công tắc vào RWX cave
        public bool dat_cong_tac_nhanh(string ten, bool bat)
        {
            int lech = lay_do_lech_cong_tac(ten);
            if (lech < 0) return false;
            lock (khoa_dong_bo)
            {
                if (!da_moc_flag || goc_cave == 0 || tay_cam == IntPtr.Zero)
                {
                    return false;
                }
                try
                {
                    UIntPtr da_ghi;
                    byte[] du_lieu = bat ? mang_bat : mang_tat;
                    if (!WriteProcessMemory(tay_cam, new UIntPtr(goc_cave + (ulong)lech), du_lieu, new UIntPtr(4u), out da_ghi) ||
                        (int)da_ghi.ToUInt64() != 4)
                    {
                        dong_tay_cam_noi_bo();
                        return false;
                    }
                    WriteProcessMemory(tay_cam, new UIntPtr(goc_cave + o_co_lam_moi_hud), mang_bat, new UIntPtr(4u), out da_ghi);
                    return true;
                }
                catch
                {
                    dong_tay_cam_noi_bo();
                    return false;
                }
            }
        }

        // Bật hoặc đảo công tắc F1..F4 theo khóa
        public bool dao_cong_tac(string ten)
        {
            int lech = lay_do_lech_cong_tac(ten);
            if (lech < 0) return false;
            bool moi;
            lock (khoa_dong_bo)
            {
                if (!da_moc_flag || goc_cave == 0 || !tien_trinh_con_song())
                {
                    return false;
                }
                try
                {
                    moi = doc_u32(goc_cave + (ulong)lech) == 0;
                }
                catch
                {
                    dong_tay_cam_noi_bo();
                    return false;
                }
            }
            return dat_cong_tac_nhanh(ten, moi) && moi;
        }

        // Gửi lệnh triệu hồi 1 kiếm +0
        public void kich_hoat_f5_trieu_hoi()
        {
            lock (khoa_dong_bo)
            {
                if (!kiem_tra_man_choi_nhanh()) return;
                uint cur = doc_u32(goc_cave + o_so_kiem_trieu_hoi);
                ghi_u32(goc_cave + o_so_kiem_trieu_hoi, cur + 1u);
            }
        }

        // Gửi lệnh hồi tố thời gian
        public void kich_hoat_f6_hoi_to()
        {
            lock (khoa_dong_bo)
            {
                if (!kiem_tra_man_choi_nhanh()) return;
                ghi_u32(goc_cave + o_co_hoi_to, 1u);
            }
        }

        // Gửi lệnh nâng cấp (+1) hoặc (+2)
        public void kich_hoat_nang_cap(int ket_qua)
        {
            lock (khoa_dong_bo)
            {
                if (!kiem_tra_man_choi_nhanh()) return;
                ghi_u32(goc_cave + o_ket_qua_nang_cap, (uint)ket_qua);
            }
        }

        // Gửi lệnh tặng Vàng và Credits vào RAM
        public void kich_hoat_tang_tien(double vang, double credits)
        {
            lock (khoa_dong_bo)
            {
                if (!kiem_tra_man_choi_nhanh()) return;
                double v_an_toan = Math.Min(Math.Max(0.0, vang), 1e300);
                double c_an_toan = Math.Min(Math.Max(0.0, Math.Round(credits)), 2000000000.0);
                ghi_f64(goc_cave + o_them_vang, v_an_toan);
                ghi_i32(goc_cave + o_them_credits, (int)c_an_toan);
                ghi_u32(goc_cave + o_co_tang_tien, 1u);
            }
        }

        // Kiểm tra nhanh đang ở trong màn chơi
        private bool kiem_tra_man_choi_nhanh()
        {
            if (!da_moc_flag || goc_cave == 0 || tay_cam == IntPtr.Zero) return false;
            try
            {
                ulong phong = doc_u64(goc_cave + o_phong_ren);
                if (phong == 0) return false;
                if (doc_u8(phong + 0x490) != 0) return false;
                return doc_u64(phong + 0x0F0) != 0 && doc_u64(phong + 0x3C8) != 0;
            }
            catch
            {
                dong_tay_cam_noi_bo();
                return false;
            }
        }

        private static void ghi_qword_mang(byte[] dich, int vi_tri, ulong gt)
        {
            byte[] b = BitConverter.GetBytes(gt);
            Buffer.BlockCopy(b, 0, dich, vi_tri, 8);
        }

        private static bool so_sanh_mang(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        // Tự kiểm tra cave, 37 AOB và 13 byte gốc
        public static string tu_kiem_tra_offline(string duong_dan_dll)
        {
            Dictionary<string, ulong> loi_vao;
            byte[] ma = dung_vung_ma(0x10000000UL, out loi_vao);
            if (ma.Length >= kich_thuoc_cave - vi_tri_ma_cave)
            {
                throw new InvalidOperationException("Code cave vuot qua kich thuoc");
            }
            if (!File.Exists(duong_dan_dll))
            {
                throw new FileNotFoundException(duong_dan_dll);
            }
            byte[] anh = File.ReadAllBytes(duong_dan_dll);
            List<int[]> phan_exec = lay_cac_phan_thuc_thi(anh, (uint)anh.Length, true);
            if (phan_exec.Count == 0)
            {
                phan_exec.Add(new int[] { 0, anh.Length, 0 });
            }
            var pos_tim_duoc = new Dictionary<string, int>();
            var so_khop = new Dictionary<string, int>();
            for (int p = 0; p < phan_exec.Count; p++)
            {
                quet_nhanh_34_aob(anh, phan_exec[p][0], phan_exec[p][1], phan_exec[p][0], pos_tim_duoc, so_khop);
            }
            foreach (KeyValuePair<string, mau_aob> cap in bang_aob)
            {
                int dem;
                so_khop.TryGetValue(cap.Key, out dem);
                if (dem != 1)
                {
                    throw new InvalidOperationException("AOB " + cap.Key + " khop " + dem + " vi tri");
                }
            }
            for (int m = 0; m < danh_sach_moc.Length; m++)
            {
                muc_moc_co_dinh moc = danh_sach_moc[m];
                int vi_tri_goc = pos_tim_duoc[moc.ten_aob] + bang_aob[moc.ten_aob].do_lech;
                for (int i = 0; i < moc.byte_goc.Length; i++)
                {
                    if (anh[vi_tri_goc + i] != moc.byte_goc[i])
                    {
                        throw new InvalidOperationException("Sai byte goc " + moc.ten_aob);
                    }
                }
            }
            return string.Format("Kiem tra OK: cave={0} bytes, AOB={1} duy nhat, 13 hook khop", ma.Length, bang_aob.Count);
        }

        // Giải phóng tài nguyên động cơ
        public void Dispose()
        {
            go_bo_moc_em_dep();
        }
    }
}
