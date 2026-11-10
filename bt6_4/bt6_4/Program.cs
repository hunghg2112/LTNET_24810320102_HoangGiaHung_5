using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace GarageManagementApp
{
    #region ==================== 1. DTO LAYER ====================
    public class KhachHangDTO
    {
        public int MaKH { get; set; }
        public string TenKH { get; set; }
        public string DienThoai { get; set; }
        public string DiaChi { get; set; }
    }

    public class XeDTO
    {
        public string BienSo { get; set; }
        public int MaKH { get; set; }
        public string TenKH { get; set; }
        public string DienThoai { get; set; }
        public string HieuXe { get; set; }
        public int NamSanXuat { get; set; }
    }

    public class PhuTungDTO
    {
        public int MaPhuTung { get; set; }
        public string TenPhuTung { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuongTon { get; set; }
    }

    public class PhieuSuaChuaDTO
    {
        public int MaPhieu { get; set; }
        public string BienSo { get; set; }
        public string TenKH { get; set; }
        public string HieuXe { get; set; }
        public DateTime NgayNhan { get; set; }
        public string NoiDungSua { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
    }

    public class ChiTietSuaChuaDTO
    {
        public int MaPhieu { get; set; }
        public int MaPhuTung { get; set; }
        public string TenPhuTung { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal TienCong { get; set; }
        public decimal ThanhTien => (SoLuong * DonGia) + TienCong;
    }
    #endregion

    #region ==================== 2. DAL LAYER ====================
    public class DatabaseAccess
    {
        protected static string connectionString = @"Server=localhost\SQLEXPRESS;Database=GarageDB;Integrated Security=True;TrustServerCertificate=True;";
    }

    public class KhachHangDAL : DatabaseAccess
    {
        public List<KhachHangDTO> GetAll()
        {
            List<KhachHangDTO> list = new List<KhachHangDTO>();
            string query = "SELECT MaKH, TenKH, DienThoai, DiaChi FROM KhachHang";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new KhachHangDTO
                            {
                                MaKH = (int)r["MaKH"],
                                TenKH = r["TenKH"].ToString(),
                                DienThoai = r["DienThoai"].ToString(),
                                DiaChi = r["DiaChi"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public int Insert(KhachHangDTO kh)
        {
            string query = @"INSERT INTO KhachHang (TenKH, DienThoai, DiaChi) 
                            VALUES (@TenKH, @DienThoai, @DiaChi);
                            SELECT SCOPE_IDENTITY();";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenKH", kh.TenKH);
                    cmd.Parameters.AddWithValue("@DienThoai", kh.DienThoai);
                    cmd.Parameters.AddWithValue("@DiaChi", (object)kh.DiaChi ?? DBNull.Value);
                    conn.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }

    public class XeDAL : DatabaseAccess
    {
        public DataTable GetAllXeWithCustomer()
        {
            string query = @"SELECT x.BienSo AS [Biển số], kh.TenKH AS [Chủ xe], kh.DienThoai AS [SĐT], 
                                   x.HieuXe AS [Hiệu xe], x.NamSanXuat AS [Năm SX], x.MaKH
                            FROM Xe x
                            JOIN KhachHang kh ON x.MaKH = kh.MaKH";
            using (SqlDataAdapter da = new SqlDataAdapter(query, connectionString))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Tra cứu biển số xe 100% bằng SqlParameter
        public XeDTO GetByBienSo(string bienSo)
        {
            string query = @"SELECT x.BienSo, x.MaKH, kh.TenKH, kh.DienThoai, x.HieuXe, x.NamSanXuat
                            FROM Xe x
                            JOIN KhachHang kh ON x.MaKH = kh.MaKH
                            WHERE x.BienSo = @BienSo";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BienSo", bienSo);
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            return new XeDTO
                            {
                                BienSo = r["BienSo"].ToString(),
                                MaKH = (int)r["MaKH"],
                                TenKH = r["TenKH"].ToString(),
                                DienThoai = r["DienThoai"].ToString(),
                                HieuXe = r["HieuXe"].ToString(),
                                NamSanXuat = Convert.ToInt32(r["NamSanXuat"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool Insert(XeDTO xe)
        {
            string query = "INSERT INTO Xe (BienSo, MaKH, HieuXe, NamSanXuat) VALUES (@BienSo, @MaKH, @HieuXe, @NamSanXuat)";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@BienSo", xe.BienSo);
                    cmd.Parameters.AddWithValue("@MaKH", xe.MaKH);
                    cmd.Parameters.AddWithValue("@HieuXe", xe.HieuXe);
                    cmd.Parameters.AddWithValue("@NamSanXuat", xe.NamSanXuat);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }

    public class PhuTungDAL : DatabaseAccess
    {
        public List<PhuTungDTO> GetAll()
        {
            List<PhuTungDTO> list = new List<PhuTungDTO>();
            string query = "SELECT MaPhuTung, TenPhuTung, DonGia, SoLuongTon FROM PhuTung";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new PhuTungDTO
                            {
                                MaPhuTung = (int)r["MaPhuTung"],
                                TenPhuTung = r["TenPhuTung"].ToString(),
                                DonGia = Convert.ToDecimal(r["DonGia"]),
                                SoLuongTon = Convert.ToInt32(r["SoLuongTon"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        public DataTable Search(string keyword)
        {
            string query = "SELECT MaPhuTung AS [Mã PT], TenPhuTung AS [Tên phụ tùng], DonGia AS [Đơn giá], SoLuongTon AS [Tồn kho] FROM PhuTung WHERE TenPhuTung LIKE @Keyword";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Keyword", "%" + keyword + "%");
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public bool Insert(PhuTungDTO pt)
        {
            string query = "INSERT INTO PhuTung (TenPhuTung, DonGia, SoLuongTon) VALUES (@TenPhuTung, @DonGia, @SoLuongTon)";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenPhuTung", pt.TenPhuTung);
                    cmd.Parameters.AddWithValue("@DonGia", pt.DonGia);
                    cmd.Parameters.AddWithValue("@SoLuongTon", pt.SoLuongTon);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool Update(PhuTungDTO pt)
        {
            string query = "UPDATE PhuTung SET TenPhuTung = @TenPhuTung, DonGia = @DonGia, SoLuongTon = @SoLuongTon WHERE MaPhuTung = @MaPhuTung";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhuTung", pt.MaPhuTung);
                    cmd.Parameters.AddWithValue("@TenPhuTung", pt.TenPhuTung);
                    cmd.Parameters.AddWithValue("@DonGia", pt.DonGia);
                    cmd.Parameters.AddWithValue("@SoLuongTon", pt.SoLuongTon);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }

    public class PhieuSuaChuaDAL : DatabaseAccess
    {
        // THỰC THI GIAO DỊCH SQLTRANSACTION LẬP PHIẾU & TRỪ PHỤ TÙNG
        public bool SavePhieuSuaChuaTransaction(PhieuSuaChuaDTO phieu, List<ChiTietSuaChuaDTO> details, out string errorMessage)
        {
            errorMessage = "";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // 1. Chèn bản ghi vào PhieuSuaChua
                    string queryPhieu = @"INSERT INTO PhieuSuaChua (BienSo, NgayNhan, NoiDungSua, TongTien, TrangThai) 
                                         VALUES (@BienSo, GETDATE(), @NoiDungSua, @TongTien, @TrangThai);
                                         SELECT SCOPE_IDENTITY();";
                    int maPhieuGenerated = 0;
                    using (SqlCommand cmdP = new SqlCommand(queryPhieu, conn, trans))
                    {
                        cmdP.Parameters.AddWithValue("@BienSo", phieu.BienSo);
                        cmdP.Parameters.AddWithValue("@NoiDungSua", (object)phieu.NoiDungSua ?? DBNull.Value);
                        cmdP.Parameters.AddWithValue("@TongTien", phieu.TongTien);
                        cmdP.Parameters.AddWithValue("@TrangThai", phieu.TrangThai);
                        maPhieuGenerated = Convert.ToInt32(cmdP.ExecuteScalar());
                    }

                    // 2. Chèn ChiTietSuaChua & Trừ số lượng phụ tùng
                    foreach (var d in details)
                    {
                        // Kiểm tra tồn kho phụ tùng
                        string checkStockQuery = "SELECT SoLuongTon FROM PhuTung WHERE MaPhuTung = @MaPT";
                        int currentStock = 0;
                        using (SqlCommand cmdCheck = new SqlCommand(checkStockQuery, conn, trans))
                        {
                            cmdCheck.Parameters.AddWithValue("@MaPT", d.MaPhuTung);
                            object res = cmdCheck.ExecuteScalar();
                            currentStock = res != null ? Convert.ToInt32(res) : 0;
                        }

                        if (currentStock < d.SoLuong)
                        {
                            trans.Rollback(); // Rollback nếu phụ tùng trong kho không đủ
                            errorMessage = $"Phụ tùng '{d.TenPhuTung}' (Mã {d.MaPhuTung}) chỉ còn {currentStock} trong kho, không đủ xuất số lượng {d.SoLuong}!";
                            return false;
                        }

                        // Chèn chi tiết
                        string queryCT = @"INSERT INTO ChiTietSuaChua (MaPhieu, MaPhuTung, SoLuong, TienCong) 
                                          VALUES (@MaPhieu, @MaPT, @SoLuong, @TienCong)";
                        using (SqlCommand cmdCT = new SqlCommand(queryCT, conn, trans))
                        {
                            cmdCT.Parameters.AddWithValue("@MaPhieu", maPhieuGenerated);
                            cmdCT.Parameters.AddWithValue("@MaPT", d.MaPhuTung);
                            cmdCT.Parameters.AddWithValue("@SoLuong", d.SoLuong);
                            cmdCT.Parameters.AddWithValue("@TienCong", d.TienCong);
                            cmdCT.ExecuteNonQuery();
                        }

                        // Trừ số lượng kho (SoLuongTon -= SoLuong)
                        string updateStockQuery = "UPDATE PhuTung SET SoLuongTon = SoLuongTon - @SoLuong WHERE MaPhuTung = @MaPT";
                        using (SqlCommand cmdStock = new SqlCommand(updateStockQuery, conn, trans))
                        {
                            cmdStock.Parameters.AddWithValue("@SoLuong", d.SoLuong);
                            cmdStock.Parameters.AddWithValue("@MaPT", d.MaPhuTung);
                            cmdStock.ExecuteNonQuery();
                        }
                    }

                    // 3. Commit giao dịch thành công
                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi hệ thống: " + ex.Message;
                    return false;
                }
            }
        }

        public DataTable GetPendingPhieu()
        {
            string query = @"SELECT p.MaPhieu AS [Mã phiếu], p.BienSo AS [Biển số], kh.TenKH AS [Chủ xe], 
                                   p.NgayNhan AS [Ngày nhận], p.NoiDungSua AS [Nội dung sửa], 
                                   p.TongTien AS [Tổng tiền], p.TrangThai AS [Trạng thái]
                            FROM PhieuSuaChua p
                            JOIN Xe x ON p.BienSo = x.BienSo
                            JOIN KhachHang kh ON x.MaKH = kh.MaKH
                            ORDER BY p.MaPhieu DESC";
            using (SqlDataAdapter da = new SqlDataAdapter(query, connectionString))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Chuyển trạng thái sang "Đã thanh toán"
        public bool UpdateStatusToPaid(int maPhieu)
        {
            string query = "UPDATE PhieuSuaChua SET TrangThai = N'Đã thanh toán' WHERE MaPhieu = @MaPhieu";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhieu", maPhieu);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
    #endregion

    #region ==================== 3. BUS LAYER ====================
    public class GarageBUS
    {
        private KhachHangDAL khDAL = new KhachHangDAL();
        private XeDAL xeDAL = new XeDAL();
        private PhuTungDAL ptDAL = new PhuTungDAL();
        private PhieuSuaChuaDAL phieuDAL = new PhieuSuaChuaDAL();

        public List<KhachHangDTO> GetKhachHangList() => khDAL.GetAll();
        public DataTable GetXeDataTable() => xeDAL.GetAllXeWithCustomer();
        public XeDTO SearchXe(string bienSo) => xeDAL.GetByBienSo(bienSo);

        public bool RegisterVehicle(KhachHangDTO kh, XeDTO xe, bool isNewCustomer, out string error)
        {
            if (string.IsNullOrWhiteSpace(xe.BienSo) || string.IsNullOrWhiteSpace(xe.HieuXe))
            {
                error = "Biển số xe và Hiệu xe không được để trống!";
                return false;
            }

            int maKH = xe.MaKH;
            if (isNewCustomer)
            {
                if (string.IsNullOrWhiteSpace(kh.TenKH) || string.IsNullOrWhiteSpace(kh.DienThoai))
                {
                    error = "Tên khách hàng và Số điện thoại không được để trống!";
                    return false;
                }
                maKH = khDAL.Insert(kh);
            }

            xe.MaKH = maKH;
            error = "";
            return xeDAL.Insert(xe);
        }

        public List<PhuTungDTO> GetPhuTungList() => ptDAL.GetAll();
        public DataTable SearchPhuTung(string keyword) => ptDAL.Search(keyword);

        public bool SavePhuTung(PhuTungDTO pt, bool isEdit, out string error)
        {
            if (string.IsNullOrWhiteSpace(pt.TenPhuTung) || pt.DonGia < 0 || pt.SoLuongTon < 0)
            {
                error = "Thông tin phụ tùng không hợp lệ!";
                return false;
            }
            error = "";
            return isEdit ? ptDAL.Update(pt) : ptDAL.Insert(pt);
        }

        public bool CreateRepairOrder(PhieuSuaChuaDTO phieu, List<ChiTietSuaChuaDTO> details, out string error)
        {
            if (string.IsNullOrWhiteSpace(phieu.BienSo))
            {
                error = "Chưa chọn Biển số xe!";
                return false;
            }
            if (details == null || details.Count == 0)
            {
                error = "Danh sách chi tiết sửa chữa / phụ tùng đang trống!";
                return false;
            }
            return phieuDAL.SavePhieuSuaChuaTransaction(phieu, details, out error);
        }

        public DataTable GetPhieuSuaChuaTable() => phieuDAL.GetPendingPhieu();
        public bool PayBill(int maPhieu) => phieuDAL.UpdateStatusToPaid(maPhieu);
    }
    #endregion

    #region ==================== 4. GUI LAYER ====================
    public class frmGarageMain : Form
    {
        private GarageBUS bus = new GarageBUS();

        // TAB 1: TIẾP NHẬN XE
        private TextBox txtSearchBienSo, txtHieuXe, txtNamSX, txtTenKH, txtSĐT, txtDiaChi;
        private CheckBox chkNewCustomer;
        private ComboBox cboExistingCustomer;
        private Button btnRegisterXe;
        private DataGridView dgvXeList;

        // TAB 2: LẬP PHIẾU SỬA CHỮA
        private ComboBox cboBienSoXe, cboPhuTung;
        private TextBox txtNoiDungSua;
        private NumericUpDown numSoLuong, numTienCong;
        private Button btnAddDetail, btnSavePhieu;
        private DataGridView dgvDetails;
        private Label lblTotalEstimated;
        private List<ChiTietSuaChuaDTO> currentDetailsList = new List<ChiTietSuaChuaDTO>();

        // TAB 3: QUẢN LÝ PHỤ TÙNG
        private DataGridView dgvPhuTung;
        private TextBox txtTenPT, txtSearchPT;
        private NumericUpDown numDonGiaPT, numTonKhoPT;
        private Button btnAddPT, btnEditPT;
        private int selectedMaPT = -1;

        // TAB 4: THANH TOÁN
        private DataGridView dgvPendingPhieu;
        private Button btnThanhToan;

        public frmGarageMain()
        {
            TaoGiaoDien();
            LoadDataTab1();
            LoadDataTab2();
            LoadDataTab3();
            LoadDataTab4();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Quản Lý Garage & Bảo Dưỡng Ô Tô (Garage Management 3-Tier)";
            this.Size = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
            TabPage tabReceive = new TabPage("1. Tiếp nhận Xe & Chủ xe");
            TabPage tabRepair = new TabPage("2. Lập Phiếu Sửa Chữa (SqlTransaction)");
            TabPage tabParts = new TabPage("3. Quản lý Phụ tùng");
            TabPage tabBilling = new TabPage("4. Thanh toán & Xuất Hóa đơn");

            // ---------------- TAB 1: TIẾP NHẬN XE ----------------
            GroupBox gbXe = new GroupBox { Text = "Thông tin tiếp nhận xe", Location = new Point(15, 15), Size = new Size(380, 600) };

            gbXe.Controls.Add(new Label { Text = "Biển số xe:", Location = new Point(15, 35), AutoSize = true });
            txtSearchBienSo = new TextBox { Location = new Point(120, 32), Width = 150 };
            Button btnLookup = new Button { Text = "Tra cứu", Location = new Point(280, 30), Width = 80, Height = 28 };
            btnLookup.Click += BtnLookup_Click;
            gbXe.Controls.Add(txtSearchBienSo); gbXe.Controls.Add(btnLookup);

            gbXe.Controls.Add(new Label { Text = "Hiệu xe:", Location = new Point(15, 80), AutoSize = true });
            txtHieuXe = new TextBox { Location = new Point(120, 77), Width = 240 };
            gbXe.Controls.Add(txtHieuXe);

            gbXe.Controls.Add(new Label { Text = "Năm sản xuất:", Location = new Point(15, 125), AutoSize = true });
            txtNamSX = new TextBox { Location = new Point(120, 122), Width = 240, Text = "2022" };
            gbXe.Controls.Add(txtNamSX);

            chkNewCustomer = new CheckBox { Text = "Chủ xe mới", Location = new Point(15, 170), AutoSize = true, Checked = true };
            chkNewCustomer.CheckedChanged += (s, e) => {
                cboExistingCustomer.Enabled = !chkNewCustomer.Checked;
                txtTenKH.Enabled = txtSĐT.Enabled = txtDiaChi.Enabled = chkNewCustomer.Checked;
            };
            gbXe.Controls.Add(chkNewCustomer);

            gbXe.Controls.Add(new Label { Text = "Chọn chủ cũ:", Location = new Point(15, 205), AutoSize = true });
            cboExistingCustomer = new ComboBox { Location = new Point(120, 202), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            gbXe.Controls.Add(cboExistingCustomer);

            gbXe.Controls.Add(new Label { Text = "Họ tên chủ xe:", Location = new Point(15, 250), AutoSize = true });
            txtTenKH = new TextBox { Location = new Point(120, 247), Width = 240 };
            gbXe.Controls.Add(txtTenKH);

            gbXe.Controls.Add(new Label { Text = "Số điện thoại:", Location = new Point(15, 295), AutoSize = true });
            txtSĐT = new TextBox { Location = new Point(120, 292), Width = 240 };
            gbXe.Controls.Add(txtSĐT);

            gbXe.Controls.Add(new Label { Text = "Địa chỉ:", Location = new Point(15, 340), AutoSize = true });
            txtDiaChi = new TextBox { Location = new Point(120, 337), Width = 240 };
            gbXe.Controls.Add(txtDiaChi);

            btnRegisterXe = new Button { Text = "TIẾP NHẬN XE MỚI", Location = new Point(15, 410), Width = 345, Height = 45, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnRegisterXe.Click += BtnRegisterXe_Click;
            gbXe.Controls.Add(btnRegisterXe);

            tabReceive.Controls.Add(gbXe);

            dgvXeList = new DataGridView { Location = new Point(410, 25), Size = new Size(710, 590), ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            tabReceive.Controls.Add(dgvXeList);

            // ---------------- TAB 2: LẬP PHIẾU SỬA CHỮA ----------------
            GroupBox gbPhieuTop = new GroupBox { Text = "Thông tin phiếu sửa chữa", Location = new Point(15, 10), Size = new Size(1105, 85) };
            gbPhieuTop.Controls.Add(new Label { Text = "Biển số xe:", Location = new Point(15, 35), AutoSize = true });
            cboBienSoXe = new ComboBox { Location = new Point(95, 32), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            gbPhieuTop.Controls.Add(cboBienSoXe);

            gbPhieuTop.Controls.Add(new Label { Text = "Nội dung sửa chữa:", Location = new Point(300, 35), AutoSize = true });
            txtNoiDungSua = new TextBox { Location = new Point(430, 32), Width = 650, Text = "Bảo dưỡng định kỳ & Thay phụ tùng" };
            gbPhieuTop.Controls.Add(txtNoiDungSua);

            tabRepair.Controls.Add(gbPhieuTop);

            GroupBox gbItem = new GroupBox { Text = "Thêm phụ tùng & tiền công", Location = new Point(15, 105), Size = new Size(1105, 80) };
            gbItem.Controls.Add(new Label { Text = "Phụ tùng:", Location = new Point(15, 32), AutoSize = true });
            cboPhuTung = new ComboBox { Location = new Point(85, 29), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            gbItem.Controls.Add(cboPhuTung);

            gbItem.Controls.Add(new Label { Text = "Số lượng:", Location = new Point(380, 32), AutoSize = true });
            numSoLuong = new NumericUpDown { Location = new Point(450, 29), Width = 80, Minimum = 1, Maximum = 100, Value = 1 };
            gbItem.Controls.Add(numSoLuong);

            gbItem.Controls.Add(new Label { Text = "Tiền công (VNĐ):", Location = new Point(550, 32), AutoSize = true });
            numTienCong = new NumericUpDown { Location = new Point(660, 29), Width = 160, Maximum = 50000000, Increment = 50000, Value = 100000 };
            gbItem.Controls.Add(numTienCong);

            btnAddDetail = new Button { Text = "+ Thêm dịch vụ", Location = new Point(840, 24), Width = 245, Height = 35, BackColor = Color.LightSkyBlue, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            btnAddDetail.Click += BtnAddDetail_Click;
            gbItem.Controls.Add(btnAddDetail);

            tabRepair.Controls.Add(gbItem);

            dgvDetails = new DataGridView { Location = new Point(15, 195), Size = new Size(1105, 360), ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            tabRepair.Controls.Add(dgvDetails);

            lblTotalEstimated = new Label { Text = "Tổng chi phí ước tính: 0 VNĐ", Location = new Point(15, 570), AutoSize = true, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.DarkRed };
            tabRepair.Controls.Add(lblTotalEstimated);

            btnSavePhieu = new Button { Text = "LƯU PHIẾU SỬA CHỮA (SqlTransaction)", Location = new Point(770, 565), Width = 350, Height = 45, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
            btnSavePhieu.Click += BtnSavePhieu_Click;
            tabRepair.Controls.Add(btnSavePhieu);

            // ---------------- TAB 3: QUẢN LÝ PHỤ TÙNG ----------------
            GroupBox gbPTInput = new GroupBox { Text = "Thông tin phụ tùng", Location = new Point(15, 15), Size = new Size(350, 595) };
            gbPTInput.Controls.Add(new Label { Text = "Tên phụ tùng:", Location = new Point(15, 40), AutoSize = true });
            txtTenPT = new TextBox { Location = new Point(120, 37), Width = 210 };
            gbPTInput.Controls.Add(txtTenPT);

            gbPTInput.Controls.Add(new Label { Text = "Đơn giá (VNĐ):", Location = new Point(15, 90), AutoSize = true });
            numDonGiaPT = new NumericUpDown { Location = new Point(120, 87), Width = 210, Maximum = 100000000, Increment = 10000, Value = 150000 };
            gbPTInput.Controls.Add(numDonGiaPT);

            gbPTInput.Controls.Add(new Label { Text = "Số lượng tồn:", Location = new Point(15, 140), AutoSize = true });
            numTonKhoPT = new NumericUpDown { Location = new Point(120, 137), Width = 210, Maximum = 1000, Value = 50 };
            gbPTInput.Controls.Add(numTonKhoPT);

            btnAddPT = new Button { Text = "Thêm mới", Location = new Point(15, 210), Width = 150, Height = 38, BackColor = Color.LightGreen };
            btnAddPT.Click += BtnAddPT_Click;
            gbPTInput.Controls.Add(btnAddPT);

            btnEditPT = new Button { Text = "Cập nhật", Location = new Point(180, 210), Width = 150, Height = 38, BackColor = Color.Khaki };
            btnEditPT.Click += BtnEditPT_Click;
            gbPTInput.Controls.Add(btnEditPT);

            tabParts.Controls.Add(gbPTInput);

            Panel pnlPTSearch = new Panel { Location = new Point(380, 15), Size = new Size(740, 45) };
            pnlPTSearch.Controls.Add(new Label { Text = "Tìm kiếm phụ tùng (SqlParameter):", Location = new Point(10, 12), AutoSize = true });
            txtSearchPT = new TextBox { Location = new Point(240, 9), Width = 480 };
            txtSearchPT.TextChanged += (s, e) => dgvPhuTung.DataSource = bus.SearchPhuTung(txtSearchPT.Text.Trim());
            pnlPTSearch.Controls.Add(txtSearchPT);
            tabParts.Controls.Add(pnlPTSearch);

            dgvPhuTung = new DataGridView { Location = new Point(380, 65), Size = new Size(740, 545), ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            dgvPhuTung.SelectionChanged += DgvPhuTung_SelectionChanged;
            tabParts.Controls.Add(dgvPhuTung);

            // ---------------- TAB 4: THANH TOÁN ----------------
            Panel pnlPayTop = new Panel { Dock = DockStyle.Top, Height = 60 };
            pnlPayTop.Controls.Add(new Label { Text = "Danh sách Phiếu sửa chữa chờ thanh toán / Đã hoàn thành", Location = new Point(15, 20), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold) });
            btnThanhToan = new Button { Text = "XÁC NHẬN THANH TOÁN (ĐÃ THU TIỀN)", Location = new Point(780, 12), Width = 340, Height = 40, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnThanhToan.Click += BtnThanhToan_Click;
            pnlPayTop.Controls.Add(btnThanhToan);

            tabBilling.Controls.Add(pnlPayTop);

            dgvPendingPhieu = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            tabBilling.Controls.Add(dgvPendingPhieu);

            tabControl.TabPages.Add(tabReceive);
            tabControl.TabPages.Add(tabRepair);
            tabControl.TabPages.Add(tabParts);
            tabControl.TabPages.Add(tabBilling);
            this.Controls.Add(tabControl);
        }

        // --- TAB 1 LOGIC ---
        private void LoadDataTab1()
        {
            var customers = bus.GetKhachHangList();
            cboExistingCustomer.DisplayMember = "TenKH";
            cboExistingCustomer.ValueMember = "MaKH";
            cboExistingCustomer.DataSource = customers;

            dgvXeList.DataSource = bus.GetXeDataTable();
            if (dgvXeList.Columns["MaKH"] != null) dgvXeList.Columns["MaKH"].Visible = false;
        }

        private void BtnLookup_Click(object sender, EventArgs e)
        {
            string bienSo = txtSearchBienSo.Text.Trim();
            if (string.IsNullOrEmpty(bienSo)) return;

            XeDTO xe = bus.SearchXe(bienSo);
            if (xe != null)
            {
                txtHieuXe.Text = xe.HieuXe;
                txtNamSX.Text = xe.NamSanXuat.ToString();
                txtTenKH.Text = xe.TenKH;
                txtSĐT.Text = xe.DienThoai;
                chkNewCustomer.Checked = false;
                cboExistingCustomer.SelectedValue = xe.MaKH;
                MessageBox.Show($"Tìm thấy thông tin xe {bienSo} thuộc sở hữu của chủ xe {xe.TenKH}!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Không tìm thấy biển số xe '{bienSo}'. Hãy nhập thông tin bên dưới để tiếp nhận mới!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRegisterXe_Click(object sender, EventArgs e)
        {
            KhachHangDTO kh = new KhachHangDTO
            {
                TenKH = txtTenKH.Text.Trim(),
                DienThoai = txtSĐT.Text.Trim(),
                DiaChi = txtDiaChi.Text.Trim()
            };

            XeDTO xe = new XeDTO
            {
                BienSo = txtSearchBienSo.Text.Trim(),
                HieuXe = txtHieuXe.Text.Trim(),
                NamSanXuat = int.TryParse(txtNamSX.Text.Trim(), out int y) ? y : 2022,
                MaKH = cboExistingCustomer.SelectedValue != null ? Convert.ToInt32(cboExistingCustomer.SelectedValue) : 0
            };

            if (bus.RegisterVehicle(kh, xe, chkNewCustomer.Checked, out string err))
            {
                MessageBox.Show("Tiếp nhận xe mới vào Garage thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDataTab1();
                LoadDataTab2();
            }
            else
            {
                MessageBox.Show(err, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // --- TAB 2 LOGIC ---
        private void LoadDataTab2()
        {
            DataTable dtXe = bus.GetXeDataTable();
            cboBienSoXe.DisplayMember = "Biển số";
            cboBienSoXe.ValueMember = "Biển số";
            cboBienSoXe.DataSource = dtXe;

            var listPT = bus.GetPhuTungList();
            cboPhuTung.DisplayMember = "TenPhuTung";
            cboPhuTung.ValueMember = "MaPhuTung";
            cboPhuTung.DataSource = listPT;

            RefreshDetailsGrid();
        }

        private void BtnAddDetail_Click(object sender, EventArgs e)
        {
            if (cboPhuTung.SelectedItem is PhuTungDTO pt)
            {
                int qty = (int)numSoLuong.Value;
                decimal tienCong = numTienCong.Value;

                var exist = currentDetailsList.Find(x => x.MaPhuTung == pt.MaPhuTung);
                if (exist != null)
                {
                    exist.SoLuong += qty;
                    exist.TienCong += tienCong;
                }
                else
                {
                    currentDetailsList.Add(new ChiTietSuaChuaDTO
                    {
                        MaPhuTung = pt.MaPhuTung,
                        TenPhuTung = pt.TenPhuTung,
                        DonGia = pt.DonGia,
                        SoLuong = qty,
                        TienCong = tienCong
                    });
                }
                RefreshDetailsGrid();
            }
        }

        private void RefreshDetailsGrid()
        {
            dgvDetails.DataSource = null;
            dgvDetails.DataSource = currentDetailsList;

            if (dgvDetails.Columns["MaPhieu"] != null) dgvDetails.Columns["MaPhieu"].Visible = false;
            if (dgvDetails.Columns["MaPhuTung"] != null) dgvDetails.Columns["MaPhuTung"].Visible = false;
            if (dgvDetails.Columns["DonGia"] != null) dgvDetails.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            if (dgvDetails.Columns["TienCong"] != null) dgvDetails.Columns["TienCong"].DefaultCellStyle.Format = "N0";
            if (dgvDetails.Columns["ThanhTien"] != null) dgvDetails.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";

            decimal total = 0;
            foreach (var d in currentDetailsList) total += d.ThanhTien;
            lblTotalEstimated.Text = $"Tổng chi phí ước tính: {total:N0} VNĐ";
        }

        private void BtnSavePhieu_Click(object sender, EventArgs e)
        {
            if (cboBienSoXe.SelectedValue == null) return;

            PhieuSuaChuaDTO phieu = new PhieuSuaChuaDTO
            {
                BienSo = cboBienSoXe.SelectedValue.ToString(),
                NoiDungSua = txtNoiDungSua.Text.Trim(),
                TongTien = 0,
                TrangThai = "Đang sửa"
            };
            foreach (var d in currentDetailsList) phieu.TongTien += d.ThanhTien;

            if (bus.CreateRepairOrder(phieu, currentDetailsList, out string err))
            {
                MessageBox.Show("Lập phiếu sửa chữa thành công! Đã tự động trừ kho phụ tùng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                currentDetailsList.Clear();
                RefreshDetailsGrid();
                LoadDataTab3();
                LoadDataTab4();
            }
            else
            {
                MessageBox.Show(err, "Lỗi lập phiếu (Rollback)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- TAB 3 LOGIC ---
        private void LoadDataTab3()
        {
            dgvPhuTung.DataSource = bus.SearchPhuTung("");
            if (dgvPhuTung.Columns["Đơn giá"] != null) dgvPhuTung.Columns["Đơn giá"].DefaultCellStyle.Format = "N0";
        }

        private void DgvPhuTung_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPhuTung.CurrentRow != null && dgvPhuTung.CurrentRow.Index >= 0)
            {
                DataGridViewRow r = dgvPhuTung.CurrentRow;
                if (r.Cells["Mã PT"].Value == null) return;
                selectedMaPT = Convert.ToInt32(r.Cells["Mã PT"].Value);
                txtTenPT.Text = r.Cells["Tên phụ tùng"].Value.ToString();
                numDonGiaPT.Value = Convert.ToDecimal(r.Cells["Đơn giá"].Value);
                numTonKhoPT.Value = Convert.ToInt32(r.Cells["Tồn kho"].Value);
            }
        }

        private void BtnAddPT_Click(object sender, EventArgs e)
        {
            PhuTungDTO pt = new PhuTungDTO
            {
                TenPhuTung = txtTenPT.Text.Trim(),
                DonGia = numDonGiaPT.Value,
                SoLuongTon = (int)numTonKhoPT.Value
            };

            if (bus.SavePhuTung(pt, false, out string err))
            {
                MessageBox.Show("Thêm phụ tùng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDataTab2();
                LoadDataTab3();
            }
            else
            {
                MessageBox.Show(err, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnEditPT_Click(object sender, EventArgs e)
        {
            if (selectedMaPT == -1) return;

            PhuTungDTO pt = new PhuTungDTO
            {
                MaPhuTung = selectedMaPT,
                TenPhuTung = txtTenPT.Text.Trim(),
                DonGia = numDonGiaPT.Value,
                SoLuongTon = (int)numTonKhoPT.Value
            };

            if (bus.SavePhuTung(pt, true, out string err))
            {
                MessageBox.Show("Cập nhật phụ tùng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDataTab2();
                LoadDataTab3();
            }
            else
            {
                MessageBox.Show(err, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // --- TAB 4 LOGIC ---
        private void LoadDataTab4()
        {
            dgvPendingPhieu.DataSource = bus.GetPhieuSuaChuaTable();
            if (dgvPendingPhieu.Columns["Tổng tiền"] != null) dgvPendingPhieu.Columns["Tổng tiền"].DefaultCellStyle.Format = "N0";
        }

        private void BtnThanhToan_Click(object sender, EventArgs e)
        {
            if (dgvPendingPhieu.CurrentRow == null || dgvPendingPhieu.CurrentRow.Index < 0)
            {
                MessageBox.Show("Vui lòng chọn một phiếu sửa chữa để thanh toán!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int maPhieu = Convert.ToInt32(dgvPendingPhieu.CurrentRow.Cells["Mã phiếu"].Value);
            string trangThaiCurrent = dgvPendingPhieu.CurrentRow.Cells["Trạng thái"].Value.ToString();

            if (trangThaiCurrent == "Đã thanh toán")
            {
                MessageBox.Show("Phiếu sửa chữa này đã được thanh toán trước đó!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show($"Xác nhận đã thu tiền và thanh toán cho Phiếu sửa chữa Mã {maPhieu}?", "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (bus.PayBill(maPhieu))
                {
                    MessageBox.Show($"Thanh toán thành công! Trạng thái phiếu {maPhieu} đã chuyển sang 'Đã thanh toán'.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDataTab4();
                }
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmGarageMain());
        }
    }
    #endregion
}