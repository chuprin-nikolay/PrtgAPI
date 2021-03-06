using System;
using PrtgAPI.Parameters;
using PrtgAPI.Request.Serialization.Cache;

namespace PrtgAPI.Tests.UnitTests.ObjectManipulation
{
    class FakeSetObjectPropertyParameters : BaseSetObjectPropertyParameters<FakeObjectProperty>
    {
        private Func<Enum, PropertyCache> getPropertyCache;

        protected override int[] ObjectIdsInternal { get; set; }

        public FakeSetObjectPropertyParameters(Func<Enum, PropertyCache> getPropertyCache)
        {
            this.getPropertyCache = getPropertyCache;
        }

        public void AddValue(Enum property, object value, bool disableDependentsOnNotReqiuiredValue)
        {
            AddTypeSafeValue(property, value, disableDependentsOnNotReqiuiredValue);
        }

        protected override PropertyCache GetPropertyCache(Enum property)
        {
            return getPropertyCache(property);
        }
    }
}