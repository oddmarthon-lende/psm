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
using System.Linq;
using System.Text.RegularExpressions;

namespace PSMonitor.Stores
{

    /// <summary>
    /// A store that connects to an Advantage database
    /// </summary>
    public sealed class Advantage : DB
    {
        
        public new enum IndexType
        {

            Time,
            Depth,
            Index

        }

        public override Enum Default
        {
            get
            {
                return IndexType.Index;
            }
        }

        public override Type Index
        {
            get
            {
                return typeof(IndexType);
            }
        }     

        private class ParameterizedPath : DB.Path
        {
            
            /// <summary>
            /// The well name
            /// </summary>
            public string Well { get; private set; }

            /// <summary>
            /// The wellbore name
            /// </summary>
            public string Wellbore { get; private set; }


            /// <summary>
            /// The run number
            /// </summary>
            public string RunNo { get; private set; }

            /// <summary>
            /// The name of the table
            /// </summary>
            public string Table { get; private set; }


            /// <summary>
            /// The name of the column
            /// </summary>
            public string Column { get; private set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="path"></param>
            public ParameterizedPath(Path path) : base(path)
            {

                int i = 0;
                foreach (string p_value in path)
                {

                    switch (i++)
                    {

                        case 0:

                            Well = p_value;
                            break;

                        case 1:

                            Wellbore = p_value;
                            break;

                        case 2:

                            RunNo = p_value;
                            break;

                        case 3:

                            Table = p_value;
                            break;

                        case 4:

                            Column = p_value;
                            break;

                    }

                }

            }


            /// <summary>
            /// Extracts the path as parameters.
            /// </summary>
            /// <param name="path"></param>
            /// <returns></returns>
            public static new ParameterizedPath Extract(string path)
            {
                return new ParameterizedPath(Path.Create(path));
            }

        }
                        
        /// <summary>
        /// Constructor
        /// </summary>
        public Advantage() : base(false) { }

        /// <summary>
        /// This method has been disabled in this implementation of the <see cref="IStore"/> interface
        /// Using it will result in an exception being thrown.
        /// </summary>
        public override void Write(Envelope data)
        {
            throw new InvalidOperationException("Writing to the Advantage Database has not been implemented.");
        }

        /// <summary>
        /// Get the tables and columns as <see cref="Key"/>'s
        /// </summary>
        /// <param name="path">The path</param>
        /// <returns>An array of <see cref="Key"/> type</returns>
        public override Key[] Keys(string path)
        {

            ParameterizedPath p = ParameterizedPath.Extract(path);

            List<Key> keys = new List<Key>();
            Configuration config = (Configuration)Options;
            
            using (SqlConnection connection = CreateConnection())
            {

                VerifyConnection(connection);

                using (SqlCommand command = connection.CreateCommand())
                {

                    command.CommandType = System.Data.CommandType.Text;

                    switch(p.Length)
                    {

                        case 0:

                            command.CommandText = "select well_name as name from well;";
                            break;

                        case 1:

                            command.CommandText = "select wlbr_name as name from wellbore where well_identifier = (select well_identifier from well where well_name = @WellName);";

                            command.Parameters.Add(new SqlParameter("@WellName", SqlDbType.NVarChar)
                            {
                                Direction = ParameterDirection.Input,
                                SqlValue = p.Well
                            });

                            break;

                        case 2:

                            command.CommandText = "select distinct RunNo from GenDataset where WellboreId = (select wlbr_identifier from wellbore where wlbr_name = @WellboreName);";

                            command.Parameters.Add(new SqlParameter("@WellboreName", SqlDbType.NVarChar)
                            {
                                Direction = ParameterDirection.Input,
                                SqlValue = p.Wellbore
                            });

                            break;

                        case 3:

                            command.CommandText = "select name from sys.tables where name like 'Gen%' and name not like 'GenAux%'";

                            break;

                        case 4:

                            command.CommandText = "select t1.name, t2.name as type from sys.columns t1 left join sys.types t2 on (t1.user_type_id = t2.user_type_id) where t1.object_id = (select object_id from sys.tables where name = @Table);";

                            command.Parameters.Add(new SqlParameter("@Table", SqlDbType.NVarChar)
                            {
                                Direction = ParameterDirection.Input,
                                SqlValue = p.Table
                            });

                            break;

                        default:

                            return keys.ToArray();
                    }                    
                    
                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            
                            keys.Add(new Key(Convert.ToString(reader.GetValue(0)), p.Length == 4 ? DB.GetType(reader.GetString(1)) : null));
                        }
                    }
                }
            }

            return keys.OrderBy((k) => { return k.Name; }).ToArray();
        }
        
