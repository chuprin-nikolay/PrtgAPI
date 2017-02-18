﻿using System.Management.Automation;
using PrtgAPI.Objects.Shared;
using PrtgAPI.PowerShell.Base;

namespace PrtgAPI.PowerShell.Cmdlets
{
    /// <summary>
    /// Request an object and any if its children refresh themselves immediately.
    /// </summary>
    [Cmdlet(VerbsData.Update, "Object", SupportsShouldProcess = true)]
    public class RefreshObject : PrtgCmdlet
    {
        /// <summary>
        /// The object to refresh.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "The object to refresh.")]
        public SensorOrDeviceOrGroupOrProbe Object { get; set; }

        /// <summary>
        /// Performs record-by-record processing functionality for the cmdlet.
        /// </summary>
        protected override void ProcessRecordEx()
        {
            if(ShouldProcess($"'{Object.Name}' (ID: {Object.Id})"))
                client.CheckNow(Object.Id);
        }
    }
}
