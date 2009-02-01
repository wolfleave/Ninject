﻿using System;
using System.Collections.Generic;
using System.Linq;
using Ninject.Infrastructure.Components;
using Xunit;

namespace Ninject.Tests.ComponentContainerTests
{
	public class ComponentContainerContext
	{
		protected readonly ComponentContainer container;

		public ComponentContainerContext()
		{
			container = new ComponentContainer();
		}
	}

	public class WhenGetIsCalled : ComponentContainerContext
	{
		[Fact]
		public void ReturnsNullWhenNoImplementationRegisteredForService()
		{
			var service = container.Get<ITestService>();
			Assert.Null(service);
		}

		[Fact]
		public void ReturnsInstanceWhenOneImplementationIsRegistered()
		{
			container.Add<ITestService, TestServiceA>();
			var service = container.Get<ITestService>();

			Assert.NotNull(service);
			Assert.IsType<TestServiceA>(service);
		}

		[Fact]
		public void ReturnsInstanceOfFirstRegisteredImplementation()
		{
			container.Add<ITestService, TestServiceA>();
			container.Add<ITestService, TestServiceB>();
			var service = container.Get<ITestService>();

			Assert.NotNull(service);
			Assert.IsType<TestServiceA>(service);
		}

		[Fact]
		public void InjectsEnumeratorOfServicesWhenConstructorArgumentIsIEnumerable()
		{
			container.Add<ITestService, TestServiceA>();
			container.Add<ITestService, TestServiceB>();
			container.Add<IAsksForEnumerable, AsksForEnumerable>();
			var asks = container.Get<IAsksForEnumerable>();

			Assert.NotNull(asks);
			Assert.NotNull(asks.SecondService);
			Assert.IsType<TestServiceB>(asks.SecondService);
		}

		[Fact]
		public void InjectsListOfServicesWhenConstructorArgumentIsICollection()
		{
			container.Add<ITestService, TestServiceA>();
			container.Add<ITestService, TestServiceB>();
			container.Add<IAsksForCollection, AsksForCollection>();
			var asks = container.Get<IAsksForCollection>();

			Assert.NotNull(asks);
			Assert.NotNull(asks.Services);
			Assert.Equal(2, asks.Services.Count);

			var list = asks.Services as List<ITestService>;

			Assert.IsType<TestServiceA>(list[0]);
			Assert.IsType<TestServiceB>(list[1]);
		}

		[Fact]
		public void InjectsListOfServicesWhenConstructorArgumentIsList()
		{
			container.Add<ITestService, TestServiceA>();
			container.Add<ITestService, TestServiceB>();
			container.Add<IAsksForList, AsksForList>();
			var asks = container.Get<IAsksForList>();

			Assert.NotNull(asks);
			Assert.NotNull(asks.Services);
			Assert.Equal(2, asks.Services.Count);

			var list = asks.Services;

			Assert.IsType<TestServiceA>(list[0]);
			Assert.IsType<TestServiceB>(list[1]);
		}

		[Fact]
		public void InjectsArrayOfServicesWhenConstructorArgumentIsArray()
		{
			container.Add<ITestService, TestServiceA>();
			container.Add<ITestService, TestServiceB>();
			container.Add<IAsksForArray, AsksForArray>();
			var asks = container.Get<IAsksForArray>();

			Assert.NotNull(asks);
			Assert.NotNull(asks.Services);
			Assert.Equal(2, asks.Services.Length);
			Assert.IsType<TestServiceA>(asks.Services[0]);
			Assert.IsType<TestServiceB>(asks.Services[1]);
		}
	}

	public class WhenGetAllIsCalledOnComponentContainer : ComponentContainerContext
	{
		[Fact]
		public void ReturnsSeriesWithSingleItem()
		{
			container.Add<ITestService, TestServiceA>();
			var services = container.GetAll<ITestService>().ToList();

			Assert.NotNull(services);
			Assert.Equal(1, services.Count);
			Assert.IsType<TestServiceA>(services[0]);
		}

		[Fact]
		public void ReturnsInstanceOfEachRegisteredImplementation()
		{
			container.Add<ITestService, TestServiceA>();
			container.Add<ITestService, TestServiceB>();
			var services = container.GetAll<ITestService>().ToList();

			Assert.NotNull(services);
			Assert.Equal(2, services.Count);
			Assert.IsType<TestServiceA>(services[0]);
			Assert.IsType<TestServiceB>(services[1]);
		}

		[Fact]
		public void ReturnsSameInstanceForTwoCallsForSameService()
		{
			container.Add<ITestService, TestServiceA>();

			var service1 = container.Get<ITestService>();
			var service2 = container.Get<ITestService>();

			Assert.NotNull(service1);
			Assert.NotNull(service2);
			Assert.Same(service1, service2);
		}
	}

	public class WhenRemoveAllIsCalled : ComponentContainerContext
	{
		[Fact]
		public void RemovesAllMappings()
		{
			container.Add<ITestService, TestServiceA>();
			var service1 = container.Get<ITestService>();
			Assert.NotNull(service1);

			container.RemoveAll<ITestService>();
			var service2 = container.Get<ITestService>();
			Assert.Null(service2);
		}

		[Fact]
		public void DisposesOfAllInstances()
		{
			container.Add<ITestService, TestServiceA>();
			container.Add<ITestService, TestServiceB>();
			var services = container.GetAll<ITestService>().ToList();

			Assert.NotNull(services);
			Assert.Equal(2, services.Count);

			container.RemoveAll<ITestService>();

			Assert.True(services[0].IsDisposed);
			Assert.True(services[1].IsDisposed);
		}
	}

	internal class AsksForList : IAsksForList
	{
		public List<ITestService> Services { get; set; }

		public AsksForList(List<ITestService> services)
		{
			Services = services;
		}
	}

	internal interface IAsksForList : INinjectComponent
	{
		List<ITestService> Services { get; set; }
	}

	internal class AsksForArray : IAsksForArray
	{
		public ITestService[] Services { get; set; }

		public AsksForArray(ITestService[] services)
		{
			Services = services;
		}
	}

	internal interface IAsksForArray : INinjectComponent
	{
		ITestService[] Services { get; set; }
	}

	internal class AsksForCollection : IAsksForCollection
	{
		public ICollection<ITestService> Services { get; set; }

		public AsksForCollection(ICollection<ITestService> services)
		{
			Services = services;
		}
	}

	internal interface IAsksForCollection : INinjectComponent
	{
		ICollection<ITestService> Services { get; set; }
	}

	internal class AsksForEnumerable : IAsksForEnumerable
	{
		public ITestService SecondService { get; set; }

		public AsksForEnumerable(IEnumerable<ITestService> services)
		{
			SecondService = services.Skip(1).First();
		}
	}

	internal interface IAsksForEnumerable : INinjectComponent
	{
		ITestService SecondService { get; set; }
	}

	internal class TestServiceA : ITestService
	{
		public bool IsDisposed { get; set; }
		public void Dispose() { IsDisposed = true; }
	}

	internal class TestServiceB : ITestService
	{
		public bool IsDisposed { get; set; }
		public void Dispose() { IsDisposed = true; }
	}

	internal interface ITestService : INinjectComponent, IDisposable
	{
		bool IsDisposed { get; set; }
	}
}