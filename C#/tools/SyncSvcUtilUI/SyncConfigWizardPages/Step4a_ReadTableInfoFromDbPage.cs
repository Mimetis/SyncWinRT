// Copyright 2010 Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License"); 
// You may not use this file except in compliance with the License. 
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 

// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, 
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR 
// CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, 
// MERCHANTABLITY OR NON-INFRINGEMENT. 

// See the Apache 2 License for the specific language governing 
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Synchronization.ClientServices.Configuration;
using System.Data.SqlClient;
using Microsoft.Synchronization.Data.SqlServer;
using Microsoft.Synchronization.Data;

namespace SyncSvcUtilUI.SyncConfigWizardPages
{
    public partial class Step4a_ReadTableInfoFromDbPage : Form
    {
        SyncScopeConfigElement selectedScope = null;
        DbSyncTableDescription tableDesc;
        const string AndFilterClauseFormat = " AND [side].{0} = {1}";
        const string FilterClauseFormat = "[side].{0} = {1}";
        public Step4a_ReadTableInfoFromDbPage(int index)
        {
            InitializeComponent();
            statusLbl.Visible = false;
            selectedScope = WizardHelper.Instance.SyncConfigSection.SyncScopes.GetElementAt(index);
            this.Init();
        }

        private void Init()
        {
            this.dbsComboBox.Items.Clear();
            foreach (TargetDatabaseConfigElement db in WizardHelper.Instance.SyncConfigSection.Databases)
            {
                this.dbsComboBox.Items.Add(db.Name);
            }

            this.dbsComboBox.SelectedIndex = -1;
        }

        private void dbsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.tablesBox.Items.Clear();
            this.colsView.Rows.Clear();
            this.filterClauseTxtBox.Text = string.Empty;

            TargetDatabaseConfigElement db = WizardHelper.Instance.SyncConfigSection.Databases.GetElementAt(this.dbsComboBox.SelectedIndex);
            statusLbl.Visible = true;

