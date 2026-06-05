$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=InnovaTecBD;Integrated Security=True;Encrypt=False"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT name, parent_id, type_desc, create_date FROM sys.triggers"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        $name = $reader["name"]
        $parent = $reader["parent_id"]
        $type = $reader["type_desc"]
        Write-Output "Trigger: $name, Parent: $parent, Type: $type"
    }
    $reader.Close()
}
catch {
    Write-Error $_.Exception.Message
}
finally {
    $connection.Close()
}
