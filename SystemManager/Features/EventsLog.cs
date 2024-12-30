
using System.Diagnostics;
using System;
using System.Collections.Generic;

namespace SystemManager.Features
{
    internal class EventsLog
    {
        public class Event
        {
            public DateTime TimeWritten, TimeGenerated;
            public string Source;
            public string Category;
            public string MachineName;
            public string Message;
            public string UserName;
            public EventLogEntryType Type;

            public Event(DateTime timeWritten, DateTime timeGenerated, string source, string category, string machineName, string message, string userName, EventLogEntryType type)
            {
                this.UserName = userName;
                this.TimeWritten = timeWritten;
                this.TimeGenerated = timeGenerated;
                this.Source = source;
                this.Category = category;
                this.MachineName = machineName;
                this.Message = message;
                this.Type = type;
            }
        }

        EventLog app;
        EventLog sys;

        public EventsLog() 
        {
            app = new EventLog("Application");
            sys = new EventLog("System");
        }

        public List<Event> ReadApplicationLogs()
        {
            List<Event> events = new List<Event>();
            foreach (EventLogEntry entry in app.Entries)
            {
                events.Add(new Event(entry.TimeWritten, entry.TimeGenerated, entry.Source, entry.Category, entry.MachineName, entry.Message, entry.UserName, entry.EntryType));
            }
            return events;
        }
        public List<Event> ReadSystemLogs()
        {
            List<Event> events = new List<Event>();
            foreach (EventLogEntry entry in sys.Entries)
            {
                events.Add(new Event(entry.TimeWritten, entry.TimeGenerated, entry.Source, entry.Category, entry.MachineName, entry.Message, entry.UserName, entry.EntryType));
            }
            return events;
        }
    }
}
