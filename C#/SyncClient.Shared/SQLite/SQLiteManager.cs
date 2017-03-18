using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Synchronization.ClientServices.Common;
using Microsoft;
#if ( WINDOWS_PHONE || NETFX_CORE) 
using Windows.Storage;
using Windows.Storage.Streams;
#endif

using System.Reflection;
using System.Globalization;
using SQLitePCL;
using SQLitePCL.pretty;

namespace Microsoft.Synchronization.ClientServices.SQLite
{
    internal class SQLiteManager
    {
        private OfflineSchema schema;
        private SQLiteHelper sqliteHelper;
        private String localFilePath;

        /// <summary>
        /// The set of entitiesChanges that have been sent to the server but for which there has been
        /// no response received yet.  The guid is the unique identifier passed in to the GetChanges
        /// method and is used in the event that there are multiple batches uploaded before a
        /// success response is received (in case queued upload is ever implemented).
        /// </summary>
        private Dictionary<Guid, IEnumerable<SQLiteOfflineEntity>> sentChangesAwaitingResponse;

        private Dictionary<string, TableMapping> mappings;

        /// <summary>
        /// Ctor
        /// </summary>
        public SQLiteManager(OfflineSchema schema, String localfilePath)
        {
            this.schema = schema;

            this.localFilePath = localfilePath;

            this.sqliteHelper = new SQLiteHelper(this.localFilePath, this);
        }

        public TableMapping GetMapping<T>()
        {
            return GetMapping(typeof(T));
        }

        public TableMapping GetMapping(Type type)
        {
            if (mappings == null)
                mappings = new Dictionary<string, TableMapping>();

            TableMapping map;

            if (!mappings.TryGetValue(type.FullName, out map))
            {
                map = new TableMapping(type);
                mappings[type.FullName] = map;
            }
            return map;
        }
   
        internal bool ScopeTableExist()
        {
            using (var connection = SQLitePCL.pretty.SQLite3.Open(localFilePath))
            {
                try
                {
                    string tableScope = connection.Query(SQLiteConstants.ScopeExist).Select(r => r[0].ToString()).FirstOrDefault();
                    return tableScope == "ScopeInfoTable";

                }
                catch (Exception)
                {
                    return false;
                }

            }


        }

        /// <summary>
        /// Create Table
        /// </summary>
        internal void CreateTable(Type table)
        {
            this.sqliteHelper.CreateTable(table);
        }

        /// <summary>
        /// Create the scope info table
        /// </summary>
        internal void CreateScopeTable()
        {
            this.sqliteHelper.CreateTable(typeof(ScopeInfoTable));
        }

