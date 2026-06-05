$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InnovaTecBD;Integrated Security=True;Encrypt=False"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    
    Write-Output "--- VENTA DETALLE IMEIS ---"
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT ID, ID_DETALLE, ID_EQUIPO_IMEI, IMEI_SNAP FROM VEN.VENTA_DETALLE_IMEI WHERE ID_DETALLE IN (104, 105, 106, 107)"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        $id = $reader["ID"]
        $det = $reader["ID_DETALLE"]
        $imeiId = $reader["ID_EQUIPO_IMEI"]
        $imei = $reader["IMEI_SNAP"]
        Write-Output "ID: $id, ID_DETALLE: $det, ID_EQUIPO_IMEI: $imeiId, IMEI_SNAP: $imei"
    }
    $reader.Close()

}
catch {
    Write-Error $_.Exception.Message
}
finally {
    $connection.Close()
}
