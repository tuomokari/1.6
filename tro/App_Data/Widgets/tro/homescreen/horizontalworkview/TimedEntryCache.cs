using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading;
using System.Threading.Tasks;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.Core.Runtime;

namespace SystemsGarden.mc2.widgets.horizontalworkview
{

	/// <summary>
	/// Class for caching daily hour totals for users
	/// </summary>
	public class TimedEntryCache
	{
		private const string DateStringFormat = "yyyy-MM-dd";

		private DataTree userEntries;
		private Dictionary<string, DataTree> documents;
		private object lockObject = new object();
		private ILogger logger;

		private IRuntime runtime;

		Dictionary<string, SemaphoreSlim> userDataSemaphores;

		public TimedEntryCache(IRuntime runtime, ILogger logger)
		{
			this.runtime = runtime;
			this.logger = logger.CreateChildLogger("workviewcache");
			userEntries = new DataTree();
			userDataSemaphores = new Dictionary<string, SemaphoreSlim>();
		}

		public async Task<DataTree> GetCachedEntitiesAsync(DateTime start, DateTime end, string userId)
		{
			var results = new DataTree();

			try
			{
				logger.LogTrace("Getting cached entities for user", userId, start, end);

				if (start.Kind != DateTimeKind.Utc)
					throw new ArgumentException("Start time for GetCachedEntities is not in UTC.", "start");

				if (end.Kind != DateTimeKind.Utc)
					throw new ArgumentException("End time for GetCachedEntities is not in UTC.", "end");

				DataTree userTree;

				// Lock whole cache when getting a single user entry
				lock (userEntries)
				{
					userTree = userEntries[userId];
					userTree.Create();
				}

				// Regular locks are thread affine which makes them unusable
				// for locking async tasks. Use semaphore and waitasync instead.
				// See http://blogs.msdn.com/b/pfxteam/archive/2012/02/29/10274035.aspx
				SemaphoreSlim semaphore;
				lock (userDataSemaphores)
				{
					if (userDataSemaphores.ContainsKey(userId))
					{
						semaphore = userDataSemaphores[userId];
					}
					else
					{
						semaphore = new SemaphoreSlim(1);
						userDataSemaphores[userId] = semaphore;
					}
				}

				// Lock the user entry when working on it.
				await semaphore.WaitAsync();

				try
				{
					results = await GetUserEntriesForDayRangeAsync(userTree, start, end);
				}
				finally
				{
					semaphore.Release();
				}

				lock(userDataSemaphores)
				{
					userDataSemaphores.Remove(userId);
				}

			}
			catch(Exception ex)
			{
				logger.LogError("Failed to get cached entities.", ex);
				results = new DataTree();
			}

			return results;
		}

		/// <summary>
		/// Get user entries for given date range. Fetch from server if needed.
		/// </summary>
		/// <param name="userTree"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		/// <remarks>Expects user data to be locked during the call</remarks>
		private async Task<DataTree> GetUserEntriesForDayRangeAsync(DataTree userTree, DateTime start, DateTime end)
		{
			// Number of days that can have changed before the entire range is invalidated
			const int MaxDirtyEntriesToAccept = 4;

			int dirtyDayblocksInRange = CountDirtyItemsInDayRange(userTree, start, end);

			if (dirtyDayblocksInRange > MaxDirtyEntriesToAccept)
			{
				logger.LogTrace("Cache too dirty, invalidating entire day range", userTree, start, end);
				await CacheAllDaysInRangeForUserAsync(userTree, start, end);
			}
			else if (dirtyDayblocksInRange > 0)
			{
				logger.LogTrace("Cache partially dirty. Invalidating per day basis.", userTree, start, end, "Dirty cached days: " + dirtyDayblocksInRange);
				await CacheIndividualDaysInRangeForUserAsync(userTree, start, end);
			}
			else
			{
				logger.LogTrace("All range entries were found in cache. Returning from cache.");
			}

			return GetDayEntriesFromLocalCache(userTree, start, end);
		}

		private DataTree GetDayEntriesFromLocalCache(DataTree userTree, DateTime start, DateTime end)
		{
			var results = new DataTree();

			while (DateTime.Compare(start, end) <= 0)
			{
				string dayKey = start.ToString(DateStringFormat);
				InvalidateDay(userTree[dayKey]);
				start = start.AddDays(1);
			}

			return results;
		}

		private async Task CacheAllDaysInRangeForUserAsync(DataTree userTree, DateTime start, DateTime end)
		{
			logger.LogTrace("Caching days for user in a single query", userTree.Name, start, end);

			var query = new DBQuery( "horizontalworkview", "dailyentriesforcache_range");
			query.AddParameter("start", start);
			query.AddParameter("end", end);
			query.AddParameter("user", new ObjectId(userTree.Name));

			DBResponse queryResult = await query.FindAsync();

			InvalidateDayRange(userTree, start, end);

			// Cache each entry
			foreach (DataTree entryType in queryResult.FirstCollection)
			{
				foreach (DataTree entry in entryType)
				{
					documents[entry["_id"]] = entry;
				}
			}
		}

		private async Task CacheIndividualDaysInRangeForUserAsync(DataTree userTree, DateTime start, DateTime end)
		{
			while (DateTime.Compare(start, end) <= 0)
			{
				string dayKey = start.ToString(DateStringFormat);

				if ((bool)userTree[dayKey]["dirty"].GetValueOrDefault(true))
					await RefreshUserDataForDayAsync(userTree, start, dayKey);
				start = start.AddDays(1);
			}
		}

