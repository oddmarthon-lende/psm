/// <copyright file="advantage.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// <summary>Advantage database implementation</summary>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace PSMonitor.Stores
{

    /// <summary>
    /// A store that connects to an Advantage database
    /// </summary>
    public sealed class Advantage : DB
    {

        private new class Configuration : Store.Configuration
        {

            [Category("Database")]
            [Description("The connection string that is used to connect to the database")]
            public string ConnectionString
            {

                get {
                    
                    return Setup.Get<DB, string>("connectionString");
                }

                set {

                    Setup.Set<DB>("connectionString", value);

                }
            }

            [Category("Advantage")]
            public string Well
            {

                get
                {

                    return Setup.Get<Advantage, string>("well", true);
                }

                set
                {

                    Setup.Set<Advantage>("well", value);

                }
            }

            [Category("Advantage")]
            public string Wellbore
            {

                get
                {

                    return Setup.Get<Advantage, string>("wellbore", true);
                }

                set
                {

                    Setup.Set<Advantage>("wellbore", value);

                }
            }

            [Category("Advantage")]
            public int Run
            {

                get
                {

                    return Setup.Get<Advantage, int>("run", true);
                }

                set
                {

                    Setup.Set<Advantage>("run", value);

                }
            }

            [Category("Information")]
            public double? MaxDepth { get; private set; }

            [Category("Information")]
            public DateTime? MaxTime { get; private set; }

            [Category("Information")]
            public double? MinDepth { get; private set; }

            [Category("Information")]
            public DateTime? MinTime { get; private set; }


            private Advantage _advantage = null;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="store">The <see cref="Advantage"/> store instance</param>
            public Configuration(Advantage store) : base()
            {
                this._advantage = store;
            }

            public override Properties Get()
            {

                Properties properties = base.Get();

                foreach (KeyValuePair<PropertyDescriptor, Dictionary<object, object>> p in properties)
                {

                    switch (p.Key.Name)
                    {
                        case "Well"     :

                            GetWells(p.Value);
                            break;

                        case "Wellbore" :

                            GetWellbores(p.Value);
                            break;

                        case "Run":

                            GetRunNumbers(p.Value);
                            break;
                    }

                }

                SetBounds();

                return properties;
            }

            private void GetWells(Dictionary<object, object> container)
            {

                using (SqlConnection connection = _advantage.CreateConnection())
                {

                    VerifyConnection(connection);

                    using (SqlCommand command = connection.CreateCommand())
                    {

                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "select well_name, well_identifier from well order by well_name asc;";

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                                container.Add(reader.GetString(0), reader.GetString(1));
                        }

                    }
                }
            }

            private void GetWellbores(Dictionary<object, object> container)
            {

                using (SqlConnection connection = _advantage.CreateConnection())
                {

                    VerifyConnection(connection);

                    using (SqlCommand command = connection.CreateCommand())
                    {

                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = String.Format("select wlbr_name, wlbr_identifier from wellbore where well_identifier = '{0}' order by wlbr_name asc;", Well);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                                container.Add(reader.GetString(0), reader.GetString(1));
                        }

                    }
                }
            }

            private void GetRunNumbers(Dictionary<object, object> container)
            {

                using (SqlConnection connection = _advantage.CreateConnection())
                {

                    VerifyConnection(connection);

                    using (SqlCommand command = connection.CreateCommand())
                    {

                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = String.Format("select distinct runno from gendataset where wellboreid = '{0}' and runno is not null order by runno asc;", Wellbore);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read()) {
                                object runno = reader.GetValue(0);
                                container.Add(runno.ToString(), Convert.ToInt32(runno));
                            }
                        }

                    }
                }
            }

            private void SetBounds()
            {

                using (SqlConnection connection = _advantage.CreateConnection())
                {

                    VerifyConnection(connection);

                    using (SqlCommand command = connection.CreateCommand())
                    {

                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = String.Format("select max(t1.Depth), max(t1.Time), min(t1.Depth), min(t1.Time) from gendataindex t1 where t1.gendatasetid in (select Id from gendataset where wellboreid = '{0}' and runno = {1});", Wellbore, Run);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                            if (reader.Read())
                            {

                                try {

                                    MaxDepth = reader.GetDouble(0);
                                    MinDepth = reader.GetDouble(2);

                                    MaxTime = reader.GetDateTime(1);
                                    MinTime = reader.GetDateTime(3);
                                }
                                catch(SqlNullValueException e)
                                {
                                    Debug.WriteLine(e);
                                }

                            }

                        }

                    }
                }
            }
        }        
        
        /// <summary>
        /// Constructor
        /// </summary>
        public Advantage() : base(false)
        {

            Options = new Configuration(this);

        }

        /// <summary>
        /// This method has been disabled in this implementation of the <see cref="IStore"/> interface
        /// Using it will result in an exception being thrown.
        /// </summary>
        public override void Put(Envelope data)
        {
            throw new InvalidOperationException("Writing to the Advantage Database is disabled.");
        }

        /// <summary>
        /// Get the tables and columns as <see cref="Key"/>'s
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>An array of <see cref="Key"/> type</returns>
        public override Key[] GetKeys(string path)
        {
            
            int Length = String.IsNullOrEmpty(path) ? 0 : 1;

            List<Key> keys = new List<Key>();
            Configuration config = (Configuration)Options;

            using (SqlConnection connection = CreateConnection())
            {

                VerifyConnection(connection);

                using (SqlCommand command = connection.CreateCommand())
                {

                    command.CommandType = System.Data.CommandType.Text;

                    switch(Length)
                    {
                        case 0:

                            command.CommandText = "select name from sys.tables where name like 'Gen%' and name not like 'GenAux%'";
                            break;

                        case 1:

                            command.CommandText = String.Format("select t1.name, t2.name as type from sys.columns t1 left join sys.types t2 on (t1.user_type_id = t2.user_type_id) where t1.object_id = (select object_id from sys.tables where name = '{0}');", path);
                            break;

                        default:

                            throw new KeyNotFoundException("Cannot find this path in the Advantage database");
                    }                    
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            keys.Add(new Key(reader.GetString(0), Length == 0 ? null : DB.GetType(reader.GetString(1))));
                        }

                    }

                }

            }

            return keys.ToArray();
        }

        public override Entry Get(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets data
        /// </summary>
        /// <param name="path">The data path</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The end index</param>
        /// <returns>The data enumerable</returns>
        protected override IEnumerable<Entry> Get_(string path, object start, object end)
        {
            
            Path p = Path.Extract(path);
            SqlConnection connection = CreateConnection();
            SqlCommand command = connection.CreateCommand();
            string indexColumnName = start is DateTime ? "Time" : "Depth";

            VerifyConnection(connection);

            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = String.Format("select t2.{0} as Value, t1.Depth, t1.Time as Timestamp from gendataindex t1 left join {1} t2 on (t1.Id = t2.gendataindexid) where t1.gendatasetid = (select Id from gendataset where wellboreid = '{2}' and [name] = '{3}' and runno = {5}) and t1.{4} >= @StartIndex and t1.{4} <= @EndIndex order by t1.{4} desc;", p.Key, p.Namespace, Options.Get<string>("Wellbore"), Regex.Replace(p.Namespace, @"^(Gen)", "", RegexOptions.Compiled), indexColumnName, Options.Get<int>("Run"));

            command.Parameters.Add(new SqlParameter("@StartIndex", GetType(start.GetType()))
            {
                Direction = ParameterDirection.Input,
                SqlValue = start
            });

            command.Parameters.Add(new SqlParameter("@EndIndex", GetType(end.GetType()))
            {
                Direction = ParameterDirection.Input,
                SqlValue = end
            });

            return new Entries(p, indexColumnName, connection, command, true);

        }

        /// <summary>
        /// This method has been disabled in this implementation of the <see cref="IStore"/> interface
        /// </summary>
        /// <param name="path"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        protected override long Delete_(string path, DateTime? start, DateTime? end)
        {
            throw new InvalidOperationException("Deletion of data in the Advantage Database is disabled.");
        }

        protected override void Dispatch(Store.Path path, Dictionary<Store.Path, Entry[]> processed, SqlConnection connection, ref int totalCount)
        {

            Type indexType = path.StartIndex.GetType();
            TypeCode indexTypeCode = Type.GetTypeCode(indexType);

            using (SqlCommand command = connection.CreateCommand())
            {

                string indexColumnName = null;

                switch (indexTypeCode)
                {

                    case TypeCode.DateTime:
                        indexColumnName = "Time";
                        break;

                    case TypeCode.Int64:
                        indexColumnName = "Depth";
                        break;

                    default:
                        throw new Exception("Invalid index type");

                }

                command.CommandType = CommandType.Text;
                command.CommandText = String.Format("select t2.{0} as Value, t1.Depth, t1.Time as Timestamp from gendataindex t1 left join {1} t2 on (t1.Id = t2.gendataindexid) where t1.gendatasetid = (select Id from gendataset where wellboreid = '{2}' and [name] = '{3}' and runno = {5}) and t1.{4} > @StartIndex order by t1.{4} desc;", path.Key, path.Namespace, Options.Get<string>("Wellbore"), Regex.Replace(path.Namespace, @"^(Gen)", "", RegexOptions.Compiled), indexColumnName, Options.Get<int>("Run"));

                command.Parameters.Add(new SqlParameter("@StartIndex", GetType(indexType))
                {
                    Direction = ParameterDirection.Input,
                    SqlValue = path.StartIndex
                });                                

                Entry[] entries = null;

                if (processed.TryGetValue(path, out entries))
                {


                    switch (indexTypeCode)
                    {

                        case TypeCode.DateTime:
                            entries = (from entry in entries where entry.Timestamp > (DateTime)path.StartIndex select entry).ToArray();
                            break;

                        case TypeCode.Int64:
                            entries = (from entry in entries where (double)entry.Index > (long)path.StartIndex select entry).ToArray();
                            break;

                        default:
                            entries = null;
                            break;

                    }

                }

                entries = entries ?? (new Entries(new Path(path), indexColumnName, connection, command)).ToArray();

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

                    if (!processed.ContainsKey(path))
                    {
                        totalCount += entries.Length;
                        processed.Add(path, entries);
                    }

                }

            }

        }

    }
}
