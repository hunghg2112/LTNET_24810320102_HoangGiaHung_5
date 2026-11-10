using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProjectTaskApp
{
    #region ==================== 1. DTO LAYER ====================
    public class DuAnDTO
    {
        public int MaDuAn { get; set; }
        public string TenDuAn { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string TrangThai { get; set; }
    }

    public class ThanhVienDTO
    {
        public int MaThanhVien { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string ChuyenMon { get; set; }
    }

    public class CongViecDTO
    {
        public int MaTask { get; set; }
        public int MaDuAn { get; set; }
        public string TenDuAn { get; set; }
        public string TenTask { get; set; }
        public string MoTa { get; set; }
        public DateTime? HanNop { get; set; }
        public string DoUuTien { get; set; }
        public string TrangThai { get; set; }
        public string NguoiThucHien { get; set; }
    }

    public class OverdueTaskDTO
    {
        public int MaTask { get; set; }
        public string TenTask { get; set; }
        public string TenDuAn { get; set; }
        public DateTime HanNop { get; set; }
        public string DoUuTien { get; set; }
        public string TrangThai { get; set; }
        public int SoNgayTre { get; set; }
    }

    public class MemberStatsDTO
    {
        public int MaThanhVien { get; set; }
        public string HoTen { get; set; }
        public int TongTask { get; set; }
        public int TaskHoanThanh { get; set; }
        public double TyLeHoanThanh => TongTask > 0 ? Math.Round((double)TaskHoanThanh / TongTask * 100, 1) : 0;
    }
    #endregion

    #region ==================== 2. DAL LAYER ====================
    public class DatabaseAccess
    {
        protected static string connectionString = @"Server=localhost\SQLEXPRESS;Database=ProjectTaskDB;Integrated Security=True;TrustServerCertificate=True;";
    }

    public class DuAnDAL : DatabaseAccess
    {
        public List<DuAnDTO> GetAll()
        {
            List<DuAnDTO> list = new List<DuAnDTO>();
            string query = "SELECT MaDuAn, TenDuAn, NgayBatDau, NgayKetThuc, TrangThai FROM DuAn";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new DuAnDTO
                            {
                                MaDuAn = (int)reader["MaDuAn"],
                                TenDuAn = reader["TenDuAn"].ToString(),
                                NgayBatDau = reader["NgayBatDau"] != DBNull.Value ? (DateTime?)reader["NgayBatDau"] : null,
                                NgayKetThuc = reader["NgayKetThuc"] != DBNull.Value ? (DateTime?)reader["NgayKetThuc"] : null,
                                TrangThai = reader["TrangThai"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }

    public class ThanhVienDAL : DatabaseAccess
    {
        public List<ThanhVienDTO> GetAll()
        {
            List<ThanhVienDTO> list = new List<ThanhVienDTO>();
            string query = "SELECT MaThanhVien, HoTen, Email, ChuyenMon FROM ThanhVien";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new ThanhVienDTO
                            {
                                MaThanhVien = (int)reader["MaThanhVien"],
                                HoTen = reader["HoTen"].ToString(),
                                Email = reader["Email"]?.ToString(),
                                ChuyenMon = reader["ChuyenMon"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }

    public class CongViecDAL : DatabaseAccess
    {
        public DataTable GetTasksFiltered(int? maDuAn, string trangThai)
        {
            string query = @"SELECT cv.MaTask AS [Mã Task], da.TenDuAn AS [Dự án], cv.TenTask AS [Tên công việc], 
                                   cv.MoTa AS [Mô tả], cv.HanNop AS [Hạn nộp], cv.DoUuTien AS [Độ ưu tiên], 
                                   cv.TrangThai AS [Trạng thái],
                                   ISNULL(STRING_AGG(tv.HoTen, ', '), N'Chưa phân công') AS [Người thực hiện],
                                   cv.MaDuAn
                            FROM CongViec cv
                            JOIN DuAn da ON cv.MaDuAn = da.MaDuAn
                            LEFT JOIN PhanCong pc ON cv.MaTask = pc.MaTask
                            LEFT JOIN ThanhVien tv ON pc.MaThanhVien = tv.MaThanhVien
                            WHERE 1=1";

            if (maDuAn.HasValue && maDuAn.Value > 0)
                query += " AND cv.MaDuAn = @MaDuAn";

            if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
                query += " AND cv.TrangThai = @TrangThai";

            query += " GROUP BY cv.MaTask, da.TenDuAn, cv.TenTask, cv.MoTa, cv.HanNop, cv.DoUuTien, cv.TrangThai, cv.MaDuAn";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (maDuAn.HasValue && maDuAn.Value > 0)
                        cmd.Parameters.AddWithValue("@MaDuAn", maDuAn.Value);
                    if (!string.IsNullOrEmpty(trangThai) && trangThai != "Tất cả")
                        cmd.Parameters.AddWithValue("@TrangThai", trangThai);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        // Cập nhật Trạng thái Task 100% bằng Parameterized SQL Update
        public bool UpdateStatus(int maTask, string newStatus)
        {
            string query = "UPDATE CongViec SET TrangThai = @TrangThai WHERE MaTask = @MaTask";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TrangThai", newStatus);
                    cmd.Parameters.AddWithValue("@MaTask", maTask);
                    conn.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        // Tạo Task mới & Phân công đa thành viên bằng SqlTransaction
        public bool InsertTaskWithAssignees(CongViecDTO task, List<int> memberIds, out string errorMessage)
        {
            errorMessage = "";
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction trans = conn.BeginTransaction();

                try
                {
                    string queryTask = @"INSERT INTO CongViec (MaDuAn, TenTask, MoTa, HanNop, DoUuTien, TrangThai)
                                         VALUES (@MaDuAn, @TenTask, @MoTa, @HanNop, @DoUuTien, @TrangThai);
                                         SELECT SCOPE_IDENTITY();";
                    int generatedTaskId = 0;
                    using (SqlCommand cmdTask = new SqlCommand(queryTask, conn, trans))
                    {
                        cmdTask.Parameters.AddWithValue("@MaDuAn", task.MaDuAn);
                        cmdTask.Parameters.AddWithValue("@TenTask", task.TenTask);
                        cmdTask.Parameters.AddWithValue("@MoTa", (object)task.MoTa ?? DBNull.Value);
                        cmdTask.Parameters.AddWithValue("@HanNop", (object)task.HanNop ?? DBNull.Value);
                        cmdTask.Parameters.AddWithValue("@DoUuTien", task.DoUuTien);
                        cmdTask.Parameters.AddWithValue("@TrangThai", task.TrangThai);
                        generatedTaskId = Convert.ToInt32(cmdTask.ExecuteScalar());
                    }

                    if (memberIds != null && memberIds.Count > 0)
                    {
                        string queryPC = "INSERT INTO PhanCong (MaTask, MaThanhVien) VALUES (@MaTask, @MaThanhVien)";
                        foreach (int memberId in memberIds)
                        {
                            using (SqlCommand cmdPC = new SqlCommand(queryPC, conn, trans))
                            {
                                cmdPC.Parameters.AddWithValue("@MaTask", generatedTaskId);
                                cmdPC.Parameters.AddWithValue("@MaThanhVien", memberId);
                                cmdPC.ExecuteNonQuery();
                            }
                        }
                    }

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    errorMessage = "Lỗi tạo công việc: " + ex.Message;
                    return false;
                }
            }
        }

        // BÁO CÁO CẢNH BÁO TRỄ HẠN (OVERDUE) SỬ DỤNG SQLDATAREADER
        public List<OverdueTaskDTO> GetOverdueTasks()
        {
            List<OverdueTaskDTO> list = new List<OverdueTaskDTO>();
            string query = @"SELECT cv.MaTask, cv.TenTask, da.TenDuAn, cv.HanNop, cv.DoUuTien, cv.TrangThai,
                                   DATEDIFF(DAY, cv.HanNop, GETDATE()) AS SoNgayTre
                            FROM CongViec cv
                            JOIN DuAn da ON cv.MaDuAn = da.MaDuAn
                            WHERE cv.HanNop < GETDATE() AND cv.TrangThai != N'Done'";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new OverdueTaskDTO
                            {
                                MaTask = (int)reader["MaTask"],
                                TenTask = reader["TenTask"].ToString(),
                                TenDuAn = reader["TenDuAn"].ToString(),
                                HanNop = Convert.ToDateTime(reader["HanNop"]),
                                DoUuTien = reader["DoUuTien"].ToString(),
                                TrangThai = reader["TrangThai"].ToString(),
                                SoNgayTre = Convert.ToInt32(reader["SoNgayTre"])
                            });
                        }
                    }
                }
            }
            return list;
        }

        // THỐNG KÊ TỶ LỆ HOÀN THÀNH SỬ DỤNG EXECUTESCALAR & GROUP BY
        public List<MemberStatsDTO> GetMemberStats()
        {
            List<MemberStatsDTO> list = new List<MemberStatsDTO>();
            string queryMembers = "SELECT MaThanhVien, HoTen FROM ThanhVien";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                List<ThanhVienDTO> members = new List<ThanhVienDTO>();
                using (SqlCommand cmdM = new SqlCommand(queryMembers, conn))
                {
                    using (SqlDataReader r = cmdM.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            members.Add(new ThanhVienDTO { MaThanhVien = (int)r["MaThanhVien"], HoTen = r["HoTen"].ToString() });
                        }
                    }
                }

                foreach (var m in members)
                {
                    string sqlTotal = "SELECT COUNT(*) FROM PhanCong WHERE MaThanhVien = @MaTV";
                    int total = 0;
                    using (SqlCommand cmdT = new SqlCommand(sqlTotal, conn))
                    {
                        cmdT.Parameters.AddWithValue("@MaTV", m.MaThanhVien);
                        total = Convert.ToInt32(cmdT.ExecuteScalar());
                    }

                    string sqlDone = @"SELECT COUNT(*) FROM PhanCong pc 
                                       JOIN CongViec cv ON pc.MaTask = cv.MaTask 
                                       WHERE pc.MaThanhVien = @MaTV AND cv.TrangThai = N'Done'";
                    int done = 0;
                    using (SqlCommand cmdD = new SqlCommand(sqlDone, conn))
                    {
                        cmdD.Parameters.AddWithValue("@MaTV", m.MaThanhVien);
                        done = Convert.ToInt32(cmdD.ExecuteScalar());
                    }

                    list.Add(new MemberStatsDTO
                    {
                        MaThanhVien = m.MaThanhVien,
                        HoTen = m.HoTen,
                        TongTask = total,
                        TaskHoanThanh = done
                    });
                }
            }
            return list;
        }
    }
    #endregion

    #region ==================== 3. BUS LAYER ====================
    public class ProjectTaskBUS
    {
        private DuAnDAL daDAL = new DuAnDAL();
        private ThanhVienDAL tvDAL = new ThanhVienDAL();
        private CongViecDAL cvDAL = new CongViecDAL();

        public List<DuAnDTO> GetDuAnList() => daDAL.GetAll();
        public List<ThanhVienDTO> GetThanhVienList() => tvDAL.GetAll();

        public DataTable GetTasks(int? maDuAn, string trangThai) => cvDAL.GetTasksFiltered(maDuAn, trangThai);

        public bool ChangeStatus(int maTask, string newStatus) => cvDAL.UpdateStatus(maTask, newStatus);

        public bool CreateTask(CongViecDTO task, List<int> memberIds, out string error)
        {
            if (string.IsNullOrWhiteSpace(task.TenTask))
            {
                error = "Tên công việc không được để trống!";
                return false;
            }
            if (task.MaDuAn <= 0)
            {
                error = "Vui lòng chọn Dự án thuộc về!";
                return false;
            }
            return cvDAL.InsertTaskWithAssignees(task, memberIds, out error);
        }

        public List<OverdueTaskDTO> GetOverdueTasks() => cvDAL.GetOverdueTasks();
        public List<MemberStatsDTO> GetMemberStats() => cvDAL.GetMemberStats();
    }
    #endregion

    #region ==================== 4. GUI LAYER ====================
    public class frmProjectTaskMain : Form
    {
        private ProjectTaskBUS bus = new ProjectTaskBUS();

        // TAB 1: KANBAN & CÔNG VIỆC
        private ComboBox cboFilterProject, cboFilterStatus;
        private DataGridView dgvTasks;
        private Button btnStatusToDo, btnStatusInProgress, btnStatusReview, btnStatusDone;

        // TAB 1 (Phần tạo mới Task):
        private TextBox txtTenTask, txtMoTa;
        private ComboBox cboAddProject, cboDoUuTien;
        private DateTimePicker dtpHanNop;
        private CheckedListBox clbThanhVien;
        private Button btnCreateTask;

        // TAB 2: CẢNH BÁO TRỄ HẠN & THỐNG KÊ
        private DataGridView dgvOverdue, dgvStats;
        private Button btnRefreshReport;

        public frmProjectTaskMain()
        {
            TaoGiaoDien();
            LoadDataTab1();
            LoadDataTab2();
        }

        private void TaoGiaoDien()
        {
            this.Text = "Hệ Thống Quản Lý Công Việc & Tiến Độ Dự Án (Project & Task Management 3-Tier)";
            this.Size = new Size(1150, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5F);

            TabControl tabControl = new TabControl { Dock = DockStyle.Fill };
            TabPage tabTasks = new TabPage("Quản lý Tiến độ & Task");
            TabPage tabReport = new TabPage("Cảnh báo Trễ hạn & Thống kê %");

            // ---------------- TAB 1: CÔNG VIỆC ----------------
            Panel pnlTopFilter = new Panel { Dock = DockStyle.Top, Height = 55 };
            pnlTopFilter.Controls.Add(new Label { Text = "Dự án:", Location = new Point(15, 18), AutoSize = true });
            cboFilterProject = new ComboBox { Location = new Point(65, 15), Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterProject.SelectedIndexChanged += (s, e) => LoadTasksGrid();
            pnlTopFilter.Controls.Add(cboFilterProject);

            pnlTopFilter.Controls.Add(new Label { Text = "Trạng thái:", Location = new Point(340, 18), AutoSize = true });
            cboFilterStatus = new ComboBox { Location = new Point(415, 15), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFilterStatus.Items.AddRange(new string[] { "Tất cả", "To Do", "In Progress", "Review", "Done" });
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged += (s, e) => LoadTasksGrid();
            pnlTopFilter.Controls.Add(cboFilterStatus);

            // Các nút chuyển trạng thái nhanh
            btnStatusToDo = new Button { Text = "→ To Do", Location = new Point(590, 13), Width = 90, Height = 32, BackColor = Color.LightGray };
            btnStatusInProgress = new Button { Text = "→ In Progress", Location = new Point(685, 13), Width = 110, Height = 32, BackColor = Color.LightSkyBlue };
            btnStatusReview = new Button { Text = "→ Review", Location = new Point(800, 13), Width = 95, Height = 32, BackColor = Color.Khaki };
            btnStatusDone = new Button { Text = "→ Done (Hoàn tất)", Location = new Point(900, 13), Width = 140, Height = 32, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            btnStatusToDo.Click += (s, e) => ChangeSelectedTaskStatus("To Do");
            btnStatusInProgress.Click += (s, e) => ChangeSelectedTaskStatus("In Progress");
            btnStatusReview.Click += (s, e) => ChangeSelectedTaskStatus("Review");
            btnStatusDone.Click += (s, e) => ChangeSelectedTaskStatus("Done");

            pnlTopFilter.Controls.Add(btnStatusToDo);
            pnlTopFilter.Controls.Add(btnStatusInProgress);
            pnlTopFilter.Controls.Add(btnStatusReview);
            pnlTopFilter.Controls.Add(btnStatusDone);

            tabTasks.Controls.Add(pnlTopFilter);

            // GroupBox Tạo Task mới bên trái
            GroupBox gbCreate = new GroupBox { Text = "Tạo công việc mới (SqlTransaction)", Location = new Point(15, 65), Size = new Size(340, 580) };

            gbCreate.Controls.Add(new Label { Text = "Dự án:", Location = new Point(15, 30), AutoSize = true });
            cboAddProject = new ComboBox { Location = new Point(110, 27), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            gbCreate.Controls.Add(cboAddProject);

            gbCreate.Controls.Add(new Label { Text = "Tên Task:", Location = new Point(15, 70), AutoSize = true });
            txtTenTask = new TextBox { Location = new Point(110, 67), Width = 210 };
            gbCreate.Controls.Add(txtTenTask);

            gbCreate.Controls.Add(new Label { Text = "Mô tả:", Location = new Point(15, 110), AutoSize = true });
            txtMoTa = new TextBox { Location = new Point(110, 107), Width = 210, Multiline = true, Height = 60 };
            gbCreate.Controls.Add(txtMoTa);

            gbCreate.Controls.Add(new Label { Text = "Hạn nộp:", Location = new Point(15, 185), AutoSize = true });
            dtpHanNop = new DateTimePicker { Location = new Point(110, 182), Width = 210, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy" };
            gbCreate.Controls.Add(dtpHanNop);

            gbCreate.Controls.Add(new Label { Text = "Độ ưu tiên:", Location = new Point(15, 225), AutoSize = true });
            cboDoUuTien = new ComboBox { Location = new Point(110, 222), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            cboDoUuTien.Items.AddRange(new string[] { "Cao", "Trung bình", "Thấp" });
            cboDoUuTien.SelectedIndex = 1;
            gbCreate.Controls.Add(cboDoUuTien);

            gbCreate.Controls.Add(new Label { Text = "Phân công cho:", Location = new Point(15, 265), AutoSize = true });
            clbThanhVien = new CheckedListBox { Location = new Point(15, 290), Size = new Size(305, 210) };
            gbCreate.Controls.Add(clbThanhVien);

            btnCreateTask = new Button { Text = "TẠO TASK & PHÂN CÔNG", Location = new Point(15, 515), Width = 305, Height = 45, BackColor = Color.LightGreen, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            btnCreateTask.Click += BtnCreateTask_Click;
            gbCreate.Controls.Add(btnCreateTask);

            tabTasks.Controls.Add(gbCreate);

            dgvTasks = new DataGridView { Location = new Point(370, 65), Size = new Size(745, 580), ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            tabTasks.Controls.Add(dgvTasks);

            // ---------------- TAB 2: BÁO CÁO & THỐNG KÊ ----------------
            Panel pnlReportTop = new Panel { Dock = DockStyle.Top, Height = 50 };
            btnRefreshReport = new Button { Text = "Làm mới Báo cáo & Thống kê", Location = new Point(15, 10), Width = 240, Height = 32 };
            btnRefreshReport.Click += (s, e) => LoadDataTab2();
            pnlReportTop.Controls.Add(btnRefreshReport);
            tabReport.Controls.Add(pnlReportTop);

            GroupBox gbOverdue = new GroupBox { Text = "Cảnh báo Công việc Trễ hạn (Thực thi SqlDataReader)", Location = new Point(15, 60), Size = new Size(1100, 270) };
            dgvOverdue = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            gbOverdue.Controls.Add(dgvOverdue);
            tabReport.Controls.Add(gbOverdue);

            GroupBox gbStats = new GroupBox { Text = "Thống kê tỷ lệ hoàn thành công việc theo Thành viên (Thực thi ExecuteScalar & GROUP BY)", Location = new Point(15, 340), Size = new Size(1100, 300) };
            dgvStats = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            gbStats.Controls.Add(dgvStats);
            tabReport.Controls.Add(gbStats);

            tabControl.TabPages.Add(tabTasks);
            tabControl.TabPages.Add(tabReport);
            this.Controls.Add(tabControl);
        }

        // --- TAB 1 LOGIC ---
        private void LoadDataTab1()
        {
            var projects = bus.GetDuAnList();
            var filterProjects = new List<DuAnDTO> { new DuAnDTO { MaDuAn = 0, TenDuAn = "Tất cả dự án" } };
            filterProjects.AddRange(projects);

            cboFilterProject.DisplayMember = "TenDuAn"; cboFilterProject.ValueMember = "MaDuAn";
            cboFilterProject.DataSource = filterProjects;

            cboAddProject.DisplayMember = "TenDuAn"; cboAddProject.ValueMember = "MaDuAn";
            cboAddProject.DataSource = projects;

            var members = bus.GetThanhVienList();
            clbThanhVien.Items.Clear();
            foreach (var m in members)
            {
                clbThanhVien.Items.Add(m, false);
            }
            clbThanhVien.DisplayMember = "HoTen";

            LoadTasksGrid();
        }

        private void LoadTasksGrid()
        {
            int? maDuAn = cboFilterProject.SelectedValue != null ? (int?)Convert.ToInt32(cboFilterProject.SelectedValue) : null;
            string trangThai = cboFilterStatus.SelectedItem != null ? cboFilterStatus.SelectedItem.ToString() : "Tất cả";

            dgvTasks.DataSource = bus.GetTasks(maDuAn, trangThai);

            if (dgvTasks.Columns["MaDuAn"] != null) dgvTasks.Columns["MaDuAn"].Visible = false;
            if (dgvTasks.Columns["Hạn nộp"] != null) dgvTasks.Columns["Hạn nộp"].DefaultCellStyle.Format = "dd/MM/yyyy";
        }

        private void ChangeSelectedTaskStatus(string newStatus)
        {
            if (dgvTasks.CurrentRow == null || dgvTasks.CurrentRow.Index < 0)
            {
                MessageBox.Show("Vui lòng chọn một công việc trong bảng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int maTask = Convert.ToInt32(dgvTasks.CurrentRow.Cells["Mã Task"].Value);
            if (bus.ChangeStatus(maTask, newStatus))
            {
                LoadTasksGrid();
                LoadDataTab2();
            }
        }

        private void BtnCreateTask_Click(object sender, EventArgs e)
        {
            if (cboAddProject.SelectedValue == null) return;

            List<int> selectedMemberIds = new List<int>();
            foreach (var item in clbThanhVien.CheckedItems)
            {
                if (item is ThanhVienDTO m)
                {
                    selectedMemberIds.Add(m.MaThanhVien);
                }
            }

            CongViecDTO task = new CongViecDTO
            {
                MaDuAn = Convert.ToInt32(cboAddProject.SelectedValue),
                TenTask = txtTenTask.Text.Trim(),
                MoTa = txtMoTa.Text.Trim(),
                HanNop = dtpHanNop.Value,
                DoUuTien = cboDoUuTien.SelectedItem.ToString(),
                TrangThai = "To Do"
            };

            if (bus.CreateTask(task, selectedMemberIds, out string err))
            {
                MessageBox.Show("Tạo Task mới và phân công thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtTenTask.Clear();
                txtMoTa.Clear();
                for (int i = 0; i < clbThanhVien.Items.Count; i++) clbThanhVien.SetItemChecked(i, false);

                LoadTasksGrid();
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
            dgvOverdue.DataSource = bus.GetOverdueTasks();
            if (dgvOverdue.Columns["MaTask"] != null) dgvOverdue.Columns["MaTask"].HeaderText = "Mã Task";
            if (dgvOverdue.Columns["TenTask"] != null) dgvOverdue.Columns["TenTask"].HeaderText = "Tên công việc";
            if (dgvOverdue.Columns["TenDuAn"] != null) dgvOverdue.Columns["TenDuAn"].HeaderText = "Dự án";
            if (dgvOverdue.Columns["HanNop"] != null) { dgvOverdue.Columns["HanNop"].HeaderText = "Hạn nộp"; dgvOverdue.Columns["HanNop"].DefaultCellStyle.Format = "dd/MM/yyyy"; }
            if (dgvOverdue.Columns["DoUuTien"] != null) dgvOverdue.Columns["DoUuTien"].HeaderText = "Độ ưu tiên";
            if (dgvOverdue.Columns["TrangThai"] != null) dgvOverdue.Columns["TrangThai"].HeaderText = "Trạng thái";
            if (dgvOverdue.Columns["SoNgayTre"] != null) { dgvOverdue.Columns["SoNgayTre"].HeaderText = "Số ngày trễ"; dgvOverdue.Columns["SoNgayTre"].DefaultCellStyle.ForeColor = Color.Red; }

            dgvStats.DataSource = bus.GetMemberStats();
            if (dgvStats.Columns["MaThanhVien"] != null) dgvStats.Columns["MaThanhVien"].HeaderText = "Mã TV";
            if (dgvStats.Columns["HoTen"] != null) dgvStats.Columns["HoTen"].HeaderText = "Họ và tên thành viên";
            if (dgvStats.Columns["TongTask"] != null) dgvStats.Columns["TongTask"].HeaderText = "Tổng số Task giao";
            if (dgvStats.Columns["TaskHoanThanh"] != null) dgvStats.Columns["TaskHoanThanh"].HeaderText = "Số Task đã xong";
            if (dgvStats.Columns["TyLeHoanThanh"] != null) { dgvStats.Columns["TyLeHoanThanh"].HeaderText = "Tỷ lệ hoàn thành (%)"; dgvStats.Columns["TyLeHoanThanh"].DefaultCellStyle.Format = "N1"; }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmProjectTaskMain());
        }
    }
    #endregion
}