        /// <summary>
        /// Generate the query string
        /// </summary>
        /// <param name="index">The index field identifier</param>
        /// <param name="hasEnd">Set to false when query is used to fetch new rows</param>
        /// <param name="parameters">The parameters used to format the string</param>
        /// <returns>The generated SQL query</returns>
        private string GenerateQuery(Enum index, bool hasEnd, ParameterizedPath path)
        {

            object[] parameters = new object[] {

                path.Column,
                path.Table,
                path.Wellbore,
                Regex.Replace(path.Table, @"^(Gen)", "", RegexOptions.Compiled),
                index.ToString(),
                path.RunNo

            };

            switch (index.ToString())
            {
                
                case "Index":

                    return String.Format("select ([RowNumber_Reversed] - 1) as [Index], {0} as [Value], [Time] as [Timestamp] from (select ROW_NUMBER() over (order by [Time] desc) as RowNumber, ROW_NUMBER() over (order by [Time] asc) as [RowNumber_Reversed], * from gendataindex t1 left join {1} t2 on (t1.Id = t2.gendataindexid) where t1.gendatasetid = (select Id from gendataset where wellboreid = (select wlbr_identifier from wellbore where wlbr_name = '{2}') and [name] = '{3}' and runno = {5})) as Result where " + (hasEnd ? "(RowNumber - 1) >=" : "([RowNumber_Reversed] - 1) >") +" @StartIndex " + (hasEnd ? "and (RowNumber - 1) <= @EndIndex" : "") + " order by RowNumber asc", parameters);

                case "Time":

                    return String.Format("select t1.{4} as [Index], t2.{0} as [Value], t1.Time as [Timestamp] from gendataindex t1 left join {1} t2 on (t1.Id = t2.gendataindexid) where t1.gendatasetid = (select Id from gendataset where wellboreid = (select wlbr_identifier from wellbore where wlbr_name = '{2}') and [name] = '{3}' and runno = {5}) and t1.Time >" + (hasEnd ? "" : "=") + " @StartIndex " + (hasEnd ? "and t1.Time <= @EndIndex" : "") + " order by t1.Time desc", parameters);

                case "Depth":

                    return String.Format("select [Index], [Value], [Timestamp] from (select max(t1.{4}) over (partition by 0) - t1.{4} as [Index_Inversed], t1.{4} as [Index], t2.{0} as [Value], t1.Time as [Timestamp] from gendataindex t1  left join {1} t2 on (t1.Id = t2.gendataindexid)  where t1.gendatasetid = (select Id from gendataset where wellboreid = (select wlbr_identifier from wellbore where wlbr_name = '{2}') and [name] = '{3}' and runno = {5})) as Result where " + (hasEnd ? "[Index_Inversed] >=" : "[Index] >") + " @StartIndex " + (hasEnd ? "and [Index_Inversed] <= @EndIndex" : "") + " order by [Index] desc", parameters);

            }

            return "";
        }
        
        /// <summary>
        /// Gets data
        /// <see cref="IStore.Read(string, object, object, Enum)"/>
        /// </summary>
        public override IEnumerable<Entry> Read(string path, object start, object end, Enum index)
        {

            ParameterizedPath p = ParameterizedPath.Extract(path);
            SqlConnection connection = CreateConnection();
                
            VerifyConnection(connection);

            SqlCommand command = connection.CreateCommand();

            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = GenerateQuery(index, true, p);


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

            return new Entries(p, index, connection, command, true);


        }
        
        /// <summary>
        /// This method has been disabled in this implementation of the <see cref="IStore"/> interface
        /// </summary>
        public override void Delete(string path)
        {
            throw new InvalidOperationException("Deletion of data in the Advantage Database is disabled.");
        }
        
        /// <summary>
        /// <see cref="DB.Dispatch(Store.Path, Dictionary{Store.Path, Entry[]}, SqlConnection, ref int)"/>
        /// </summary>
        protected override void Dispatch(Store.Path path, Dictionary<Store.Path, Entry[]> processed, SqlConnection connection, ref int totalCount)
        {

            Type indexType = path.StartIndex.GetType();

            using (SqlCommand command = connection.CreateCommand())
            {

                IndexType indexIdentifier = (IndexType)path.Index;

                command.CommandType = CommandType.Text;
                command.CommandText = GenerateQuery(path.Index, false, new ParameterizedPath(new DB.Path(path)));

                command.Parameters.Add(new SqlParameter("@StartIndex", GetType(indexType))
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

                Entry[] entries = null;

                if (processed.TryGetValue(path, out entries))
                {

                    switch (path.Index.ToString())
                    {

                        case "Depth":
                        case "Index":

                            entries = (from entry in entries where Convert.ToDouble(entry.Index) > Convert.ToDouble(path.StartIndex) select entry).ToArray();
                            break;

                        case "Time":

                            entries = (from entry in entries where (DateTime)entry.Index > (DateTime)path.StartIndex select entry).ToArray();
                            break;                       

                        default:

                            entries = null;
                            break;

                    }
                    
                }

                entries = entries ?? (new Entries(new Path(path), indexIdentifier, connection, command)).ToArray();

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
