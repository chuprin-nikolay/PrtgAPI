﻿using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrtgAPI.Tests.UnitTests.Support.TestResponses;

namespace PrtgAPI.Tests.UnitTests.ObjectData
{
    [TestClass]
    public class GetObjectPropertyTests : BaseTest
    {
        [TestMethod]
        public void GetObjectProperty_AsObject() => GetObjectProperty(c => c.GetObjectProperty(1001, ObjectProperty.Name));

        [TestMethod]
        public async Task GetObjectPropertyAsync_AsObject() => await GetObjectPropertyAsync(async c => await c.GetObjectPropertyAsync(1001, ObjectProperty.Name));


        [TestMethod]
        public void GetObjectProperty_AsType() => GetObjectProperty(c => c.GetObjectProperty<string>(1001, ObjectProperty.Name));

        [TestMethod]
        public void GetObjectProperty_Nullable_AsUnderlying() => GetObjectProperty(c => c.GetObjectProperty<bool>(1001, ObjectProperty.InheritAccess), "True");

        [TestMethod]
        public void GetObjectProperty_Nullable_AsUnderlying_WithNull_Throws()
        {
            AssertEx.Throws<InvalidCastException>(
                () => GetObjectProperty(c => c.GetObjectProperty<bool>(1001, ObjectProperty.InheritInterval), "True"),
                "Cannot convert a value of type 'null' to type 'System.Boolean'"
            );
        }

        [TestMethod]
        public void GetObjectProperty_Nullable_AsNullable() => GetObjectProperty(c => c.GetObjectProperty<bool?>(1001, ObjectProperty.InheritAccess), "True");

        [TestMethod]
        public void GetObjectProperty_Nullable_AsNullable_WithNull() => GetObjectProperty(c => c.GetObjectProperty<bool?>(1001, ObjectProperty.InheritInterval), null);

        [TestMethod]
        public async Task GetObjectPropertyAsync_AsType() => await GetObjectPropertyAsync(async c => await c.GetObjectPropertyAsync<string>(1001, ObjectProperty.Name));

        [TestMethod]
        public void GetObjectPropertiesRaw() => GetObjectProperty(c => c.GetObjectPropertiesRaw(1001, ObjectType.Sensor)["name"], "Server CPU Usage");

        [TestMethod]
        public async Task GetObjectPropertiesRawAsync() => await GetObjectPropertyAsync(async c => (await c.GetObjectPropertiesRawAsync(1001, ObjectType.Sensor))["name"], "Server CPU Usage");

        [TestMethod]
        public void GetObjectProperty_Throws_RetrievingAnInvalidProperty()
        {
            AssertEx.Throws<PrtgRequestException>(
                () => GetObjectProperty(c => c.GetObjectPropertyRaw(1001, "banana")),
                "A value for property 'banana' could not be found"
            );
        }

        [TestMethod]
        public void GetObjectProperty_Processes_SpecialCases()
        {
            Execute(c => c.GetObjectProperty(1001, ObjectProperty.Comments), "api/getobjectstatus.htm?id=1001&name=comments");
        }

        [TestMethod]
        public void GetObjectProperty_MultiCheckbox_None()
        {
            var client = Initialize_Client(new MultiCheckBoxResponse());

            var properties = client.GetObjectPropertiesRaw(1001, ObjectType.Device);

            Assert.AreEqual(string.Empty, properties["trafficmode"]);
        }

        [TestMethod]
        public void GetObjectProperty_MultiCheckbox_One()
        {
            var client = Initialize_Client(new MultiCheckBoxResponse(discards: true));

            var properties = client.GetObjectPropertiesRaw(1001, ObjectType.Device);

            Assert.AreEqual("discards", properties["trafficmode"]);
        }

        [TestMethod]
        public void GetObjectProperty_MultiCheckbox_Multiple()
        {
            var client = Initialize_Client(new MultiCheckBoxResponse(discards: true, broadcast: true, unknown: true));

            var properties = client.GetObjectPropertiesRaw(1001, ObjectType.Device);

            Assert.AreEqual("discards broadcast unknown", properties["trafficmode"]);
        }

        private void GetObjectProperty<T>(Func<PrtgClient, T> getProperty, string expected = "testName")
        {
            var client = Initialize_Client(new MultiTypeResponse());

            T val = getProperty(client);

            Assert.AreEqual(expected, val?.ToString());
        }

        private async Task GetObjectPropertyAsync<T>(Func<PrtgClient, Task<T>> getProperty, string expected = "testName")
        {
            var client = Initialize_Client(new MultiTypeResponse());

            T val = await getProperty(client);

            Assert.AreEqual(expected, val);
        }
    }
}
