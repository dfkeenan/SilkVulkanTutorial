
if ($args.Count -ne 1) {
    write-host "usage: .\incremental_patch.ps1 <project-folder>"
    exit
}

$projectFolder = $args[0]

if ( (Test-Path -Path "$projectFolder\Program.cs") -eq $False) {
    write-host "'$projectFolder\Program.cs'" does not exist
    exit
}

git diff --exit-code "$projectFolder\Program.cs"
if ($LastExitCode -eq 0) {
    write-host "'$projectFolder\Program.cs'" has no changes
    exit
}

$patchFolder = Get-item $projectFolder

write-host "Patching starting at $patchFolder"

#Copy changes
copy "$patchFolder\Program.cs" -destination "$patchFolder\Program.cs.bak" -force
#Undo changes
git checkout -- "$patchFolder\Program.cs"
#Create patch file
$patchFile = "$patchFolder\patch.txt"
if ( (Test-Path -Path $patchFile)) {
    remove-item -Path "$patchFolder\patch.txt" -Force
}
diff.exe -Naur "$patchFolder\Program.cs" "$patchFolder\Program.cs.bak" > $patchFile

$patching = $false

foreach($item in (Get-ChildItem .\[0-9]* -Attributes Directory | Sort-Object)) {
    if (($patchFolder.Name -eq $item.Name) -or $patching) {
       $patching = $true
        write-host $item.Name

        $patchOutput = get-content $patchFile | patch.exe -f $item\Program.cs
        if ($patchOutput -like "*FAILED*") {
            write-host "Failed to patch '$item'"
            exit
        }
    
        remove-item -Path "$item\*.orig" -Force
    }
}

remove-item -Path "$patchFolder\Program.cs.bak" -Force
remove-item -Path "$patchFolder\patch.txt" -Force