﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrtgAPI.Objects.Shared;

namespace PrtgAPI.Tests.IntegrationTests
{
    [TestClass]
    public class BasePrtgClientTest
    {
        private static string PrtgConfig => $"\\\\{Settings.Server}\\c$\\ProgramData\\Paessler\\PRTG Network Monitor\\PRTG Configuration.dat";
        public static string PrtgConfigBackup => $"\\\\{Settings.Server}\\c$\\Users\\{Settings.WindowsUsername}\\AppData\\Local\\Temp\\PRTG Configuration.dat";

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext testContext)
        {
            Logger.Log($"Pinging {Settings.Server}");

            var sender = new Ping();
            var reply = sender.Send(Settings.Server);

            if (reply.Status != IPStatus.Success)
            {
                Assert.Fail("Ping responded with" + reply.Status.ToString());
            }

            Logger.Log("Connecting to local server");

            Impersonator.ExecuteAction(() =>
            {
                Logger.Log("Retrieving service details");

                var coreService = new ServiceController("PRTGCoreService", Settings.Server);
                var probeService = new ServiceController("PRTGProbeService", Settings.Server);

                if (coreService.Status != ServiceControllerStatus.Running)
                    Assert.Fail("Core Service is not running");

                if (probeService.Status != ServiceControllerStatus.Running)
                    Assert.Fail("Probe Service is not running");

                Logger.Log("Backing up PRTG Config");

                if (Settings.ResetAfterTests)
                    File.Copy(PrtgConfig, PrtgConfigBackup);
            });

            Logger.Log("Refreshing CI device");

            try
            {
                new BasePrtgClientTest().client.RefreshObject(Settings.Device);
            }
            catch(Exception ex)
            {
                Logger.Log($"Exception occurred while refreshing device: {ex.Message}", true);
                AssemblyCleanup();
                throw;
            }


            Logger.Log("Ready for tests");
        }

        [AssemblyCleanup] public static void AssemblyCleanup()
        {
            Logger.Log("Cleaning up after tests");

            if (Settings.ResetAfterTests)
            {
                Logger.Log("Connecting to server");

                Impersonator.ExecuteAction(() =>
                {
                    Logger.Log("Retrieving service details");

                    var controller = new ServiceController("PRTGCoreService", Settings.Server);

                    Logger.Log("Stopping service");

                    controller.Stop();
                    controller.WaitForStatus(ServiceControllerStatus.Stopped);

                    Logger.Log("Restoring config");

                    try
                    {
                        File.Copy(PrtgConfigBackup, PrtgConfig, true);
                        File.Delete(PrtgConfigBackup);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.Message);
                        throw;
                    }

                    Logger.Log("Starting service");

                    controller.Start();
                    controller.WaitForStatus(ServiceControllerStatus.Running);

                    Logger.Log("Finished");
                });
            }
        }

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void ValidateSettings()
        {
            Logger.LogTest($"Running test '{TestContext.TestName}'");

            var properties = typeof (Settings).GetFields();

            foreach (var property in properties)
            {
                var value = property.GetValue(null);

                Assert2.IsTrue(value != null && value.ToString() != "-1", $"Setting '{property.Name}' must be initialized before running tests. Please specify a value in file Settings.cs");
            }
        }

        protected PrtgClient client
        {
            get
            {
                if (Settings.Protocol == null)
                    throw new Exception("Please specify either HTTP or HTTPS in file Settings.cs");

                return new PrtgClient(Settings.ServerWithProto, Settings.Username, Settings.Password);
            }
        }

        protected void HasAnyResults<T>(Func<List<T>> method)
        {
            var results = method();

            Assert.IsTrue(results.Any());
        }

        protected void ReturnsJustObjectsOfType<T>(Func<Property, object, List<T>> method, int parentId, int expectedCount, BaseType baseType) where T : SensorOrDeviceOrGroupOrProbe
        {
            var results = method(Property.ParentId, parentId);

            Assert2.AreEqual(expectedCount, results.Count, "Expected number of results was incorrect");
            Assert.IsTrue(results.Count(r => r.BaseType != baseType) == 0);
        }

        protected void CheckAndSleep(int objectId)
        {
            client.RefreshObject(objectId);
            Logger.LogTestDetail("Refreshed object; sleeping for 30 seconds");
            Thread.Sleep(30000);
        }
    }
}
