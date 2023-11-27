using System;

namespace Shared.Properties
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NullableAttribute : Attribute
    {
        public NullableAttribute()
        {
        }
    }
}