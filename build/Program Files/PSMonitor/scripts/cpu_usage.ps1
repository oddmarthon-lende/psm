[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

Set-Namespace "computers.servers.$computername.cpu"
Set-Interval 5000

Try {

    $CPUCoresArray = Get-WmiObject Win32_PerfFormattedData_PerfOS_Processor 
    
    [decimal]$total = 0
    For ($i=0; $i -lt $CPUCoresArray.Count ; $i++) {
        $total += ($CPUCoresArray[$i].PercentProcessorTime -as [decimal])
        
    }

    Push-Data "%" ($total / $i)
    
    Exit 0
}
Catch {
    
    Exit 1001
}