        /// <summary>
        /// Read Internal Configuration
        /// </summary>
        internal SQLiteConfiguration ReadConfiguration(string databaseScopeName)
        {
            SQLiteConfiguration configuration = new SQLiteConfiguration();

            using (var connection = SQLitePCL.pretty.SQLite3.Open(localFilePath))
            {
                string s = null;
                List<String> t = new List<string>();
                bool scopeInfoTableFounded;
                DateTime d = new DateTime(1900, 1, 1);
                Byte[] blob = null;
                try
                {

                    string name = databaseScopeName;

                    ScopeInfoTable scopeInfoTable = null;
                    // Check if Scope Table Exist
                    string tableScope = connection.Query(SQLiteConstants.ScopeExist).Select(r => r[0].ToString()).FirstOrDefault();

                    bool scopeTableExist = tableScope == "ScopeInfoTable";

                    if (scopeTableExist)
                    {
                        String commandSelect = "Select * From ScopeInfoTable Where ScopeName = ?;";
                        foreach(var row in connection.Query(commandSelect, name))
                        {
                            scopeInfoTable = new ScopeInfoTable();
                            scopeInfoTable.ScopeName = row[0].ToString();//(String)SQLiteHelper.ReadCol(stmtSelect, 0, typeof(String));
                            scopeInfoTable.ServiceUri = row[1].ToString();//(String)SQLiteHelper.ReadCol(stmtSelect, 1, typeof(String));
                            scopeInfoTable.LastSyncDate = row[2].ToDateTime();//(DateTime)SQLiteHelper.ReadCol(stmtSelect, 2, typeof(DateTime));
                            scopeInfoTable.AnchorBlob = row[3].ToBlob();// (Byte[])SQLiteHelper.ReadCol(stmtSelect, 3, typeof(Byte[]));
                            scopeInfoTable.Configuration = row[4].ToString();//(String)SQLiteHelper.ReadCol(stmtSelect, 4, typeof(String));
                        }
                    }


                    if (scopeInfoTable == null)
                        return null;

                    XDocument document = XDocument.Parse(scopeInfoTable.Configuration);

                    s = scopeInfoTable.ServiceUri;

                    t = (from tt in document.Descendants()
                         where tt.Name == "Types"
                         select tt.Value).ToList();

                    d = scopeInfoTable.LastSyncDate;

                    blob = scopeInfoTable.AnchorBlob;

                    scopeInfoTableFounded = true;


                }
                catch
                {
                    scopeInfoTableFounded = false;
                }

                if (!scopeInfoTableFounded)
                    return null;

                // Configure Configuration en return it
                configuration.ScopeName = databaseScopeName;
                configuration.ServiceUri = new Uri(s);
                configuration.Types = t;
                configuration.LastSyncDate = d;
                configuration.AnchorBlob = blob;


            }

            return configuration;



        }

        /// <summary>
        /// Save the configuration of the Sync SQLite Database
        /// </summary>
        /// <param name="configuration">Sync Configuration </param>
        internal void SaveConfiguration(SQLiteConfiguration configuration)
        {
            XElement xScopeInfoTable = new XElement("ScopeInfoTable");

            // Create Types xml doc.
            foreach (var t in configuration.Types)
                xScopeInfoTable.Add(new XElement("Types", t));

            XDocument doc = new XDocument(xScopeInfoTable);

            var scopeInfoTable = new ScopeInfoTable
            {
                ScopeName = configuration.ScopeName,
                ServiceUri = configuration.ServiceUri.AbsoluteUri,
                Configuration = doc.ToString(),
                AnchorBlob = configuration.AnchorBlob,
                LastSyncDate = configuration.LastSyncDate
            };

            // Saving Configuration
            using (var connection = SQLitePCL.pretty.SQLite3.Open(localFilePath))
            {
                try
                {
                    string tableScope = connection.Query(SQLiteConstants.ScopeExist).Select(r => r[0].ToString()).FirstOrDefault();

                    bool scopeTableExist = tableScope == "ScopeInfoTable";

                    if (scopeTableExist)
                    {
                        String commandSelect = "Select * From ScopeInfoTable Where ScopeName = ?;";
                        var exist = connection.Query(commandSelect, configuration.ScopeName).Select(r => true).Any();

                        string stmtText = exist
                            ? "Update ScopeInfoTable Set ServiceUri = ?, LastSyncDate = ?, Configuration = ?, AnchorBlob = ? Where ScopeName = ?;"
                            : "Insert into ScopeInfoTable (ServiceUri, LastSyncDate, Configuration, AnchorBlob, ScopeName) Values (?, ?, ?, ?, ?);";

                        connection.Execute(stmtText, scopeInfoTable.ServiceUri, scopeInfoTable.LastSyncDate, scopeInfoTable.Configuration, scopeInfoTable.AnchorBlob, scopeInfoTable.ScopeName);
                    }
                }
                catch (Exception ex)
                {

                    throw new Exception("Impossible to save Sync Configuration", ex);
                }
            }
        }

