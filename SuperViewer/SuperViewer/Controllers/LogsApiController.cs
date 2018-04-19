using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Rover.Models;

namespace Rover.Controllers
{
    [Produces("application/json")]
    [Route("api/LogsApi")]
    public class LogsApiController : Controller
    {
        private List<LogEnvironment> _environments { get; }
        public LogsApiController(IOptions<List<LogEnvironment>> environments)
        {
            _environments = environments.Value;
        }

        [HttpGet]
        [Route("Environments")]
        public List<string> Environments()
        {
            return _environments.Select(a => a.Name).OrderBy(a=> a).ToList();
        }

        [HttpGet]
        [Route("Options")]
        public List<string> Options(string env, string fieldName)
        {
            var environment = _environments.FirstOrDefault(a => a.Name == env);
            if (environment == null)
                throw new Exception($"No known environment for passed env code of [{env}]!");

            var results = new List<string> {"ALL"};
            string sql = "";
            switch (fieldName)
            {
                case "Hostname":
                    sql = "SELECT DISTINCT ISNULL(Hostname, '') as Hostname FROM ErrorLogs WHERE Timestamp > DATEADD(day, -300, GETDATE()) ORDER BY Hostname";
                    break;
                case "Layer":
                    sql = "SELECT DISTINCT Layer FROM ErrorLogs WHERE Timestamp > DATEADD(day, -300, GETDATE()) ORDER BY Layer";
                    break;
                case "UserName":
                    sql = "SELECT DISTINCT UserName FROM ErrorLogs WHERE Timestamp > DATEADD(day, -300, GETDATE()) ORDER BY UserName";
                    break;
                default:
                    throw new ArgumentException($"Unrecognized option field [{fieldName}]!");

            }
            using (var db = new SqlConnection(environment.ConnectionStr))
            {
                results.AddRange(db.Query<string>(sql).ToList());
            }

            return results;
        }

        [Route("Entries")]
        public List<LogEntry> GetLogEntries(string env, string machineList, string layerList, string userList,
            DateTime beginDate, DateTime? endDate, string like, string notLike, int limitTo,
            int id, bool includeInformational, bool informationalOnly, string correlationId)
        {
            var environment = _environments.FirstOrDefault(a => a.Name == env);
            if (environment == null)
                throw new Exception($"No known environment for passed env code of [{env}]!");

            var where = GetWhereCondition(machineList, layerList, userList, beginDate,
                                    endDate, like, notLike, id, correlationId);

            using (var db = new SqlConnection(environment.ConnectionStr))
            {
                var logEntries = db.Query<LogEntry>(@"
                                    SELECT TOP " +
                                            (limitTo > 1000 ? "1000" : limitTo.ToString()) + @"   
                                        Id, Timestamp, Product, Layer, Location, 
                                        Message, Hostname, UserId, UserName, 
                                        ElapsedMilliseconds, CorrelationId, ISNULL(Exception, CustomException) as Exception, 
                                        AdditionalInfo 
                                    FROM ErrorLogs " +
                                    where + @"
                                    ORDER BY Id DESC").ToList();

                foreach (var result in logEntries)
                {
                    Regex htmlRx;
                    result.Env = env;
                    if (result.Exception != null)
                    {
                        htmlRx = new Regex("<");
                        result.Exception = htmlRx.Replace(result.Exception, "&lt;");
                        htmlRx = new Regex(">");
                        result.Exception = htmlRx.Replace(result.Exception, "&gt;");
                        htmlRx = new Regex("\\n");
                        result.Exception = htmlRx.Replace(result.Exception, "<br/>");
                        var tabRx = new Regex("\\t");
                        result.Exception = tabRx.Replace(result.Exception, "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                        var messageRx = new Regex("(.Message:)(.*?)<br/>");
                        result.Exception = messageRx.Replace(result.Exception, @"$1 <span style=""background-color:yellow;"">$2</span><br/>");
                    }

                    if (result.AdditionalInfo != null)
                    {
                        htmlRx = new Regex("\\(\"");
                        result.AdditionalInfo = htmlRx.Replace(result.AdditionalInfo, "<br/>(\"");
                    }
                }

                return logEntries;
            }
        }

        private string GetWhereCondition(string machineList, string layerList, string userList,
            DateTime beginDate, DateTime? endDate, string like, string notLike, int logId, string correlationId)
        {
            if (logId > 0)
                return $"WHERE Id = {logId} ";

            if (!string.IsNullOrEmpty(correlationId))
                return $"WHERE CorrelationId = '{correlationId}'";

            var where = @"
                    WHERE Timestamp > '" + beginDate.ToString("G") + "' ";

            if (endDate != null && endDate.Value != DateTime.MinValue)
                where += $" AND Timestamp <= {endDate.Value.ToString("G")}";

            if (machineList != "ALL")
            {
                var toAdd = "AND Hostname IN (";
                foreach (var machine in machineList.Split(','))
                {
                    if (toAdd != "AND Hostname IN (")
                        toAdd += ",";
                    toAdd += $"'{machine}'";
                }
                where += toAdd + ") ";
            }
            if (layerList != "ALL")
            {
                var toAdd = "AND Layer IN (";
                foreach (var layer in layerList.Split(','))
                {
                    if (toAdd != "AND Layer IN (")
                        toAdd += ",";
                    toAdd += $"'{layer}'";
                }
                where += toAdd + ") ";
            }

            if (userList != "ALL")
            {
                var toAdd = "AND Username IN (";
                foreach (var user in userList.Split(','))
                {
                    if (toAdd != "AND Username IN (")
                        toAdd += ",";
                    toAdd += $"'{user}'";
                }
                where += toAdd + ") ";
            }

            if (!string.IsNullOrEmpty(like))
                where += $" AND (Message LIKE '%{like}%' " +
                    $"OR Exception LIKE '%{like}%' " +
                    $"OR CustomException LIKE '%{like}%') ";

            if (!string.IsNullOrEmpty(notLike))
                where += $" AND (Message NOT LIKE '%{notLike}%' " +
                    $"OR Exception NOT LIKE '%{notLike}%' " +
                    $"OR CustomException NOT LIKE '%{notLike}%') ";

            return where;
        }
    }
}