// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.Lightup
{
    using System;

    internal struct DtePropertyWrapper
    {
        private static readonly Type WrappedType;
        private static readonly Func<object, object> ValueGetter;
        private static readonly Action<object, object> ValueSetter;

        private readonly object _instance;

        static DtePropertyWrapper()
        {
            WrappedType = AutomationLightupHelpers.FindType(
                "EnvDTE.Property, EnvDTE",
                "EnvDTE.Property, envdte",
                "EnvDTE.Property, Microsoft.VisualStudio.Interop",
                "EnvDTE.Property");
            ValueGetter = AutomationLightupHelpers.CreatePropertyAccessor<object>(WrappedType, "Value");
            ValueSetter = AutomationLightupHelpers.CreatePropertySetter<object>(WrappedType, typeof(object), "Value");
        }

        private DtePropertyWrapper(object instance)
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

        public object Value
        {
            get
            {
                return GetValue(_instance);
            }

            set
            {
                SetValue(_instance, value);
            }
        }

        public static DtePropertyWrapper FromObject(object instance)
        {
            if (instance == null)
            {
                return default(DtePropertyWrapper);
            }

            return new DtePropertyWrapper(instance);
        }

        private static object GetValue(object instance)
        {
            return ValueGetter(instance);
        }

        private static void SetValue(object instance, object value)
        {
            ValueSetter(instance, value);
        }
    }
}
