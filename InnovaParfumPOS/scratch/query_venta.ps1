$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InnovaTecBD;Integrated Security=True;Encrypt=False"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    
    Write-Output "--- VENTAS ALREDEDOR ---"
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT ID_VENTA, NUMERO_FACTURA, SUBTOTAL_NIO, TOTAL_NIO, FECHA_VENTA FROM VEN.VENTAS WHERE ID_VENTA BETWEEN 99 AND 105"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        $idV = $reader["ID_VENTA"]
        $num = $reader["NUMERO_FACTURA"]
        $sub = $reader["SUBTOTAL_NIO"]
        $tot = $reader["TOTAL_NIO"]
        $fecha = $reader["FECHA_VENTA"]
        Write-Output "ID_VENTA: $idV, NUMERO_FACTURA: $num, SUBTOTAL: $sub, TOTAL: $tot, FECHA: $fecha"
    }
    $reader.Close()

    Write-Output "`n--- DETALLES ALREDEDOR ---"
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT ID_DETALLE, ID_VENTA, ID_PRODUCTO, CANTIDAD, PRECIO_UNITARIO_NIO, SUBTOTAL_NIO, DESCRIPCION_SNAP FROM VEN.VENTA_DETALLE WHERE ID_VENTA BETWEEN 99 AND 105"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        $idDet = $reader["ID_DETALLE"]
        $idV = $reader["ID_VENTA"]
        $idP = $reader["ID_PRODUCTO"]
        $cant = $reader["CANTIDAD"]
        $prec = $reader["PRECIO_UNITARIO_NIO"]
        $sub = $reader["SUBTOTAL_NIO"]
        $desc = $reader["DESCRIPCION_SNAP"]
        Write-Output "ID_DET: $idDet, ID_VENTA: $idV, ID_PROD: $idP, DESC: $desc, CANT: $cant, PRECIO: $prec, SUBTOTAL: $sub"
    }
    $reader.Close()

}
catch {
    Write-Error $_.Exception.Message
}
finally {
    $connection.Close()
}
