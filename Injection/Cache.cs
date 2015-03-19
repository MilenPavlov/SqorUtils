﻿using System;
using System.Collections.Generic;
using Sqor.Utils.Dictionaries;

namespace Sqor.Utils.Injection
{
    public class Cache : ICache
    {
        private Dictionary<Type, object> storage = new Dictionary<Type, object>();

        public object Get(Type type)
        {
            return storage.Get(type);
        }

        public void Set(Type type, object value)
        {
            storage[type] = value;
        }
    }
}
