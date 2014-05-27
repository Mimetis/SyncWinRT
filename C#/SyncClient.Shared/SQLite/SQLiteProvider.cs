using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

#if NETFX_CORE
#elif WINDOWS_PHONE
using Microsoft.Synchronization.ClientServices.WindowsPhone8.Proxy;
#endif


namespace Microsoft.Synchronization.ClientServices.SQLite
{
    internal class SQLiteProvider
    {
        /// <summary>
        /// A singleton instance of the <see cref="SQLite3Provider"/>.
        /// </summary>
        private static SQLiteProvider instance = new SQLiteProvider();

        private SQLiteProvider()
        {
        }

        /// <summary>
        /// A singleton instance of the <see cref="SQLite3Provider"/>.
        /// </summary>
        public static SQLiteProvider Instance
        {
            get
            {
                return instance;
            }
        }

        string GetLocalFilePath(string filename)
        {
            var result = filename;

            if (!Path.IsPathRooted(filename))
            {
                result = Path.Combine(ApplicationData.Current.LocalFolder.Path, filename);
            }
            return result;
        }

        public int Sqlite3Open(IntPtr filename, out IntPtr db)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_open(filename, out db);
#elif WINDOWS_PHONE
            long databasePtr;
            var result = SQLite3RuntimeProvider.sqlite3_open(filename.ToInt64(), out databasePtr);
            db = new IntPtr(databasePtr);
            return result;
#endif
        }

        int SQLiteProvider.Sqlite3CloseV2(IntPtr db)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_close_v2(db);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_close_v2(db.ToInt64());
#endif
        }

        int SQLiteProvider.Sqlite3PrepareV2(IntPtr db, IntPtr sql, int length, out IntPtr stm, IntPtr tail)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_prepare_v2(db, sql, length, out stm, tail);
#elif WINDOWS_PHONE
            long stmPtr;

            var result = SQLite3RuntimeProvider.sqlite3_prepare_v2(db.ToInt64(), sql.ToInt64(), length, out stmPtr, tail.ToInt64());

            stm = new IntPtr(stmPtr);

            return result;
#endif
        }

        IntPtr SQLiteProvider.Sqlite3Errmsg(IntPtr db)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_errmsg(db);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_errmsg(db.ToInt64()));
#endif
        }

        int SQLiteProvider.Sqlite3BindInt(IntPtr stm, int paramIndex, int value)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_int(stm, paramIndex, value);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_int(stm.ToInt64(), paramIndex, value);
#endif
        }

        int SQLiteProvider.Sqlite3BindInt64(IntPtr stm, int paramIndex, long value)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_int64(stm, paramIndex, value);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_int64(stm.ToInt64(), paramIndex, value);
#endif
        }

        int SQLiteProvider.Sqlite3BindText(IntPtr stm, int paramIndex, IntPtr value, int length, IntPtr destructor)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_text(stm, paramIndex, value, length, destructor);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_text(stm.ToInt64(), paramIndex, value.ToInt64(), length, destructor.ToInt64());
#endif
        }

        int SQLiteProvider.Sqlite3BindDouble(IntPtr stm, int paramIndex, double value)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_double(stm, paramIndex, value);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_double(stm.ToInt64(), paramIndex, value);
#endif
        }

        int SQLiteProvider.Sqlite3BindBlob(IntPtr stm, int paramIndex, byte[] value, int length, IntPtr destructor)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_blob(stm, paramIndex, value, length, destructor);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_blob(stm.ToInt64(), paramIndex, value, length, destructor.ToInt64());
#endif
        }

        int SQLiteProvider.Sqlite3BindNull(IntPtr stm, int paramIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_null(stm, paramIndex);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_null(stm.ToInt64(), paramIndex);
#endif
        }

        int SQLiteProvider.Sqlite3BindParameterCount(IntPtr stm)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_parameter_count(stm);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_parameter_count(stm.ToInt64());
#endif
        }

        IntPtr SQLiteProvider.Sqlite3BindParameterName(IntPtr stm, int paramIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_parameter_name(stm, paramIndex);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_bind_parameter_name(stm.ToInt64(), paramIndex));
#endif
        }

        int SQLiteProvider.Sqlite3BindParameterIndex(IntPtr stm, IntPtr paramName)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_bind_parameter_index(stm, paramName);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_bind_parameter_index(stm.ToInt64(), paramName.ToInt64());
#endif
        }

        int SQLiteProvider.Sqlite3Step(IntPtr stm)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_step(stm);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_step(stm.ToInt64());
#endif
        }

        int SQLiteProvider.Sqlite3ColumnInt(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_int(stm, columnIndex);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_column_int(stm.ToInt64(), columnIndex);
#endif
        }

        long SQLiteProvider.Sqlite3ColumnInt64(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_int64(stm, columnIndex);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_column_int64(stm.ToInt64(), columnIndex);
#endif
        }

        IntPtr SQLiteProvider.Sqlite3ColumnText(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_text(stm, columnIndex);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_column_text(stm.ToInt64(), columnIndex));
#endif
        }

        double SQLiteProvider.Sqlite3ColumnDouble(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_double(stm, columnIndex);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_column_double(stm.ToInt64(), columnIndex);
#endif
        }

        IntPtr SQLiteProvider.Sqlite3ColumnBlob(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_blob(stm, columnIndex);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_column_blob(stm.ToInt64(), columnIndex));
#endif
        }

        int SQLiteProvider.Sqlite3ColumnType(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_type(stm, columnIndex);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_column_type(stm.ToInt64(), columnIndex);
#endif
        }

        int SQLiteProvider.Sqlite3ColumnBytes(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_bytes(stm, columnIndex);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_column_bytes(stm.ToInt64(), columnIndex);
#endif
        }

        int SQLiteProvider.Sqlite3ColumnCount(IntPtr stm)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_count(stm);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_column_count(stm.ToInt64());
#endif
        }

        IntPtr SQLiteProvider.Sqlite3ColumnName(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_name(stm, columnIndex);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_column_name(stm.ToInt64(), columnIndex));
#endif
        }

        IntPtr SQLiteProvider.Sqlite3ColumnOriginName(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_origin_name(stm, columnIndex);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_column_origin_name(stm.ToInt64(), columnIndex));
#endif
        }

        IntPtr SQLiteProvider.Sqlite3ColumnTableName(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_table_name(stm, columnIndex);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_column_table_name(stm.ToInt64(), columnIndex));
#endif
        }

        IntPtr SQLiteProvider.Sqlite3ColumnDatabaseName(IntPtr stm, int columnIndex)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_column_database_name(stm, columnIndex);
#elif WINDOWS_PHONE
            return new IntPtr(SQLite3RuntimeProvider.sqlite3_column_database_name(stm.ToInt64(), columnIndex));
#endif
        }

        int SQLiteProvider.Sqlite3Reset(IntPtr stm)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_reset(stm);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_reset(stm.ToInt64());
#endif
        }

        int SQLiteProvider.Sqlite3ClearBindings(IntPtr stm)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_clear_bindings(stm);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_clear_bindings(stm.ToInt64());
#endif
        }

        int SQLiteProvider.Sqlite3Finalize(IntPtr stm)
        {
#if NETFX_CORE
            return NativeMethods.sqlite3_finalize(stm);
#elif WINDOWS_PHONE
            return SQLite3RuntimeProvider.sqlite3_finalize(stm.ToInt64());
#endif
        }
    }
}
