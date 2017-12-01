﻿using System;
using System.Management.Automation;
using PrtgAPI.Objects.Shared;
using PrtgAPI.Parameters;
using PrtgAPI.PowerShell.Base;

namespace PrtgAPI.PowerShell.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Adds a new device to a PRTG Group or Probe.</para>
    /// 
    /// <para type="description">The Add-Device cmdlet adds a new device to a PRTG Group or Probe. When adding a new
    /// device, Add-Device supports two methods of specifying the parameters required to create the object. For basic scenarios
    /// where you wish to inherit all settings from the parent object, the <see cref="Name"/>, <see cref="Host"/> and auto-discovery method
    /// can all be specified as arguments directly to Add-Device. If a -<see cref="Host"/> is not specified, Add-Device will automatically
    /// use the -<see cref="Name"/> as the device's hostname. If -<see cref="AutoDiscover"/> is specified, Add-Device will perform an
    /// <see cref="AutoDiscoveryMode.Automatic"/> auto-discovery.</para>
    /// <para type="description">For more advanced scenarios where you wish to specify more advanced parameters (such as the Internet Protocol
    /// version used to communicate with the device) a <see cref="NewDeviceParameters"/> object can instead be created with the New-DeviceParameters cmdlet.
    /// When the parameters object is passed to Add-Device, PrtgAPI will validate that all mandatory parameter fields contain values.
    /// If a mandatory field is missing a value, Add-Sensor will throw an <see cref="InvalidOperationException"/>, listing the field whose value was missing.</para>
    /// 
    /// <example>
    ///     <code>C:\> Get-Probe contoso | Add-Device dc-1</code>
    ///     <para>Add a new device named "dc-1" to the Contoso probe, using "dc-1" as its hostname, without performing an automatic auto-discovery.</para>
    ///     <para/>
    /// </example>
    /// <example>
    ///     <code>C:\> Get-Group -Id 2305 | Add-Device exch-1 192.168.0.2 -AutoDiscover</code>
    ///     <para>Add a device named "exch-1" to the group with ID 2305, using 192.168.0.2 as its IP Address and performing an automatic auto-discovery after the device is created.</para>
    ///     <para/>
    /// </example>
    /// <example>
    ///     <code>C:\> $params = New-DeviceParameters sql-1 "2001:db8::ff00:42:8329"</code>
    ///     <para>C:\> $params.IPVersion = "IPv6"</para>
    ///     <para>C:\> Get-Probe contoso | Add-Device $params</para>
    ///     <para>Add a device named sql-1 using an IPv6 Address to the probe Contoso probe.</para>
    /// </example>
    /// 
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "Device", SupportsShouldProcess = true)]
    public class AddDevice : AddObject<NewDeviceParameters, GroupOrProbe>
    {
        /// <summary>
        /// <para type="description">The name to use for the device. If a <see cref="Host"/> is not specified, this value will be used as the hostname as well.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "Basic")]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The IPv4 Address/HostName to use for monitoring this device. If this value is not specified, the <see cref="Name"/> will be used as the hostname.</para>
        /// </summary>
        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "Basic")]
        public new string Host { get; set; }

        /// <summary>
        /// <para type="description">Whether to perform an <see cref="AutoDiscoveryMode.Automatic"/> auto-discovery on the newly created device.</para>
        /// </summary>
        [Parameter(Mandatory = false, ParameterSetName = "Basic")]
        public SwitchParameter AutoDiscover { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddDevice"/> 
        /// </summary>
        public AddDevice() : base(BaseType.Device, CommandFunction.AddDevice2)
        {
        }

        /// <summary>
        /// Performs record-by-record processing functionality for the cmdlet.
        /// </summary>
        protected override void ProcessRecordEx()
        {
            if (ParameterSetName == "Basic")
            {
                Parameters = new NewDeviceParameters(Name, string.IsNullOrEmpty(Host) ? Name : Host);

                if (AutoDiscover)
                {
                    Parameters.AutoDiscoveryMode = AutoDiscoveryMode.Automatic;
                }
            }

            base.ProcessRecordEx();
        }
    }
}