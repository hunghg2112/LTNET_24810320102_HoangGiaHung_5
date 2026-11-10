using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace KaraokeManagementApp
{
    #region ==================== 1. DTO LAYER ====================
    public class PhongHatDTO
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; }
        public string LoaiPhong { get; set; }
        public decimal GiaTheoGio { get; set; }
        public string TrangThai { get; set; }
    }

    public class DichVuDTO
    {
        public int MaDV { get; set; }
        public string TenDV { get; set; }
        public decimal DonGia { get; set; }
        public int TonKho { get; set; }
    }

    public class HoaDonDTO
    {
        public int MaHD { get; set; }
        public int MaPhong { get; set; }
        public DateTime GioVao { get; set; }
        public DateTime? GioRa { get; set; }
        public decimal TongTienPhong { get; set; }
        public decimal TongTienDichVu { get; set; }
        public bool TrangThai { get; set; }
    }

    public class ChiTietDichVuDTO
    {
        public int MaHD { get; set; }
        public int MaDV { get; set; }
        public string TenDV { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
    #endregion

    #region ==================== 2. DAL LAYER ====================
    public class DatabaseAccess
    {
        protected static string connectionString = @"Server=localhost\SQLEXPRESS;Database=KaraokeDB;Integrated Security=True;TrustServerCertificate=True;";
    }

    public class PhongHatDAL : DatabaseAccess
    {
        public List<PhongHatDTO> GetAll()
        {
            List<PhongHatDTO> list = new List<PhongHatDTO>();
            string query = "SELECT MaPhong, TenPhong, LoaiPhong, GiaTheoGio, TrangThai FROM PhongHat";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new PhongHatDTO
                            {
                                MaPhong = (int)r["MaPhong"],
                                TenPhong = r["TenPhong"].ToString(),
                                LoaiPhong = r["LoaiPhong"]?.ToString(),
                                GiaTheoGio = Convert.ToDecimal(r["GiaTheoGio"]),
                                TrangThai = r["TrangThai"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }

    public class DichVuDAL : DatabaseAccess
    {
        public List<DichVuDTO> GetAll()
        {
            List<DichVuDTO> list = new List<DichVuDTO>();
            string query = "SELECT MaDV, TenDV, DonGia, TonKho FROM DichVu";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new DichVuDTO
                            {
                                MaDV = (int)r["MaDV"],
                                TenDV = r["TenDV"].ToString(),
                                DonGia = Convert.ToDecimal(r["DonGia"]),
                                TonKho = Convert.ToInt32(r["TonKho"])
                            });
                        }
                    }
                }
            }
            return list;
        }
    }

    public class HoaDonDAL : DatabaseAccess
    {
        public bool OpenRoom(int maPhong, out int newMaHD)
        {
            newMaHD = 0;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();
                try
                {
                    // 1. Tạo hóa đơn mới
                    string queryHD = @"INSERT INTO HoaDon (MaPhong, GioVao, TrangThai) 
                                       VALUES (@MaPhong, GETDATE(), 0);
                                       SELECT SCOPE_IDENTITY();";
                    using (SqlCommand cmdHD = new SqlCommand(queryHD, conn, trans))
                    {
                        cmdHD.Parameters.AddWithValue("@MaPhong", maPhong);
                        newMaHD = Convert.ToInt32(cmdHD.ExecuteScalar());
                    }

                    // 2. Cập nhật trạng thái phòng -> 'Đang hát'
                    string queryRoom = "UPDATE PhongHat SET TrangThai = N'Đang hát' WHERE MaPhong = @MaPhong";
                    using (SqlCommand cmdRoom = new SqlCommand(queryRoom, conn, trans))
                    {
                        cmdRoom.Parameters.AddWithValue("@MaPhong", maPhong);
                        cmdRoom.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return true;
                }
                catch
                {
                    trans.Rollback();
                    return false;
                }
            }
        }

        public HoaDonDTO GetActiveInvoiceByRoom(int maPhong)
        {
            string query = "SELECT MaHD, MaPhong, GioVao, TongTienPhong, TongTienDichVu, TrangThai FROM HoaDon WHERE MaPhong = @MaPhong AND TrangThai = 0";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MaPhong", maPhong);
                    conn.Open();
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            return new HoaDonDTO
                            {
                                MaHD = (int)r["MaHD"],
                                MaPhong = (int)r["MaPhong"],
                                GioVao = Convert.ToDateTime(r["GioVao"]),
                                TongTienPhong = Convert.ToDecimal(r["TongTienPhong"]),
                                TongTienDichVu = Convert.ToDecimal(r["TongTienDichVu"]),
                                TrangThai = Convert.ToBoolean(r["TrangThai"])
                            };
                        }
                    }
                }
            }
            return null;
        }

        // NẠP DỊCH VỤ ĐÃ GỌI BẰNG DISCONNECTED MODEL (SqlDataAdapter + DataTable)
        public DataTable GetOrderedServicesDataTable(int maHD, out SqlDataAdapter adapter)
        {
            string query = @"SELECT ct.MaHD, ct.MaDV, dv.TenDV AS [Tên dịch vụ], ct.SoLuong AS [Số lượng], 
                                   ct.DonGia AS [Đơn giá], (ct.SoLuong * ct.DonGia) AS [Thành tiền]
                            FROM ChiTietDichVu ct
                            JOIN DichVu dv ON ct.MaDV = dv.MaDV
                            WHERE ct.MaHD = @MaHD";

            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MaHD", maHD);

            adapter = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            return dt;
        }

        public bool AddOrUpdateService(int maHD, int maDV, int soLuong, decimal donGia)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string checkQuery = "SELECT SoLuong FROM ChiTietDichVu WHERE MaHD = @MaHD AND MaDV = @MaDV";
                using (SqlCommand cmdCheck = new SqlCommand(checkQuery, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@MaHD", maHD);
                    cmdCheck.Parameters.AddWithValue("@MaDV", maDV);
                    object res = cmdCheck.ExecuteScalar();

                    if (res != null)
                    {
                        string updateQuery = "UPDATE ChiTietDichVu SET SoLuong = SoLuong + @SoLuong WHERE MaHD = @MaHD AND MaDV = @MaDV";
                        using (SqlCommand cmdUp = new SqlCommand(updateQuery, conn))
                        {
                            cmdUp.Parameters.AddWithValue("@SoLuong", soLuong);
                            cmdUp.Parameters.AddWithValue("@MaHD", maHD);
                            cmdUp.Parameters.AddWithValue("@MaDV", maDV);
                            return cmdUp.ExecuteNonQuery() > 0;
                        }
                    }
                    else
                    {
                        string insertQuery = "INSERT INTO ChiTietDichVu (MaHD, MaDV, SoLuong, DonGia) VALUES (@MaHD, @MaDV, @SoLuong, @DonGia)";
                        using (SqlCommand cmdIns = new SqlCommand(insertQuery, conn))
                        {
                            cmdIns.Parameters.AddWithValue("@MaHD", maHD);
                            cmdIns.Parameters.AddWithValue("@MaDV", maDV);
                            cmdIns.Parameters.AddWithValue("@SoLuong", soLuong);
                            cmdIns.Parameters.AddWithValue("@DonGia", donGia);
                            return cmdIns.ExecuteNonQuery() > 0;
                        }
                    }
                }
            }
        }

        // THỰC THI GIAO DỊCH SQLTRANSACTION THANH TOÁN HÓA ĐƠN KARAOKE
        public bool CheckoutTransaction(int maHD, int maPhong, decimal giaTheoGio, out decimal tongPhong, out decimal tongDV, out string errorMsg)
        {
            errorMsg = "";
            tongPhong = 0;
            tongDV = 0;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    // 1. Lấy thông tin Giờ Vào để tính số giờ hát
                    DateTime gioVao = DateTime.Now;
                    string queryGetGV = "SELECT GioVao FROM HoaDon WHERE MaHD = @MaHD";
                    using (SqlCommand cmdGV = new SqlCommand(queryGetGV, conn, trans))
                    {
                        cmdGV.Parameters.AddWithValue("@MaHD", maHD);
                        object resGV = cmdGV.ExecuteScalar();
                        if (resGV != null) gioVao = Convert.ToDateTime(resGV);
                    }

                    DateTime gioRa = DateTime.Now;
                    TimeSpan timeSpan = gioRa - gioVao;
                    double totalHours = timeSpan.TotalHours;
                    if (totalHours < 0.1) totalHours = 0.1; // Tối thiểu tính 0.1 giờ

                    tongPhong = Math.Round((decimal)totalHours * giaTheoGio, 0);

                    // 2. Tính tổng tiền dịch vụ
                    string querySumDV = "SELECT ISNULL(SUM(SoLuong * DonGia), 0) FROM ChiTietDichVu WHERE MaHD = @MaHD";
                    using (SqlCommand cmdSum = new SqlCommand(querySumDV, conn, trans))
                    {
                        cmdSum.Parameters.AddWithValue("@MaHD", maHD);
                        tongDV = Convert.ToDecimal(cmdSum.ExecuteScalar());
                    }

                    // 3. Trừ TonKho trong bảng DichVu & Kiểm tra số lượng tồn kho
                    string queryGetItems = "SELECT MaDV, SoLuong FROM ChiTietDichVu WHERE MaHD = @MaHD";
                    List<Tuple<int, int>> listItems = new List<Tuple<int, int>>();
                    using (SqlCommand cmdItems = new SqlCommand(queryGetItems, conn, trans))
                    {
                        using (SqlDataReader r = cmdItems.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                listItems.Add(new Tuple<int, int>((int)r["MaDV"], (int)r["SoLuong"]));
                            }
                        }
                    }

                    foreach (var item in listItems)
                    {
                        int maDV = item.Item1;
                        int qtyNeeded = item.Item2;

                        string queryCheckStock = "SELECT TonKho, TenDV FROM DichVu WHERE MaDV = @MaDV";
                        int currentStock = 0;
                        string tenDV = "";
                        using (SqlCommand cmdCheck = new SqlCommand(queryCheckStock, conn, trans))
                        {
                            cmdCheck.Parameters.AddWithValue("@MaDV", maDV);
                            using (SqlDataReader rCheck = cmdCheck.ExecuteReader())
                            {
                                if (rCheck.Read())
                                {
                                    currentStock = Convert.ToInt32(rCheck["TonKho"]);
                                    tenDV = rCheck["TenDV"].ToString();
                                }
                            }
                        }

                        if (currentStock < qtyNeeded)
                        {
                            trans.Rollback();
                            errorMsg = $"Mặt hàng '{tenDV}' chỉ còn {currentStock} trong kho, không đủ xuất số lượng {qtyNeeded}!";
                            return false;
                        }

                        // Trừ tồn kho
                        string queryUpdateStock = "UPDATE DichVu SET TonKho = TonKho - @SoLuong WHERE MaDV = @MaDV";
                        using (SqlCommand cmdSub = new SqlCommand(queryUpdateStock, conn, trans))
                        {
                            cmdSub.Parameters.AddWithValue("@SoLuong", qtyNeeded);
                            cmdSub.Parameters.AddWithValue("@MaDV", maDV);
                            cmdSub.ExecuteNonQuery();
                        }
                    }

                    // 4. Cập nhật HoaDon: GioRa, TongTienPhong, TongTienDichVu, TrangThai = 1
                    string queryUpdateHD = @"UPDATE HoaDon 
                                            SET GioRa = GETDATE(), TongTienPhong = @TongPhong, 
                                                TongTienDichVu = @TongDV, TrangThai = 1 
                                            WHERE MaHD = @MaHD";
                    using (SqlCommand cmdUpHD = new SqlCommand(queryUpdateHD, conn, trans))
                    {
                        cmdUpHD.Parameters.AddWithValue("@TongPhong", tongPhong);
                        cmdUpHD.Parameters.AddWithValue("@TongDV", tongDV);
                        cmdUpHD.Parameters.AddWithValue("@MaHD", maHD);
                        cmdUpHD.ExecuteNonQuery();
                    }

                    // 5. Cập nhật PhongHat -> 'Trống'
                    string queryResetRoom = "UPDATE PhongHat SET TrangThai = N'Trống' WHERE MaPhong = @MaPhong";
                    using (SqlCommand cmdReset = new SqlCommand(queryResetRoom, conn, trans))
                    {
                        cmdReset.Parameters.AddWithValue("@MaPhong", maPhong);
                        cmdReset.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMsg = "Lỗi hệ thống: " + ex.Message;
                    return false;
                }
            }
        }
    }
    #endregion

    #region ==================== 3. BUS LAYER ====================
    public class KaraokeBUS
    {
        private PhongHatDAL phongDAL = new PhongHatDAL();
        private DichVuDAL dvDAL = new DichVuDAL();
        private HoaDonDAL hdDAL = new HoaDonDAL();

        public List<PhongHatDTO> GetPhongHatList() => phongDAL.GetAll();
        public List<DichVuDTO> GetDichVuList() => dvDAL.GetAll();
        public HoaDonDTO GetActiveHoaDon(int maPhong) => hdDAL.GetActiveInvoiceByRoom(maPhong);

        public bool OpenRoom(int maPhong, out int maHD) => hdDAL.OpenRoom(maPhong, out maHD);

        public DataTable GetOrderedServices(int maHD, out SqlDataAdapter adapter) => hdDAL.GetOrderedServicesDataTable(maHD, out adapter);

        public bool AddServiceToRoom(int maHD, int maDV, int soLuong, decimal donGia)
        {
            if (soLuong <= 0) return false;
            return hdDAL.AddOrUpdateService(maHD, maDV, soLuong, donGia);
        }

        public bool Checkout(int maHD, int maPhong, decimal giaTheoGio, out decimal tongPhong, out decimal tongDV, out string errorMsg)
        {
            return hdDAL.CheckoutTransaction(maHD, maPhong, giaTheoGio, out tongPhong, out tongDV, out errorMsg);
        }
    }
    #endregion

    #region ==================== 4. GUI LAYER ====================
    public class frmKaraokeMain : Form
    {
        private KaraokeBUS bus = new KaraokeBUS();
        private FlowLayoutPanel flpRooms;
        private PhongHatDTO selectedRoom = null;
        private HoaDonDTO currentActiveHD = null;

        // PANEL CHI TIẾT PHÒNG VÀ GỌI DỊCH VỤ
        private GroupBox gbRoomDetail;
        private Label lblRoomName, lblRoomStatus, lblRoomPrice, lblTimeIn;
        private Button btnOpenRoom, btnCheckout;

        private ComboBox cboDichVu;
        private NumericUpDown numSoLuongDV;
        private Button btnAddDV;
        private DataGridView dgvServices;
        private Label lblTotalServices, lblGrandTotal;

        public frmKaraokeMain()
        {
            TaoGiaoDien();
            LoadRoomGrid();
            LoadDichVuCombo();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Quản Lý Đặt Phòng & Dịch Vụ Karaoke (Visual Grid Layout & 3-Tier)";
            this.Size = new Size(1150, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            // Bên trái: Sơ đồ phòng hát (Visual Room Grid Layout)
            GroupBox gbGrid = new GroupBox { Text = "Sơ đồ phòng hát trực quan (Xanh: Trống | Đỏ: Đang hát)", Location = new Point(15, 15), Size = new Size(520, 630) };
            flpRooms = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10) };
            gbGrid.Controls.Add(flpRooms);
            this.Controls.Add(gbGrid);

            // Bên phải: Chi tiết phòng & Gọi dịch vụ
            gbRoomDetail = new GroupBox { Text = "Thông tin phòng & Gọi dịch vụ", Location = new Point(550, 15), Size = new Size(570, 630) };

            lblRoomName = new Label { Text = "Chưa chọn phòng", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.DarkBlue };
            lblRoomStatus = new Label { Text = "Trạng thái: --", Location = new Point(20, 65), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            lblRoomPrice = new Label { Text = "Giá phòng: -- VNĐ/giờ", Location = new Point(250, 65), AutoSize = true };
            lblTimeIn = new Label { Text = "Giờ vào: --", Location = new Point(20, 95), AutoSize = true, ForeColor = Color.DarkMagenta };

            gbRoomDetail.Controls.Add(lblRoomName);
            gbRoomDetail.Controls.Add(lblRoomStatus);
            gbRoomDetail.Controls.Add(lblRoomPrice);
            gbRoomDetail.Controls.Add(lblTimeIn);

            btnOpenRoom = new Button { Text = "MỞ PHÒNG HÁT", Location = new Point(20, 125), Width = 250, Height = 40, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Enabled = false };
            btnOpenRoom.Click += BtnOpenRoom_Click;
            gbRoomDetail.Controls.Add(btnOpenRoom);

            btnCheckout = new Button { Text = "THANH TOÁN (SqlTransaction)", Location = new Point(290, 125), Width = 260, Height = 40, BackColor = Color.Khaki, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Enabled = false };
            btnCheckout.Click += BtnCheckout_Click;
            gbRoomDetail.Controls.Add(btnCheckout);

            // GroupBox Gọi Dịch vụ / Đồ uống (Disconnected Model)
            GroupBox gbOrder = new GroupBox { Text = "Danh sách dịch vụ / đồ uống gọi tại phòng", Location = new Point(15, 180), Size = new Size(540, 435) };

            gbOrder.Controls.Add(new Label { Text = "Dịch vụ:", Location = new Point(15, 30), AutoSize = true });
            cboDichVu = new ComboBox { Location = new Point(75, 27), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            gbOrder.Controls.Add(cboDichVu);

            gbOrder.Controls.Add(new Label { Text = "SL:", Location = new Point(300, 30), AutoSize = true });
            numSoLuongDV = new NumericUpDown { Location = new Point(330, 27), Width = 60, Minimum = 1, Maximum = 100, Value = 1 };
            gbOrder.Controls.Add(numSoLuongDV);

            btnAddDV = new Button { Text = "+ Thêm", Location = new Point(405, 24), Width = 120, Height = 32, BackColor = Color.LightSkyBlue, Enabled = false };
            btnAddDV.Click += BtnAddDV_Click;
            gbOrder.Controls.Add(btnAddDV);

            dgvServices = new DataGridView { Location = new Point(15, 70), Size = new Size(510, 280), ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White };
            gbOrder.Controls.Add(dgvServices);

            lblTotalServices = new Label { Text = "Tiền dịch vụ: 0 VNĐ", Location = new Point(15, 360), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.DarkGreen };
            lblGrandTotal = new Label { Text = "TỔNG TẠM TÍNH: 0 VNĐ", Location = new Point(15, 395), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.DarkRed };
            gbOrder.Controls.Add(lblTotalServices);
            gbOrder.Controls.Add(lblGrandTotal);

            gbRoomDetail.Controls.Add(gbOrder);
            this.Controls.Add(gbRoomDetail);
        }

        private void LoadRoomGrid()
        {
            flpRooms.Controls.Clear();
            var rooms = bus.GetPhongHatList();

            foreach (var room in rooms)
            {
                Button btn = new Button
                {
                    Width = 145,
                    Height = 110,
                    Text = $"{room.TenPhong}\n({room.LoaiPhong})\n{room.GiaTheoGio:N0}đ/h\n[{room.TrangThai}]",
                    Tag = room,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Margin = new Padding(8)
                };

                if (room.TrangThai == "Đang hát")
                {
                    btn.BackColor = Color.LightCoral;
                    btn.ForeColor = Color.DarkRed;
                }
                else
                {
                    btn.BackColor = Color.LightGreen;
                    btn.ForeColor = Color.DarkGreen;
                }

                btn.Click += RoomButton_Click;
                flpRooms.Controls.Add(btn);
            }
        }

        private void LoadDichVuCombo()
        {
            cboDichVu.DataSource = bus.GetDichVuList();
            cboDichVu.DisplayMember = "TenDV";
            cboDichVu.ValueMember = "MaDV";
        }

        private void RoomButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            selectedRoom = btn.Tag as PhongHatDTO;
            if (selectedRoom == null) return;

            lblRoomName.Text = selectedRoom.TenPhong + $" ({selectedRoom.LoaiPhong})";
            lblRoomStatus.Text = "Trạng thái: " + selectedRoom.TrangThai;
            lblRoomPrice.Text = $"Giá phòng: {selectedRoom.GiaTheoGio:N0} VNĐ/giờ";

            if (selectedRoom.TrangThai == "Đang hát")
            {
                currentActiveHD = bus.GetActiveHoaDon(selectedRoom.MaPhong);
                lblTimeIn.Text = currentActiveHD != null ? $"Giờ vào: {currentActiveHD.GioVao:HH:mm dd/MM/yyyy}" : "Giờ vào: --";

                btnOpenRoom.Enabled = false;
                btnCheckout.Enabled = true;
                btnAddDV.Enabled = true;

                RefreshOrderedServicesGrid();
            }
            else
            {
                currentActiveHD = null;
                lblTimeIn.Text = "Giờ vào: --";

                btnOpenRoom.Enabled = true;
                btnCheckout.Enabled = false;
                btnAddDV.Enabled = false;

                dgvServices.DataSource = null;
                lblTotalServices.Text = "Tiền dịch vụ: 0 VNĐ";
                lblGrandTotal.Text = "TỔNG TẠM TÍNH: 0 VNĐ";
            }
        }

        private void RefreshOrderedServicesGrid()
        {
            if (currentActiveHD == null) return;

            SqlDataAdapter adapter;
            DataTable dt = bus.GetOrderedServices(currentActiveHD.MaHD, out adapter);
            dgvServices.DataSource = dt;

            if (dgvServices.Columns["MaHD"] != null) dgvServices.Columns["MaHD"].Visible = false;
            if (dgvServices.Columns["MaDV"] != null) dgvServices.Columns["MaDV"].Visible = false;
            if (dgvServices.Columns["Đơn giá"] != null) dgvServices.Columns["Đơn giá"].DefaultCellStyle.Format = "N0";
            if (dgvServices.Columns["Thành tiền"] != null) dgvServices.Columns["Thành tiền"].DefaultCellStyle.Format = "N0";

            decimal totalDV = 0;
            foreach (DataRow r in dt.Rows)
            {
                totalDV += Convert.ToDecimal(r["Thành tiền"]);
            }
            lblTotalServices.Text = $"Tiền dịch vụ: {totalDV:N0} VNĐ";

            // Tính tạm thời tiền phòng
            if (currentActiveHD != null)
            {
                TimeSpan duration = DateTime.Now - currentActiveHD.GioVao;
                double hours = duration.TotalHours;
                if (hours < 0.1) hours = 0.1;
                decimal tempRoomTotal = Math.Round((decimal)hours * selectedRoom.GiaTheoGio, 0);

                lblGrandTotal.Text = $"TỔNG TẠM TÍNH: {(tempRoomTotal + totalDV):N0} VNĐ";
            }
        }

        private void BtnOpenRoom_Click(object sender, EventArgs e)
        {
            if (selectedRoom == null || selectedRoom.TrangThai == "Đang hát") return;

            if (bus.OpenRoom(selectedRoom.MaPhong, out int maHD))
            {
                MessageBox.Show($"Mở {selectedRoom.TenPhong} thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadRoomGrid();

                // Tự động chọn lại phòng vừa mở
                selectedRoom.TrangThai = "Đang hát";
                currentActiveHD = bus.GetActiveHoaDon(selectedRoom.MaPhong);
                lblRoomStatus.Text = "Trạng thái: Đang hát";
                lblTimeIn.Text = currentActiveHD != null ? $"Giờ vào: {currentActiveHD.GioVao:HH:mm dd/MM/yyyy}" : "Giờ vào: --";

                btnOpenRoom.Enabled = false;
                btnCheckout.Enabled = true;
                btnAddDV.Enabled = true;
                RefreshOrderedServicesGrid();
            }
        }

        private void BtnAddDV_Click(object sender, EventArgs e)
        {
            if (currentActiveHD == null || cboDichVu.SelectedItem == null) return;

            DichVuDTO dv = cboDichVu.SelectedItem as DichVuDTO;
            int qty = (int)numSoLuongDV.Value;

            if (bus.AddServiceToRoom(currentActiveHD.MaHD, dv.MaDV, qty, dv.DonGia))
            {
                RefreshOrderedServicesGrid();
            }
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (selectedRoom == null || currentActiveHD == null) return;

            if (MessageBox.Show($"Xác nhận thanh toán hóa đơn cho {selectedRoom.TenPhong}?", "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (bus.Checkout(currentActiveHD.MaHD, selectedRoom.MaPhong, selectedRoom.GiaTheoGio, out decimal tongPhong, out decimal tongDV, out string errorMsg))
                {
                    decimal grandTotal = tongPhong + tongDV;
                    MessageBox.Show($"THANH TOÁN THÀNH CÔNG!\n- Tiền phòng: {tongPhong:N0} VNĐ\n- Tiền dịch vụ: {tongDV:N0} VNĐ\n=> TỔNG CỘNG: {grandTotal:N0} VNĐ\n(Phòng đã chuyển về trạng thái 'Trống' & đã tự động trừ tồn kho dịch vụ)", "Hóa đơn Karaoke", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadRoomGrid();
                    selectedRoom = null;
                    currentActiveHD = null;

                    lblRoomName.Text = "Chưa chọn phòng";
                    lblRoomStatus.Text = "Trạng thái: --";
                    lblRoomPrice.Text = "Giá phòng: -- VNĐ/giờ";
                    lblTimeIn.Text = "Giờ vào: --";

                    btnOpenRoom.Enabled = false;
                    btnCheckout.Enabled = false;
                    btnAddDV.Enabled = false;
                    dgvServices.DataSource = null;
                    lblTotalServices.Text = "Tiền dịch vụ: 0 VNĐ";
                    lblGrandTotal.Text = "TỔNG TẠM TÍNH: 0 VNĐ";
                }
                else
                {
                    MessageBox.Show(errorMsg, "Lỗi thanh toán (Rollback)", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Application.Run(new frmKaraokeMain());
        }
    }
    #endregion
}