using System;

namespace Rover.Models
{
    public class LogEntry
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Product { get; set; }
        public string Layer { get; set; }
        public string Location { get; set; }
        public string Message { get; set; }
        public string Hostname { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Exception { get; set; }
        public int? EllapsedMilliseconds { get; set; }
        public string FormattedMessage { get; set; }
        public string CorrelationId { get; set; }        
        public string AdditionalInfo { get; set; }   
        public string Env { get; set; }
    }
}
