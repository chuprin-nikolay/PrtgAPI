﻿using System;
using System.Management.Automation;

namespace PrtgAPI.PowerShell.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Initializes a new instance of a <see cref="PrtgClient"/>. This cmdlet must be called at least once before attempting to use any other cmdlets.</para>
    /// 
    /// <para type="description">The Connect-PrtgServer cmdlet establishes a new connection to a PRTG Server. As PRTG uses a stateless REST API,
    /// Connect-PrtgServer initializes a <see cref="PrtgClient"/> which stores the settings that will be used when generating each request.</para>
    /// <para type="description">PRTG supports two authentication methods: username/password and username/passhash. A PassHash is a numeric
    /// password generated by PRTG that can be used in place of your regular password. When your <see cref="PrtgClient"/> is initialized,
    /// if you have specified a password PrtgAPI will automatically retrieve your account's PassHash for use in all future requests.</para>
    /// <para type="description">By default when PRTG times out while processing a request PrtgAPI will throw an exception. This can be
    /// quite problematic when attempting to batch process a large number of items with PrtgAPI in PowerShell. You can request
    /// PrtgAPI automatically attempt to retry failed requests by specifying the -RetryCount and -RetryDelay parameters.
    /// If -RetryCount is greater than 0, PrtgAPI will send a warning to the pipeline indicating a failure has occurred,
    /// as well as the number of retries remaining before PrtgAPI gives up. Each request invocation uses a separate retry count.</para>
    /// <para>If a protocol is not specified, PrtgAPI will connect with HTTPS. If your PRTG Server is HTTP only, this will cause an exception.
    /// For HTTP only servers, prefix your URL with http://</para>
    /// <para type="description">When Connect-PrtgServer is run from outside of a script or the PowerShell ISE, PrtgAPI will
    /// display PowerShell progress when piping between PrtgAPI cmdlets or when piping from variables containing PrtgAPI objects.
    /// This default setting can be overridden by specifying a value to the -Progress parameter, or by using the Enable-PrtgProgress
    /// and Disable-PrtgProgress cmdlets.</para>
    /// <para type="description">Attempting to invoke Connect-PrtgServer after a <see cref="PrtgClient"/> has already been initialized
    /// for the current session will generate an exception. To override the existing <see cref="PrtgClient"/>, specify the -Force
    /// parameter to Connect-PrtgServer, or invalidate the <see cref="PrtgClient"/> by calling Disconnect-PrtgServer.</para>
    /// <para type="description">For information on viewing and editing the session's <see cref="PrtgClient"/> settings see Get-PrtgClient.</para> 
    /// 
    /// <example>
    ///     <code>C:\> Connect-PrtgServer prtg.example.com</code>
    ///     <para>Connect to a PRTG Server. You will receive a login prompt requesting you enter your credentials.</para>
    ///     <para/>
    /// </example>
    /// <example>
    ///     <code>C:\> Connect-PrtgServer prtg.example.com (New-Credential prtgadmin prtgadmin)</code>
    ///     <para>Connect to a PRTG Server specifying your credentials as part of the command. The New-Credential cmdlet can be used
    /// when developing scripts to avoid a login prompt.</para>
    ///     <para/>
    /// </example>
    /// <example>
    ///     <code>C:\> Connect-PrtgServer prtg.example2.com -Force</code>
    ///     <para>Connect to a PRTG Server, overriding the session's existing PrtgClient (if applicable)</para>
    ///     <para/>
    /// </example>
    /// <example>
    ///     <code>C:\> Connect-PrtgServer http://prtg.example.com</code>
    ///     <para>Connect to a PRTG Server using HTTP. http:// must be specified for servers that do not accept HTTPS.</para>
    ///     <para/>
    /// </example>
    /// <example>
    ///     <code>C:\> Connect-PrtgServer prtg.example.com -RetryCount 3 -RetryDelay 5</code>
    ///     <para>Connect to a PRTG Server, indicating that any requests that fail during the use of the PrtgClient should be attempted
    /// at most 3 times, with a delay of 5 seconds between each attempt.</para>
    /// </example>
    /// 
    /// <para type="link">Get-PrtgClient</para>
    /// <para type="link">Disconnect-PrtgServer</para>
    /// <para type="link">Enable-PrtgProgress</para>
    /// <para type="link">Disable-PrtgProgress</para>
    /// </summary>
    [Cmdlet(VerbsCommunications.Connect, "PrtgServer")]
    public class ConnectPrtgServer : PSCmdlet
    {
        /// <summary>
        /// <para type="description">Specifies the PRTG Server requests will be made against.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The address of the PRTG Server to connect to. If the server does not use HTTPS, http:// must be specified.")]
        public string Server { get; set; }

        /// <summary>
        /// <para type="description">Specifies the username and password to authenticate with. If <see cref="PassHash"/> is specified, the password will be treated as a PassHash instead.</para>
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 1, HelpMessage = "The username and password to authenticate with. If -PassHash is specified, the password will be treated as a PassHash instead.")]
        public PSCredential Credential { get; set; }

        /// <summary>
        /// <para type="description">Forces a <see cref="PrtgClient"/> to be replaced if one already exists.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Forces the sessions PrtgClient to be replaced if one already exists.")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// <para type="description">Specifies that the <see cref="Credential"/>'s password contains a PassHash instead of a Password.</para>
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "Indicates that the password field of -Credential contains a PassHash instead of a Password.")]
        public SwitchParameter PassHash { get; set; }

        /// <summary>
        /// <para type="description">The number of times to retry a request that times out while communicating with PRTG.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? RetryCount { get; set; } = 1;

        /// <summary>
        /// <para type="description">The base delay (in seconds) between retrying a timed out request. Each successive failure of a given request will wait an additional multiple of this value.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? RetryDelay { get; set; } = 3;

        /// <summary>
        /// <para type="description">Enable or disable PowerShell Progress when piping between cmdlets. By default, if Connect-PrtgServer is being called from within a script or the PowerShell ISE this value is false. Otherwise, true.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public bool? Progress { get; set; }

        /// <summary>
        /// Performs record-by-record processing functionality for the cmdlet.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (PrtgSessionState.Client == null || Force.IsPresent)
            {
                PrtgSessionState.Client = PassHash.IsPresent ?
                    new PrtgClient(Server, Credential.GetNetworkCredential().UserName, Credential.GetNetworkCredential().Password, AuthMode.PassHash) :
                    new PrtgClient(Server, Credential.GetNetworkCredential().UserName, Credential.GetNetworkCredential().Password);

                if (RetryCount != null)
                    PrtgSessionState.Client.RetryCount = RetryCount.Value;

                if (RetryDelay != null)
                    PrtgSessionState.Client.RetryDelay = RetryDelay.Value;

                if (Progress == false || (!string.IsNullOrEmpty(MyInvocation.ScriptName) && !GoPrtgScript()) || GetVariableValue("global:psISE") != null)
                    PrtgSessionState.EnableProgress = false;
                else
                    PrtgSessionState.EnableProgress = true;
            }
            else
            {
                throw new Exception($"Already connected to server {PrtgSessionState.Client.Server}. To override please specify -Force");
            }
        }

        private bool GoPrtgScript()
        {
            var script = MyInvocation.ScriptName;

            return script.EndsWith("Connect-GoPrtgServer.ps1") || script.EndsWith("Update-GoPrtgCredential.ps1");
        }
    }
}
