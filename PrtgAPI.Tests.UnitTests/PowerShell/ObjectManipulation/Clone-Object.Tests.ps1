﻿. $PSScriptRoot\..\..\Support\PowerShell\Standalone.ps1

function SetCloneResponse
{
    $client = [PrtgAPI.Tests.UnitTests.BaseTest]::Initialize_Client((New-Object PrtgAPI.Tests.UnitTests.Support.TestResponses.CloneResponse))

    SetPrtgClient $client
}

function CloneSourceId($sourceId, $content, $properties, $name, $realSourceId)
{
    $devices = WithResponse "MultiTypeResponse" {
        Get-Device -Count 1
    }

    try
    {
        # MultiTypeResponse will respond with an object to the request for sensors, despite the fact this is a device
        SetAddressValidatorResponse @(
            (GetLookup $content $properties $sourceId)
            "api/duplicateobject.htm?id=$realSourceId&name=$name&targetid=3000&"
            "api/table.xml?content=$($content | Select -Last 1)&columns=$($properties|select -Last 1)&count=*&filter_objid=9999&"
        )

        $devices | Clone-Object -SourceId $sourceId
    }
    finally
    {
        SetCloneResponse
    }
}

function GetLookup($content, $properties, $sourceId)
{
    $list = @()

    for($i = 0; $i -lt $content.Length; $i++)
    {
        $list += "api/table.xml?content=$($content[$i])&columns=$($properties[$i])&count=*&filter_objid=$sourceId&"
    }

    $list
}

Describe "Clone-Object" -Tag @("PowerShell", "UnitTest") {

    SetCloneResponse

    $sensorProperties = "objid,name,probe,group,favorite,lastvalue,device,downtime,downtimetime,downtimesince,uptime,uptimetime,uptimesince,knowntime,cumsince,lastcheck,lastup,lastdown,minigraph,schedule,basetype,baselink,parentid,notifiesx,intervalx,access,dependency,position,status,comments,priority,message,tags,type,active"
    $deviceProperties = "objid,name,location,host,group,probe,favorite,condition,upsens,downsens,downacksens,partialdownsens,warnsens,pausedsens,unusualsens,undefinedsens,totalsens,schedule,basetype,baselink,parentid,notifiesx,intervalx,access,dependency,position,status,comments,priority,message,tags,type,active"
    $groupProperties = "objid,name,probe,condition,fold,groupnum,devicenum,upsens,downsens,downacksens,partialdownsens,warnsens,pausedsens,unusualsens,undefinedsens,totalsens,schedule,basetype,baselink,parentid,notifiesx,intervalx,access,dependency,position,status,comments,priority,message,tags,type,active"

    It "Retries resolving an object" {
        $sensor = Run Sensor { Get-Sensor }

        $sensor.Count | Should Be 1

        $output = [string]::Join("`n",(&{try { $sensor | Clone-Object 1234 3>&1 | %{$_.Message} } catch [exception] { }}))

        $expected = "'Copy-Object' failed to resolve sensor: object is still being created. Retries remaining: 4`n" +
                    "'Copy-Object' failed to resolve sensor: object is still being created. Retries remaining: 3`n" +
                    "'Copy-Object' failed to resolve sensor: object is still being created. Retries remaining: 2`n" +
                    "'Copy-Object' failed to resolve sensor: object is still being created. Retries remaining: 1"

        $output | Should Be $expected
    }

    $triggerCases = @(
        @{name = "State"}
        @{name = "Speed"}
        @{name = "Volume"}
        @{name = "Threshold"}
        @{name = "Change"}
    )

    It "Clones a <name> trigger" -TestCases $triggerCases {
        param($name)

        $group = Run Group { Get-Group }

        $trigger = Run NotificationTrigger { $group | Get-Trigger -Type $name } | Select -First 1

        $clone = {
                WithResponseArgs "DiffBasedResolveResponse" $false {
                $trigger | Clone-Object 1001
            }
        }

        if($name -eq "State" -or $name -eq "Change")
        {
            & $clone
        }
        else
        {
            { & $clone } | Should Throw "Channel 'Primary' is not a valid value"
        }
    }

    It "clones a trigger without resolving it" {
        $group = Run Group { Get-Group }

        $trigger = Run NotificationTrigger { $group | Get-Trigger } | Select -First 1

        WithResponseArgs "DiffBasedResolveResponse" $false {
            $trigger | Clone-Object 1001 -Resolve:$false
        }
    }

    It "Clones a sensor source ID" {
        
        CloneSourceId -1 @("sensors") @($sensorProperties) "Volume+IO+_Total0" 4000
    }

    It "Clones a device source ID" {
        CloneSourceId -2 @("sensors", "devices") @($sensorProperties, $deviceProperties) "Probe+Device0" 3000
    }

    It "Clones a group source ID" {
        CloneSourceId -3 @("sensors", "devices", "groups") @($sensorProperties, $deviceProperties, $groupProperties) "Windows+Infrastructure0" 2000
    }

    It "throws cloning an unknown source ID" {
        $devices = Run Device { Get-Device }

        WithResponse "MultiTypeResponse" {
            { $devices | Clone-Object -SourceId -4 } | Should Throw "Cannot clone object with ID '-4' as it is not a sensor, device or group"
        }
    }

    It "doesn't resolve a sensor/group" {
        $sensor = Run Sensor { Get-Sensor }

        $result = $sensor | Clone-Object 1234 "new sensor" -Resolve:$false

        $result.GetType().Name | Should Be "PSCustomObject"
    }

    It "doesn't resolve a device" {
        $device = Run Device { Get-Device }

        $result = $device | Clone-Object 1234 "new device" -Resolve:$false

        $result.GetType().Name | Should Be "PSCustomObject"
    }

    It "executes with -WhatIf" {
        $sensor = Run Sensor { Get-Sensor }
        $device = Run Device { Get-Device }
        $group = Run Group { Get-Group }
        $trigger = Run NotificationTrigger { $group | Get-Trigger | Select -First 1 }

        $sensor | Clone-Object 1234 -WhatIf
        $device | Clone-Object 1234 -WhatIf
        $group | Clone-Object 1234 -WhatIf
        $trigger | Clone-Object 1234 -WhatIf
    }
}