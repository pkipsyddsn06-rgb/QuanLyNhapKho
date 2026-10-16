using Microsoft.Data.SqlClient;
namespace QuanLyNhapKho;
internal static class Database
{
    // Đổi Server theo SQL Server trên máy của bạn nếu cần.
    public static string ConnectionString =
        @"Server=localhost\\SQLEXPRESS;Database=QuanLyKhoSach;Integrated Security=True;TrustServerCertificate=True;";
    public static SqlConnection OpenConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
