﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MR.AspNetCore.Jobs.Server
{
	public class ProcessingContext : IDisposable
	{
		private IServiceScope _scope;

		private ProcessingContext(ProcessingContext other)
		{
			Provider = other.Provider;
			Storage = other.Storage;
			CancellationToken = other.CancellationToken;
		}

		public ProcessingContext()
		{
		}

		public ProcessingContext(
			IServiceProvider provider,
			IStorage storage,
			CronJobRegistry cronJobRegistry,
			CancellationToken cancellationToken)
		{
			Provider = provider;
			Storage = storage;
			CronJobRegistry = cronJobRegistry;
			CancellationToken = cancellationToken;
		}

		public IServiceProvider Provider { get; private set; }

		public IStorage Storage { get; }

		public CronJobRegistry CronJobRegistry { get; private set; }

		public CancellationToken CancellationToken { get; }

		public bool IsStopping => CancellationToken.IsCancellationRequested;

		public void ThrowIfStopping() => CancellationToken.ThrowIfCancellationRequested();

		public ProcessingContext CreateScope()
		{
			var n = new ProcessingContext(this);
			n._scope = Provider
				.GetRequiredService<IServiceScopeFactory>()
				.CreateScope();
			n.Provider = n._scope.ServiceProvider;
			n.CronJobRegistry = CronJobRegistry;
			return n;
		}

		public Task WaitAsync(TimeSpan timeout)
		{
			return Task.Delay(timeout, CancellationToken);
		}

		public void Dispose()
		{
			if (_scope != null)
			{
				_scope.Dispose();
			}
		}
	}
}