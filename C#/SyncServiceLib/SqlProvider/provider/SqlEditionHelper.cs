// Copyright © Microsoft Corporation. All rights reserved.

// Microsoft Limited Permissive License (Ms-LPL)

// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

// 1. Definitions
// The terms “reproduce,” “reproduction,” “derivative works,” and “distribution” have the same meaning here as under U.S. copyright law.
// A “contribution” is the original software, or any additions or changes to the software.
// A “contributor” is any person that distributes its contribution under this license.
// “Licensed patents” are a contributor’s patent claims that read directly on its contribution.

// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors’ name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// (E) The software is licensed “as-is.” You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
// (F) Platform Limitation- The licenses granted in sections 2(A) & 2(B) extend only to the software or derivative works that you create that run on a Microsoft Windows operating system product.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.Synchronization.Services.SqlProvider
{
    /// <summary>
    /// SqlEdition - enum for different SQL Server versions
    /// </summary>
    internal enum SqlEdition
    {
        /// <summary>
        /// 2008 = Major version 10
        /// </summary>
        Sql2008,
        /// <summary>
        /// 2005 = Major version 9
        /// </summary>
        Sql2005,
        /// <summary>
        /// Azure
        /// </summary>
        SqlAzure
    }

    /// <summary>
    /// SqlEditionHelper
    /// </summary>
    internal static class SqlEditionHelper
    {
        private static string ServerPropertyQuery = "SELECT SERVERPROPERTY('Edition'), SERVERPROPERTY('ProductVersion')";

        internal const int RetryAmount = 5; //Number of retries to attempt when opening a SqlConnection
        internal const int RetryWaitMilliseconds = 100; //The base retry time between retry attempts

        public static ulong GetServerTickCountFromDatabase(SqlConnection connection, SqlTransaction transaction)
        {
            WebUtil.CheckArgumentNull(connection, "connection");

            SqlEdition edition = GetEdition(connection, transaction);

            string commandText = string.Empty;

            switch (edition)
            {
                case SqlEdition.SqlAzure:
                    commandText = "SELECT CAST(get_new_rowversion() AS BIGINT)";
                    break;
                default:
                    commandText = "SELECT CAST(@@DBTS AS BIGINT)";
                    break;
            }

            using (var command = new SqlCommand(commandText, connection, transaction))
            {
                return Convert.ToUInt64(command.ExecuteScalar());
            }
        }

        /// <summary>
        /// Returns the edition of the SQL Server to which connection object is connected
        /// </summary>
        public static SqlEdition GetEdition(SqlConnection connection, SqlTransaction transaction)
        {
            WebUtil.CheckArgumentNull(connection, "connection");

            bool openedConnection = OpenConnection(connection);

            SqlCommand cmd = null;
            SqlDataReader reader = null;
            SqlEdition edition;
            try
            {
                cmd = new SqlCommand(ServerPropertyQuery, connection, transaction);
                reader = cmd.ExecuteReader();
                reader.Read();
                string editionStr = reader.GetString(0);
                string versionStr = reader.GetString(1);

                if (editionStr.Equals("SQL Azure"))
                {
                    edition = SqlEdition.SqlAzure;
                }
                else
                {
                    String[] versionParts = versionStr.Split('.');
                    int majorVersion = Int32.Parse(versionParts[0], CultureInfo.InvariantCulture);
                    if (majorVersion == 9)
                        edition = SqlEdition.Sql2005;
                    else if (majorVersion >= 10)
                        edition = SqlEdition.Sql2008;
                    else
                        throw SyncServiceException.CreateInternalServerError(String.Format("Unsupported Sql Edition {0}", versionStr));
                }

                SyncServiceTracer.TraceInfo("Version of connection detected to be {0}", edition);
            }
            finally
            {
                if (reader != null) { reader.Dispose(); }
                if (cmd != null) { cmd.Dispose(); }
                if (openedConnection) { connection.Close(); }
            }
            return edition;
        }

        /// <summary>
        /// OpenConnection - opens the passed in connection
        /// </summary>
        /// <param name="connection">The connection to open</param>
        /// <returns>True if the connection needed to be opened, False if it was already open</returns>        
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        static internal bool OpenConnection(IDbConnection connection)
        {
            WebUtil.CheckArgumentNull(connection, "connection");

            bool openedConnection = false;

            switch (connection.State)
            {
                case ConnectionState.Open:
                    break;
                case ConnectionState.Broken:
                    SyncServiceTracer.TraceVerbose("Closing broken connection");
                    connection.Close();
                    goto case ConnectionState.Closed;
                case ConnectionState.Closed:
                        if (connection is System.Data.SqlClient.SqlConnection)
                        {
                            // Blank out the password
                            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
                            builder.ConnectionString = connection.ConnectionString;
                            if (!String.IsNullOrEmpty(builder.Password))
                            {
                                builder.Password = "****";
                            }

                            SyncServiceTracer.TraceVerbose("Connecting using string: {0}", builder.ConnectionString);
                        }
                        else
                        {
                            SyncServiceTracer.TraceVerbose("Connecting to database: {0}", connection.Database);
                        }

                    // Check for SqlConnection
                    if (connection is SqlConnection)
                    {
                        TryOpenConnection(connection);
                    }
                    else
                    {
                        connection.Open();
                    }

                    openedConnection = true;
                    break;
                default:
                    throw SyncServiceException.CreateInternalServerError(String.Format("Unhandled ConnectionState {0}", connection.State));
            }
            return openedConnection;
        }

        static internal void TryOpenConnection(IDbConnection connection)
        {
            // This is the retry loop, handling the retries session
            // is done in the catch for performance reasons
            // RetryAmount + 1 because it's initial attempt + RetryAmount (5)
            for (int attempt = 0; attempt < RetryAmount + 1; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        SyncServiceTracer.TraceInfo("Retrying opening connection, attempt {0} of {1}.", attempt, RetryAmount);
                    }

                    // Open the connection
                    connection.Open();

                    // Test the open connection
                    SqlConnection sqlConn = connection as SqlConnection;
                    if (sqlConn != null)
                    {
                        using (SqlCommand sqlCommand = new SqlCommand("Select 1", sqlConn))
                        {
                            sqlCommand.ExecuteScalar();
                            // Connection is valid, return successful
                            return;
                        }
                    }
                    else
                    {
                        //Don't test the injected connection
                        return;
                    }
                }
                catch (SqlException sqlException)
                {
                    // Throw Error if we have reach the maximum number of retries
                    if (attempt == RetryAmount)
                    {
                        SyncServiceTracer.TraceError("Open connection failed after max retry attempts, due to exception: {0}", sqlException.Message);
                        throw;
                    }

                    // Determine if we should retry or abort.
                    if (!RetryLitmus(sqlException))
                    {
                        SyncServiceTracer.TraceError("Open connection failed on attempt {0} of {1}, due to unretryable exception: {2}",
                            attempt + 1, RetryAmount, sqlException.Message);
                        throw;
                    }
                    else
                    {
                        SyncServiceTracer.TraceWarning("Open connection failed on attempt {0} of {1}, due to retryable exception: {2}",
                            attempt + 1, RetryAmount, sqlException.Message);
                        // Backoff Throttling
                        Thread.Sleep(RetryWaitMilliseconds * (int)Math.Pow(2, attempt));
                    }
                }
            }
        }

        /// <summary>
        /// Determine from the exception if the execution of the connection should be attempted again.  
        /// The 3 error codes below are the only exceptions listed as retryable(when opening a connection) 
        /// in the Sql Azure guidelines listed here: 
        /// http://blogs.msdn.com/b/sqlazure/archive/2010/05/11/10011247.aspx
        /// </summary>
        /// <param name="sqlException">SqlException to parse</param>
        /// <returns>True if a a retry is needed, false if not</returns>
        static internal bool RetryLitmus(SqlException sqlException)
        {
            switch (sqlException.Number)
            {
                // The service has encountered an error
                // processing your request. Please try again.
                // Error code %d.
                case 40197:
                // The service is currently busy. Retry
                // the request after 10 seconds. Code: %d.
                case 40501:
                // Database XXXX on server YYYY is not currently 
                // available. Please retry the connection later. If the 
                // problem persists, contact customer support, and 
                // provide them the session tracing ID of {GUID}
                case 40613:
                //A transport-level error has occurred when
                // receiving results from the server. (provider:
                // TCP Provider, error: 0 - An established connection
                // was aborted by the software in your host machine.)               
                case 10053:
                case 64://same as 10053
                // A transport-level error has occurred when sending
                // the request to the server. An existing connection
                // was forcibly closed by the remote host.
                case 10054:
                // A network-related or instance-specific error 
                // occurred while establishing a connection to SQL Server.  
                // A connection attempt failed because the connected party 
                // did not properly respond after a period of time, or 
                // established connection failed because connected host 
                // has failed to respond.
                case 10060:
                    return true;
            }

            return false;
        }
    }
}
