[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

Set-Interval (1000 * 60 * 60) # 1 Hour

Function db-query ()
{
    [CmdletBinding()]
    Param(
    [Parameter(Position=0,Mandatory=$true)][String]$SQLServer,
    [Parameter(Position=1,Mandatory=$true)][String]$SQLDBName,
    [Parameter(Position=2,Mandatory=$true)][String]$SQLQuery
    )
   
        $SqlConnection = New-Object System.Data.SqlClient.SqlConnection 
        $SqlConnection.ConnectionString = "Server=$SQLServer;Database=$SQLDBName;Integrated Security=True"
         
        $SqlCmd = New-Object System.Data.SqlClient.SqlCommand 
        $SqlCmd.CommandText = $SQLQuery 
        $SqlCmd.Connection = $SqlConnection
         
        $SqlAdapter = New-Object System.Data.SqlClient.SqlDataAdapter 
        $SqlAdapter.SelectCommand = $SqlCmd
         
        $DataSet = New-Object System.Data.DataSet
         
        [void]$SqlAdapter.Fill($DataSet)
        [void]$SqlConnection.Close()
        
        ,$DataSet.Tables
        
        
}

$tables = (db-query "$computername\advantage2005" "master" "exec sp_helpdb 'advantage'")

# Database statistics
foreach ( $row in $tables[0].Rows )
 
{
    Set-Namespace "computers.servers.$computername.database.statistics.advantage.db.size"
    
    $size = ([Convert]::ToDouble($row.db_size.Trim().Replace(" MB", "")))

    Push-Data "MB" $size
    Push-Data "KB" ([Convert]::ToInt32(($size * 1024)))
}

# File stats
foreach ( $row in $tables[1].Rows )
{
    Set-Namespace "computers.servers.$computername.database.statistics.advantage.files.$($row.name).size"
    Push-Data "KB" ([convert]::ToInt32($row.size.Trim().Replace(" KB", ""), 10))
}
