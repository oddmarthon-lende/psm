using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Collections;

namespace PSMonitor.Stores
{
    public class DB : IStore
    {
        public event DataReceivedHandler DataReceived;

        private ConcurrentQueue<Envelope> queue;
        private List<Thread> threads;

        private bool disposed = false;

        public DB()
        {

            queue = new ConcurrentQueue<Envelope>();

            //Test the connection
            using (SqlConnection connection = new SqlConnection(Setup.Get<DB, string>("connectionString")))
            {
                CheckConnection(connection);
            }

            threads = new List<Thread>();

            threads.Add(new Thread(Dispatch));
            threads.Add(new Thread(Cleanup));

            threads.ForEach(thread =>
            {
                thread.Name = String.Format("DB Store Thread #{0}", threads.IndexOf(thread));
                thread.Start(this);
            });

        }

        public long Delete(string path)
        {
            return Delete_(path, null, null);
        }
        
        public long Delete(string path, DateTime start, DateTime end)
        {
            return Delete_(path, start, end);
        }

        private long Delete_(string path, DateTime? start, DateTime? end)
        {

            using (SqlConnection connection = new SqlConnection(Setup.Get<DB, string>("connectionString")))
            {

                CheckConnection(connection);

                Path p = Path.Extract(path);

                using (SqlCommand command = connection.CreateCommand())
                {

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "usp_delete";

                    p.ToCommandParameters(command);

                    command.Parameters.Add(new SqlParameter("starttime", SqlDbType.DateTime)
                    {

                        ParameterName = "@Start",
                        Direction = ParameterDirection.Input,
                        SqlValue = start

                    });

                    command.Parameters.Add(new SqlParameter("endtime", SqlDbType.DateTime)
                    {

                        ParameterName = "@End",
                        Direction = ParameterDirection.Input,
                        SqlValue = end

                    });

                    command.Parameters.Add(new SqlParameter("timespan", SqlDbType.BigInt)
                    {

                        ParameterName = "@Span",
                        Direction = ParameterDirection.Input,
                        SqlValue = null

                    });

                    return command.ExecuteNonQuery();
                }

            }

        }

        public void Dispose()
        {

            disposed = true;
            threads.ForEach(thread =>
            {
                thread.Interrupt();
                Debug.WriteLine("DB Store : Waiting for threads to exit");
                thread.Join();
            });

        }

        public struct Path
        {

            public string Namespace { get; private set; }
            public string Key { get; private set; }

            public static Path Extract(string path)
            {
                string[] result; string key;

                result = path.Trim(' ', '\t').Split('.');

                if (result.Length < 2)
                    throw new Exception("The path given is too short.");

                key = result[result.GetUpperBound(0)];

                Array.Resize<string>(ref result, result.Length - 1);

                return new Path { Namespace = String.Join(".", result), Key = key };

            }

            public void ToCommandParameters(SqlCommand command)
            {

                command.Parameters.Add(new SqlParameter("namespace", SqlDbType.VarChar)
                {

                    ParameterName = "@Namespace",
                    Direction = ParameterDirection.Input,
                    SqlDbType = SqlDbType.VarChar,
                    SqlValue = this.Namespace


                });

                command.Parameters.Add(new SqlParameter("key", SqlDbType.VarChar)
                {

                    ParameterName = "@Key",
                    Direction = ParameterDirection.Input,
                    SqlDbType = SqlDbType.VarChar,
                    SqlValue = this.Key

                });

            }

            public Entry ToEntry(IDataRecord record)
            {

                int i = record.GetOrdinal("Value");
                object v = record.GetValue(i);

                return new Entry
                {

                    Key = this.Key,
                    Value = v,
                    Timestamp = record.GetDateTime(record.GetOrdinal("Timestamp")),
                    Type = v.GetType()

                };

            }

        }

        public Entry Get(string path)
        {

            SqlConnection connection = new SqlConnection(Setup.Get<DB, string>("connectionString"));

            CheckConnection(connection);

            Path p = Path.Extract(path);

            SqlCommand command = connection.CreateCommand();


            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "usp_get_one";

            p.ToCommandParameters(command);

            foreach (Entry entry in new Entries(p, connection, command))
            {
                return entry;
            }

            throw new KeyNotFoundException("Could not find the specified key or path");

        }

        private class Entries : IEnumerable<Entry>, IEnumerator<Entry>, IDisposable
        {

            private SqlDataReader reader;
            private Path path;
            private SqlConnection connection;
            private SqlCommand command;

            public Entries(Path p, SqlConnection connection, SqlCommand command)
            {
                path = p;
                reader = command.ExecuteReader();

                this.command = command;
                this.connection = connection;
            }

            public IEnumerator<Entry> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            public Entry Current
            {

                get
                {
                    return path.ToEntry(reader);
                }

            }

