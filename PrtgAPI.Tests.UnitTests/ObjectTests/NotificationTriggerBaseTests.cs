﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrtgAPI.Tests.UnitTests.InfrastructureTests.Support;
using PrtgAPI.Tests.UnitTests.ObjectTests.Items;
using PrtgAPI.Tests.UnitTests.ObjectTests.Responses;

namespace PrtgAPI.Tests.UnitTests.ObjectTests
{
    public abstract class NotificationTriggerBaseTests : ObjectTests<NotificationTrigger, NotificationTriggerItem, NotificationTriggerResponse>
    {
        protected NotificationAction GetNotificationAction()
        {
            var webClient = new MockWebClient(new NotificationActionResponse(new[] { new NotificationActionItem() }));

            var client = new PrtgClient("prtg.example.com", "username", "12345678", AuthMode.PassHash, webClient);

            return client.GetNotificationActions().First();
        }

        protected override NotificationTriggerItem[] GetItems() => new[]
        {
            NotificationTriggerItem.ChangeTrigger(),
            NotificationTriggerItem.StateTrigger(),
            NotificationTriggerItem.ThresholdTrigger(),
            NotificationTriggerItem.SpeedTrigger(),
            NotificationTriggerItem.VolumeTrigger()
        };

        protected override List<NotificationTrigger> GetObjects(PrtgClient client) => client.GetNotificationTriggers(1234);

        protected override Task<List<NotificationTrigger>> GetObjectsAsync(PrtgClient client) => client.GetNotificationTriggersAsync(1234);

        public override NotificationTriggerItem GetItem()
        {
            throw new NotSupportedException();
        }

        protected override NotificationTriggerResponse GetResponse(NotificationTriggerItem[] items) => new NotificationTriggerResponse(items);
    }
}
