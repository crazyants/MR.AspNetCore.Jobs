using System;
using System.Threading.Tasks;
using MR.AspNetCore.Jobs.Models;
using MR.AspNetCore.Jobs.Server;

namespace MR.AspNetCore.Jobs
{
	public interface IStorageConnection : IDisposable
	{
		Task StoreJobAsync(DelayedJob job);

		Task StoreJobAsync(CronJob job);

		Task UpdateCronJobAsync(CronJob job);

		Task<IFetchedJob> FetchNextDelayedJobAsync(DateTime from, DateTime to);

		Task<CronJob[]> GetCronJobsAsync();

		Task<CronJob> GetCronJobByNameAsync(string name);

		Task RemoveCronJobAsync(string name);
	}

	public static class JobStorageConnectionExtensions
	{
		public static Task<IFetchedJob> FetchNextDelayedJobAsync(this IStorageConnection @this, DateTime to)
			=> @this.FetchNextDelayedJobAsync(DateTime.MinValue, to);

		public static Task<IFetchedJob> FetchNextDelayedJobAsync(this IStorageConnection @this)
			=> @this.FetchNextDelayedJobAsync(DateTime.MinValue, DateTime.UtcNow);
	}
}
