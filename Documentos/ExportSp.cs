using System;
using System.IO;
using Microsoft.Data.SqlClient;

class Program {
    static void Main() {
        string connStr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InnovaParfumBD;Integrated Security=True";
        using (var conn = new SqlConnection(connStr)) {
            conn.Open();
            using (var cmd = new SqlCommand("SELECT OBJECT_DEFINITION(OBJECT_ID('VEN.sp_ProcesarVenta'))", conn)) {
                var def = cmd.ExecuteScalar()?.ToString();
                File.WriteAllText("sp_export.sql", def);
            }
        }
    }
}
