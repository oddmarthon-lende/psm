
[String] $computername = (Get-WmiObject Win32_ComputerSystem).Name

Set-Namespace "computers.servers.$computername.database.backup_synchronization"
Set-Interval (1000 * 60 * 60) # 1 Hour

If($computername.ToLower().IndexOf("rldb01") -gt -1)
{

    $p1 = $computername.ToLower().Replace("rldb01", "")
    $p2 = @("backup", "bkup", "bkup01", "backup01")

    foreach($p in $p2)
    {

        Try {
    
            $diff = (Compare-Object -ReferenceObject (Get-ChildItem "D:\Database_Backup") -DifferenceObject (Get-ChildItem "\\$($p1)$p\d\Database_backup_synchronisation"))

            Push-Data "diff_count" (Measure-Object -InputObject $diff).Count
    
            break

        }
        Catch {
    
            continue
        }

    }
}


