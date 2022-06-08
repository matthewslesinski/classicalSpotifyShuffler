using System;
using System.Collections.Generic;
using System.Threading;
using CustomResources.Utils.Concepts;
using CustomResources.Utils.Concepts.DataStructures;
using CustomResources.Utils.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationResources.Services
{
	public static class GlobalDependencies
	{
		public static IServiceProvider GlobalDependencyContainer
		{
			get
			{
				if (_globalDependencyContainerWrapper == null)
					throw new NotImplementedException("Global Dependencies have not been initialized or have been disposed");
				return _globalDependencyContainerWrapper.WrappedServiceProvider;
			}
		}
		private static ServiceProviderDisposableWrapper _globalDependencyContainerWrapper;

		private static Reference<bool> _initialized = false;

		public static IDisposable InitializeWith(IServiceProvider services)
		{
			if (Interlocked.Exchange(ref _initialized, true))
				throw new InvalidOperationException("The dependency container has already been initialized");
			var servicesWrapper = new ServiceProviderDisposableWrapper(services);
			ApplyServiceProvider(servicesWrapper);
			return servicesWrapper;
		}

		public static IServiceBuilder Initialize(string[] args = null)
		{
			if (Interlocked.Exchange(ref _initialized, true))
				throw new InvalidOperationException("The dependency container has already been initialized");
			var serviceBuilder = new ServiceBuilder(args);
			serviceBuilder.OnBuild += ApplyServiceProvider;
			return serviceBuilder;
		}

		public static T Get<T>() => GlobalDependencyContainer.GetRequiredService<T>();

		public static void ReleaseResource()
		{
			_globalDependencyContainerWrapper.Dispose();
			_globalDependencyContainerWrapper = null;
		}

		private static void ApplyServiceProvider(ServiceProviderDisposableWrapper serviceProviderWrapper)
		{
			if (Interlocked.CompareExchange(ref _globalDependencyContainerWrapper, serviceProviderWrapper, null) != null)
				throw new InvalidOperationException("The dependency container has already been initialized with a service provider");
		}

		public interface IServiceBuilder
		{
			IServiceBuilder AddGlobalService<ServiceT, ImplementationT>() where ServiceT : class where ImplementationT : class, ServiceT;
			IServiceBuilder AddGlobalService<ServiceT>(ServiceT service) where ServiceT : class;
			IServiceBuilder AddGlobalService<ServiceT>(Func<IServiceProvider, ServiceT> serviceConstructor) where ServiceT : class;
			IServiceBuilder AddImplementationForMultipleGlobalServices(object service, params Type[] serviceTypes);
			IServiceBuilder AddImplementationForMultipleGlobalServices<ImplementationT>(params Type[] serviceTypes) where ImplementationT : class, new();
			IDisposable Build();
		}

		private class ServiceBuilder : IServiceBuilder
		{
			private readonly string[] _args;
			private readonly List<Action<IServiceCollection>> _registerActions = new();
			internal event Action<ServiceProviderDisposableWrapper> OnBuild;

			public ServiceBuilder(string[] args = null)
			{
				_args = args;
			}

			public IServiceBuilder AddGlobalService<ServiceT, ImplementationT>() where ServiceT : class where ImplementationT : class, ServiceT
			{
				_registerActions.Add(services => services.AddSingleton<ServiceT, ImplementationT>());
				return this;
			}

			public IServiceBuilder AddGlobalService<ServiceT>(ServiceT service) where ServiceT : class
			{
				_registerActions.Add(services => services.AddSingleton(service));
				return this;
			}

			public IServiceBuilder AddGlobalService<ServiceT>(Func<IServiceProvider, ServiceT> serviceConstructor) where ServiceT : class
			{
				_registerActions.Add(services => services.AddSingleton(serviceConstructor));
				return this;
			}

			public IServiceBuilder AddImplementationForMultipleGlobalServices<ImplementationT>(params Type[] serviceTypes) where ImplementationT : class, new()
			{
				_registerActions.Add(services => services.AddImplementationForMultipleGlobalServices<ImplementationT>(serviceTypes));
				return this;
			}

			public IServiceBuilder AddImplementationForMultipleGlobalServices(object service, params Type[] serviceTypes)
			{
				_registerActions.Add(services => services.AddImplementationForMultipleGlobalServices(service, serviceTypes));
				return this;
			}

			public IDisposable Build()
			{
				var hostBuilder = _args == null ? Host.CreateDefaultBuilder() : Host.CreateDefaultBuilder(_args);
				var host = hostBuilder
					.ConfigureServices(services => _registerActions.EachIndependently(action => action(services)))
					.Build();
				_registerActions.Clear();
				var disposableWrapper = new ServiceProviderDisposableWrapper(host);
				OnBuild?.Invoke(disposableWrapper);
				return disposableWrapper;
			}
		}

		private class ServiceProviderDisposableWrapper : DisposableAction
		{
			public IServiceProvider WrappedServiceProvider => _services;
			private IServiceProvider _services;

			public ServiceProviderDisposableWrapper(IHost host) : base(host.Dispose)
			{
				_services = host.Services;
			}

			public ServiceProviderDisposableWrapper(IServiceProvider services) : base(() => { })
			{
				_services = services;
			}

			protected override void DoDispose()
			{
				base.DoDispose();
				_services = null;
			}
		}
	}

	public interface IGlobalServiceUser
	{
		public IDataStoreAccessor DataStore => GlobalDependencies.GlobalDependencyContainer.GetLocalDataStore();
		public IUserInterface UserInterface => GlobalDependencies.GlobalDependencyContainer.GetUserInterface();
	}

	public static class IGlobalDependencyExtensions
	{
		public static void AddImplementationForMultipleGlobalServices<ImplementationT>(this IServiceCollection services, params Type[] serviceTypes) where ImplementationT : class, new() =>
			AddImplementationForMultipleGlobalServices(services, new ImplementationT(), serviceTypes);

		public static void AddImplementationForMultipleGlobalServices(this IServiceCollection services, object service, params Type[] serviceTypes)
		{
			serviceTypes.EachIndependently(serviceType =>
			{
				if (!serviceType.IsInstanceOfType(service))
					throw new ArgumentException($"Cannot add an object of type {service?.GetType()?.Name} as a service of type {serviceType?.Name}");
				services.AddSingleton(serviceType, service);
			});
		}

		public static IDataStoreAccessor GetLocalDataStore(this IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<IDataStoreAccessor>();
		public static IUserInterface GetUserInterface(this IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<IUserInterface>();

		public static IDataStoreAccessor AccessLocalDataStore(this IGlobalServiceUser dependent) => dependent.DataStore;
		public static IUserInterface AccessUserInterface(this IGlobalServiceUser dependent) => dependent.UserInterface;
	}
}