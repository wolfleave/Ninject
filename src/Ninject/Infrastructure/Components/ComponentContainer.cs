﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Ninject.Syntax;

namespace Ninject.Infrastructure.Components
{
	public class ComponentContainer : IComponentContainer
	{
		private readonly Multimap<Type, Type> _mappings = new Multimap<Type, Type>();
		private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

		public void Dispose()
		{
			foreach (Type service in _mappings.Keys)
				RemoveAll(service);

			GC.SuppressFinalize(this);
		}

		public void Add<TService, TImplementation>()
			where TService : INinjectComponent
			where TImplementation : TService, INinjectComponent
		{
			_mappings.Add(typeof(TService), typeof(TImplementation));
		}

		public void RemoveAll<T>()
			where T : INinjectComponent
		{
			RemoveAll(typeof(T));
		}

		public void RemoveAll(Type service)
		{
			foreach (object instance in _instances.Values)
			{
				var disposable = instance as IDisposable;

				if (disposable != null)
					disposable.Dispose();
			}

			_instances.Remove(service);
			_mappings.RemoveAll(service);
		}

		public T Get<T>()
			where T : INinjectComponent
		{
			return (T) Get(typeof(T));
		}

		public IEnumerable<T> GetAll<T>()
			where T : INinjectComponent
		{
			return GetAll(typeof(T)).Cast<T>();
		}

		public object Get(Type service)
		{
			return GetAll(service).FirstOrDefault();
		}

		public IEnumerable<object> GetAll(Type service)
		{
			foreach (Type implementation in _mappings[service])
				yield return ResolveInstance(implementation);
		}

		private object ResolveInstance(Type type)
		{
			return _instances.ContainsKey(type) ? _instances[type] : CreateNewInstance(type);
		}

		private object CreateNewInstance(Type type)
		{
			object instance = FormatterServices.GetSafeUninitializedObject(type);
			_instances.Add(type, instance);

			ConstructorInfo constructor = SelectConstructor(type);
			var arguments = constructor.GetParameters().Select(parameter => GetValueForParameter(parameter)).ToArray();

			try
			{
				constructor.Invoke(instance, arguments);
				return instance;
			}
			catch (TargetInvocationException ex)
			{
				ex.RethrowInnerException();
				return null;
			}
		}

		private object GetValueForParameter(ParameterInfo parameter)
		{
			Type service = parameter.ParameterType;

			if (service.IsArray)
			{
				Type element = service.GetElementType();
				return LinqReflection.ToArraySlow(GetAllSlow(element), element);
			}

			if (service.IsGenericType)
			{
				Type gtd = service.GetGenericTypeDefinition();
				Type argument = service.GetGenericArguments()[0];

				if (typeof(List<>).IsAssignableFrom(gtd))
					return LinqReflection.ToListSlow(GetAllSlow(argument), argument);

				if (gtd.IsInterface && typeof(ICollection<>).IsAssignableFrom(gtd))
					return LinqReflection.ToListSlow(GetAllSlow(argument), argument);

				if (gtd.IsInterface && typeof(IEnumerable<>).IsAssignableFrom(gtd))
					return GetAllSlow(argument);
			}

			return Get(service);
		}

		private ConstructorInfo SelectConstructor(Type type)
		{
			return type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
		}

		private IEnumerable GetAllSlow(Type service)
		{
			var method = GetType().GetMethod("GetAll", Type.EmptyTypes).MakeGenericMethod(service);
			return method.Invoke(this, null) as IEnumerable;
		}
	}
}