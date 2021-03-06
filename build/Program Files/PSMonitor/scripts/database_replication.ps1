[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

Set-Interval 1000 # 1 second

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

If($id -is [Int]) { } 
Else
{

    $query = "select max(id) as max_id from [distribution].[dbo].[MSrepl_errors];"
    $tables = (db-query "(local)\advantage2005" "advantage" $query)
    $id = $tables[0].Rows[0].max_id
    $id = ($id - 1)
}

$query = @"

    if exists (select * from sys.databases where name = 'distribution') 
    begin

    		---- Queue Size
    	select count(*) as queue_size 
    		from [distribution].[dbo].[MSrepl_commands]

    		---- Last error message
    	select *
    	from [distribution].[dbo].[MSrepl_errors]
        where [id] > $id
    	order by [time] asc;

    end
"@

$tables = (db-query "(local)\advantage2005" "advantage" $query)

Set-Namespace "computers.servers.$computername.database.replication"

# Get the replication queue size table
foreach ( $row in $tables[0].Rows )
 
{
    Push-Data "queue_size" $row.queue_size
}

Set-Namespace "computers.servers.$computername.database.replication.last_error"

# Get the last replication error message
foreach ( $row in $tables[1].Rows )
 
{
    $id = $row.id
    Push-Data "error_text" $row.error_text $row.time

}
