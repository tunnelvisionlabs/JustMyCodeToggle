// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.Lightup
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.InteropServices;

    internal static class AutomationLightupHelpers
    {
        public static Func<object, TResult> CreatePropertyAccessor<TResult>(Type type, string propertyName)
        {
            ConcurrentDictionary<Type, Func<object, TResult>> runtimeAccessors =
                new ConcurrentDictionary<Type, Func<object, TResult>>();

            TResult FallbackAccessor(object instance)
            {
                return runtimeAccessors.GetOrAdd(
                    GetRuntimeType(instance),
                    runtimeType => CreateRuntimePropertyAccessor<TResult>(runtimeType, type, propertyName))(instance);
            }

            if (type == null)
            {
                return FallbackAccessor;
            }

            PropertyInfo property = GetInstanceProperty(type, propertyName);
            if (property == null || property.GetMethod == null)
            {
                return FallbackAccessor;
            }

            ValidateResultType<TResult>(property.PropertyType);
            Func<object, TResult> accessor = CreateObjectAccessor<TResult>(type, property.GetMethod);
            return instance => type.IsInstanceOfType(instance)
                ? accessor(instance)
                : FallbackAccessor(instance);
        }

        public static Action<object, TValue> CreatePropertySetter<TValue>(Type type, Type valueType, string propertyName)
        {
            ConcurrentDictionary<Type, Action<object, TValue>> runtimeAccessors =
                new ConcurrentDictionary<Type, Action<object, TValue>>();

            void FallbackAccessor(object instance, TValue value)
            {
                runtimeAccessors.GetOrAdd(
                    GetRuntimeType(instance),
                    runtimeType => CreateRuntimePropertySetter<TValue>(runtimeType, type, valueType, propertyName))(
                        instance,
                        value);
            }

            if (type == null)
            {
                return FallbackAccessor;
            }

            PropertyInfo property = GetInstanceProperty(type, propertyName);
            if (property == null || property.SetMethod == null)
            {
                return FallbackAccessor;
            }

            Action<object, TValue> accessor = CreateObjectSetter<TValue>(type, property.SetMethod, valueType);
            return (instance, value) =>
            {
                if (type.IsInstanceOfType(instance))
                {
                    accessor(instance, value);
                }
                else
                {
                    FallbackAccessor(instance, value);
                }
            };
        }

        public static Func<object, TArg, TResult> CreateMethodAccessor<TArg, TResult>(Type type, Type argumentType, string methodName)
        {
            ConcurrentDictionary<Type, Func<object, TArg, TResult>> runtimeAccessors =
                new ConcurrentDictionary<Type, Func<object, TArg, TResult>>();

            TResult FallbackAccessor(object instance, TArg argument)
            {
                return runtimeAccessors.GetOrAdd(
                    GetRuntimeType(instance),
                    runtimeType => CreateRuntimeMethodAccessor<TArg, TResult>(
                        runtimeType,
                        type,
                        argumentType,
                        methodName))(instance, argument);
            }

            if (type == null)
            {
                return FallbackAccessor;
            }

            MethodInfo method = GetInstanceMethod(type, methodName, argumentType);
            if (method == null)
            {
                return FallbackAccessor;
            }

            ValidateResultType<TResult>(method.ReturnType);
            Func<object, TArg, TResult> accessor = CreateObjectAccessor<TArg, TResult>(type, method);
            return (instance, argument) => type.IsInstanceOfType(instance)
                ? accessor(instance, argument)
                : FallbackAccessor(instance, argument);
        }

        public static Func<object, TArg1, TArg2, TResult> CreateMethodAccessor<TArg1, TArg2, TResult>(
            Type type,
            Type argument1Type,
            Type argument2Type,
            string methodName)
        {
            ConcurrentDictionary<Type, Func<object, TArg1, TArg2, TResult>> runtimeAccessors =
                new ConcurrentDictionary<Type, Func<object, TArg1, TArg2, TResult>>();

            TResult FallbackAccessor(object instance, TArg1 argument1, TArg2 argument2)
            {
                return runtimeAccessors.GetOrAdd(
                    GetRuntimeType(instance),
                    runtimeType => CreateRuntimeMethodAccessor<TArg1, TArg2, TResult>(
                        runtimeType,
                        type,
                        argument1Type,
                        argument2Type,
                        methodName))(instance, argument1, argument2);
            }

            if (type == null)
            {
                return FallbackAccessor;
            }

            MethodInfo method = GetInstanceMethod(type, methodName, argument1Type, argument2Type);
            if (method == null)
            {
                return FallbackAccessor;
            }

            ValidateResultType<TResult>(method.ReturnType);
            Func<object, TArg1, TArg2, TResult> accessor = CreateObjectAccessor<TArg1, TArg2, TResult>(type, method);
            return (instance, argument1, argument2) => type.IsInstanceOfType(instance)
                ? accessor(instance, argument1, argument2)
                : FallbackAccessor(instance, argument1, argument2);
        }

        public static Type FindType(params string[] typeNames)
        {
            foreach (string typeName in typeNames)
            {
                Type type = Type.GetType(typeName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                foreach (string typeName in typeNames)
                {
                    Type type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        private static PropertyInfo GetInstanceProperty(Type type, string propertyName)
        {
            return type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static MethodInfo GetInstanceMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(method => method.Name == methodName && HasParameters(method, parameterTypes));
        }

        private static bool HasParameters(MethodInfo method, Type[] parameterTypes)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static Type GetRuntimeType(object instance)
        {
            if (instance == null)
            {
                throw new NullReferenceException();
            }

            return instance.GetType();
        }

        private static Func<object, TResult> CreateRuntimePropertyAccessor<TResult>(
            Type runtimeType,
            Type declaredType,
            string propertyName)
        {
            PropertyInfo property = GetInstanceProperty(runtimeType, propertyName);
            if (property != null && property.GetMethod != null)
            {
                ValidateResultType<TResult>(property.PropertyType);
                return CreateObjectAccessor<TResult>(runtimeType, property.GetMethod);
            }

            property = declaredType == null ? null : GetInstanceProperty(declaredType, propertyName);
            if (property != null && property.GetMethod != null)
            {
                ValidateResultType<TResult>(property.PropertyType);
                Func<object, TResult> accessor = CreateObjectAccessor<TResult>(declaredType, property.GetMethod);
                return instance => accessor(GetTypedObject(instance, declaredType));
            }

            throw new MissingMemberException(runtimeType.FullName, propertyName);
        }

        private static Action<object, TValue> CreateRuntimePropertySetter<TValue>(
            Type runtimeType,
            Type declaredType,
            Type valueType,
            string propertyName)
        {
            PropertyInfo property = GetInstanceProperty(runtimeType, propertyName);
            if (property != null && property.SetMethod != null)
            {
                return CreateObjectSetter<TValue>(runtimeType, property.SetMethod, valueType);
            }

            property = declaredType == null ? null : GetInstanceProperty(declaredType, propertyName);
            if (property != null && property.SetMethod != null)
            {
                Action<object, TValue> accessor = CreateObjectSetter<TValue>(declaredType, property.SetMethod, valueType);
                return (instance, value) => accessor(GetTypedObject(instance, declaredType), value);
            }

            throw new MissingMemberException(runtimeType.FullName, propertyName);
        }

        private static Func<object, TArg, TResult> CreateRuntimeMethodAccessor<TArg, TResult>(
            Type runtimeType,
            Type declaredType,
            Type argumentType,
            string methodName)
        {
            MethodInfo method = GetInstanceMethod(runtimeType, methodName, argumentType);
            if (method != null)
            {
                ValidateResultType<TResult>(method.ReturnType);
                return CreateObjectAccessor<TArg, TResult>(runtimeType, method);
            }

            method = declaredType == null ? null : GetInstanceMethod(declaredType, methodName, argumentType);
            if (method != null)
            {
                ValidateResultType<TResult>(method.ReturnType);
                Func<object, TArg, TResult> accessor = CreateObjectAccessor<TArg, TResult>(declaredType, method);
                return (instance, argument) => accessor(GetTypedObject(instance, declaredType), argument);
            }

            throw new MissingMemberException(runtimeType.FullName, methodName);
        }

        private static Func<object, TArg1, TArg2, TResult> CreateRuntimeMethodAccessor<TArg1, TArg2, TResult>(
            Type runtimeType,
            Type declaredType,
            Type argument1Type,
            Type argument2Type,
            string methodName)
        {
            MethodInfo method = GetInstanceMethod(runtimeType, methodName, argument1Type, argument2Type);
            if (method != null)
            {
                ValidateResultType<TResult>(method.ReturnType);
                return CreateObjectAccessor<TArg1, TArg2, TResult>(runtimeType, method);
            }

            method = declaredType == null ? null : GetInstanceMethod(declaredType, methodName, argument1Type, argument2Type);
            if (method != null)
            {
                ValidateResultType<TResult>(method.ReturnType);
                Func<object, TArg1, TArg2, TResult> accessor =
                    CreateObjectAccessor<TArg1, TArg2, TResult>(declaredType, method);
                return (instance, argument1, argument2) =>
                    accessor(GetTypedObject(instance, declaredType), argument1, argument2);
            }

            throw new MissingMemberException(runtimeType.FullName, methodName);
        }

        private static object GetTypedObject(object instance, Type type)
        {
            if (instance == null)
            {
                throw new NullReferenceException();
            }

            if (type.IsInstanceOfType(instance))
            {
                return instance;
            }

            if (!Marshal.IsComObject(instance))
            {
                throw new InvalidCastException();
            }

            IntPtr unknown = Marshal.GetIUnknownForObject(instance);
            try
            {
                return Marshal.GetTypedObjectForIUnknown(unknown, type);
            }
            finally
            {
                Marshal.Release(unknown);
            }
        }

        private static void ValidateResultType<TResult>(Type resultType)
        {
            if (!typeof(TResult).GetTypeInfo().IsAssignableFrom(resultType.GetTypeInfo()))
            {
                throw new InvalidOperationException();
            }
        }

        private static Func<object, TResult> CreateObjectAccessor<TResult>(Type type, MethodInfo method)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            Expression instance = Expression.Convert(instanceParameter, type);
            Expression call = Expression.Call(instance, method);
            Expression body = Expression.Convert(call, typeof(TResult));

            return Expression.Lambda<Func<object, TResult>>(body, instanceParameter).Compile();
        }

        private static Func<object, TArg, TResult> CreateObjectAccessor<TArg, TResult>(Type type, MethodInfo method)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            ParameterExpression argumentParameter = Expression.Parameter(typeof(TArg), "argument");
            Expression instance = Expression.Convert(instanceParameter, type);
            ParameterInfo[] parameters = method.GetParameters();
            Expression argument = Expression.Convert(argumentParameter, parameters[0].ParameterType);
            Expression call = Expression.Call(instance, method, argument);
            Expression body = Expression.Convert(call, typeof(TResult));

            return Expression.Lambda<Func<object, TArg, TResult>>(body, instanceParameter, argumentParameter).Compile();
        }

        private static Func<object, TArg1, TArg2, TResult> CreateObjectAccessor<TArg1, TArg2, TResult>(Type type, MethodInfo method)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            ParameterExpression argument1Parameter = Expression.Parameter(typeof(TArg1), "argument1");
            ParameterExpression argument2Parameter = Expression.Parameter(typeof(TArg2), "argument2");
            Expression instance = Expression.Convert(instanceParameter, type);
            ParameterInfo[] parameters = method.GetParameters();
            Expression argument1 = Expression.Convert(argument1Parameter, parameters[0].ParameterType);
            Expression argument2 = Expression.Convert(argument2Parameter, parameters[1].ParameterType);
            Expression call = Expression.Call(instance, method, argument1, argument2);
            Expression body = Expression.Convert(call, typeof(TResult));

            return Expression.Lambda<Func<object, TArg1, TArg2, TResult>>(
                body,
                instanceParameter,
                argument1Parameter,
                argument2Parameter).Compile();
        }

        private static Action<object, TValue> CreateObjectSetter<TValue>(Type type, MethodInfo method, Type valueType)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
            ParameterExpression valueParameter = Expression.Parameter(typeof(TValue), "value");
            Expression instance = Expression.Convert(instanceParameter, type);
            Expression value = Expression.Convert(valueParameter, valueType);
            Expression call = Expression.Call(instance, method, value);

            return Expression.Lambda<Action<object, TValue>>(call, instanceParameter, valueParameter).Compile();
        }
    }
}
