using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MR.AspNetCore.Jobs.Models;
using MR.AspNetCore.Jobs.Server;
using Xunit;

namespace MR.AspNetCore.Jobs
{
	public class SqlServerBootstrapperTest : DatabaseTestHost
	{
		private Mock<IProcessingServer> _mockProcessingServer;
		private Mock<IStorage> _mockStorage;
		private ServiceCollection _services;
		private Mock<IApplicationLifetime> _mockApplicationLifetime;
		private Mock<IStorageConnection> _mockStorageConnection;

		public SqlServerBootstrapperTest()
		{
			_services = new ServiceCollection();
			_services.AddLogging();
			_services.AddSingleton(new JobsOptions());
			_mockProcessingServer = new Mock<IProcessingServer>();
			_services.AddSingleton(_mockProcessingServer.Object);
			_mockStorage = new Mock<IStorage>();
			_mockStorageConnection = new Mock<IStorageConnection>();
			_mockStorage.Setup(m => m.GetConnection()).Returns(_mockStorageConnection.Object);
			_services.AddSingleton(_mockStorage.Object);
			_mockApplicationLifetime = new Mock<IApplicationLifetime>();
			_services.AddSingleton(_mockApplicationLifetime.Object);
			_services.AddTransient<SqlServerBootstrapper>();
		}

		[Fact]
		public void Bootstrap_CallsStorage_Initialize()
		{
			// Arrange
			_services.AddSingleton(new JobsOptions());
			var provider = _services.BuildServiceProvider();
			var bootstrapper = provider.GetService<SqlServerBootstrapper>();

			// Act
			bootstrapper.Bootstrap();

			// Assert
			_mockStorage.Verify(s => s.Initialize());
		}

		[Fact]
		public void Bootstrap_CallsProcessingServer_Start()
		{
			// Arrange
			_services.AddSingleton(new JobsOptions());
			var provider = _services.BuildServiceProvider();
			var bootstrapper = provider.GetService<SqlServerBootstrapper>();

			// Act
			bootstrapper.Bootstrap();

			// Assert
			_mockProcessingServer.Verify(s => s.Start());
		}

		[Fact]
		public void Bootstrap_UpdatesCronJobs()
		{
			// Arrange
			_services.AddSingleton(
				CreateOptionsWithRegistry(new BazCronJobRegistry()));
			_mockStorageConnection.Setup(m => m.GetCronJobs())
				.Returns(GetCronJobsFromRegistry(new FooCronJobRegistry()));
			var provider = _services.BuildServiceProvider();
			var bootstrapper = provider.GetService<SqlServerBootstrapper>();

			// Act
			bootstrapper.Bootstrap();

			// Assert
			_mockStorageConnection
				.Verify(m => m.UpdateCronJob(It.Is<CronJob>(j => j.Name == nameof(FooJob))), Times.Once());
		}

		[Fact]
		public void Bootstrap_RemovesOldCronJobs()
		{
			// Arrange
			_services.AddSingleton(
				CreateOptionsWithRegistry(new BarCronJobRegistry()));
			_mockStorageConnection.Setup(m => m.GetCronJobs())
				.Returns(GetCronJobsFromRegistry(new FooCronJobRegistry()));
			var provider = _services.BuildServiceProvider();
			var bootstrapper = provider.GetService<SqlServerBootstrapper>();

			// Act
			bootstrapper.Bootstrap();

			// Assert
			_mockStorageConnection.Verify(m => m.RemoveCronJob(nameof(FooJob)), Times.Once());
		}

		private CronJob[] GetCronJobsFromRegistry(CronJobRegistry registry)
		{
			return registry.Build().Select(j => new CronJob()
			{
				Cron = j.Cron,
				Name = j.Name,
				TypeName = j.JobType.AssemblyQualifiedName
			}).ToArray();
		}

		private JobsOptions CreateOptionsWithRegistry(CronJobRegistry registry)
		{
			var options = new JobsOptions();
			options.UseCronJobRegistry(registry);
			return options;
		}

		private class FooCronJobRegistry : CronJobRegistry
		{
			public FooCronJobRegistry()
			{
				RegisterJob<FooJob>(nameof(FooJob), Cron.Daily());
			}
		}

		private class BarCronJobRegistry : CronJobRegistry
		{
			public BarCronJobRegistry()
			{
			}
		}

		private class BazCronJobRegistry : CronJobRegistry
		{
			public BazCronJobRegistry()
			{
				RegisterJob<FooJob>(nameof(FooJob), Cron.Monthly());
			}
		}

		private class FooJob : IJob
		{
			public Task ExecuteAsync()
			{
				return Task.FromResult(0);
			}
		}
	}
}
