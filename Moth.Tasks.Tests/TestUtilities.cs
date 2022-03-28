using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Moth.Tasks.Tests
{
    public static class TestUtilities
    {
        public static T GetPrivateValue<T> (this object obj, string fieldName) => (T)obj.GetType ().GetField (fieldName, BindingFlags.NonPublic | BindingFlags.Instance).GetValue (obj);
    }
}
