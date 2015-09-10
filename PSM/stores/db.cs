using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Linq;
using System.ComponentModel;

namespace PSMonitor.Stores
{

    /// <summary>
    /// Database Store
    /// </summary>
    public class DB : Store
    {

        #region Fields and Properties

        protected class Configuration
        {
            [Description("The connection string that is used to connect to the database")]
            public string ConnectionString
            {
                get
                {
                    return Setup.Get<DB, string>("connectionString");
                }

                set
                {
                    //Setup.Set<DB, string, string>("connectionString", value);
                }
            }

            [Description("Entries older than the specified amount in days are deleted")]
            public int MaxAge
            {
                get
                {
                    return Setup.Get<DB, int>("maxAge");
                }

                set
                {

                }
            }

        }

        /// <summary>
        /// A class that implements the <see cref="IEnumerable{Entry}"/> and <see cref="IEnumerator{Entry}"/> interfaces and provides access to the results from the server through these interfaces.
        /// </summary>
        protected class Entries : IEnumerable<Entry>, IEnumerator<Entry>, IDisposable
        {

            /// <summary>
            /// Reference to the <see cref="SqlDataReader" /> that is used to read the data from the database.
            /// </summary>
            private SqlDataReader reader;

            /// <summary>
            /// The path that was used to obtain the results from the database.
            /// </summary>
            private Path path;

            /// <summary>
            /// The connection used to connect to the database.
            /// </summary>
            private SqlConnection connection;

            /// <summary>
            /// The command that will be executed.
            /// </summary>
            private SqlCommand command;


            /// <summary>
            /// The constructor
            /// Some parameters are there, just so they will be disposed of properly when this instance is returned from a function to the outside world.
            /// </summary>
            /// <param name="p">The path that was used to obtain the results from the database</param>
            /// <param name="connection">The connection used to connect to the database.</param>
            /// <param name="command">The command that will be executed.</param>
            public Entries(Path p, SqlConnection connection, SqlCommand command)
            {
                
                path = p;
                reader = command.ExecuteReader();

                this.command = command;
                this.connection = connection;
            }

            /// <summary>
            /// <see cref="IEnumerable{Entry}.GetEnumerator"/>
            /// </summary>
            public IEnumerator<Entry> GetEnumerator()
            {
                return this;
            }

            /// <summary>
            /// <see cref="IEnumerable{Entry}.GetEnumerator"/>
            /// </summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            /// <summary>
            /// <see cref="IEnumerator{Entry}.Current"/>
            /// </summary>
            public Entry Current
            {

                get
                {
                    return path.ToEntry(reader);
                }

            }

            /// <summary>
            /// <see cref="IEnumerator{Entry}.Current"/>
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    return path.ToEntry(reader);
                }
            }

            /// <summary>
            /// Releases any resources that is used or was created by this object.
            /// </summary>
            public void Dispose()
            {
                reader.Close();
                connection.Close();
                command.Dispose();
            }

            /// <summary>
            /// <see cref="IEnumerator.MoveNext"/>
            /// </summary>
            public bool MoveNext()
            {
                bool done = reader.IsClosed || !reader.Read();

                return !done;
            }            

