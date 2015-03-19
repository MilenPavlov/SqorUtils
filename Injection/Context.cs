﻿using System;
using System.Collections.Generic;
using Sqor.Utils.Dictionaries;

namespace Sqor.Utils.Injection
{
    public class Context
    {
        private ICache cache;
        private IDictionary<Type, IBinding> transientBindings;

        public Context(Context context = null, ICache cache = null, IDictionary<Type, IBinding> transientBindings = null)
        {
            cache = cache ?? new Cache();
            this.cache = context != null ? new HybridCache(cache, context.Cache) : cache;
            this.transientBindings = transientBindings;
        }

        public ICache Cache
        {
            get { return cache; }
        }

        public IBinding GetCustomBinding(Type type)
        {
            return transientBindings.Get(type);
        }
    }
}
