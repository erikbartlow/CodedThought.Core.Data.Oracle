using CodedThought.Core.Data.Interfaces;
using CodedThought.Core.Exceptions;

using System;
using System.Configuration;
using System.Data;
using System.Text;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Data.SqlClient;
namespace CodedThought.Core.Data.Oracle
{

    /// <summary>
    /// OracleDatabaseObject provides all Oracle specific functionality needed by DBStore and its family of classes..
    /// </summary>
    public class OracleDatabaseObject : DatabaseObject, IDatabaseObject, IDbSchema
    {
        #region Declarations

        private OracleConnection _connection;

        #endregion

        #region Constructor

        public OracleDatabaseObject()
        {
        }

        #endregion Constructor

        #region Transaction and Connection Methods

        /// <summary>
        /// Commits updates and inserts.  This is only for Oracle database operations.
        /// </summary>
        public override void Commit() => ExecuteNonQuery("COMMIT", CommandType.Text);

        /// <summary>
        /// Opens an Oracle Connection
        /// </summary>
        /// <returns></returns>
        protected override IDbConnection OpenConnection()
        {
            try
            {
                _connection = new(ConnectionString);
                _connection.Open();
                return _connection;

            }
            catch (OracleException ex)
            {
                throw new CodedThoughtApplicationException("Could not open Connection.  Check connection string" + "/r/n" + ex.Message + "/r/n" + ex.StackTrace, ex);
            }
        }

        #endregion Transaction and Connection Methods

        #region Other Override Methods

        /// <summary>
        /// Tests the connection to the database.
        /// </summary>
        /// <returns></returns>
        public override bool TestConnection()
        {
            try
            {
                OpenConnection();
                return Connection.State == ConnectionState.Open;
            }
            catch (CodedThoughtException)
            {
                throw;
            }
        }
        /// <summary>
        /// Creates a Sql Data Adapter object with the passed Command object.
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        protected override IDataAdapter CreateDataAdapter(IDbCommand cmd) => new OracleDataAdapter(cmd as OracleCommand);

        /// <summary>Convert any data type to Char</summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public override string ConvertToChar(string columnName) => "CONVERT(varchar, " + columnName + ")";
        /// <summary>Creates the parameter collection.</summary>
        /// <returns></returns>
        public override ParameterCollection CreateParameterCollection() => new(this);

        public override IDataParameter CreateApiParameter(string paraemterName, string parameterValue) => throw new NotImplementedException();

        #region Parameters

        /// <summary>Returns the param connector for Oracle, @</summary>
        /// <returns></returns>
        public override string ParameterConnector => ":";

        /// <summary>Gets the wild card character.</summary>
        /// <value>The wild card character.</value>
        public override string WildCardCharacter => "%";

        /// <summary>Gets the column delimiter character.</summary>
        public override string ColumnDelimiter => throw new NotImplementedException();

        /// <summary>Creates the SQL server param.</summary>
        /// <param name="srcTableColumnName">Name of the SRC table column.</param>
        /// <param name="paramType">         Type of the param.</param>
        /// <returns></returns>
        private OracleParameter CreateDbServerParam(string srcTableColumnName, OracleDbType paramType)
        {
            OracleParameter param = new(ToSafeParamName(srcTableColumnName), paramType)
            {
                SourceColumn = srcTableColumnName
            };
            return param;
        }

        /// <summary>Creates the SQL server param.</summary>
        /// <param name="srcTableColumnName">Name of the SRC table column.</param>
        /// <param name="paramType">         Type of the param.</param>
        /// <param name="size">              The size.</param>
        /// <returns></returns>
        private OracleParameter CreateDbServerParam(string srcTableColumnName, OracleDbType paramType, int size)
        {
            OracleParameter param = new(ToSafeParamName(srcTableColumnName), paramType, size)
            {
                SourceColumn = srcTableColumnName
            };
            return param;
        }

        /// <summary>Creates the XML parameter.</summary>
        /// <param name="srcTaleColumnName">Name of the SRC tale column.</param>
        /// <param name="parameterValue">   The parameter value.</param>
        /// <returns></returns>
        public override IDataParameter CreateXMLParameter(string srcTaleColumnName, string parameterValue) => throw new NotImplementedException();