            object IEnumerator.Current
            {
                get
                {
                    return path.ToEntry(reader);
                }
            }

            public void Dispose()
            {
                reader.Close();
                connection.Close();
                command.Dispose();
            }

            public bool MoveNext()
            {
                bool done = reader.IsClosed || !reader.Read();
                
                return !done;
            }

            public void Reset()
            {
                throw new NotImplementedException("The underlying SqlDataReader is forward only");
            }

        }

        private IEnumerable<Entry> Get_(string path, object start, object end)
        {
            SqlConnection connection = new SqlConnection(Setup.Get<DB, string>("connectionString"));

            CheckConnection(connection);

            Path p = Path.Extract(path);

            SqlCommand command = connection.CreateCommand();

            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "usp_get_many";

            p.ToCommandParameters(command);

            command.Parameters.Add(new SqlParameter("starttime", SqlDbType.Variant)
            {

                ParameterName = "@Start",
                Direction = ParameterDirection.Input,
                SqlValue = start

            });

            command.Parameters.Add(new SqlParameter("endtime", SqlDbType.Variant)
            {

                ParameterName = "@End",
                Direction = ParameterDirection.Input,
                SqlValue = end

            });

            return new Entries(p, connection, command);



        }

        public IEnumerable<Entry> Get(string path, DateTime start, DateTime end)
        {
            return Get_(path, start, end);
        }

        public IEnumerable<Entry> Get(string path, long start, long end)
        {
            return Get_(path, start, end);
        }

        private static DateTime cleanup_Starttime = new DateTime(1970, 1, 1, 0, 0, 0);

        private static void Cleanup(object ctx)
        {

            int maxAge = Setup.Get<DB, int>("maxAge");

            DB context = (DB)ctx;

            using (SqlConnection connection = new SqlConnection(Setup.Get<DB, string>("connectionString")))
            {

                while (!context.disposed && maxAge > 0)
                {

                    try
                    {

                        Thread.Sleep(1000 * 60 * 60 * 24);

                        if (CheckConnection(connection))
                        {

                            using (SqlCommand command = connection.CreateCommand())
                            {

                                DateTime d = DateTime.Now.Subtract(new TimeSpan(maxAge, 0, 0, 0));

                                command.CommandType = CommandType.StoredProcedure;
                                command.CommandText = "usp_clean";

                                command.Parameters.Add(new SqlParameter("before", SqlDbType.DateTime)
                                {

                                    ParameterName = "@Before",
                                    Direction = ParameterDirection.Input,
                                    SqlValue = d

                                });

                                int count = command.ExecuteNonQuery();

                                if (count > 0)
                                {
                                    Logger.info(String.Format("Deleted {0] rows that was older than {1} days. ({2})", count, maxAge, d));
                                }

                            }

                        }
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Debug.WriteLine(e.ToString());
                    }
                    catch (Exception error)
                    {
                        Logger.error(error);
                    }

                }

            }

        }

        private int sleepTime = 1000;

        private static void Dispatch(object ctx)
        {

            DB context = (DB)ctx;

            using (SqlConnection connection = new SqlConnection(Setup.Get<DB, string>("connectionString")))
            {

                while (!context.disposed)
                {

                    try
                    {
                        Thread.Sleep(context.sleepTime);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Debug.WriteLine(e.ToString());
                    }

                    if (!context.queue.IsEmpty)
                    {
                        Envelope envelope;

                        try
                        {

                            CheckConnection(connection);

                            int count = context.queue.Count;

                            using (SqlCommand command = connection.CreateCommand())
                            {

                                command.CommandType = CommandType.StoredProcedure;
                                command.CommandText = "usp_insert_value";

                                for (int i = 0; i < count; i++)
                                {

                                    while (!context.queue.TryDequeue(out envelope));

                                    foreach (Entry entry in envelope.Entries)
                                    {

                                        command.Parameters.Clear();

                                        command.Parameters.Add(new SqlParameter("namespace", SqlDbType.VarChar)
                                        {
                                            ParameterName = "@Namespace",
                                            Direction = ParameterDirection.Input,
                                            SqlDbType = SqlDbType.VarChar,
                                            SqlValue = envelope.Path

                                        });

                                        command.Parameters.Add(new SqlParameter("key", SqlDbType.VarChar)
                                        {
                                            ParameterName = "@Key",
                                            Direction = ParameterDirection.Input,
                                            SqlValue = entry.Key

                                        });

                                        command.Parameters.Add(new SqlParameter("value", SqlDbType.Variant)
                                        {
                                            ParameterName = "@Value",
                                            Direction = ParameterDirection.Input,
                                            SqlValue = entry.Value

                                        });

                                        command.Parameters.Add(new SqlParameter("timestamp", SqlDbType.DateTime)
                                        {

                                            ParameterName = "@Timestamp",
                                            Direction = ParameterDirection.Input,
                                            SqlValue = entry.Timestamp

                                        });

                                        try {

                                            command.ExecuteNonQuery();

                                        }
                                        catch (Exception error)
                                        {
                                            Logger.error(error);
                                            continue;
                                        }

                                    }

                                }

                            }

                            context.sleepTime = 1000;

                        }
                        catch (Exception e)
                        {
                            Logger.error(e);
                            context.sleepTime = Math.Max(context.sleepTime * 2, 1000 * 60 * 60 * 24);

                        }
                    }
                }
            }
        }

