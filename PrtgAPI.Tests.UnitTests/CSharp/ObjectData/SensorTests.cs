﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PrtgAPI.Parameters;
using PrtgAPI.Tests.UnitTests.Support;
using PrtgAPI.Tests.UnitTests.Support.TestItems;
using PrtgAPI.Tests.UnitTests.Support.TestResponses;

namespace PrtgAPI.Tests.UnitTests.ObjectData
{
    [TestClass]
    public class SensorTests : QueryableObjectTests<Sensor, SensorItem, SensorResponse>
    {
        [TestMethod]
        public void Sensor_CanDeserialize() => Object_CanDeserialize();

        [TestMethod]
        public async Task Sensor_CanDeserializeAsync() => await Object_CanDeserializeAsync();

        [TestMethod]
        [TestCategory("SlowCoverage")]
        public void Sensor_CanStream_Ordered_FastestToSlowest() => Object_CanStream_Ordered_FastestToSlowest();

        [TestMethod]
        public void Sensor_GetObjectsOverloads_CanExecute()
        {
            Object_GetObjectsOverloads_CanExecute
            (
                (c1, c2) => new List<Func<int, object>>                              { c1.GetSensor, c2.GetSensorAsync },
                (c1, c2) => new List<Func<Property, object, object>>                 { c1.GetSensors, c2.GetSensorsAsync },
                (c1, c2) => new List<Func<Property, FilterOperator, string, object>> { c1.GetSensors, c2.GetSensorsAsync },
                (c1, c2) => new List<Func<SearchFilter[], object>>                   { c1.GetSensors, c2.GetSensorsAsync },
                (c1, c2) =>
                {
                    Sensor_GetObjectsOverloads_SensorStatus_CanExecute(new List<Func<Status[], object>> {c1.GetSensors, c2.GetSensorsAsync});
                }
            );
        }

        private async void Sensor_GetObjectsOverloads_SensorStatus_CanExecute(List<Func<Status[], object>> sensorStatus)
        {
            var status = new[] { Status.Down, Status.DownAcknowledged };

            CheckResult<List<Sensor>>(sensorStatus[0](status));
            CheckResult<List<Sensor>>(await (Task<List<Sensor>>)sensorStatus[1](status));
        }

        [TestMethod]
        public void Sensor_GetSensor_Throws_WhenNoObjectReturned() => Object_GetSingle_Throws_WhenNoObjectReturned(c => c.GetSensor(1001));

        [TestMethod]
        public void Sensor_GetSensor_Throws_WhenMultipleObjectsReturned() => Object_GetSingle_Throws_WhenMultipleObjectsReturned(c => c.GetSensor(1001));

        [TestMethod]
        public void Sensor_GetObjectsOverloads_Stream_CanExecute() => Object_GetObjectsOverloads_Stream_CanExecute(
            client => client.StreamSensors,
            client => client.StreamSensors,
            client => client.StreamSensors,
            client => client.StreamSensors,
            client => CheckResult<IEnumerable<Sensor>>(client.StreamSensors(Status.Down, Status.DownAcknowledged))
        );

        [TestMethod]
        public void Sensor_StreamSerially() => Object_SerialStreamObjects(
            c => c.StreamSensors,
            c => c.StreamSensors,
            new SensorParameters()
        );

        [TestMethod]
        public void Sensor_GetObjectsOverloads_Query_CanExecute() => Object_GetObjectsOverloads_Query_CanExecute(
            client => client.QuerySensors,
            client => client.QuerySensors,
            client => client.QuerySensors,
            client => client.QuerySensors
        );

        [TestMethod]
        public void Sensor_AllFields_HaveValues()
        {
            Object_AllFields_HaveValues(prop =>
            {
                if (prop.Name == nameof(Sensor.BaseType)) //As Sensor is the default value of BaseType, we cannot verify whether the value was actually set
                    return true;

                return false;
            });
        }

        [TestMethod]
        public void Sensor_Stream_WithCorrectPageSize()
        {
            var urls = new object[]
            {
                TestHelpers.RequestSensor("count=500", UrlFlag.Columns),
                TestHelpers.RequestSensor("count=500&start=500", UrlFlag.Columns),
                TestHelpers.RequestSensor("count=500&start=1000", UrlFlag.Columns),
                TestHelpers.RequestSensor("count=100&start=1500", UrlFlag.Columns),
            };

            var client = Initialize_Client(new AddressValidatorResponse(urls)
            {
                CountOverride = new Dictionary<Content, int>
                {
                    [Content.Sensors] = 1600
                }
            });

            var items = client.StreamSensors(true).ToList();

            Assert.AreEqual(1600, items.Count);
        }

        protected override List<Sensor> GetObjects(PrtgClient client) => client.GetSensors();

        protected override Task<List<Sensor>> GetObjectsAsync(PrtgClient client) => client.GetSensorsAsync();

        protected override IEnumerable<Sensor> Stream(PrtgClient client) => client.StreamSensors();

        public override SensorItem GetItem() => new SensorItem();

        protected override SensorResponse GetResponse(SensorItem[] items) => new SensorResponse(items);
    }
}