        /// <summary>
        /// Returns the number of changes that exist currently
        /// </summary>
        /// <param name="state"></param>
        /// <param name="lastSyncDate"></param>
        /// <returns></returns>
        internal long GetChangeCount(Guid state, DateTime lastSyncDate)
        {
            return this.sqliteHelper.GetChangeCount(this.schema, lastSyncDate);
        }

        /// <summary>
        /// Get Change
        /// </summary>
        /// <param name="state">A Guid made to identify the sync process uniquely</param>
        /// <param name="lastSyncDate">Last Sync Date </param>
        /// <param name="uploadBatchSize"></param>
        /// <returns></returns>
        internal IEnumerable<IOfflineEntity> GetChanges(Guid state, DateTime lastSyncDate, int uploadBatchSize)
        {
            IEnumerable<SQLiteOfflineEntity> getChanges = this.sqliteHelper.GetChanges(this.schema, lastSyncDate, uploadBatchSize);

            // Save all the Reference. 
            // If there is a problem, rollback all informations on thoses Entities
            if (sentChangesAwaitingResponse == null)
                sentChangesAwaitingResponse = new Dictionary<Guid, IEnumerable<SQLiteOfflineEntity>>();

            sentChangesAwaitingResponse[state] = getChanges;


            return getChanges;
        }

        /// <summary>
        /// After an upload is successed. 
        /// * Delete tracking lines where IsTombstone=1
        /// * Update tracking lines where IsDirty = 1 (to 0)
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Conflict> UploadSucceeded(Guid state)
        {
            var entities = sentChangesAwaitingResponse[state];

            // Get all entities Tombstoned
            var groupTombstoned = from e in entities
                                  where e.ServiceMetadata.IsTombstone
                                  group e by e.GetType()
                                      into entitiesTombstone
                                      select new { Type = entitiesTombstone.Key, Entities = entitiesTombstone };

            // For each Type, delete tombstone in tracking tables
            foreach (var groupedEntities in groupTombstoned)
                this.sqliteHelper.DeleteTombstoneTrackingEntities(groupedEntities.Type, groupedEntities.Entities.ToList());

            // Get all entities Tombstoned
            var groupDirty = from e in entities
                             where !e.ServiceMetadata.IsTombstone
                             group e by e.GetType()
                                 into entitiesTombstone
                                 select new { Type = entitiesTombstone.Key, Entities = entitiesTombstone };

            // For each Type, update tracking table
            foreach (var groupedEntities in groupDirty)
                this.sqliteHelper.UpdateDirtyTrackingEntities(groupedEntities.Type, groupedEntities.Entities.ToList());


            // Don't need the sent entitiesChanges anymore
            sentChangesAwaitingResponse.Remove(state);

            return new List<Conflict>();

        }

        /// <summary>
        /// After downloaded entities from Server, Save them
        /// </summary>
        internal void SaveDownloadedChanges(IEnumerable<SQLiteOfflineEntity> entities)
        {
            if (entities == null || !entities.Any()) return;

            // Save all the table
            var group = from e in entities
                        group e by e.GetType()
                            into entitiesPerGroup
                            select new { Type = entitiesPerGroup.Key, Entities = entitiesPerGroup };

            foreach (var groupedEntities in @group)
                this.sqliteHelper.MergeEntities(groupedEntities.Type, groupedEntities.Entities.ToList());
        }
    }

    public class TableMapping
    {
        public Type MappedType { get; private set; }

        public string TableName { get; private set; }

        public Column[] Columns { get; private set; }

        public Column[] PrimaryKeys { get; private set; }

        public string GetByPrimaryKeySql { get; private set; }

        public string GetPrimaryKeysWhereClause { get; private set; }

        readonly Column autoPk;
        Column[] insertColumns;
        Column[] insertOrReplaceColumns;

        public TableMapping(Type type)
        {
            MappedType = type;

#if NETFX_CORE
            var tableAttr = (TableAttribute)type.GetTypeInfo().GetCustomAttribute(typeof(TableAttribute), true);
#else
            var tableAttr = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute), true).FirstOrDefault();
#endif