        public void Put(Envelope data)
        {

            if (!disposed)
                queue.Enqueue(data);

        }

        public Key[] GetKeys(string path)
        {

            using (SqlConnection connection = new SqlConnection(Setup.Get<DB, string>("connectionString")))
            {

                CheckConnection(connection);

                List<Key> keys = new List<Key>();
                string[] p = path.Trim(' ', '.').Split('.');
                long? id = null;

                using (SqlCommand command = connection.CreateCommand())
                {

                    command.CommandType = CommandType.Text;
                    command.CommandText = "select * from [namespaces];";

                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        while (reader.Read())
                        {


                            string ns = reader.GetString(reader.GetOrdinal("Namespace"));
                            string[] n = ns.Split('.');

                            if (path == ns)
                            {
                                id = reader.GetInt64(reader.GetOrdinal("Id"));
                            }

                            if (path.Length == 0)
                            {

                                if (keys.Find(key => { return key.Name == n[0]; }) == null)
                                {

                                    keys.Add(new Key(n[0], null));

                                }

                                continue;

                            }
                            else if (p.Length >= n.Length)
                                continue;

                            for (int i = 0; i < p.Length; i++)
                            {

                                if (p[i] != n[i])
                                    break;
                                else if (i == (p.Length - 1) && p[p.Length - 1] == n[i] && (i + 1) < n.Length)
                                {

                                    if (keys.Find(key => { return key.Name == n[i + 1]; }) != null)
                                        continue;

                                    keys.Add(new Key(n[i + 1], null));

                                    break;

                                }

                            }


                        }

                    }

                }

                if (id != null)
                {

                    using (SqlCommand command = connection.CreateCommand())
                    {

                        command.CommandType = CommandType.Text;
                        command.CommandText = String.Format("select [Name], [Type] from [keys] where [NamespaceId] = {0};", id);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                            while (reader.Read())
                            {
                                keys.Add(new Key(
                                    reader.GetString(0),
                                    GetType(reader.GetString(1))
                                ));

                            }

                        }

                    }

                }

                return keys.ToArray();
            }
        }

        private static bool CheckConnection(SqlConnection connection)
        {

            switch (connection.State)
            {

                case ConnectionState.Broken:
                    connection.Close();
                    connection.Open();
                    break;
                case ConnectionState.Closed:
                    connection.Open();
                    break;
            }

            return connection.State == ConnectionState.Open;

        }

        //
        // From https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
        //
        private static Type GetType(string sql_type)
        {

            switch (sql_type)
            {
                case "bigint":
                    return typeof(Int64);
                case "binary":
                    return typeof(Byte[]);
                case "bit":
                    return typeof(Boolean);
                case "char":
                    return typeof(String);
                case "date":
                    return typeof(DateTime);
                case "datetime":
                    return typeof(DateTime);
                case "datetime2":
                    return typeof(DateTime);
                case "datetimeoffset":
                    return typeof(DateTimeOffset);
                case "decimal":
                    return typeof(Decimal);
                case "float":
                    return typeof(Double);
                case "image":
                    return typeof(Byte[]);
                case "int":
                    return typeof(Int32);
                case "money":
                    return typeof(Decimal);
                case "nchar":
                    return typeof(String);
                case "ntext":
                    return typeof(String);
                case "numeric":
                    return typeof(Decimal);
                case "nvarchar":
                    return typeof(String);
                case "real":
                    return typeof(Single);
                case "rowversion":
                    return typeof(Byte[]);
                case "smalldatetime":
                    return typeof(DateTime);
                case "smallint":
                    return typeof(Int16);
                case "smallmoney":
                    return typeof(Decimal);
                case "sql_variant":
                    return typeof(Object);
                case "text":
                    return typeof(String);
                case "time":
                    return typeof(TimeSpan);
                case "timestamp":
                    return typeof(Byte[]);
                case "tinyint":
                    return typeof(Byte);
                case "uniqueidentifier":
                    return typeof(Guid);
                case "varbinary":
                    return typeof(Byte[]);
                case "varchar":
                    return typeof(String);
                default:
                    throw new Exception(String.Format("Unknown type: {0}", sql_type));
            }
        }

    }


}