        /// <summary>Creates a boolean parameter.</summary>
        /// <param name="srcTaleColumnName">Name of the SRC tale column.</param>
        /// <param name="parameterValue">   The parameter value.</param>
        /// <returns></returns>
        public override IDataParameter CreateBooleanParameter(string srcTableColumnName, bool parameterValue)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Boolean);
            returnValue.Value = parameterValue;
            return returnValue;
        }


        /// <summary>
        /// Creates parameters for the supported database.  
        /// </summary>
        /// <param name="obj">The Business Entity from which to extract the data</param>
        /// <param name="col">The column for which the data must be extracted from the buisiness entity</param>
        /// <param name="store">The store that handles the IO</param>
        /// <returns></returns>
        public override IDataParameter CreateParameter(object obj, TableColumn col, IDBStore store)
        {
            Boolean isNull = false;
            int sqlDataType = 0;

            object extractedData = store.Extract(obj, col.Name);
            try
            {
                switch (col.Type)
                {
                    case DbTypeSupported.dbVarChar:
                        isNull = (extractedData == null || (string) extractedData == "");
                        sqlDataType = (int) OracleDbType.Varchar2;
                        break;
                    case DbTypeSupported.dbInt32:
                        isNull = ((int) extractedData == int.MinValue);
                        sqlDataType = (int) OracleDbType.Int32;
                        break;
                    case DbTypeSupported.dbDouble:
                        isNull = ((double) extractedData == double.MinValue);
                        sqlDataType = (int) OracleDbType.Double;
                        break;
                    case DbTypeSupported.dbDateTime:
                        isNull = ((DateTime) extractedData == DateTime.MinValue);
                        sqlDataType = (int) OracleDbType.Date;
                        break;
                    case DbTypeSupported.dbChar:
                        isNull = (extractedData == null || System.Convert.ToString(extractedData) == "");
                        sqlDataType = (int) OracleDbType.Char;
                        break;
                    case DbTypeSupported.dbBlob:    // Text, not Image
                    case DbTypeSupported.dbVarBinary:
                        isNull = (extractedData == null);
                        sqlDataType = (int) OracleDbType.Blob;
                        break;
                    case DbTypeSupported.dbDecimal:
                        isNull = ((decimal) extractedData == decimal.MinValue);
                        sqlDataType = (int) OracleDbType.Double;
                        break;
                    case DbTypeSupported.dbBit:
                        isNull = (extractedData == null);
                        sqlDataType = (int) OracleDbType.Byte;
                        break;
                    default:
                        throw new ApplicationException("Data type not supported.  DataTypes currently suported are: DbTypeSupported.dbString, DbTypeSupported.dbInt32, DbTypeSupported.dbDouble, DbTypeSupported.dbDateTime, DbTypeSupported.dbChar");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error creating Parameter", ex);
            }

            OracleParameter parameter = CreateDbServerParam(col.Name, (OracleDbType) sqlDataType);

            if (isNull)
            {
                parameter.Value = DBNull.Value;
            }
            else
            {
                parameter.Value = extractedData;
            }

            return parameter;
        }
        /// <summary>
        /// Create an empty parameter for SQLServer
        /// </summary>
        /// <returns></returns>
        public override IDataParameter CreateEmptyParameter()
        {
            IDataParameter returnValue = null;

            returnValue = new OracleParameter();

            return returnValue;
        }
        /// <summary>
        /// Creates and returns a string parameter for the supported database.
        /// </summary>
        /// <param name="srcTableColumnName"></param>
        /// <param name="parameterValue"></param>
        /// <returns></returns>
        public override IDataParameter CreateStringParameter(string srcTableColumnName, string parameterValue)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Varchar2);
            if (parameterValue != string.Empty)
            {
                returnValue.Value = parameterValue;
            }
            else
            {
                returnValue.Value = DBNull.Value;
            }

            return returnValue;
        }
        /// <summary>
        /// Creates and returns an output parameter for the supported database.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        /// <exception cref="HPApplicationException">Data type not supported.  DataTypes currently suported are: DbTypeSupported.dbString, DbTypeSupported.dbInt32, DbTypeSupported.dbDouble, DbTypeSupported.dbDateTime, DbTypeSupported.dbChar</exception>
        public override IDataParameter CreateOutputParameter(string parameterName, DbTypeSupported returnType)
        {
            IDataParameter returnParam = null;
            OracleDbType sqlDataType;
            switch (returnType)
            {
                case DbTypeSupported.dbVarChar:
                    sqlDataType = OracleDbType.Varchar2;
                    break;
                case DbTypeSupported.dbInt32:
                    sqlDataType = OracleDbType.Int32;
                    break;
                case DbTypeSupported.dbDouble:
                    sqlDataType = OracleDbType.Double;
                    break;
                case DbTypeSupported.dbDateTime:
                    sqlDataType = OracleDbType.Date;
                    break;
                case DbTypeSupported.dbChar:
                    sqlDataType = OracleDbType.Char;
                    break;
                case DbTypeSupported.dbBlob:    // Text, not Image
                    sqlDataType = OracleDbType.Blob;
                    break;
                case DbTypeSupported.dbDecimal:
                    sqlDataType = OracleDbType.Double;
                    break;
                case DbTypeSupported.dbBit:
                    sqlDataType = OracleDbType.Byte;
                    break;
                default:
                    throw new ApplicationException("Data type not supported.  DataTypes currently suported are: DbTypeSupported.dbString, DbTypeSupported.dbInt32, DbTypeSupported.dbDouble, DbTypeSupported.dbDateTime, DbTypeSupported.dbChar");
            }
            returnParam = CreateDbServerParam(parameterName, sqlDataType);
            returnParam.Direction = ParameterDirection.Output;
            return returnParam;

        }
        /// <summary>
        /// Creates and returns a return parameter for the supported database.
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="returnType"></param>
        /// <returns></returns>
        /// <exception cref="HPApplicationException">Data type not supported.  DataTypes currently suported are: DbTypeSupported.dbString, DbTypeSupported.dbInt32, DbTypeSupported.dbDouble, DbTypeSupported.dbDateTime, DbTypeSupported.dbChar</exception>
        public override IDataParameter CreateReturnParameter(string parameterName, DbTypeSupported returnType)
        {
            IDataParameter returnParam = null;
            OracleDbType sqlDataType;
            switch (returnType)
            {
                case DbTypeSupported.dbVarChar:
                    sqlDataType = OracleDbType.Varchar2;
                    break;
                case DbTypeSupported.dbInt32:
                    sqlDataType = OracleDbType.Int32;
                    break;
                case DbTypeSupported.dbDouble:
                    sqlDataType = OracleDbType.Double;
                    break;
                case DbTypeSupported.dbDateTime:
                    sqlDataType = OracleDbType.Date;
                    break;
                case DbTypeSupported.dbChar:
                    sqlDataType = OracleDbType.Char;
                    break;
                case DbTypeSupported.dbBlob:    // Text, not Image
                    sqlDataType = OracleDbType.Blob;
                    break;
                case DbTypeSupported.dbDecimal:
                    sqlDataType = OracleDbType.Double;
                    break;
                case DbTypeSupported.dbBit:
                    sqlDataType = OracleDbType.Byte;
                    break;
                default:
                    throw new ApplicationException("Data type not supported.  DataTypes currently suported are: DbTypeSupported.dbString, DbTypeSupported.dbInt32, DbTypeSupported.dbDouble, DbTypeSupported.dbDateTime, DbTypeSupported.dbChar");
            }
            returnParam = CreateDbServerParam(parameterName, sqlDataType);
            returnParam.Direction = ParameterDirection.ReturnValue;
            return returnParam;

        }

        /// <summary>Creates a Int32 parameter for the supported database</summary>
        /// <param name="srcTableColumnName"></param>
        /// <param name="parameterValue">    </param>
        /// <returns></returns>
        public override IDataParameter CreateInt32Parameter(string srcTableColumnName, int parameterValue)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Int32);
            returnValue.Value = parameterValue != int.MinValue ? parameterValue : DBNull.Value;

            return returnValue;
        }

        /// <summary>Creates a Double parameter based on supported database</summary>
        /// <param name="srcTableColumnName"></param>
        /// <param name="parameterValue">    </param>
        /// <returns></returns>
        public override IDataParameter CreateDoubleParameter(string srcTableColumnName, double parameterValue)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Double);
            returnValue.Value = parameterValue != double.MinValue ? parameterValue : DBNull.Value;

            return returnValue;
        }

        /// <summary>Create a data time parameter based on supported database.</summary>
        /// <param name="srcTableColumnName"></param>
        /// <param name="parameterValue">    </param>
        /// <returns></returns>
        public override IDataParameter CreateDateTimeParameter(string srcTableColumnName, DateTime parameterValue)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Date);
            returnValue.Value = parameterValue != DateTime.MinValue ? parameterValue : DBNull.Value;

            return returnValue;
        }

        /// <summary>Creates a Char parameter based on supported database.</summary>
        /// <param name="srcTableColumnName"></param>
        /// <param name="parameterValue">    </param>
        /// <param name="size">              </param>
        /// <returns></returns>
        public override IDataParameter CreateCharParameter(string srcTableColumnName, string parameterValue, int size)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Varchar2);
            returnValue.Value = parameterValue != string.Empty ? parameterValue : DBNull.Value;

            return returnValue;
        }

        /// <summary>Creates a Blob parameter based on supported database.</summary>
        /// <param name="srcTableColumnName"></param>
        /// <param name="parameterValue">    </param>
        /// <param name="size">              </param>
        /// <returns></returns>
        public IDataParameter CreateBlobParameter(string srcTableColumnName, byte[] parameterValue, int size)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Blob, size);
            returnValue.Value = parameterValue;

            return returnValue;
        }

        /// <summary>Creates the GUID parameter.</summary>
        /// <param name="srcTableColumnName">Name of the SRC table column.</param>
        /// <param name="parameterValue">    The parameter value.</param>
        /// <returns></returns>
        public override IDataParameter CreateGuidParameter(string srcTableColumnName, Guid parameterValue)
        {
            IDataParameter returnValue = null;

            returnValue = CreateDbServerParam(srcTableColumnName, OracleDbType.Raw);
            returnValue.Value = parameterValue;

            return returnValue;
        }

        public override IDataParameter CreateBetweenParameter(string srcTableColumnName, BetweenParameter betweenParam) => throw new NotImplementedException();


        #endregion Parameters

        #region Add method
        /// <summary>
        /// Adds data to the database
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="obj"></param>
        /// <param name="columns"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        /// 

        public override void Add(string tableName, object obj, List<TableColumn> columns, IDBStore store)
        {
            try
            {
                ParameterCollection parameters = new ParameterCollection();
                StringBuilder sbColumns = new StringBuilder();
                StringBuilder sbValues = new StringBuilder();

                for (int i = 0; i < columns.Count; i++)
                {
                    TableColumn col = columns[i];

                    if (col.IsInsertable)
                    {
                        //we do not insert columns such as autonumber columns
                        IDataParameter parameter = CreateParameter(obj, col, store);
                        sbColumns.Append(__comma).Append(col.Name);
                        sbValues.Append(__comma).Append(ParameterConnector).Append(parameter.ParameterName);
                        parameters.Add(parameter);
                    }
                }

                StringBuilder sql = new StringBuilder("INSERT INTO " + tableName + " (");
                sql.Append(sbColumns.Remove(0, 2));
                sql.Append(") VALUES (");
                sql.Append(sbValues.Remove(0, 2));
                sql.Append(") ");

                // ================================================================
                // print sql to output window to debuging purpose
                DebugParameters(sql, tableName, parameters);
                // ================================================================


                //Check if we have an identity Column
                if (store.GetPrimaryKey(obj) == 0 || store.GetPrimaryKey(obj) == Int32.MinValue)
                {
                    sql.Append("SELECT MAX(" + store.GetPrimaryKeyName(obj) + ") FROM " + tableName);
                    // ExecuteScalar will execute both the INSERT statement and the SELECT statement.
                    int retVal = System.Convert.ToInt32(this.ExecuteScalar(sql.ToString(), System.Data.CommandType.Text, parameters));
                    store.SetPrimaryKey(obj, retVal);
                }
                else
                {
                    this.ExecuteNonQuery(sql.ToString(), System.Data.CommandType.Text, parameters);
                }

                // this is the way to get the CONTEXT_INFO of a SQL connection session
                // string contextInfo = System.Convert.ToString( this.ExecuteScalar( "SELECT dbo.AUDIT_LOG_GET_USER_NAME() ", System.Data.CommandType.Text, null ) );
            }
            catch (ApplicationException irEx)
            {
                RollbackTransaction();
                // this is not a good method to catch DUPLICATE
                if (irEx.Message.IndexOf("duplicate key") >= 0)
                {
                    throw new FolderException(irEx.Message, irEx);
                }
                else
                {
                    throw new ApplicationException("Failed to add record to: " + tableName + "<BR>" + irEx.Message + "<BR>" + irEx.Source, irEx);
                }
            }
            catch (Exception ex)
            {
                RollbackTransaction();
                throw new ApplicationException("Failed to add record to: " + tableName + "<BR>" + ex.Message + "<BR>" + ex.Source, ex);
            }
        }


        #endregion Add method

        #region Executing Queries





        #endregion Executing Queries

        #region GetValue Methods



        /// <summary>
        /// Get a BLOB from a TEXT or IMAGE column.
        /// In order to get BLOB, a IDataReader's CommandBehavior must be set to SequentialAccess.
        /// That also means to Get columns in sequence is extremely important. 
        /// Otherwise the GetBlobValue method won't return correct data.
        /// [EXAMPLE]
        /// this.DataReaderBehavior = CommandBehavior.SequentialAccess;
        ///	using(IDataReader reader = this.ExecuteReader("select BigName, ID, BigBlob from BigTable", CommandType.Text)) 
        ///	{
        ///		while (reader.Read())
        ///		{
        ///			string bigName = reader.GetString(0);
        ///			int id = this.GetInt32Value( reader, 1);
        ///			byte[] bigText = this.GetBlobValue( reader, 2 );
        ///		}
        ///	}
        /// </summary>
        /// <param name="reader"></param>
        ///<param name="columnName"></param>
        /// <returns></returns>
        protected override byte[] GetBlobValue(IDataReader reader, string columnName)
        {

            int position = reader.GetOrdinal(columnName);

            // The DataReader's CommandBehavior must be CommandBehavior.SequentialAccess. 
            if (DataReaderBehavior != CommandBehavior.SequentialAccess)
            {
                throw new ApplicationException("Please set the DataReaderBehavior to SequentialAccess to call this method.");
            }
            OracleDataReader sqlReader = (OracleDataReader) reader;
            int bufferSize = 100;                   // Size of the BLOB buffer.
            byte[] outBuff = new byte[bufferSize];  // a buffer for every read in "bufferSize" bytes
            long totalBytes;                        // The total chars returned from GetBytes.
            long retval;                            // The bytes returned from GetBytes.
            long startIndex = 0;                    // The starting position in the BLOB output.
            byte[] outBytes = null;                 // The BLOB byte[] buffer holder.

            // Read the total bytes into outbyte[] and retain the number of chars returned.
            totalBytes = sqlReader.GetBytes(position, startIndex, outBytes, 0, bufferSize);
            outBytes = new byte[totalBytes];

            // initial reading from the BLOB column
            retval = sqlReader.GetBytes(position, startIndex, outBytes, 0, bufferSize);

            // Continue reading and writing while there are bytes beyond the size of the buffer.
            while (retval == bufferSize)
            {
                // Reposition the start index to the end of the last buffer and fill the buffer.
                startIndex += bufferSize;
                retval = sqlReader.GetBytes(position, startIndex, outBytes, System.Convert.ToInt32(startIndex), bufferSize);
            }

            return outBytes;
        }

        /// <summary>
        /// Gets a string from a BLOB, Text (SQLServer) or CLOB (Oracle),. developers should use
        /// this method only if they know for certain that the data stored in the field is a string.
        /// </summary>
        /// <param name="reader"></param>
        ///<param name="columnName"></param>
        /// <returns></returns>
        public override string GetStringFromBlob(IDataReader reader, string columnName) => Encoding.ASCII.GetString(GetBlobValue(reader, columnName));

        #endregion GetValue Methods

        #region Database Specific

        public override string GetTableName(string schemaName, string tableName)
        {
            if (!String.IsNullOrEmpty(schemaName))
            {
                return $"{schemaName}.{tableName}";
            }
            else
            {
                return tableName;
            }

        }

        #region Schema Definition Queries

        /// <summary>
        /// Gets the query used to list all tables in the database.
        /// </summary>
        /// <returns></returns>
        public override string GetTableListQuery() => "SELECT TABLE_NAME, OWNER FROM ALL_TABLES ORDER BY TABLE_NAME";
        /// <summary>
        /// Gets the query used to list all views in the database.
        /// </summary>
        /// <returns></returns>
        public override string GetViewListQuery() => "SELECT VIEW_NAME, OWNER as V FROM ALL_TABLES ORDER BY VIEW_NAME";

        /// <summary>
        /// Gets the table's column definition query.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns><see cref="System.String"/></returns>
        public override String GetTableDefinitionQuery(string tableName)
        {
            try
            {
                StringBuilder sql = new();
                List<TableColumn> tableDefinition = new();
                // Remove any brackets since the definitiion query doesn't support that.
                string tName = tableName.Replace("[", "").Replace("]", "");
                string schemaName = DefaultSchemaName.Replace("[", "").Replace("]", "");
                if (tName.Split(".".ToCharArray()).Length > 0)
                {
                    // The schema name appears to have been passed along with the table name. So parse them out and use them instead of the default values.
                    string[] tableNameData = tName.Split(".".ToCharArray());
                    schemaName = tableNameData[0].Replace("[", "").Replace("]", "");
                    tName = tableNameData[1].Replace("[", "").Replace("]", "");
                }
                sql.Append("SELECT C.column_name AS COLUMN_NAME, C.data_type AS DATA_TYPE, ");
                sql.Append("CASE WHEN C.IS_NULLABLE = 'NO' THEN 0 ELSE 1 END as IS_NULLABLE, ");
                sql.Append("CASE WHEN C.COLUMN_SIZE IS NULL THEN 0 ELSE C.COLUMN_SIZE END AS CHARACTER_MAXIMUM_LENGTH, ");
                sql.Append("C.ORDINAL_POSITION - 1 as ORDINAL_POSITION, NVL(C.IS_AUTOINCREMENT, '0') as IS_IDENTITY ");
                sql.Append("FROM USER_TAB_COLUMNS C");
                sql.AppendFormat("WHERE C.table_name = '{0}' ORDER BY C.ORDINAL_POSITION", tableName);
                return sql.ToString();
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Gets the query necessary to get a view's high level schema.  This does not include the columns.
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string GetViewDefinitionQuery(string viewName)
        {
            try
            {
                return GetTableDefinitionQuery(viewName);
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Gets the current session default schema name.
        /// </summary>
        /// <returns></returns>
        public override String GetDefaultSessionSchemaNameQuery()
        {
            try
            {
                return "SELECT SCHEMA_NAME()";
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion Schema Definition Queries

        #region Schema Methods

        /// <summary>
        /// Gets an enumerable list of <see cref="TableColumn"/> objects for the passed table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override List<TableColumn> GetTableDefinition(string tableName)
        {
            try
            {
                List<TableColumn> tableDefinitions = [];

                DataTable dtColumns = ExecuteDataTable(GetTableDefinitionQuery(tableName));
                foreach (DataRow row in dtColumns.Rows)
                {
                    TableColumn column = new("", DbTypeSupported.dbVarChar, 0, true)
                    {
                        Name = row["COLUMN_NAME"].ToString(),
                        IsNullable = Convert.ToBoolean(row["IS_NULLABLE"]),
                        SystemType = ToSystemType(row["DATA_TYPE"].ToString()),
                        MaxLength = Convert.ToInt32(row["CHARACTER_MAXIMUM_LENGTH"]),
                        IsIdentity = Convert.ToBoolean(row["IS_IDENTITY"]),
                        OrdinalPosition = Convert.ToInt32(row["ORDINAL_POSITION"])
                    };

                    tableDefinitions.Add(column);
                }
                return tableDefinitions;
            }
            catch { throw; }
        }
        /// <summary>
        /// Gets an enumerable list of <see cref="TableColumn"/> objects for the passed view.
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        public override List<TableColumn> GetViewDefinition(string viewName)
        {
            try
            {
                return GetTableDefinition(viewName);
            }
            catch (Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Gets an enumerable list of <see cref="TableSchema"/> objects unless tableName is passed to filter it.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override List<TableSchema> GetTableDefinitions()
        {
            try
            {
                List<TableSchema> tableDefinitions = [];
                DataTable dtTables = ExecuteDataTable(GetTableListQuery());
                foreach (DataRow row in dtTables.Rows)
                {
                    TableSchema tableSchema = new()
                    {
                        Name = row["TABLE_NAME"].ToString(),
                        Owner = row["TABLE_SCHEMA"].ToString(),
                        Columns = []
                    };
                    DataTable dtColumns = ExecuteDataTable(GetTableDefinitionQuery(tableSchema.Name));
                    foreach (DataRow col in dtColumns.Rows)
                    {
                        TableColumn column = new("", DbTypeSupported.dbVarChar, 0, true)
                        {
                            Name = col["COLUMN_NAME"].ToString(),
                            IsNullable = Convert.ToBoolean(col["IS_NULLABLE"]),
                            SystemType = ToSystemType(col["DATA_TYPE"].ToString()),
                            Type = ToDbSupportedType(col["DATA_TYPE"].ToString()),
                            MaxLength = Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]),
                            IsIdentity = Convert.ToBoolean(col["IS_IDENTITY"]),
                            OrdinalPosition = Convert.ToInt32(col["ORDINAL_POSITION"])
                        };
                        // Add this column to the list.
                        tableSchema.Columns.Add(column);
                    }
                    tableDefinitions.Add(tableSchema);
                }

                return tableDefinitions;
            }
            catch { throw; }
        }
        /// <summary>
        /// Gets an enumerable list of <see cref="ViewSchema"/> objects unless viewName is passed to filter it.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override List<ViewSchema> GetViewDefinitions()
        {
            try
            {

                List<ViewSchema> viewDefinitions = [];
                DataTable dtTables = ExecuteDataTable(GetViewListQuery());
                foreach (DataRow row in dtTables.Rows)
                {
                    ViewSchema viewSchema = new()
                    {
                        Name = row["TABLE_NAME"].ToString(),
                        Owner = row["TABLE_SCHEMA"].ToString(),
                        Columns = []
                    };
                    DataTable dtColumns = ExecuteDataTable(GetViewDefinitionQuery(viewSchema.Name));
                    foreach (DataRow col in dtColumns.Rows)
                    {
                        TableColumn column = new("", DbTypeSupported.dbVarChar, 0, true)
                        {
                            Name = col["COLUMN_NAME"].ToString(),
                            IsNullable = Convert.ToBoolean(col["IS_NULLABLE"]),
                            SystemType = ToSystemType(col["DATA_TYPE"].ToString()),
                            MaxLength = Convert.ToInt32(col["CHARACTER_MAXIMUM_LENGTH"]),
                            IsIdentity = Convert.ToBoolean(col["IS_IDENTITY"]),
                            OrdinalPosition = Convert.ToInt32(col["ORDINAL_POSITION"])
                        };
                        // Add this column to the list.
                        viewSchema.Columns.Add(column);
                    }
                    viewDefinitions.Add(viewSchema);
                }

                return viewDefinitions;
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion Schema Methods

        /// <summary>
        /// Gets SQL syntax of Year
        /// </summary>
        /// <param name="dateString"></param>
        /// <returns></returns>
        public override string GetYearSQLSyntax(string dateString) => "TO_DATE('" + dateString + "', \"YYYY\")";
        /// <summary>
        /// Gets database function name
        /// </summary>
        /// <param name="functionName"></param>
        /// <returns></returns>
        public override string GetFunctionName(FunctionName functionName)
        {
            string retStr = string.Empty;
            switch (functionName)
            {
                case FunctionName.SUBSTRING:
                    retStr = "SUBSTR";
                    break;
                case FunctionName.ISNULL:
                    retStr = "ISNULL";
                    break;
                case FunctionName.CURRENTDATE:
                    retStr = "SYSDATE";
                    break;
                case FunctionName.CONCATENATE:
                    retStr = "||";
                    break;
            }
            return retStr;
        }

        /// <summary>
        /// Gets Date string format.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="dateFormat">The date format.</param>
        /// <returns></returns>
        public override string GetDateToStringForColumn(string columnName, DateFormat dateFormat)
        {
            StringBuilder sb = new();
            switch (dateFormat)
            {
                case DateFormat.MMDDYYYY:
                    sb.Append(" TO_DATE(").Append(columnName).AppendFormat(", '{0}') ", "MMDDYYYY");
                    break;
                case DateFormat.MMDDYYYY_Hyphen:
                    sb.Append(" TO_DATE(").Append(columnName).AppendFormat(", '{0}') ", "MM/DD/YYYY");
                    break;
                case DateFormat.MonDDYYYY:
                    sb.Append(" TO_DATE(").Append(columnName).AppendFormat(", '{0}') ", "MonDDYYYY");
                    break;
                default:
                    sb.Append(columnName);
                    break;
            }
            return sb.ToString();
        }
        /// <summary>
        /// Gets the date to string for value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="dateFormat">The date format.</param>
        /// <returns></returns>
        public override string GetDateToStringForValue(string value, DateFormat dateFormat)
        {
            StringBuilder sb = new();
            switch (dateFormat)
            {
                case DateFormat.MMDDYYYY:
                    sb.Append(" TO_DATE('").Append(value).AppendFormat("', '{0}') ", dateFormat);
                    break;
                case DateFormat.MMDDYYYY_Hyphen:
                    sb.Append(" TO_DATE('").Append(value).AppendFormat("', '{0}') ", dateFormat);
                    break;
                case DateFormat.MonDDYYYY:
                    sb.Append(" TO_DATE('").Append(value).AppendFormat("', '{0}') ", dateFormat);
                    break;
                default:
                    sb.Append(value);
                    break;
            }
            return sb.ToString();
        }
        /// <summary>
        /// Get CASE (SQL Server) or DECODE (Oracle) SQL syntax.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="equalValue"></param>
        /// <param name="trueValue"></param>
        /// <param name="falseValue"></param>
        /// <param name="alias"></param>
        /// <returns></returns>
        public override string GetCaseDecode(string columnName, string equalValue, string trueValue, string falseValue, string alias)
        {
            StringBuilder sb = new();

            sb.AppendFormat("(IF {0} = {1} THEN ", columnName, equalValue);
            sb.AppendFormat("{0}:={1} ", alias, trueValue);
            sb.AppendFormat("ELSE {0}:='{1}')", alias, falseValue);
            return sb.ToString();

        }


        /// <summary>
        /// Get an IsNull (SQLServer) or NVL (Oracle)
        /// </summary>
        /// <param name="validateColumnName"></param>
        /// <param name="optionColumnName"></param>
        /// <returns></returns>
        public override string GetIfNullFunction(string validateColumnName, string optionColumnName) => " NVL(" + validateColumnName + ", " + optionColumnName + ") ";


        /// <summary>
        /// Get a function name for NULL validation
        /// </summary>
        /// <returns></returns>
        public override string GetIfNullFunction() => "NVL";

        /// <summary>
        /// Get a function name that return current date
        /// </summary>
        /// <returns></returns>
        public override string GetCurrentDateFunction() => "SYSDATE";

        /// <summary>
        /// Get a database specific date only SQL syntax.
        /// </summary>
        /// <param name="dateColumn"></param>
        /// <returns></returns>
        public override string GetDateOnlySqlSyntax(string dateColumn) => "TO_CHAR(" + dateColumn + ", 'MM/DD/YYYY')";

        /// <summary>
        /// Get a database specific syntax that converts string to date.
        /// Oracle does not convert date string to date implicitly like SQL Server does
        /// when there is a date comparison.
        /// </summary>
        /// <param name="dateString"></param>
        /// <returns></returns>
        public override string GetStringToDateSqlSyntax(string dateString) => String.Format("TO_DATE('{0}', 'MM/DD/YYYY')", dateString);

        /// <summary>
        /// Get a database specific syntax that converts string to date.
        /// Oracle does not convert date string to date implicitly like SQL Server does
        /// when there is a date comparison.
        /// </summary>
        /// <param name="dateSQL"></param>
        /// <returns></returns>
        public override string GetStringToDateSqlSyntax(DateTime dateSQL) => String.Format("TO_DATE('{0}', 'MM/DD/YYYY')", dateSQL.ToString("MM/dd/yyyy"));


        /// <summary>
        /// Gets  date part(Day, month or year) of date
        /// </summary>
        /// <param name="datestring"></param>
        /// <param name="dateFormat"></param>
        /// <param name="datePart"></param>
        /// <returns></returns>
        public override string GetDatePart(string datestring, DateFormat dateFormat, DatePart datePart)
        {
            string datePartstring = string.Empty;
            switch (datePart)
            {
                case DatePart.DAY:
                    datePartstring = $"Day({datestring})";
                    break;
                case DatePart.MONTH:
                    datePartstring = $"Month({datestring})";
                    break;
                case DatePart.YEAR:
                    datePartstring = $"Year({datestring})";
                    break;
            }
            return datePartstring;
        }

        /// <summary>
        /// Convert a datestring to datetime when used for between.... and 
        /// </summary>
        /// <param name="datestring">string</param>
        /// <param name="dateFormat">DatabaseObject.DateFormat</param>
        /// <returns></returns>
        public override string ToDate(string datestring, DateFormat dateFormat) => __singleQuote + datestring + __singleQuote;
        /// <summary>
        /// Converts a database type name to a system type.
        /// </summary>
        /// <param name="dbTypeName">Name of the db type.</param>
        /// <returns>
        /// System.Type
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Type ToSystemType(string oracleType)
        {
            Dictionary<string, Type> typeMap = new Dictionary<string, Type>()
        {
            { "VARCHAR2", typeof(string) },
            { "CHAR", typeof(string) },
            { "NCHAR", typeof(string) },
            { "NVARCHAR2", typeof(string) },
            { "CLOB", typeof(string) },
            { "NCLOB", typeof(string) },
            { "NUMBER", typeof(decimal) },
            { "BINARY_FLOAT", typeof(float) },
            { "BINARY_DOUBLE", typeof(double) },
            { "DATE", typeof(DateTime) },
            { "TIMESTAMP", typeof(DateTime) }
        };

            if (typeMap.ContainsKey(oracleType.ToUpper()))
            {
                return typeMap[oracleType.ToUpper()];
            }
            else
            {
                throw new ArgumentException("Oracle data type not supported or recognized.");
            }
        }
        public override DbTypeSupported ToDbSupportedType(string oracleType)
        {
            Dictionary<string, DbTypeSupported> typeMap = new()
        {
            { "VARCHAR2", DbTypeSupported.dbVarChar },
            { "CHAR", DbTypeSupported.dbChar },
            { "NCHAR", DbTypeSupported.dbNVarChar },
            { "NVARCHAR2", DbTypeSupported.dbNVarChar},
            { "CLOB", DbTypeSupported.dbVarChar },
            { "NCLOB",DbTypeSupported.dbNVarChar },
            { "NUMBER", DbTypeSupported.dbDecimal },
            { "BINARY_FLOAT", DbTypeSupported.dbDouble },
            { "BINARY_DOUBLE", DbTypeSupported.dbDouble },
            { "DATE", DbTypeSupported.dbDateTime },
            { "TIMESTAMP", DbTypeSupported.dbDateTime }
        };

            if (typeMap.ContainsKey(oracleType.ToUpper()))
            {
                return typeMap[oracleType.ToUpper()];
            }
            else
            {
                throw new ArgumentException("Oracle data type not supported or recognized.");
            }
        }

        #endregion



        #endregion Other Override Methods
    }
}