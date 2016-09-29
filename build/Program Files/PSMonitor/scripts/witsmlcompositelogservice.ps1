[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

Set-Namespace "computers.servers.$computername.processes.witsmlcompositelogservice"
Set-Interval 5000

$processes = ps | Where-Object { $_.ProcessName -eq "WITSMLCompositeLogService" }

[int]$i = 0
foreach ( $row in $processes )
 
{
    $i++
    Push-Data "status" $i
    Exit 0
}



$i--
Push-Data "status" $i