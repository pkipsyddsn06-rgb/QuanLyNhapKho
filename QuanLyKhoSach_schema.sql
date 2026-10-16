IF DB_ID(N'QuanLyKhoSach') IS NULL CREATE DATABASE QuanLyKhoSach;
GO
USE QuanLyKhoSach;
GO
IF OBJECT_ID('dbo.vw_BaoCaoKiemKe','V') IS NOT NULL DROP VIEW dbo.vw_BaoCaoKiemKe;
IF OBJECT_ID('dbo.vw_ThongKeTonKho','V') IS NOT NULL DROP VIEW dbo.vw_ThongKeTonKho;
IF OBJECT_ID('dbo.vw_DanhSachSach','V') IS NOT NULL DROP VIEW dbo.vw_DanhSachSach;
IF OBJECT_ID('dbo.ChiTietKiemKe','U') IS NOT NULL DROP TABLE dbo.ChiTietKiemKe;
IF OBJECT_ID('dbo.DotKiemKe','U') IS NOT NULL DROP TABLE dbo.DotKiemKe;
IF OBJECT_ID('dbo.ChiTietPhieuNhap','U') IS NOT NULL DROP TABLE dbo.ChiTietPhieuNhap;
IF OBJECT_ID('dbo.PhieuNhap','U') IS NOT NULL DROP TABLE dbo.PhieuNhap;
IF OBJECT_ID('dbo.Sach','U') IS NOT NULL DROP TABLE dbo.Sach;
IF OBJECT_ID('dbo.NhaCungCap','U') IS NOT NULL DROP TABLE dbo.NhaCungCap;
IF OBJECT_ID('dbo.TheLoai','U') IS NOT NULL DROP TABLE dbo.TheLoai;
GO
CREATE TABLE TheLoai(MaTheLoai INT IDENTITY(1,1) PRIMARY KEY,TenTheLoai NVARCHAR(100) NOT NULL UNIQUE,MoTa NVARCHAR(255) NULL);
CREATE TABLE NhaCungCap(MaNCC INT IDENTITY(1,1) PRIMARY KEY,TenNCC NVARCHAR(150) NOT NULL,DiaChi NVARCHAR(255) NULL,SoDienThoai VARCHAR(15) NULL,Email VARCHAR(100) NULL);
CREATE TABLE Sach(MaSach VARCHAR(10) PRIMARY KEY,TenSach NVARCHAR(255) NOT NULL,TacGia NVARCHAR(150) NOT NULL,MaTheLoai INT NOT NULL REFERENCES TheLoai(MaTheLoai),NhaXuatBan NVARCHAR(150) NULL,NamXuatBan INT NULL CHECK(NamXuatBan BETWEEN 1000 AND 2100),GiaBia DECIMAL(12,0) NOT NULL DEFAULT 0 CHECK(GiaBia>=0),SoLuongTon INT NOT NULL DEFAULT 0 CHECK(SoLuongTon>=0),ViTri NVARCHAR(50) NULL,TrangThai NVARCHAR(10) NOT NULL DEFAULT N'Hoạt động' CHECK(TrangThai IN(N'Hoạt động',N'Ẩn')));
CREATE TABLE PhieuNhap(MaPN VARCHAR(12) PRIMARY KEY,NgayNhap DATETIME NOT NULL DEFAULT GETDATE(),MaNCC INT NOT NULL REFERENCES NhaCungCap(MaNCC),NguoiLap NVARCHAR(100) NULL,GhiChu NVARCHAR(255) NULL);
CREATE TABLE ChiTietPhieuNhap(MaCTPN INT IDENTITY(1,1) PRIMARY KEY,MaPN VARCHAR(12) NOT NULL REFERENCES PhieuNhap(MaPN) ON DELETE CASCADE,MaSach VARCHAR(10) NOT NULL REFERENCES Sach(MaSach),SoLuong INT NOT NULL CHECK(SoLuong>0),DonGia DECIMAL(12,0) NOT NULL CHECK(DonGia>=0),CONSTRAINT UQ_CTPN UNIQUE(MaPN,MaSach));
CREATE TABLE DotKiemKe(MaDot VARCHAR(12) PRIMARY KEY,TenDot NVARCHAR(150) NOT NULL,NgayBatDau DATETIME NOT NULL DEFAULT GETDATE(),NgayKetThuc DATETIME NULL,TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Đang kiểm kê' CHECK(TrangThai IN(N'Đang kiểm kê',N'Đã đối chiếu',N'Đã hoàn tất')),NguoiLap NVARCHAR(100) NULL,GhiChu NVARCHAR(255) NULL);
CREATE TABLE ChiTietKiemKe(MaCTKK INT IDENTITY(1,1) PRIMARY KEY,MaDot VARCHAR(12) NOT NULL REFERENCES DotKiemKe(MaDot) ON DELETE CASCADE,MaSach VARCHAR(10) NOT NULL REFERENCES Sach(MaSach),SoLuongHeThong INT NOT NULL,SoLuongThucTe INT NULL CHECK(SoLuongThucTe>=0),ChenhLech AS (SoLuongThucTe-SoLuongHeThong) PERSISTED,NguyenNhan NVARCHAR(255) NULL,DaXuLy BIT NOT NULL DEFAULT 0,NgayXuLy DATETIME NULL,CONSTRAINT UQ_CTKK UNIQUE(MaDot,MaSach));
GO
CREATE VIEW vw_DanhSachSach AS SELECT s.MaSach,s.TenSach,s.TacGia,s.MaTheLoai,t.TenTheLoai,s.NhaXuatBan,s.NamXuatBan,s.GiaBia,s.SoLuongTon,s.ViTri,s.TrangThai FROM Sach s JOIN TheLoai t ON t.MaTheLoai=s.MaTheLoai;
GO
CREATE VIEW vw_ThongKeTonKho AS SELECT t.TenTheLoai,COUNT(s.MaSach) SoDauSach,ISNULL(SUM(s.SoLuongTon),0) TongSoLuong,ISNULL(SUM(s.SoLuongTon*s.GiaBia),0) TongGiaTri FROM TheLoai t LEFT JOIN Sach s ON s.MaTheLoai=t.MaTheLoai AND s.TrangThai=N'Hoạt động' GROUP BY t.TenTheLoai;
GO
CREATE VIEW vw_BaoCaoKiemKe AS SELECT d.MaDot,d.TenDot,d.TrangThai,c.MaSach,s.TenSach,c.SoLuongHeThong,c.SoLuongThucTe,c.ChenhLech,CASE WHEN c.SoLuongThucTe IS NULL THEN N'Chưa đếm' WHEN c.ChenhLech=0 THEN N'Khớp' WHEN c.ChenhLech>0 THEN N'Thừa' ELSE N'Thiếu' END KetQua,c.NguyenNhan,c.DaXuLy,c.NgayXuLy FROM DotKiemKe d JOIN ChiTietKiemKe c ON c.MaDot=d.MaDot JOIN Sach s ON s.MaSach=c.MaSach;
GO
INSERT TheLoai(TenTheLoai,MoTa) VALUES(N'Toán học',N'Giáo trình toán'),(N'Công nghệ thông tin',N'Lập trình, cơ sở dữ liệu'),(N'Ngoại ngữ',NULL);
INSERT NhaCungCap(TenNCC,DiaChi,SoDienThoai,Email) VALUES(N'Nhà sách Tuổi Trẻ',N'Hà Nội','0241234567','tuoitre@example.com'),(N'Công ty Sách Đại học',N'Hà Nội','0247654321','daihoc@example.com');
INSERT Sach(MaSach,TenSach,TacGia,MaTheLoai,NhaXuatBan,NamXuatBan,GiaBia,SoLuongTon,ViTri) VALUES
('SA001',N'Giải tích 1',N'Nguyễn Văn A',1,N'NXB Giáo dục',2020,85000,10,N'Kệ A - Ngăn 1'),
('SA002',N'Lập trình C#',N'Trần Văn B',2,N'NXB Bách khoa',2022,120000,6,N'Kệ B - Ngăn 2'),
('SA003',N'Cơ sở dữ liệu',N'Lê Thị C',2,N'NXB Bách khoa',2021,95000,1,N'Kệ B - Ngăn 3'),
('SA004',N'Đại số tuyến tính',N'Phạm Văn D',1,N'NXB Giáo dục',2019,70000,8,N'Kệ A - Ngăn 2'),
('SA005',N'Tiếng Anh chuyên ngành',N'Hoàng Thị E',3,N'NXB Đại học',2023,60000,0,N'Kệ D - Ngăn 1');
INSERT PhieuNhap(MaPN,MaNCC,NguoiLap,GhiChu) VALUES('PN0001',1,N'Thủ thư 1',N'Nhập đầu kỳ');
INSERT ChiTietPhieuNhap(MaPN,MaSach,SoLuong,DonGia) VALUES('PN0001','SA001',10,68000),('PN0001','SA002',6,96000);
INSERT DotKiemKe(MaDot,TenDot,NguoiLap) VALUES('KK0001',N'Kiểm kê quý 3',N'Thủ thư 1');
INSERT ChiTietKiemKe(MaDot,MaSach,SoLuongHeThong,SoLuongThucTe) VALUES('KK0001','SA001',10,10),('KK0001','SA002',6,5),('KK0001','SA003',1,2),('KK0001','SA004',8,NULL),('KK0001','SA005',0,0);
GO
