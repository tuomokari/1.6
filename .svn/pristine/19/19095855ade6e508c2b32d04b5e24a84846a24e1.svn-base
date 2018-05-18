using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Core;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin;

namespace SystemsGarden.mc2.tro
{
	public class defaultvalues : MC2Controller
	{
		private DataTree socialProject = null;

		/// <summary>
		/// Defaults to current day or day given in selecteddatekey parameter
		/// </summary>
		/// <param name="itemSchema"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public MC2Value selecteddate(DataTree itemSchema)
		{
			string resultDateKey = Runtime.CurrentActionCall.Parameters["selecteddatekey"];

			DateTime resultDate;

			if (string.IsNullOrEmpty(resultDateKey))
			{
				DateTime now = MC2DateTimeValue.Now();
				resultDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
			}
			else
			{
				resultDate = DateTime.ParseExact(resultDateKey, "yyyyMMdd", CultureInfo.InvariantCulture);
				resultDate = new DateTime(resultDate.Year, resultDate.Month, resultDate.Day, 0, 0, 0, DateTimeKind.Utc);
			}

			return resultDate;
		}

		/// <summary>
		/// Defaults to current time and day or day given in selecteddatekey parameter
		/// </summary>
		/// <param name="itemSchema"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public MC2Value selectedstart(DataTree itemSchema)
		{
			DateTime resultDate;

			if (this.Runtime.CurrentActionCall.Parameters.Contains("start"))
			{
				resultDate = (MC2DateTimeValue)MC2DateTimeValue.TryConvertValueFromString(Runtime.CurrentActionCall.Parameters["start"]);
			}
			else
			{
				// Get current time for selected day.
				DateTime now = MC2DateTimeValue.Now().ToUniversalTime();
				resultDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);
			}

			resultDate = ApplyTimeAccuracy(resultDate, itemSchema);

			return resultDate;
		}

		/// <summary>
		/// Defaults to current time and day or day given in selecteddatekey parameter
		/// </summary>
		/// <param name="itemSchema"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public MC2Value selectedend(DataTree itemSchema)
		{
			DateTime resultDate;

			if (this.Runtime.CurrentActionCall.Parameters.Contains("end"))
			{
				resultDate = (MC2DateTimeValue)MC2DateTimeValue.TryConvertValueFromString(Runtime.CurrentActionCall.Parameters["end"]);
			}
			else
			{
				// Get current time for selected day.
				DateTime now = MC2DateTimeValue.Now().ToUniversalTime();
				resultDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc);

				// Default to length of one hour.
				resultDate = resultDate.AddHours(1);
			}

			resultDate = ApplyTimeAccuracy(resultDate, itemSchema);

			return resultDate;
		}

        /// <summary>
        /// Defaults to current time and day or day given in selecteddatekey parameter
        /// </summary>
        /// <param name="itemSchema"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public MC2Value duration(DataTree itemSchema)
        {
            TimeSpan defaultDuration = new TimeSpan();

            if (itemSchema.Contains("defaultdurationminutes"))
            {
                defaultDuration = new TimeSpan(0, (int)itemSchema["defaultdurationminutes"], 0);
            }

            return (MC2Value)defaultDuration.TotalMilliseconds;
        }

        public MC2Value trocurrentproject(DataTree itemSchema)
		{
			MC2Value value = null;

			if ((bool)Runtime.CurrentActionCall.Parameters["socialproject"] == true && (bool)Runtime.Config["application"]["features"]["enablesocialproject"])
			{
				// Todo: cache value?
				if (socialProject == null)
					socialProject = new DBQuery("tro", "getsocialproject").FindOneAsync().Result;

				value = socialProject;
			}
			else if ((bool)Runtime.CurrentActionCall.Parameters["fromhomescreen"] == true)
			{
				// Use either provided project from url or user's project
				if (Runtime.CurrentActionCall.Parameters["workscheduleprojectid"].Exists)
				{
					value = DBDocument.FindOne("project", Runtime.CurrentActionCall.Parameters["workscheduleprojectid"]);
				}
				else
				{
					var userQuery = new DBQuery();

					userQuery["user"][DBQuery.Condition] = Query.EQ(DBQuery.Id, new ObjectId(Runtime.SessionManager.CurrentUser["_id"]))
						.ToString();

					DataTree currentUser = userQuery.FindOne();

					value = currentUser["currentproject"];
				}
			}
			else
			{
				value = MC2EmptyValue.EmptyValue;
			}

			return value;
		}

		public MC2Value defaultassets(
			string collection,
			string valuename,
			string relationtarget,
			string itemid,
			string filtercontroller,
			string filterblock)
		{
			string projectId = Runtime.HistoryManager.GetCurrentHistoryEntryState()["__form_project"]["formvalue"];

			if (string.IsNullOrEmpty(projectId))
			{
				// Use default project if nothing else is provided
				projectId = ((DataTree)this.trocurrentproject(null))[DBQuery.Id];
			}

			if (string.IsNullOrEmpty(projectId))
			{
				logger.LogDebug("No project when looking for default assets.");
				return null;
			}

			var query = new DBQuery("tro", "assetentry_defaultassets");
			query.AddParameter("projectid", new ObjectId(projectId));

			var task = query.FindAsync();

			DBCollection projects = task.Result["project"];

			DataTree root = new DataTree();
			DataTree assets = root["asset"];

			foreach (DataTree project in projects)
			{
				foreach (DataTree allocationEntry in project["allocationentry"])
				{
					if (allocationEntry["asset"].Count > 0 && !assets.Contains(allocationEntry["asset"][DBQuery.Id]))
					{
						assets[allocationEntry["asset"][DBQuery.Id]] = allocationEntry["asset"];
					}
				}
			}

			return assets;
		}


		[Obsolete]
		public MC2Value trotimesheetduration(DataTree itemSchema)
		{
			MC2Value value = null;

			if (Runtime.CurrentActionCall.Parameters.Contains("relationid"))
			{
				var timesheetQuery = new DBQuery();

				var timesheet = DBDocument.FindOne(
					new DBCallProperties() { DisableEventFiring = true },
					"timesheetentry",
					Runtime.CurrentActionCall.Parameters["relationid"]);

				TimeSpan durationSpan = ((DateTime)timesheet["endtimestamp"]) - ((DateTime)timesheet["starttimestamp"]);

				value = (int)durationSpan.TotalMilliseconds;
			}
			else
			{
				value = 0;
			}

			return null;
		}

		private DateTime ApplyTimeAccuracy(DateTime date, DataTree itemSchema)
		{
			if (itemSchema.Contains("timeaccuracy"))
			{
				int timeAccuracy = (int)itemSchema["timeaccuracy"];

				
				date = new DateTime(
					date.Year, date.Month, date.Day, date.Hour,
					(int)((decimal)date.Minute / (decimal)timeAccuracy) * timeAccuracy,
					0,
					DateTimeKind.Utc);				
			}

			return date;
		}

		public MC2Value timesheetentryuser(DataTree itemSchema)
		{
			if (Runtime.CurrentActionCall.Parameters["relation"] == "parent")
			{
				string parentId = Runtime.CurrentActionCall.Parameters["relationid"];

				DBDocument parentEntry = DBDocument.FindOne("timesheetentry", parentId);

				if (parentEntry != null && parentEntry["user"][DBQuery.Id].Exists)
				{
					DBDocument parentUser = DBDocument.FindOne("user", parentEntry["user"][DBQuery.Id]);
					return parentUser;
				}
			}

			// Use current user in normal case
			return Runtime.SessionManager.CurrentUser;
		}
	}
}