            TableName = tableAttr != null ? tableAttr.Name : MappedType.Name;

#if !NETFX_CORE
            var props = MappedType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
#else
            var props = from p in MappedType.GetRuntimeProperties()
                        where ((p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic) || (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic))
                        select p;
#endif
            var cols = new List<Column>();
            foreach (var p in props)
            {
#if !NETFX_CORE
                var ignore = p.GetCustomAttributes(typeof(IgnoreAttribute), true).Length > 0;
#else
                var ignore = p.GetCustomAttributes(typeof(IgnoreAttribute), true).Any();
#endif
                if (p.CanWrite && !ignore)
                {
                    cols.Add(new Column(p));
                }
            }
            Columns = cols.ToArray();
            var pks = new List<Column>();
            foreach (var c in Columns)
            {
                if (c.IsAutoInc && c.IsPK)
                {
                    autoPk = c;
                }
                if (c.IsPK)
                {
                    pks.Add(c);
                }
            }

            HasAutoIncPK = autoPk != null;

            if (pks.Count > 0)
            {
                PrimaryKeys = pks.ToArray();
                var declWhere = PrimaryKeys.Select(primaryKey => String.Format("\"{0}\" = ? ", primaryKey.Name)).ToList();
                GetPrimaryKeysWhereClause = String.Join(" and ", declWhere);
                GetByPrimaryKeySql = string.Format("select * from \"{0}\" where {1}", TableName, GetPrimaryKeysWhereClause);
            }
            else
            {
                // People should not be calling Get/Find without a PK
                GetByPrimaryKeySql = string.Format("select * from \"{0}\" limit 1", TableName);
            }
        }

        public bool HasAutoIncPK { get; private set; }

        public void SetAutoIncPK(object obj, long id)
        {
            if (autoPk != null)
            {
                autoPk.SetValue(obj, Convert.ChangeType(id, autoPk.ColumnType, CultureInfo.InvariantCulture));
            }
        }

        public Column[] InsertColumns
        {
            get
            {
                if (insertColumns == null)
                    insertColumns = Columns.Where(c => !c.IsAutoInc).ToArray();

                return insertColumns;
            }
        }

        public Column[] InsertOrReplaceColumns
        {
            get
            {
                if (insertOrReplaceColumns == null)
                    insertOrReplaceColumns = Columns.ToArray();

                return insertOrReplaceColumns;
            }
        }

        public Column FindColumnWithPropertyName(string propertyName)
        {
            var exact = Columns.FirstOrDefault(c => c.PropertyName == propertyName);
            return exact;
        }

        public Column FindColumn(string columnName)
        {
            var exact = Columns.FirstOrDefault(c => c.Name == columnName);
            return exact;
        }



        protected internal void Dispose()
        {
        }

        public class Column
        {
            PropertyInfo _prop;

            public string Name { get; private set; }

            public string PropertyName { get { return _prop.Name; } }

            public Type ColumnType { get; private set; }

            public string Collation { get; private set; }

            public bool IsAutoInc { get; private set; }
            public bool IsAutoGuid { get; private set; }

            public bool IsPK { get; private set; }

            public IEnumerable<IndexedAttribute> Indices { get; set; }

            public bool IsNullable { get; private set; }

            public int MaxStringLength { get; private set; }

            public Column(PropertyInfo prop)
            {
                var colAttr = (ColumnAttribute)prop.GetCustomAttributes(typeof(ColumnAttribute), true).FirstOrDefault();

                _prop = prop;
                Name = colAttr == null ? prop.Name : colAttr.Name;
                //If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T, otherwise it returns null, so get the actual type instead
                ColumnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                Collation = Orm.Collation(prop);

                IsPK = Orm.IsPK(prop);

                var isAuto = Orm.IsAutoInc(prop);
                IsAutoGuid = isAuto && ColumnType == typeof(Guid);
                IsAutoInc = isAuto && !IsAutoGuid;

                Indices = Orm.GetIndices(prop);
                if (!Indices.Any()
                    && !IsPK
                    && Name.EndsWith(Orm.ImplicitIndexSuffix, StringComparison.OrdinalIgnoreCase)
                    )
                {
                    Indices = new[] { new IndexedAttribute() };
                }
                IsNullable = !IsPK;
                MaxStringLength = Orm.MaxStringLength(prop);
            }

            public void SetValue(object obj, object val)
            {
                _prop.SetValue(obj, val, null);
            }

            public object GetValue(object obj)
            {
                return _prop.GetValue(obj, null);
            }
        }
    }

    public static class Orm
    {
        public const int DefaultMaxStringLength = 140;
        public const string ImplicitPkName = "Id";
        public const string ImplicitIndexSuffix = "Id";

        public static string SqlDecl(TableMapping.Column p, bool storeDateTimeAsTicks)
        {
            string decl = "\"" + p.Name + "\" " + SqlType(p) + " ";

            if (p.IsPK)
            {
                decl += "primary key ";
            }
            if (p.IsAutoInc)
            {
                decl += "autoincrement ";
            }
            if (!p.IsNullable)
            {
                decl += "not null ";
            }
            if (!string.IsNullOrEmpty(p.Collation))
            {
                decl += "collate " + p.Collation + " ";
            }

            return decl;
        }

        public static string SqlType(TableMapping.Column p)
        {
            var clrType = p.ColumnType;
            if (clrType == typeof(Boolean) || clrType == typeof(Byte) || clrType == typeof(UInt16) || clrType == typeof(SByte) || clrType == typeof(Int16) || clrType == typeof(Int32))
            {
                return "integer";
            }
            if (clrType == typeof(UInt32) || clrType == typeof(Int64))
            {
                return "bigint";
            }
            if (clrType == typeof(Single) || clrType == typeof(Double) || clrType == typeof(Decimal))
            {
                return "float";
            }
            if (clrType == typeof(String))
            {
                int len = p.MaxStringLength;
                return "varchar(" + len + ")";
            }
            if (clrType == typeof(DateTimeOffset))
            {
                return "datetime";
            }
            if (clrType == typeof(DateTime))
            {
                return "datetime";
#if !NETFX_CORE
            }
            if (clrType.IsEnum)
            {
#else
            }
            if (clrType.GetTypeInfo().IsEnum)
            {
#endif
                return "integer";
            }
            if (clrType == typeof(TimeSpan))
            {
                return "bigint";
            }
            if (clrType == typeof(byte[]))
            {
                return "blob";
            }
            if (clrType == typeof(Guid))
            {
                return "varchar(36)";
            }
            throw new NotSupportedException("Don't know about " + clrType);
        }

        public static bool IsPK(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
            return attrs.Any();
#endif
        }

        public static string Collation(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(CollationAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0)
            {
                return ((CollationAttribute)attrs[0]).Value;
#else
            if (attrs.Any())
            {
                return ((CollationAttribute)attrs.First()).Value;
#endif
            }
            return string.Empty;
        }

        public static bool IsAutoInc(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(AutoIncrementAttribute), true);
#if !NETFX_CORE
            return attrs.Length > 0;
#else
            return attrs.Any();
#endif
        }

        public static IEnumerable<IndexedAttribute> GetIndices(MemberInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(IndexedAttribute), true);
            return attrs.Cast<IndexedAttribute>();
        }

        public static int MaxStringLength(PropertyInfo p)
        {
            var attrs = p.GetCustomAttributes(typeof(MaxLengthAttribute), true);
#if !NETFX_CORE
            if (attrs.Length > 0)
                return ((MaxLengthAttribute)attrs[0]).Value;
#else
            if (attrs.Any())
                return ((MaxLengthAttribute)attrs.First()).Value;
#endif

            return DefaultMaxStringLength;
        }
    }
}
