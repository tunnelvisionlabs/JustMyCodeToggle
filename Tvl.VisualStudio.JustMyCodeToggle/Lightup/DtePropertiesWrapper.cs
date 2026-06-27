// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.Lightup
{
    using System;

    internal struct DtePropertiesWrapper
    {
        private static readonly Type WrappedType;
        private static readonly Func<object, string, object> ItemAccessor;

        private readonly object _instance;

        static DtePropertiesWrapper()
        {
            WrappedType = AutomationLightupHelpers.FindType(
                "EnvDTE.Properties, EnvDTE",
                "EnvDTE.Properties, envdte",
                "EnvDTE.Properties, Microsoft.VisualStudio.Interop",
                "EnvDTE.Properties");
            ItemAccessor = AutomationLightupHelpers.CreateMethodAccessor<string, object>(
                WrappedType,
                typeof(object),
                "Item");
        }

        private DtePropertiesWrapper(object instance)
        {
            _instance = instance;
        }

        public bool IsDefault
        {
            get
            {
                return _instance == null;
            }
        }

        public static DtePropertiesWrapper FromObject(object instance)
        {
            if (instance == null)
            {
                return default(DtePropertiesWrapper);
            }

            return new DtePropertiesWrapper(instance);
        }

        public DtePropertyWrapper Item(string name)
        {
            return DtePropertyWrapper.FromObject(GetItem(_instance, name));
        }

        private static object GetItem(object instance, string name)
        {
            return ItemAccessor(instance, name);
        }
    }
}
