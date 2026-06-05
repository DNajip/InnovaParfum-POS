$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InnovaTecBD;Integrated Security=True;Encrypt=False"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT definition FROM sys.sql_modules WHERE object_id = OBJECT_ID('VEN.sp_ProcesarVenta')"
    $def = $command.ExecuteScalar()
    Write-Output $def
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    $connection.Close()
}