            /// <summary>
            /// <see cref="IEnumerator.Reset" />
            /// </summary>
            public void Reset()
            {
                throw new NotImplementedException("The underlying SqlDataReader is forward only");
            }

        }
              
        /// <summary>
        /// Extends <see cref="Store.Path"/>
        /// </summary>
        protected new class Path : Store.Path
        {

            /// <summary>
            /// <see cref="Store.Path.Path(PSMonitor.Path)"/>
            /// </summary>
            public Path(PSMonitor.Path path) : base(path) { }

            /// <summary>
            /// Converts a data record to <see cref="Entry" />
            /// </summary>
            /// <param name="record">The record to convert</param>
            /// <returns>The converted data as an <see cref="Entry"/></returns>
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

            /// <summary>
            /// <see cref="PSMonitor.Path.Extract(string)"/>
            /// </summary>
            public static new Path Extract(string path)
            {
                return new Path(PSMonitor.Path.Extract(path));
            }

            /// <summary>
            /// Adds the <see cref="PSMonitor.Path.Namespace"/> and <see cref="PSMonitor.Path.Key"/> as parameters to the provided <see cref="SqlCommand"/>.
            /// </summary>
            /// <param name="command">The command to add the parameters to.</param>
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
        }

        /// <summary>
        /// Holds the envelopes that are waiting to be dispatched to the database.
        /// </summary>
        protected ConcurrentQueue<Envelope> queue;

        /// <summary>
        /// Holds the threads created by this instance
        /// </summary>
        protected IReadOnlyCollection<Thread> _threads;
        

        /// <summary>
        /// Holds a time when cleanup was executed.
        /// </summary>
        private static DateTime cleanup_Starttime = new DateTime(1970, 1, 1, 0, 0, 0);

        /// <summary>
        /// The number of milliseconds to wait
        /// </summary>
        private int sleepTime = 1000;

        /// <summary>
        /// If <c>true</c>, this object has been disposed.
        /// </summary>
        private bool _disposed = false;

        private Guid _id = Guid.NewGuid();

        #endregion

        /// <summary>
        /// The constructor
        /// </summary>
        public DB()
        {

            Options = new Configuration();

            queue = new ConcurrentQueue<Envelope>();

            //Test the connection
            using (SqlConnection connection = new SqlConnection(((Configuration)Options).ConnectionString))
            {
                VerifyConnection(connection);
            }

            List<Thread> threads = new List<Thread>();

            threads.Add(new Thread(Dispatch));
            threads.Add(new Thread(Cleanup));
            threads.Add(new Thread(Receive));

            int index = 0;
            foreach (Thread thread in threads)
            {
                thread.Name = String.Format("DB Store [{0}] Thread #{1}", _id, index++);
                thread.Start(this);
            }

            _threads = threads;

        }

        /// <summary>
        /// <see cref="IStore.Delete(string)"/>
        /// </summary>
        public override long Delete(string path)
        {
            return Delete_(path, null, null);
        }

        /// <summary>
        /// <see cref="IStore.Delete(string, DateTime, DateTime)"/>
        /// </summary>
        public override long Delete(string path, DateTime start, DateTime end)
        {
            return Delete_(path, start, end);
        }

        /// <summary>
        /// Handles both methods defined by the <see cref="IStore"/> interface.
        /// <see cref="IStore.Delete(string)"/>
        /// <see cref="IStore.Delete(string, DateTime, DateTime)"/>
        /// </summary>
        protected long Delete_(string path, DateTime? start, DateTime? end)
        {

            using (SqlConnection connection = new SqlConnection(((Configuration)Options).ConnectionString))
            {

                VerifyConnection(connection);

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

        /// <summary>
        /// Releases any resources created by this object
        /// </summary>
        public override void Dispose()
        {

            _disposed = true;

            foreach(Thread thread in _threads)
            {
                thread.Interrupt();
                Debug.WriteLine("DB Store : Waiting for threads to exit");
                thread.Join();
            }

            base.Dispose();

        }

        /// <summary>
        /// <see cref="IStore.Get(string)"/>
        /// </summary>
        public override Entry Get(string path)
        {

            SqlConnection connection = new SqlConnection( ((Configuration)Options).ConnectionString);

            VerifyConnection(connection);

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

        /// <summary>
        /// Handles both methods defined by the <see cref="IStore"/> interface.
        /// <see cref="IStore.Get(string)"/>
        /// <see cref="IStore.Get(string, DateTime, DateTime)"/>
        /// </summary>
        protected virtual IEnumerable<Entry> Get_(string path, object start, object end)
        {

            SqlConnection connection = new SqlConnection( ((Configuration)Options).ConnectionString );

            VerifyConnection(connection);

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

        /// <summary>
        /// <see cref="IStore.Get(string, DateTime, DateTime)"/>
        /// </summary>
        public override IEnumerable<Entry> Get(string path, DateTime start, DateTime end)
        {
            return Get_(path, start, end);
        }

        /// <summary>
        /// <see cref="IStore.Get(string, long, long)"/>
        /// </summary>
        public override IEnumerable<Entry> Get(string path, long start, long end)
        {
            return Get_(path, start, end);
        }

        
        /// <summary>
        /// <see cref="IStore.Put(Envelope)"/>
        /// </summary>
        public override void Put(Envelope data)
        {

            if (!_disposed)
                queue.Enqueue(data);

        }

        /// <summary>
        /// <see cref="IStore.GetKeys(string)"/>
        /// </summary>
        public override Key[] GetKeys(string path)
        {

            using (SqlConnection connection = new SqlConnection(((Configuration)Options).ConnectionString))
            {

                VerifyConnection(connection);

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

        /// <summary>
        /// Entry point for the thread that will poll the database for new data and transfer it to the listeners.
        /// </summary>
        /// <param name="ctx">The <see cref="DB"/> instance that the thread belongs to.</param>
        protected static void Receive(object ctx)
        {

            DB context = (DB)ctx;

            while (!context._disposed)
            {

                try
                {

                    foreach (KeyValuePair<object, ConcurrentBag<Store.Path>> pair in Receivers)
                    {

                        ConcurrentBag<Store.Path> paths = pair.Value;

                        foreach (Store.Path path in paths)
                        {

                            using (SqlConnection connection = new SqlConnection(((Configuration)context.Options).ConnectionString))
                            {

                                VerifyConnection(connection);

                                using (SqlCommand command = connection.CreateCommand())
                                {

                                    string column = null;
                                    Type t = path.StartIndex.GetType();

                                    switch (t.Name)
                                    {

                                        case "DateTime":
                                            column = "Timestamp";
                                            break;

                                        default:
                                            column = "Id";
                                            break;

                                    }

                                    command.CommandType = CommandType.Text;
                                    command.CommandText = String.Format("select [Value], [Timestamp] from [Data] where [{0}] > @StartIndex and [Namespace] = @Namespace and [Key] = @Key", column);

                                    command.Parameters.Add(new SqlParameter("@StartIndex", GetType(path.StartIndex.GetType()))
                                    {
                                        Direction = ParameterDirection.Input,
                                        SqlValue = path.StartIndex
                                    });

                                    command.Parameters.Add(new SqlParameter("@Namespace", SqlDbType.VarChar)
                                    {
                                        Direction = ParameterDirection.Input,
                                        SqlValue = path.Namespace
                                    });

                                    command.Parameters.Add(new SqlParameter("@Key", SqlDbType.VarChar)
                                    {
                                        Direction = ParameterDirection.Input,
                                        SqlValue = path.Key
                                    });

                                    Entry[] entries = new Entries(new Path(path), connection, command).ToArray();

                                    if (entries.Length > 0)
                                    {

                                        path.StartIndex = path.Handler(new Envelope()
                                        {
                                            Path = path.Namespace,
                                            Entries = entries,
                                            Timestamp = DateTime.Now
                                        });

                                        if (path.StartIndex == null)
                                            throw new NullReferenceException("The delegate returned null. Delegate must return a valid next StartIndex");

                                    }

                                }
                            }
                        }
                    }

                    Thread.Sleep(1000);

                }
                catch (InvalidOperationException e)
                {
                    throw e;
                }
                catch (NullReferenceException e)
                {
                    throw e;
                }
                catch (ThreadInterruptedException e)
                {
                    Debug.WriteLine(e.ToString());
                }
                catch (Exception error)
                {
                    Logger.Error(error);
                }
                
            }
        }

        /// <summary>
        /// The entry point for the thread that will do the cleanup in the database.
        /// Entries that are older that the <see cref="Setup"/>.MaxAge property will be deleted.
        /// </summary>
        /// <param name="ctx">The <see cref="DB"/> instance that the thread belongs to.</param>
        protected static void Cleanup(object ctx)
        {

            
            DB context = (DB)ctx;
            int maxAge = ((Configuration)context.Options).MaxAge;

            using (SqlConnection connection = new SqlConnection( ((Configuration)context.Options).ConnectionString))
            {

                while (!context._disposed && maxAge > 0)
                {

                    try
                    {

                        Thread.Sleep(1000 * 60 * 60 * 24);

                        if (VerifyConnection(connection))
                        {

                            using (SqlCommand command = connection.CreateCommand())
                            {

                                DateTime d = DateTime.Now.Subtract(new TimeSpan(maxAge, 0, 0, 0));

                                command.CommandType = CommandType.StoredProcedure;
                                command.CommandText = "usp_clean";

                                command.Parameters.Add(new SqlParameter("@Before", SqlDbType.DateTime)
                                {

                                    Direction = ParameterDirection.Input,
                                    SqlValue = d

                                });

                                int count = command.ExecuteNonQuery();

                                if (count > 0)
                                {
                                    Logger.Info(String.Format("Deleted {0] rows that was older than {1} days. ({2})", count, maxAge, d));
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
                        Logger.Error(error);
                    }

                }

            }

        }

        /// <summary>
        /// The entry point for the thread that will do the task of saving the data that has been added into the database.
        /// <see cref="Put(Envelope)"/>
        /// </summary>
        /// <param name="ctx">The <see cref="DB"/> instance that the thread belongs to.</param>
        protected static void Dispatch(object ctx)
        {

            DB context = (DB)ctx;

            using (SqlConnection connection = new SqlConnection( ((Configuration)context.Options).ConnectionString))
            {

                while (!context._disposed)
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

                            VerifyConnection(connection);

                            int count = context.queue.Count;

                            using (SqlCommand command = connection.CreateCommand())
                            {

                                command.CommandType = CommandType.StoredProcedure;
                                command.CommandText = "usp_insert_value";

                                for (int i = 0; i < count; i++)
                                {

                                    while (!context.queue.TryDequeue(out envelope)) ;

                                    foreach (Entry entry in envelope.Entries)
                                    {

                                        command.Parameters.Clear();

                                        command.Parameters.Add(new SqlParameter("@Namespace", SqlDbType.VarChar)
                                        {

                                            Direction = ParameterDirection.Input,
                                            SqlDbType = SqlDbType.VarChar,
                                            SqlValue = envelope.Path

                                        });

                                        command.Parameters.Add(new SqlParameter("@Key", SqlDbType.VarChar)
                                        {

                                            Direction = ParameterDirection.Input,
                                            SqlValue = entry.Key

                                        });

                                        command.Parameters.Add(new SqlParameter("@Value", SqlDbType.Variant)
                                        {

                                            Direction = ParameterDirection.Input,
                                            SqlValue = entry.Value

                                        });

                                        command.Parameters.Add(new SqlParameter("@Timestamp", SqlDbType.DateTime)
                                        {


                                            Direction = ParameterDirection.Input,
                                            SqlValue = entry.Timestamp

                                        });

                                        try
                                        {

                                            command.ExecuteNonQuery();

                                        }
                                        catch (Exception error)
                                        {
                                            Logger.Error(error);
                                            continue;
                                        }

                                    }

                                }

                            }

                            context.sleepTime = 1000;

                        }
                        catch (Exception e)
                        {
                            Logger.Error(e);
                            context.sleepTime = Math.Max(context.sleepTime * 2, 1000 * 60 * 60 * 24);

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Verify that the connection is open for business.
        /// </summary>
        /// <param name="connection">The connection the verify.</param>
        /// <returns><c>true</c> if the connection is open.</returns>
        protected static bool VerifyConnection(SqlConnection connection)
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

        /// <summary>
        /// Gets the corresponding <see cref="SqlDbType"/> from the provided <see cref="Type"/>
        /// </summary>
        /// <param name="type">The framework <see cref="Type"/> to convert.</param>
        /// <returns>A corresponding <see cref="SqlDbType"/></returns>
        protected static SqlDbType GetType(Type type)
        {

            //
            // From https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
            //
            switch (type.Name)
            {
                case "Int64":
                    return SqlDbType.BigInt;
                case "Boolean":
                    return SqlDbType.Bit;
                case "String":
                    return SqlDbType.NVarChar;
                case "DateTime":
                    return SqlDbType.DateTime;
                case "DateTimeOffset":
                    return SqlDbType.DateTimeOffset;
                case "Decimal":
                    return SqlDbType.Decimal;
                case "Double":
                    return SqlDbType.Float;
                case "Int32":
                    return SqlDbType.Int;
                case "Single":
                    return SqlDbType.Real;
                case "Int16":
                    return SqlDbType.SmallInt;
                case "Object":
                    return SqlDbType.Variant;
                case "TimeSpan":
                    return SqlDbType.Time;
                case "Byte":
                    return SqlDbType.TinyInt;
                case "Guid":
                    return SqlDbType.UniqueIdentifier;
                default:
                    throw new Exception(String.Format("Unknown type: {0}", type));
            }

        }

        /// <summary>
        /// Gets a corresponding framework <see cref="Type"/> from the provided <c>string</c> that identifies an SQL Server database type.
        /// </summary>
        /// <param name="sql_type">The SQL Server database type identifier </param>
        /// <returns>The corresponding framework <see cref="Type"/></returns>
        protected static Type GetType(string sql_type)
        {

            //
            // From https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
            //
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
