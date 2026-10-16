using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace QuanLyNhapKho;
public partial class FrmNhapKho : Form
{
    private readonly DataTable _details = new();
    public FrmNhapKho()
    {
        InitializeComponent();
        _details.Columns.Add("MaSach", typeof(string));
        _details.Columns.Add("TenSach", typeof(string));
        _details.Columns.Add("SoLuong", typeof(int));
        _details.Columns.Add("DonGia", typeof(decimal));
        _details.Columns.Add("ThanhTien", typeof(decimal), "SoLuong * DonGia");
        dgvChiTiet.DataSource = _details;
        Load += (_, _) => { txtMaPN.Text = GenerateNextCode(); LoadSuppliers(); SearchHistory(); };
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) btnHuy.PerformClick(); if (e.Control && e.KeyCode == Keys.Enter) btnLuuPhieu.PerformClick(); };
    }
    private string GenerateNextCode()
    {
        try
        {
            using var c=Database.OpenConnection();
            using var cmd=new SqlCommand("SELECT ISNULL(MAX(TRY_CONVERT(int,SUBSTRING(MaPN,3,10))),0)+1 FROM PhieuNhap WHERE MaPN LIKE 'PN%'",c);
            return "PN"+Convert.ToInt32(cmd.ExecuteScalar()).ToString("0000");
        }
        catch { return "PN"+DateTime.Now.ToString("MMddHHmm"); }
    }
    private void LoadSuppliers()
    {
        try
        {
            using var c=Database.OpenConnection();
            using var da=new SqlDataAdapter("SELECT MaNCC,TenNCC FROM NhaCungCap ORDER BY TenNCC",c);
            var t=new DataTable(); da.Fill(t);
            cboNCC.DataSource=t.Copy(); cboNCC.DisplayMember="TenNCC"; cboNCC.ValueMember="MaNCC";
            var filter=t.Copy(); filter.Rows.InsertAt(filter.NewRow(),0); filter.Rows[0]["MaNCC"]=0; filter.Rows[0]["TenNCC"]="Tất cả";
            cboLocNCC.DataSource=filter; cboLocNCC.DisplayMember="TenNCC"; cboLocNCC.ValueMember="MaNCC";
        }
        catch(Exception ex) { SetStatus("Không kết nối được CSDL. Hãy kiểm tra SQL Server/chuỗi kết nối: "+ex.Message); }
    }
    private void btnThemDong_Click(object? sender,EventArgs e)
    {
        var code=txtMaSach.Text.Trim();
        if(code.Length==0 || !int.TryParse(txtSoLuong.Text,out int qty) || qty<=0 ||
           !decimal.TryParse(txtDonGia.Text.Trim().Replace(",",""),NumberStyles.Number,CultureInfo.InvariantCulture,out decimal price) || price<0)
        { MessageBox.Show("Nhập mã sách, số lượng nguyên lớn hơn 0 và đơn giá không âm.","Dữ liệu chưa hợp lệ",MessageBoxButtons.OK,MessageBoxIcon.Warning); return; }
        try
        {
            string name;
            using(var c=Database.OpenConnection())
            using(var cmd=new SqlCommand("SELECT TenSach FROM Sach WHERE MaSach=@ma AND TrangThai=N'Hoạt động'",c))
            { cmd.Parameters.Add("@ma",SqlDbType.VarChar,10).Value=code; name=Convert.ToString(cmd.ExecuteScalar())??""; }
            if(name=="") { MessageBox.Show("Không tìm thấy sách đang hoạt động với mã "+code); return; }
            var existing=_details.Rows.Cast<DataRow>().FirstOrDefault(r=>r["MaSach"].ToString()==code);
            if(existing!=null) { existing["SoLuong"]=Convert.ToInt32(existing["SoLuong"])+qty; existing["DonGia"]=price; }
            else _details.Rows.Add(code,name,qty,price);
            txtMaSach.Clear(); txtSoLuong.Clear(); txtDonGia.Clear(); UpdateTotal();
        }
        catch(Exception ex) { MessageBox.Show("Không thể thêm sách: "+ex.Message); }
    }
    private void btnSuaDong_Click(object? sender,EventArgs e)
    {
        if(dgvChiTiet.CurrentRow?.DataBoundItem is not DataRowView r) return;
        if(!int.TryParse(txtSoLuong.Text,out int qty)||qty<=0||!decimal.TryParse(txtDonGia.Text,out decimal price)||price<0)
        { MessageBox.Show("Số lượng phải lớn hơn 0, đơn giá không âm."); return; }
        r["SoLuong"]=qty; r["DonGia"]=price; UpdateTotal();
    }
    private void btnXoaDong_Click(object? sender,EventArgs e)
    { if(dgvChiTiet.CurrentRow?.DataBoundItem is DataRowView r) { r.Row.Delete(); UpdateTotal(); } }
    private void dgvChiTiet_SelectionChanged(object? sender,EventArgs e)
    {
        if(dgvChiTiet.CurrentRow?.DataBoundItem is DataRowView r)
        { txtMaSach.Text=r["MaSach"].ToString(); txtSoLuong.Text=r["SoLuong"].ToString(); txtDonGia.Text=Convert.ToDecimal(r["DonGia"]).ToString("0"); }
    }
    private decimal Total() => _details.Rows.Cast<DataRow>().Where(r=>r.RowState!=DataRowState.Deleted).Sum(r=>Convert.ToInt32(r["SoLuong"])*Convert.ToDecimal(r["DonGia"]));
    private void UpdateTotal() { lblTongTien.Text=$"Tổng tiền: {Total():N0} VNĐ"; }
    private void btnLuuPhieu_Click(object? sender,EventArgs e)
    {
        if(cboNCC.SelectedValue==null || cboNCC.SelectedValue is DataRowView || _details.Rows.Count==0)
        { MessageBox.Show("Vui lòng chọn nhà cung cấp và thêm ít nhất một dòng sách."); return; }
        try
        {
            using var c=Database.OpenConnection(); using var tx=c.BeginTransaction();
            try
            {
                using(var check=new SqlCommand("SELECT COUNT(*) FROM DotKiemKe WHERE TrangThai=N'Đang kiểm kê'",c,tx))
                    if(Convert.ToInt32(check.ExecuteScalar())>0) throw new InvalidOperationException("Đang có đợt kiểm kê mở. Hãy hoàn tất kiểm kê trước khi nhập kho.");
                using(var cmd=new SqlCommand(@"INSERT INTO PhieuNhap(MaPN,NgayNhap,MaNCC,NguoiLap,GhiChu) VALUES(@pn,@ngay,@ncc,@nguoi,@ghichu)",c,tx))
                {
                    cmd.Parameters.Add("@pn",SqlDbType.VarChar,12).Value=txtMaPN.Text;
                    cmd.Parameters.Add("@ngay",SqlDbType.DateTime).Value=dtpNgayNhap.Value.Date;
                    cmd.Parameters.Add("@ncc",SqlDbType.Int).Value=Convert.ToInt32(cboNCC.SelectedValue);
                    cmd.Parameters.Add("@nguoi",SqlDbType.NVarChar,100).Value="Thủ thư";
                    cmd.Parameters.Add("@ghichu",SqlDbType.NVarChar,255).Value=txtGhiChu.Text.Trim();
                    cmd.ExecuteNonQuery();
                }
                foreach(DataRow r in _details.Rows)
                {
                    if(r.RowState==DataRowState.Deleted) continue;
                    using var cmd=new SqlCommand("INSERT INTO ChiTietPhieuNhap(MaPN,MaSach,SoLuong,DonGia) VALUES(@pn,@ma,@sl,@gia); UPDATE Sach SET SoLuongTon=SoLuongTon+@sl WHERE MaSach=@ma",c,tx);
                    cmd.Parameters.Add("@pn",SqlDbType.VarChar,12).Value=txtMaPN.Text;
                    cmd.Parameters.Add("@ma",SqlDbType.VarChar,10).Value=r["MaSach"];
                    cmd.Parameters.Add("@sl",SqlDbType.Int).Value=Convert.ToInt32(r["SoLuong"]);
                    cmd.Parameters.Add("@gia",SqlDbType.Decimal).Value=Convert.ToDecimal(r["DonGia"]);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch { tx.Rollback(); throw; }
            MessageBox.Show("Lưu phiếu nhập kho thành công!","Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Information);
            ResetForm(); SearchHistory();
        }
        catch(Exception ex) { MessageBox.Show("Không thể lưu phiếu nhập: "+ex.Message,"Lỗi",MessageBoxButtons.OK,MessageBoxIcon.Error); }
    }
    private void ResetForm()
    { _details.Clear(); txtGhiChu.Clear(); dtpNgayNhap.Value=DateTime.Today; txtMaPN.Text=GenerateNextCode(); UpdateTotal(); }
    private void btnHuy_Click(object? sender,EventArgs e) { if(_details.Rows.Count>0 && MessageBox.Show("Hủy phiếu đang nhập?","Xác nhận",MessageBoxButtons.YesNo)==DialogResult.No)return; ResetForm(); }
    private void btnTimKiem_Click(object? sender,EventArgs e) => SearchHistory();
    private void btnLamMoi_Click(object? sender,EventArgs e) { txtSearchMaPN.Clear(); dtpTuNgay.Checked=false; dtpDenNgay.Checked=false; if(cboLocNCC.Items.Count>0)cboLocNCC.SelectedIndex=0; SearchHistory(); }
    private void SearchHistory()
    {
        try
        {
            var sql=@"SELECT p.MaPN,p.NgayNhap,n.TenNCC,ISNULL(SUM(c.SoLuong*c.DonGia),0) TongTien
                      FROM PhieuNhap p JOIN NhaCungCap n ON n.MaNCC=p.MaNCC
                      LEFT JOIN ChiTietPhieuNhap c ON c.MaPN=p.MaPN WHERE 1=1";
            using var con=Database.OpenConnection(); using var cmd=new SqlCommand(); cmd.Connection=con;
            if(txtSearchMaPN.Text.Trim()!="") { sql+=" AND p.MaPN LIKE @ma"; cmd.Parameters.Add("@ma",SqlDbType.VarChar,12).Value="%"+txtSearchMaPN.Text.Trim()+"%"; }
            if(dtpTuNgay.Checked) { sql+=" AND p.NgayNhap>=@tu"; cmd.Parameters.Add("@tu",SqlDbType.DateTime).Value=dtpTuNgay.Value.Date; }
            if(dtpDenNgay.Checked) { sql+=" AND p.NgayNhap<@den"; cmd.Parameters.Add("@den",SqlDbType.DateTime).Value=dtpDenNgay.Value.Date.AddDays(1); }
            if(cboLocNCC.SelectedValue!=null && cboLocNCC.SelectedValue is not DataRowView && Convert.ToInt32(cboLocNCC.SelectedValue)>0)
            { sql+=" AND p.MaNCC=@ncc"; cmd.Parameters.Add("@ncc",SqlDbType.Int).Value=Convert.ToInt32(cboLocNCC.SelectedValue); }
            cmd.CommandText=sql+" GROUP BY p.MaPN,p.NgayNhap,n.TenNCC ORDER BY p.NgayNhap DESC,p.MaPN DESC";
            var t=new DataTable(); using(var da=new SqlDataAdapter(cmd)) da.Fill(t);
            dgvLichSu.DataSource=t;
            if(dgvLichSu.Columns["MaPN"]!=null) dgvLichSu.Columns["MaPN"].HeaderText="Mã phiếu";
            if(dgvLichSu.Columns["NgayNhap"]!=null) { dgvLichSu.Columns["NgayNhap"].HeaderText="Ngày nhập"; dgvLichSu.Columns["NgayNhap"].DefaultCellStyle.Format="dd/MM/yyyy"; }
            if(dgvLichSu.Columns["TenNCC"]!=null) dgvLichSu.Columns["TenNCC"].HeaderText="Nhà cung cấp";
            if(dgvLichSu.Columns["TongTien"]!=null) { dgvLichSu.Columns["TongTien"].HeaderText="Tổng tiền (VNĐ)"; dgvLichSu.Columns["TongTien"].DefaultCellStyle.Format="N0"; }
            SetStatus($"Tìm thấy {t.Rows.Count} phiếu nhập. Nhấp đúp một phiếu để xem chi tiết.");
        }
        catch(Exception ex) { SetStatus("Không tải được lịch sử nhập kho: "+ex.Message); }
    }
    private void dgvLichSu_CellDoubleClick(object? sender,DataGridViewCellEventArgs e)
    {
        if(e.RowIndex<0 || dgvLichSu.Rows[e.RowIndex].Cells["MaPN"].Value==null)return;
        string pn=Convert.ToString(dgvLichSu.Rows[e.RowIndex].Cells["MaPN"].Value)!;
        try
        {
            using var c=Database.OpenConnection();
            using var da=new SqlDataAdapter(@"SELECT c.MaSach,s.TenSach,c.SoLuong,c.DonGia,c.SoLuong*c.DonGia AS ThanhTien
                FROM ChiTietPhieuNhap c JOIN Sach s ON s.MaSach=c.MaSach WHERE c.MaPN=@pn ORDER BY c.MaCTPN",c);
            da.SelectCommand!.Parameters.Add("@pn",SqlDbType.VarChar,12).Value=pn;
            var t=new DataTable(); da.Fill(t);
            using var f=new Form{Text="Chi tiết phiếu "+pn,StartPosition=FormStartPosition.CenterParent,Size=new Size(680,360),BackColor=Color.White};
            var g=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,DataSource=t,AllowUserToAddRows=false,RowHeadersVisible=false};
            f.Controls.Add(g); f.ShowDialog(this);
        }
        catch(Exception ex) { MessageBox.Show("Không đọc được chi tiết phiếu: "+ex.Message); }
    }
    private void SetStatus(string message) => lblTrangThai.Text=message;
}
