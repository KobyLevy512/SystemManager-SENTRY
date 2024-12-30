using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Xml.Linq;
using SystemManager.Features.DB;

namespace SystemManager.Features
{
    public class Table : IDisposable
    {
        public enum ForEachResult
        {
            Next,
            Break,
            Repeat,
            Restart,
            Delete
        }
        public delegate ForEachResult ForEachEntry(Dictionary<string, object> entry);

        static Dictionary<Type, byte> typesCode = new Dictionary<Type, byte>()
        {
            {typeof(byte), 0 },
            {typeof(sbyte),1 },
            {typeof(short),2 },
            {typeof(ushort),3 },
            {typeof(int),4 },
            {typeof(uint),5 },
            {typeof(long),6 },
            {typeof(ulong),7 },
            {typeof(float),8 },
            {typeof(double),9 },
            {typeof(bool),10 },
            {typeof(char),11 },
            {typeof(string),12 },
        };

        public string Name;
        public string Description;

        int headerSize = 0;
        FileStream stream;

        public Table(string name)
        {
            stream = new FileStream(Properties.Settings.Default.TablesPath + "\\" + name, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, Properties.Settings.Default.TablesBuferSize);
            headerSize = HeaderSize();
        }

        ~Table()
        {
            if(stream != null) stream.Close();
        }

