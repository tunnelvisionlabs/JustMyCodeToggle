// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE.txt in the project root for license information.

namespace Tvl.VisualStudio.JustMyCodeToggle.Lightup
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    internal static class AutomationLightupHelpers
    {
        public static Func<object, TResult> CreatePropertyAccessor<TResult>(Type type, string propertyName)
        {
            TResult FallbackAccessor(object instance)
            {
                if (instance == null)
                {
                    throw new NullReferenceException();
                }

                return default(TResult);
            }

            if (type == null)
            {
                return FallbackAccessor;
            }

            PropertyInfo property = type.GetTypeInfo().GetDeclaredProperty(propertyName);
            if (property == null || property.GetMethod == null)
            {
                return FallbackAccessor;
            }

            ValidateResultType<TResult>(property.PropertyType);
            return CreateObjectAccessor<TResult>(type, property.GetMethod);
        }

        public static Action<object, TValue> CreatePropertySetter<TValue>(Type type, Type valueType, string propertyName)
        {
            void FallbackAccessor(object instance, TValue value)
            {
                if (instance == null)
                {
                    throw new NullReferenceException();
                }
            }

            if (type == null)
            {
                return FallbackAccessor;
            }

            PropertyInfo property = type.GetTypeInfo().GetDeclaredProperty(propertyName);
            return property == null || property.SetMethod == null
                ? FallbackAccessor
                : CreateObjectSetter<TValue>(type, property.SetMethod, valueType);
        }

        public static Func<object, TArg, TResult> CreateMethodAccessor<TArg, TResult>(Type type, Type argumentType, string methodName)
        {
            TResult FallbackAccessor(object instance, TArg argument)
            {
                if (instance == null)
                {
                    throw new NullReferenceException();
                }

                return default(TResult);
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
            return CreateObjectAccessor<TArg, TResult>(type, method);
        }

        public static Func<object, TArg1, TArg2, TResult> CreateMethodAccessor<TArg1, TArg2, TResult>(
            Type type,
            Type argument1Type,
            Type argument2Type,
            string methodName)
        {
            TResult FallbackAccessor(object instance, TArg1 argument1, TArg2 argument2)
            {
                if (instance == null)
                {
                    throw new NullReferenceException();
                }

                return default(TResult);
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
            return CreateObjectAccessor<TArg1, TArg2, TResult>(type, method);
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

        private static MethodInfo GetInstanceMethod(Type type, string methodName, params Type[] parameterTypes)
        {
            return type.GetTypeInfo().GetDeclaredMethods(methodName)
                .SingleOrDefault(method => !method.IsStatic && HasParameters(method, parameterTypes));
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
