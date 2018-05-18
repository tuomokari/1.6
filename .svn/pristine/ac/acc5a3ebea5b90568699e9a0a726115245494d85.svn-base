using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using System.Globalization;
using System.Net;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
	public class restapi : MC2Controller
	{
		#region Constants

		public const string SuccessResult = "success;";

		#endregion

		#region Actions

		[GrantAccessToGroup("restapi")]
		[History(false)]
		public AjaxResult doc(string collection, string id)
		{
			try
			{
				return GetDocumentFromDB(collection, id);
			}
			catch (RuntimeException ex)
			{
				return HandleRuntimeException(ex);
			}
			catch (Exception ex)
			{
				return HandleGeneralException(ex);
			}
		}

		[HttpPost]
		[GrantAccessToGroup("restapi")]
		[History(false)]
		public AjaxResult doc(DataTree document, string collection)
		{
			try
			{
				return InsertDocumentToDB(collection, document);
			}
			catch (RuntimeException ex)
			{
				return HandleRuntimeException(ex);
			}
			catch (Exception ex)
			{
				return HandleGeneralException(ex);
			}
		}

		[HttpDelete]
		[History(false)]
		[GrantAccessToGroup("restapi")]
		public AjaxResult doc(string collection, string id, string placeholder1)
		{
			try
			{
				var document = new DBDocument(collection, id);
				document.RemoveFromDatabase();

				// Todo: handle error response

				return new AjaxResult("success", HttpStatusCode.NoContent);
			}
			catch (RuntimeException ex)
			{
				return HandleRuntimeException(ex);
			}
			catch (Exception ex)
			{
				return HandleGeneralException(ex);
			}

		}

		[GrantAccessToGroup("restapi")]
		[History(false)]
		public AjaxResult find(string querycontroller, string query)
		{
			try
			{
				return Find(querycontroller, query);
			}
			catch (RuntimeException ex)
			{
				return HandleRuntimeException(ex);
			}
			catch (Exception ex)
			{
				return HandleGeneralException(ex);
			}
		}

		#endregion

		#region Helpers

		private AjaxResult GetDocumentFromDB(
			string collection,
			string id)
		{
			if (string.IsNullOrEmpty(collection))
				return new AjaxResult("error:Collection not provided when getting document.", HttpStatusCode.NotFound);

			if (string.IsNullOrEmpty(id))
				return new AjaxResult("error:Identifier not provided when getting document.", HttpStatusCode.NotFound);

			if (!Runtime.Schema.First.Contains(collection))
				return new AjaxResult("error:Collection not found", HttpStatusCode.NotFound);

			DataTree schema = Runtime.Schema.First[collection];

			DataTree result = DBDocument.FindOne(collection, id);

			if (result == null)
				return new AjaxResult("error:Document not found", HttpStatusCode.NotFound);

			if (!result.Empty)
				DBQueryHelpers.TidyUpResult(schema, result, Runtime);

			return new AjaxResult(result);
		}

		private AjaxResult Find(string controller, string query)
		{
			if (string.IsNullOrEmpty(query))
				throw new RuntimeException("Query missing from find request.");

			if (string.IsNullOrEmpty(controller))
				throw new RuntimeException("Controller missing from find request.");

			DBQuery dbQuery = GetQuery(controller, query);

			ApplyQueryParameters(dbQuery);

			DataTree result = (DataTree)dbQuery.FindAsync().Result;

			return new AjaxResult(result);
		}

		private void ApplyQueryParameters(DBQuery dtQuery)
		{
			DataTree callParameters = Runtime.CurrentActionCall.Parameters;

			foreach (DataTree parameter in callParameters)
			{
				AddParameterToQuery(parameter.Name, parameter, dtQuery);
			}
		}

		private DBQuery GetQuery(
			string controller,
			string query)
		{
			if (!Runtime.Queries[controller].Contains(query))
				throw (new RuntimeException("Query not found.", new string[] { controller, query }));

			var dbQuery = new DBQuery();

			dbQuery.Merge(Runtime.Queries[controller][query]);

			return dbQuery;
		}

		private void AddParameterToQuery(string name, string parameter, DBQuery query)
		{
			const string ParameterTypeString = "string";
			const string ParameterTypeNumber = "number";
			const string ParameterTypeDateTime = "date";
			const string ParameterTypeObjectId = "objectid";
			const string ParameterTypeNull = "null";
			const string ParameterTypeBool = "bool";

			int split = parameter.IndexOf(":");

			if (split == -1)
				return;

			string type = parameter.Substring(0, split);
			string value = parameter.Substring(split + 1);

			if (type == ParameterTypeString)
			{
				query.AddParameter(name, value);
			}
			else if (type == ParameterTypeNumber)
			{
				double number = 0;
				try
				{
					number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
				}
				catch (Exception)
				{
					throw new RuntimeException("Invalid number value", new string[] { parameter });
				}

				query.AddParameter(name, number);
			}
			else if (type == ParameterTypeDateTime)
			{
				MC2DateTimeValue dateTime = null;

				try
				{
					dateTime = (MC2DateTimeValue)MC2DateTimeValue.TryConvertValueFromString(value);
				}
				catch (Exception)
				{
					throw new RuntimeException("Invalid date time value", new string[] { parameter });
				}

				query.AddParameter(name, dateTime);
			}
			else if (type == ParameterTypeObjectId)
			{
				if (string.IsNullOrEmpty(value))
				{
					// Add empty object id if value is empty
					query.AddParameter(name, new ObjectId());
				}
				else
				{
					ObjectId objectId = new ObjectId();

					try
					{
						objectId = new ObjectId(value);
					}
					catch (Exception)
					{
						throw new RuntimeException("Invalid object id value", new string[] { parameter });
					}

					query.AddParameter(name, objectId);
				}
			}
			else if (type == ParameterTypeNull)
			{
				query.AddNullParameter(name);
			}
			else if (type == ParameterTypeBool)
			{
				if (value == "true")
					query.AddParameter(name, true);
				else if (value == "false")
					query.AddParameter(name, false);
				else
					throw new RuntimeException("Invalid value for boolean parameter", new string[] { name, value });
			}
		}

		private static AjaxResult HandleGeneralException(Exception ex)
		{
			if (ex is AggregateException)
				ex = ((AggregateException)ex).InnerException;

			if (ex is RuntimeException)
				return HandleRuntimeException((RuntimeException)ex);
			else
				return new AjaxResult("error:" + ex.Message, HttpStatusCode.InternalServerError);
		}

		private static AjaxResult HandleRuntimeException(RuntimeException ex)
		{
			string message = "error:" + ex.Message;

			foreach (string param in ex.Parameters)
				message += ", " + param;

			return new AjaxResult(message, HttpStatusCode.InternalServerError);
		}

		private AjaxResult InsertDocumentToDB(string collection, DataTree document)
		{
			if ((bool)document["__datavalid"].GetValueOrDefault(true))
			{
				new DBDocument(document).UpdateDatabase();

				return new AjaxResult(SuccessResult + document[DBQuery.Id]);
			}
			else
			{
				return new AjaxResult(document);
			}
		}

		#endregion
	}
}