		private void CacheIndividualEntry(DataTree userTree, DataTree entry)
		{
			// Get date primarily from date filed and secondarily from starttimestamp
			DateTime date;
			if (entry.Contains("date"))
			{
				date = ((DateTime)entry["date"]).Date;
			}
			else if (entry.Contains("starttimestamp"))
			{
				date = ((DateTime)entry["starttimestamp"]).Date;
			}
			else
			{
				logger.LogError("Data to be cached didn't contain date or timestamp.", "User: " + userTree, "Entry: " + entry);
				return;
			}
		}

		/// <summary>
		/// Count the number of day entries in a given range that have been modified since they were cached.
		/// </summary>
		/// <param name="userTree"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		private int CountDirtyItemsInDayRange(DataTree userTree, DateTime start, DateTime end)
		{
			int dirtyDayEntries = 0;

			while (DateTime.Compare(start, end) <= 0)
			{
				string dayKey = start.ToString(DateStringFormat);

				if ((bool)userTree[dayKey]["dirty"].GetValueOrDefault(true))
					dirtyDayEntries++;

				start = start.AddDays(1);
			}

			return dirtyDayEntries;
		}

		/// <summary>
		/// Invalidates an entire range of days from cache.
		/// </summary>
		/// <param name="userTree"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		private void InvalidateDayRange(DataTree userTree, DateTime start, DateTime end)
		{
			while (DateTime.Compare(start, end) <= 0)
			{
				string dayKey = start.ToString(DateStringFormat);
				InvalidateDay(userTree[dayKey]);
				start = start.AddDays(1);
			}
		}

		/// <summary>
		/// Invalidate entries for this day's entry and user.
		/// </summary>
		/// <param name="entry"></param>
		public void InvalidateCacheEntry(DataTree entry)
		{
			logger.LogTrace("Invalidating cached entry", entry);

			if (!documents.ContainsKey(entry["_id"]))
				return;

			DataTree cachedEntry = documents[entry["_id"]];

			// day
			//     entries
			//         timesheetentry
			//                  
			DataTree day = entry.Parent.Parent.Parent;
			logger.LogTrace("Invalidating day from cache", day);

			InvalidateDay(day);
		}

		private async Task<DataTree> GetUserEntriesForDayAsync(DateTime date, string dateKey, string userId)
		{
			DataTree userTree = new DataTree();
			DataTree result = new DataTree();
			SemaphoreSlim semaphore = null;

			try
			{
				// Lock whole cache when getting a single user entry
				lock (userEntries)
				{
					userTree = userEntries[userId];
					userTree.Create();
				}

				// Use semaphore instead of lock for async calls.
				lock (userDataSemaphores)
				{
					if (userDataSemaphores.ContainsKey(userId))
					{
						semaphore = userDataSemaphores[userId];
					}
					else
					{
						semaphore = new SemaphoreSlim(1);
						userDataSemaphores[userId] = semaphore;
					}
				}

				// Lock the user entry when working on it.
				await semaphore.WaitAsync();

				// Make sure we really have a date.
				string dateStr = date.Date.ToString(DateStringFormat);

				// Determine if day's data is dirty and if we need to get new data.
				if ((bool)userEntries[userId][dateStr]["dirty"].GetValueOrDefault(true))
				{
					// In case the data is dirty we need to refetch. We maintain lock to this user's data but not to anything else.
					result = await RefreshUserDataForDayAsync(userTree, date, dateKey);
				}
				else
				{
					return userEntries[userId][dateStr];
				}
			}
			catch (Exception ex)
			{
				logger.LogError("Failed to get user entries from cache.", ex);
			}
			finally
			{
				if (semaphore != null)
					semaphore.Release();
			}

			return result;
		}

		/// <summary>
		/// Cache single day's data for one user.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="dateKey"></param>
		/// <param name="day"></param>
		private void CacheSingleDayForUser(string userId, string dateKey, DataTree day)
		{
			try
			{
				DataTree oldDayData = userEntries[userId][dateKey];
				lock (oldDayData)
				{
					InvalidateDay(oldDayData);

					foreach (DataTree entryType in day)
					{
						foreach (DataTree entry in entryType)
						{
							documents[entry["_id"]] = entry;
						}
					}

					oldDayData["entries"].Merge(day);
					oldDayData["dirty"] = false;
				}
			}
			catch (Exception ex)
			{
				logger.LogError("Failed to cache day's events.", ex, "User: " + userId, "DateKay: " + dateKey);
			}
		}

		/// <summary>
		/// Clear single day's events from cache. Day means entries for one user and during one day.
		/// </summary>
		/// <param name="day"></param>
		private void InvalidateDay(DataTree day)
		{
			try
			{
				lock (documents)
				{
					// Clear day's entries from hashtable.
					foreach (DataTree entry in day["entries"])
					{
						if (documents.ContainsKey(entry["_id"]))
							documents.Remove(entry["id"]);
					}

					day.Clear();
				}
			}
			catch (Exception ex)
			{
				logger.LogError("Failed to clear day in document cache", ex, day);
			}
		}

		/// <summary>
		/// Get data for single user for single day, cache and return it.
		/// </summary>
		/// <param name="date"></param>
		/// <param name="dateKey"></param>
		/// <param name="userId"></param>
		/// <returns></returns>
		private async Task<DataTree> RefreshUserDataForDayAsync(DataTree userTree, DateTime date, string dateKey)
		{
			DateTime dayStart = date;

			var query = new DBQuery( "horizontalworkview", "dailyentriesforcache");

			query.AddParameter("date", date);
			query.AddParameter("user", new ObjectId(userTree.Name));

			DBResponse cacheQueryResults = await query.FindAsync();

			DBCollection dayEntries = cacheQueryResults.FirstCollection;

			//CacheSingleDayForUser(userTree, dateKey, dayEntries);

			return (DataTree)dayEntries;
		}
	}
}