            try
            {
                // Issue a query to get list of all tables
                using (SqlConnection conn = new SqlConnection(db.GetConnectionString()))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(WizardHelper.SELECT_TABLENAMES_QUERY, conn);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string schema = reader.GetString(0);
                            string tableName = reader.GetString(1);
                            string fullName = string.Format("{0}.{1}", schema, tableName);
                            string quotedTableName = string.Format("[{0}]", tableName);
                            string quotedFullName = string.Format("[{0}].[{1}]", schema, tableName);

                            if (IsTableNameValid(tableName))
                            {
                                if (schema.Equals("dbo", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.tablesBox.Items.Add(quotedTableName,
                                        (selectedScope.SyncTables.GetElement(quotedTableName) != null ||
                                         selectedScope.SyncTables.GetElement(tableName) != null));
                                }
                                else
                                {
                                    this.tablesBox.Items.Add(quotedFullName,
                                        selectedScope.SyncTables.GetElement(quotedFullName) != null ||
                                        selectedScope.SyncTables.GetElement(fullName) != null);
                                }

                            }
                        }
                    }
                }
            }
            catch (SqlException exp)
            {
                MessageBox.Show("Error in querying database. " + exp.Message, "Target Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                statusLbl.Visible = false;
            }
        }

        /// <summary>
        /// Returns whether a table is valid or not
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private bool IsTableNameValid(string tableName)
        {
            tableName = tableName.ToLowerInvariant();

            return (WizardHelper.SYNC_TABLE_NAMES.Where
                (e => e.Equals(tableName, StringComparison.Ordinal)).FirstOrDefault() == null &&
                !tableName.EndsWith("_tracking"));
        }

        private void tablesBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (tablesBox.SelectedIndex > -1)
            {
                this.colsView.Rows.Clear();
                string tableName = this.tablesBox.SelectedItem.ToString();

                if (e.NewValue == CheckState.Unchecked)
                {
                    // Remove it from the SyncTables collection
                    selectedScope.SyncTables.Remove(tableName);

                    this.filterClauseTxtBox.Text = string.Empty;
                }
                else if (e.NewValue == CheckState.Checked)
                {
                    SyncTableConfigElement table = new SyncTableConfigElement();
                    table.Name = tableName;
                    table.IncludeAllColumns = true;

                    TargetDatabaseConfigElement db = WizardHelper.Instance.SyncConfigSection.Databases.GetElementAt(this.dbsComboBox.SelectedIndex);
                    statusLbl.Visible = true;

                    try
                    {
                        tableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable(tableName, new SqlConnection(db.GetConnectionString()));

                        // Issue a query to get list of all tables
                        foreach (DbSyncColumnDescription col in tableDesc.Columns)
                        {
                            SyncColumnConfigElement colConfig = new SyncColumnConfigElement()
                            {
                                Name = col.UnquotedName,
                                IsPrimaryKey = col.IsPrimaryKey,
                                IsNullable = col.IsNullable,
                                SqlType = col.Type,
                            };
                            table.SyncColumns.Add(colConfig);
                        }
                        this.DisplaySyncTableDetails(table);
                    }
                    catch (SqlException exp)
                    {
                        MessageBox.Show("Error in querying database. " + exp.Message, "Target Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        statusLbl.Visible = false;
                    }
                    // Add it to the sync table list
                    selectedScope.SyncTables.Add(table);
                }
            }
        }

        private void DisplaySyncTableDetails(SyncTableConfigElement table)
        {
            foreach (DbSyncColumnDescription colDesc in tableDesc.Columns)
            {
                SyncColumnConfigElement colConfig = table.SyncColumns.GetElement(colDesc.UnquotedName);
                if (colConfig == null)
                {
                    colsView.Rows.Add(colDesc.UnquotedName, false, false);
                }
                else
                {
                    colsView.Rows.Add(colConfig.Name, true, // IsSync
                        table.FilterColumns.GetElement(colConfig.Name) != null); // IsFilterCol
                }

                DataGridViewRow row = colsView.Rows[colsView.Rows.Count - 1];
                if (colConfig != null && colConfig.IsPrimaryKey)
                {
                    // Freeze the row if its a primary key
                    row.ReadOnly = true;
                }

                row.Cells[2].ReadOnly = colConfig == null;
            }
            this.filterClauseTxtBox.Text = table.FilterClause;
        }

        /// <summary>
        /// Called whenever a checkbox from the list of columns (sync/filter) is checked or unchecked
        /// If Column index is 1 then its a sync column
        ///     If column is removed then remove from sync columns collection. Also disable the column if it was a filter column.
        ///         Disable the filter col checkbox so it cannot be added as a filter without adding it as a sync column first
        ///     If column is enabled then add it to sync columns collection. Enable filter col checkbox so it can be clicked
        /// If Column index is 2 then its a filter column.
        ///     If filter col is disabled then remove the column from filter params/clause/columns collection
        ///     If filter col is enabled then add the column to the filter params/clause/columns collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void colsView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            SyncTableConfigElement table = selectedScope.SyncTables.GetElement(this.tablesBox.SelectedItem.ToString());

            if (e.ColumnIndex > 0)
            {
                // Check to see if its a column being added/removed from sync
                DataGridViewCheckBoxCell cell = this.colsView.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCheckBoxCell;
                DbSyncColumnDescription col = tableDesc.Columns[e.RowIndex];

                if (e.ColumnIndex == 1)
                {
                    if (cell.Value == null || !(bool)cell.Value)
                    {
                        // Sync column unchecked
                        SyncColumnConfigElement colConfig = table.SyncColumns.GetElement(this.colsView.Rows[e.RowIndex].Cells[0].Value.ToString());
                        if (colConfig != null)
                        {
                            table.SyncColumns.Remove(colConfig.Name);
                            this.RemoveFilterColumnInfo(table, colConfig);
                            this.colsView.Rows[e.RowIndex].Cells[2].Value = false;
                            // Make filter col readonly
                            this.colsView.Rows[e.RowIndex].Cells[2].ReadOnly = true;
                        }
                    }
                    else if (table.SyncColumns.GetElement(col.UnquotedName) == null)
                    {
                        // Sync column is checked. Add it back to the SyncColumn list
                        SyncColumnConfigElement colConfig = new SyncColumnConfigElement()
                        {
                            Name = col.UnquotedName,
                            IsPrimaryKey = false,
                            IsNullable = col.IsNullable,
                            SqlType = col.Type,
                        };
                        table.SyncColumns.Add(colConfig);
                        // Set the filter col to enabled.
                        this.colsView.Rows[e.RowIndex].Cells[2].ReadOnly = false;
                    }
                    table.IncludeAllColumns = table.SyncColumns.Count == colsView.Rows.Count;
                }
                else
                {
                    // Its a filter column
                    SyncColumnConfigElement colConfig = table.SyncColumns.GetElement(colsView.Rows[e.RowIndex].Cells[0].Value.ToString());
                    if (colConfig != null)
                    {
                        string filterParamName = "@" + WizardHelper.SanitizeName(colConfig.Name);
                        string andFilterClause = string.Format(AndFilterClauseFormat, colConfig.Name, filterParamName);
                        string FilterClause = string.Format(FilterClauseFormat, colConfig.Name, filterParamName);

                        if (cell.Value != null && !(bool)cell.Value)
                        {
                            // Filter column unchecked
                            this.RemoveFilterColumnInfo(table, colConfig);
                        }
                        else if(table.FilterColumns.GetElement(colConfig.Name) == null)
                        {
                            // Add Filter column
                            table.FilterColumns.Add(new FilterColumnConfigElement()
                            {
                                Name = colConfig.Name
                            });

                            // Add Filter parameter
                            table.FilterParameters.Add(new FilterParameterConfigElement()
                            {
                                Name = filterParamName,
                                SqlType = tableDesc.Columns[e.RowIndex].Type,
                            });

                            // Fix by xperiandi, Thks !
                            if ((tableDesc.Columns[e.RowIndex].SizeSpecified))
                            {
                                // Set size
                                DbSyncColumnDescription column = tableDesc.Columns[e.RowIndex];
                                string columnsSize = column.Size;
                                if (string.Compare(columnsSize, "max", StringComparison.OrdinalIgnoreCase) == 0
                                    && (string.Compare(column.Type, "nvarchar", StringComparison.OrdinalIgnoreCase) *
                                        string.Compare(column.Type, "varchar", StringComparison.OrdinalIgnoreCase)) == 0)
                                {
                                    table.FilterParameters.GetElementAt(table.FilterParameters.Count - 1).DataSize = 4000;
                                }
                                else
                                {
                                    table.FilterParameters.GetElementAt(table.FilterParameters.Count - 1).DataSize = int.Parse(columnsSize);
                                }
                            }

                            if (string.IsNullOrEmpty(table.FilterClause))
                            {
                                table.FilterClause = string.Format(FilterClauseFormat, colConfig.Name, filterParamName);
                            }
                            else
                            {
                                table.FilterClause += string.Format(AndFilterClauseFormat, colConfig.Name, filterParamName);
                            }
                        }
                    }

                    this.filterClauseTxtBox.Text = table.FilterClause;
                }
            }
        }

        /// <summary>
        /// Called whenever a FilterColumn is unchecked from the UI. Removed the particular filter from the uber filter clause.
        /// Does the following
        /// 1. Remove from FilterColumns
        /// 2. Remove from FilterParameters
        /// 3. Remove text from FilterClause
        /// </summary>
        /// <param name="table">Table for which the filter param applies to</param>
        /// <param name="colConfig">The actual column being unchecked</param>
        private void RemoveFilterColumnInfo(SyncTableConfigElement table, SyncColumnConfigElement colConfig)
        {
            string filterParamName = "@" + WizardHelper.SanitizeName(colConfig.Name);
            string andFilterClause = string.Format(AndFilterClauseFormat, colConfig.Name, filterParamName);
            string FilterClause = string.Format(FilterClauseFormat, colConfig.Name, filterParamName);

            // Remove from Filter columns
            table.FilterColumns.Remove(colConfig.Name);


            //Remove from Filter parameters
            table.FilterParameters.Remove(filterParamName);

            // Check to see if you can remove the filter clause

            table.FilterClause = table.FilterClause.Replace(andFilterClause, string.Empty);
            table.FilterClause = table.FilterClause.Replace(FilterClause, string.Empty);

            if (table.FilterClause.StartsWith(" AND "))
            {
                table.FilterClause = table.FilterClause.Substring(5);
            }
        }

        private void tablesBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.filterClauseTxtBox.Text = string.Empty;
            SyncTableConfigElement table = selectedScope.SyncTables.GetElement(this.tablesBox.SelectedItem.ToString());
            if (table != null)
            {
                this.colsView.Rows.Clear();
                this.filterClauseTxtBox.Text = table.FilterClause;

                TargetDatabaseConfigElement db = WizardHelper.Instance.SyncConfigSection.Databases.GetElementAt(this.dbsComboBox.SelectedIndex);

                tableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable(table.Name, new SqlConnection(db.GetConnectionString()));

                // Display the list of currently selected items
                this.DisplaySyncTableDetails(table);
            }
            else
            {
                colsView.Rows.Clear();
            }
        }
    }
}
