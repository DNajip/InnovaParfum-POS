$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InnovaTecBD;Integrated Security=True;Encrypt=False"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT ID_PAGO, ID_VENTA, ID_METODO_PAGO, MONTO_PAGADO, MONTO_EN_NIO, MONTO_RECIBIDO, VUELTO_NIO FROM VEN.PAGOS WHERE ID_VENTA <= 10"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        $idPago = $reader["ID_PAGO"]
        $idVenta = $reader["ID_VENTA"]
        $montoPagado = $reader["MONTO_PAGADO"]
        $montoEnNio = $reader["MONTO_EN_NIO"]
        $montoRecibido = $reader["MONTO_RECIBIDO"]
        $vueltoNio = $reader["VUELTO_NIO"]
        Write-Output "ID_PAGO: $idPago, ID_VENTA: $idVenta, MONTO_PAGADO: $montoPagado, MONTO_EN_NIO: $montoEnNio, MONTO_RECIBIDO: $montoRecibido, VUELTO_NIO: $vueltoNio"
    }
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    $connection.Close()
}
