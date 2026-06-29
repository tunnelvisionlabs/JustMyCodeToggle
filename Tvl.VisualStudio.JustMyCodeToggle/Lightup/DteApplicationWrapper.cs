// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.Lightup
{
    using System;

    internal struct DteApplicationWrapper
    {
        private static readonly Type WrappedType;
        private static readonly Func<object, string, string, object> GetPropertiesAccessor;

        private readonly object _instance;

        static DteApplicationWrapper()
        {
            WrappedType = AutomationLightupHelpers.FindType(
                "EnvDTE.DTE, Microsoft.VisualStudio.Interop",
                "EnvDTE._DTE, EnvDTE",
                "EnvDTE._DTE, envdte",
                "EnvDTE.DTE");
            GetPropertiesAccessor = AutomationLightupHelpers.CreateMethodAccessor<string, string, object>(
                WrappedType,
                typeof(string),
                typeof(string),
                "get_Properties");
        }

        private DteApplicationWrapper(object instance)
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

        public static DteApplicationWrapper FromObject(object instance)
        {
            if (instance == null)
            {
                return default(DteApplicationWrapper);
            }

            return new DteApplicationWrapper(instance);
        }

#pragma warning disable SA1300 // Element should begin with upper-case letter
        public DtePropertiesWrapper get_Properties(string category, string page)
#pragma warning restore SA1300 // Element should begin with upper-case letter
        {
            return DtePropertiesWrapper.FromObject(GetProperties(_instance, category, page));
        }

        private static object GetProperties(object instance, string category, string page)
        {
            return GetPropertiesAccessor(instance, category, page);
        }
    }
}
