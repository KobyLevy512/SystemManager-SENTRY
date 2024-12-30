using System;
using System.Collections.Generic;
using System.IO;

namespace SystemManager.Features.DB
{
    internal static class SystemTables
    {
        public static void InsertTableAction(string tableName, string action)
        {
            Table t = Table.Open("table_actions");
            List<Dictionary<string, object>> entry = new List<Dictionary<string, object>>();
            entry.Add(new Dictionary<string, object>()
            {
                { "id", t.LastAutoIncrement<ulong>("id") + 1 },
                { "table_name", tableName },
                { "action", action },
                { "date",  DateTime.Now.Month + "/" + DateTime.Now.Day + "/" + DateTime.Now.Year},
                { "time", DateTime.Now.Hour + ":" + DateTime.Now.Minute }
            });
            t.Insert(entry, false);
            t.Dispose();
        }

        public static int TablesAmount()
        {
            Table t = Table.Open("tables_info");
            int tablesAmount = 3;//jobs, jobs_log, table_actions
            t.ForEach(x =>
            {
                tablesAmount++;
                return Table.ForEachResult.Next;
            });
            t.Dispose();
            return tablesAmount;
        }

        public static (int reads, int writes) TablesAction(string date)
        {
            (int reads, int writes) res = (0, 0);
            Table t2 = Table.Open("table_actions");
            t2.ForEach(x =>
            {
                if ((string)x["date"] == date)
                {
                    if ((string)x["action"] == "Insert New Data")
                    {
                        res.writes++;
                    }
                    else if ((string)x["action"] == "Iterate")
                    {
                        res.reads++;
                    }
                }
                return Table.ForEachResult.Next;
            });
            t2.Dispose();
            return res;
        }

        public static ulong DbSize()
        {
            ulong res = 0;
            var files = Directory.EnumerateFiles(Properties.Settings.Default.TablesPath);
            foreach (var file in files)
            {
                var stream = File.Open(file, FileMode.Open);
                res += (ulong)stream.Length;
                stream.Close();
            }
            return res;
        }
    }
}