        public int HeaderSize()
        {
            stream.Position = 0;
            BinaryReader br = new BinaryReader(stream);
            br.ReadString();//Name
            br.ReadString();//Desc
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                br.ReadString();
                br.ReadByte();
            }
            return (int)stream.Position;
        }
        public T LastAutoIncrement<T>(string fieldName)
        {
            HeaderSize();
            long start = stream.Position;
            T ret = default;
            ForEach((entry) =>
            {
                ret = (T)entry[fieldName];
                return ForEachResult.Break;
            }, false);
            return ret;
        }
        public Dictionary<string, byte> TableHeader()
        {
            stream.Position = 0;
            BinaryReader br = new BinaryReader(stream);
            Dictionary<string, byte> result = new Dictionary<string, byte>();
            br.ReadString();//Name
            br.ReadString();//Desc
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string name = br.ReadString();
                byte type = br.ReadByte();
                result.Add(name, type);
            }
            return result;
        }

        static void WriteValue(BinaryWriter bw, byte type, object value)
        {
            switch (type)
            {
                case 0:
                    bw.Write((byte)value);
                    break;
                case 1:
                    bw.Write((sbyte)value);
                    break;
                case 2:
                    bw.Write((short)value);
                    break;
                case 3:
                    bw.Write((ushort)value);
                    break;
                case 4:
                    bw.Write((int)value);
                    break;
                case 5:
                    bw.Write((uint)value);
                    break;
                case 6:
                    bw.Write((long)value);
                    break;
                case 7:
                    bw.Write((ulong)value);
                    break;
                case 8:
                    bw.Write((float)value);
                    break;
                case 9:
                    bw.Write((double)value);
                    break;
                case 10:
                    bw.Write((bool)value);
                    break;
                case 11:
                    bw.Write((char)value);
                    break;
                case 12:
                    bw.Write((string)value);
                    break;
            }
        }
        static object ReadValue(BinaryReader br, byte type)
        {
            switch(type)
            {
                case 0:
                    return br.ReadByte();
                case 1:
                    return br.ReadSByte();
                case 2:
                    return br.ReadInt16();
                case 3:
                    return br.ReadUInt16();
                case 4:
                    return br.ReadInt32();
                case 5:
                    return br.ReadUInt32();
                case 6:
                    return br.ReadInt64();
                case 7:
                    return br.ReadUInt64();
                case 8:
                    return br.ReadSingle();
                case 9:
                    return br.ReadDouble();
                case 10:
                    return br.ReadBoolean();
                case 11:
                    return br.ReadChar();
                case 12:
                    return br.ReadString();
            }
            return null;
        }

        public static Table Open(string name)
        {
            return new Table(name);
        }
        public static Table Create(string name, string desc, string[] fieldNames, byte[] types, bool log = true, bool addInfo = true)
        {
            Type[] asType = new Type[types.Length];
            for(int i = 0; i < asType.Length; i++)
            {
                foreach(var key in typesCode.Keys)
                {
                    if (typesCode[key] == types[i])
                    {
                        asType[i] = key;
                        break;
                    }
                }
            }
            return Create(name, desc, fieldNames, asType, log, addInfo);
        }
        public static Table Create(string name, string desc, string[] fieldNames, Type[] types, bool log = true, bool addInfo = true)
        {
            //Check arguments.
            if(fieldNames != null && types != null && fieldNames.Length > 0 && fieldNames.Length != types.Length)
            {
                return null;
            }

            if(addInfo)
            {
                Table tableInfo = Open("tables_info");
                bool exist = false;
                tableInfo.ForEach((entry) =>
                {
                    if ((string)entry["table_name"] == name)
                    {
                        exist = true;
                        return ForEachResult.Break;
                    }
                    return ForEachResult.Next;
                }, false);

                if (exist) return null;

                //If not exist add new entry to the tables_info table.
                List<Dictionary<string, object>> newInfoLine = new List<Dictionary<string, object>>();
                newInfoLine.Add(new Dictionary<string, object>()
                {
                    {"id", tableInfo.LastAutoIncrement<int>("id") + 1 },
                    {"table_name", name },
                    {"table_desc", desc },
                });
                tableInfo.Insert(newInfoLine);
            }
            

            //Add delete field.
            string[] fieldNamesMark;
            Type[] fieldTypes;
            if (fieldNames.Contains("delete_mark"))
            {
                fieldNamesMark = fieldNames;
                fieldTypes = types;
            }
            else
            {
                fieldNamesMark = new string[fieldNames.Length + 1];
                Array.Copy(fieldNames, 0, fieldNamesMark, 1, fieldNames.Length);
                fieldNamesMark[0] = "delete_mark";

                fieldTypes = new Type[fieldNamesMark.Length];
                Array.Copy(types, 0, fieldTypes, 1, types.Length);
                fieldTypes[0] = typeof(bool);
            }


            try
            {
                BinaryWriter bw = new BinaryWriter(File.Create(Properties.Settings.Default.TablesPath + "\\" + name));
                bw.Write(name);
                bw.Write(desc);
                bw.Write(fieldNamesMark.Length);
                for (int i = 0; i < fieldNamesMark.Length; i++)
                {
                    bw.Write(fieldNamesMark[i]);
                    if (!typesCode.ContainsKey(fieldTypes[i]))
                    {
                        bw.Close();
                        File.Delete(Properties.Settings.Default.TablesPath + "\\" + name);
                        return null;
                    }
                    bw.Write(typesCode[fieldTypes[i]]);
                }
                bw.Close();
            }
            catch
            {
                return null;
            }
            if(log)SystemTables.InsertTableAction(name, "Create");
            return new Table(name);
        }

        public static bool Drop(string name, bool log = true)
        {
            try
            {
                File.Delete(Properties.Settings.Default.TablesPath + "\\" + name);
                Table tableInfo = Open("tables_info");
                bool delete = false;
                tableInfo.ForEach(entry =>
                {
                    if (delete) return ForEachResult.Break;

                    if ((string)entry["table_name"] == name)
                    {
                        return ForEachResult.Delete;
                    }
                    return ForEachResult.Next;
                });
                if(log)SystemTables.InsertTableAction(name, "Drop");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ForEach(ForEachEntry func, bool log = true)
        {
            stream.Position = 0;
            BinaryReader br = new BinaryReader(stream);
            string name = br.ReadString();
            var header = TableHeader();
            stream.Position = headerSize;
            br = new BinaryReader(stream);
            while (stream.Position < stream.Length)
            {
                long readSize = stream.Position;
                Dictionary<string, object> entry = new Dictionary<string, object>();
                foreach (var key in header.Keys)
                {
                    entry.Add(key, ReadValue(br, header[key]));
                }
                readSize = stream.Position - readSize;
                switch (func.Invoke(entry))
                {
                    case ForEachResult.Break:
                        stream.Position = stream.Length;
                        break;
                    case ForEachResult.Repeat:
                        stream.Position -= readSize;
                        break;
                    case ForEachResult.Restart:
                        stream.Position = headerSize;
                        break;
                    case ForEachResult.Delete:
                        BinaryWriter bw = new BinaryWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), true);
                        stream.Position -= readSize;
                        bw.Write(true);
                        bw.Close();
                        stream.Position += readSize - 1;
                        break;
                }
            }
            if(log) SystemTables.InsertTableAction(name, "Iterate");
        }

        public List<Dictionary<string, object>> GetAll()
        {
            stream.Position = 0;
            BinaryReader br = new BinaryReader(stream);
            string name = br.ReadString();
            List<Dictionary<string, object>> res = new List<Dictionary<string, object>>();
            ForEach((entry) =>
            {  
                res.Add(entry);
                return ForEachResult.Next;
            });
            SystemTables.InsertTableAction(name, "Get all entries");
            return res;
        }

        public bool Insert(List<Dictionary<string, object>> data, bool log = true)
        {
            stream.Position = 0;
            BinaryReader br = new BinaryReader(stream);
            string name = br.ReadString();
            var header = TableHeader();
            stream.Position = stream.Length;
            BinaryWriter bw = new BinaryWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true), true);
            foreach (var entry in data)
            {
                if(!entry.ContainsKey("delete_mark")) entry.Add("delete_mark", false);
                foreach(var key in header.Keys)
                {
                    if(!entry.ContainsKey(key))
                    {
                        return false;
                    }
                }
                foreach (var key in header.Keys)
                {
                    WriteValue(bw, header[key], entry[key]);
                }
            }
            bw.Close();
            if(log) SystemTables.InsertTableAction(name, "Insert New Data");
            return true;
        }

        public void Clear()
        {
            BinaryReader br = new BinaryReader(stream);
            stream.Position = 0;
            string name = br.ReadString();
            string desc = br.ReadString();
            var header = TableHeader();
            string[] names = new string[header.Keys.Count];
            byte[] types = new byte[names.Length];
            int i = 0;
            foreach (var key in header.Keys) 
            {
                names[i] = key;
                types[i] = header[key];
                i++;
            }
            stream.Close();
            Drop(name, false);
            stream = Create(name, desc, names, types, false, false).stream;
            SystemTables.InsertTableAction(name, "Clear all Data");
        }

        public Table Copy(string newTableName, string newDesc, bool includedDelete = false, bool log = true, bool addInfo = true)
        {
            BinaryReader br = new BinaryReader(stream);
            stream.Position = 0;
            var header = TableHeader();
            string[] names = new string[header.Keys.Count];
            byte[] types = new byte[names.Length];
            int i = 0;
            foreach (var key in header.Keys)
            {
                names[i] = key;
                types[i] = header[key];
                i++;
            }
            Table newTable = Create(newTableName, newDesc, names, types, log, addInfo);
            List<Dictionary<string, object>> insertBuffer = new List<Dictionary<string, object>>();
            ForEach((entry) =>
            {
                if (!includedDelete && (bool)entry["delete_mark"] == true)
                {
                    return ForEachResult.Next;
                }
                insertBuffer.Add(entry);
                if (insertBuffer.Count > 2048)
                {
                    newTable.Insert(insertBuffer);
                    insertBuffer.Clear();
                }
                return ForEachResult.Next;
            }, log);
            if(log)SystemTables.InsertTableAction(newTableName, "Copy Table.");
            return newTable;
        }

        public void UpdateDeleteMark()
        {
            stream.Position = 0;
            BinaryReader br = new BinaryReader(stream);
            string name = br.ReadString();
            string desc = br.ReadString();
            Table cpy = Copy(name + "tmp", desc, false, false, false);
            stream.Close();
            stream = cpy.Copy(name, desc, true, false, false).stream;
            Drop(name + "tmp", false);
        }

        public void Dispose()
        {
            if (stream != null)
                stream.Close();
        }
    }
}
