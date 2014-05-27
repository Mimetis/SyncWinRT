<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
>
  <xsl:output method="html" indent="yes"/>

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="root">
    <html xmlns="http://www.w3.org/1999/xhtml">
      <head runat="server">
        <title>Diagnostics Dashboard</title>

        <style>
          .columnDescription
          {
          width: 350px;
          padding-left: 5px;
          }

          .columnStatus
          {
          width: 150px;
          padding-left: 5px;
          }

          .columnComments
          {
          width: 500px;
          padding-left: 5px;
          }

          body
          {
          font-family: Segoe UI, Verdana;
          font-size: medium;
          }

          table { empty-cells:show; }
        </style>
      </head>

      <body>
        <h1>Diagnostics</h1>
        <table cellpadding="0" cellspacing="0" border="1">
          <thead>
            <tr>
              <td class="columnDescription"><b>Diagnostic Test</b></td>
              <td class="columnStatus"><b>Result</b></td>
              <td class="columnComments"><b>Comments</b></td>
            </tr>
          </thead>
          
          <!-- SyncFx core -->
          <tr>
            <xsl:variable name="SyncFxCore" select="SyncFxCore/Result" />
            <xsl:variable name="ErrorInfo" select="SyncFxCore/ErrorInfo" />
            
            <td class="columnDescription">Sync Framework Runtime</td>
            <td class="columnStatus">
              <xsl:if test="$SyncFxCore = 'SUCCESS'">
                <font color="green">PASSED</font>
              </xsl:if>
              <xsl:if test="$SyncFxCore = 'SYNC_FX_CORE_ERROR'">
                <font color="red">FAILED.</font>
              </xsl:if>
              <xsl:if test="$SyncFxCore = 'UNKNOWN_ERROR'">
                <font color="red">UNKNOWN ERROR</font>
              </xsl:if>
            </td>
            <td class="columnComments">
              <xsl:choose>
                <xsl:when test="$SyncFxCore = 'SYNC_FX_CORE_ERROR'">
                  Click <a href="http://go.microsoft.com/fwlink/?LinkID=204468">here</a> to install the framework.
                </xsl:when>
                <xsl:when test="$SyncFxCore = 'UNKNOWN_ERROR'">
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:otherwise>
                  None
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>

          <!-- SqlConnection -->
          <tr>
            <xsl:variable name="SqlConnection" select="SqlConnection/Result" />
            <xsl:variable name="ErrorInfo" select="SqlConnection/ErrorInfo" />
            
            <td class="columnDescription">Connection to SQL Server</td>
            <td class="columnStatus">
              <xsl:if test="$SqlConnection = 'SUCCESS'">
                <font color="green">PASSED</font>
              </xsl:if>
              <xsl:if test="$SqlConnection = 'INVALID_SQL_CONNECTION_STRING'">
                <font color="red">FAILED</font>
              </xsl:if>
              <xsl:if test="$SqlConnection = 'ERROR_OPENING_SQL_CONNECTION'">
                <font color="red">FAILED</font>
              </xsl:if>
              <xsl:if test="$SqlConnection = 'UNKNOWN_ERROR'">
                <font color="red">UNKNOWN ERROR</font>
              </xsl:if>
            </td>
            <td class="columnComments">
              <xsl:choose>
                <xsl:when test="$SqlConnection = 'INVALID_SQL_CONNECTION_STRING'">
                  Connection string seems to be invalid.
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:when test="$SqlConnection = 'ERROR_OPENING_SQL_CONNECTION'">
                  Error opening connection. Please verify the connection string and ensure that the server and database names
                  are correct. Also check if you are using the correct permissions.
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:when test="$SqlConnection = 'UNKNOWN_ERROR'">
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:otherwise>None</xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>

          <!-- DbProvisioned -->
          <tr>
            <xsl:variable name="DbProvisioned" select="DbProvisioned/Result" />
            <xsl:variable name="ErrorInfo" select="DbProvisioned/ErrorInfo" />
            
            <td class="columnDescription">Database Provisioning</td>
            <td class="columnStatus">
              <xsl:if test="$DbProvisioned = 'SUCCESS'">
                <font color="green">PASSED</font>
              </xsl:if>
              <xsl:if test="$DbProvisioned = 'TEMPLATE_OR_SCOPE_DOES_NOT_EXIST'">
                <font color="red">FAILED</font>
              </xsl:if>
              <xsl:if test="$DbProvisioned = 'UNKNOWN_ERROR'">
                <font color="red">UNKNOWN ERROR</font>
              </xsl:if>
            </td>
            <td class="columnComments">
              <xsl:choose>
                <xsl:when test="$DbProvisioned = 'TEMPLATE_OR_SCOPE_DOES_NOT_EXIST'">
                  Could not find the configured template or scope in the database. Please ensure that the database is provisioned using the SyncSvcUtil tool.
                </xsl:when>
                <xsl:when test="$DbProvisioned = 'UNKNOWN_ERROR'">
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:otherwise>None</xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>

          <!-- BatchingFolderPresent -->
          <tr>
            <xsl:variable name="BatchingFolderPresent" select="BatchingFolderPresent/Result" />
            <xsl:variable name="ErrorInfo" select="BatchingFolderPresent/ErrorInfo" />
            
            <td class="columnDescription">Batching Folder Exists?</td>
            <td class="columnStatus">
              <xsl:if test="$BatchingFolderPresent = 'SUCCESS'">
                <font color="green">PASSED</font>
              </xsl:if>
              <xsl:if test="$BatchingFolderPresent = 'BATCHING_NOT_ENABLED'">
                <font color="green">N/A</font>
              </xsl:if>
              <xsl:if test="$BatchingFolderPresent = 'DIRECTORY_NOT_FOUND'">
                <font color="red">FAILED</font>
              </xsl:if>
              <xsl:if test="$BatchingFolderPresent = 'UNKNOWN_ERROR'">
                <font color="red">UNKNOWN ERROR</font>
              </xsl:if>
            </td>
            <td class="columnComments">
              <xsl:choose>
                <xsl:when test="$BatchingFolderPresent = 'DIRECTORY_NOT_FOUND'">
                  Batching folder could not be found. Please ensure that the directory exists and you have permissions to write to it.
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:when test="$BatchingFolderPresent = 'BATCHING_NOT_ENABLED'">
                  Batching is not enabled.
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:when test="$BatchingFolderPresent = 'UNKNOWN_ERROR'">
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:otherwise>None</xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
          
          <!-- WriteAccessToBatchingFolder -->
          <tr>
            <xsl:variable name="WriteAccessToBatchingFolder" select="WriteAccessToBatchingFolder/Result" />
            <xsl:variable name="ErrorInfo" select="WriteAccessToBatchingFolder/ErrorInfo" />
            
            <td class="columnDescription">Write access to batching folder</td>
            <td class="columnStatus">
              <xsl:if test="$WriteAccessToBatchingFolder = 'SUCCESS'">
                <font color="green">PASSED</font>
              </xsl:if>
              <xsl:if test="$WriteAccessToBatchingFolder = 'BATCHING_NOT_ENABLED'">
                <font color="green">N/A</font>
              </xsl:if>
              <xsl:if test="($WriteAccessToBatchingFolder = 'INSUFFICIENT_PERMISSIONS') or ($WriteAccessToBatchingFolder = 'PATH_TOO_LONG') or ($WriteAccessToBatchingFolder = 'IO_ERROR') or ($WriteAccessToBatchingFolder = 'DIRECTORY_NOT_FOUND')">
                <font color="red">FAILED</font>
              </xsl:if>
              <xsl:if test="$WriteAccessToBatchingFolder = 'UNKNOWN_ERROR'">
                <font color="red">UNKNOWN ERROR</font>
              </xsl:if>
            </td>
            <td class="columnComments">
              <xsl:choose>
                <xsl:when test="($WriteAccessToBatchingFolder = 'INSUFFICIENT_PERMISSIONS') or ($WriteAccessToBatchingFolder = 'PATH_TOO_LONG') or ($WriteAccessToBatchingFolder = 'IO_ERROR') or ($WriteAccessToBatchingFolder = 'DIRECTORY_NOT_FOUND')">
                  Please ensure that the service has write permissions to the batching folder.
                  <xsl:if test="string-length($ErrorInfo) != '0'">Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" /></xsl:if>
                </xsl:when>
                <xsl:when test="$WriteAccessToBatchingFolder = 'BATCHING_NOT_ENABLED'">
                  Batching is not enabled.
                </xsl:when>
                <xsl:when test="$WriteAccessToBatchingFolder = 'UNKNOWN_ERROR'">
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:otherwise>None</xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
          
          <!-- PolicyFiles -->
          <tr>
            <xsl:variable name="PolicyFiles" select="PolicyFiles/Result" />
            <xsl:variable name="ErrorInfo" select="PolicyFiles/ErrorInfo" />
            
            <td class="columnDescription">ClientAccessPolicy.xml/CrossDomain.xml files</td>
            <td class="columnStatus">
              <xsl:if test="$PolicyFiles = 'FOUND_CROSSDOMAIN_POLICY_FILE'">
                <font color="green">PASSED</font>
              </xsl:if>
              <xsl:if test="$PolicyFiles = 'FOUND_CLIENT_ACCESS_POLICY'">
                <font color="green">PASSED</font>
              </xsl:if>
              <xsl:if test="$PolicyFiles = 'CLIENT_ACCESS_POLICY_OR_CROSS_DOMAIN_NOT_FOUND'">
                <font color="orange">FAILED</font>
              </xsl:if>
              <xsl:if test="$PolicyFiles = 'UNKNOWN_ERROR'">
                <font color="red">UNKNOWN ERROR</font>
              </xsl:if>
            </td>
            <td class="columnComments">
              <xsl:choose>
                <xsl:when test="$PolicyFiles = 'CLIENT_ACCESS_POLICY_OR_CROSS_DOMAIN_NOT_FOUND'">
                  Could not find clientaccesspolicy.xml or crossdomain.xml file. Silverlight clients invoking the service from another domain will
                  be unable to make requests successfully.

                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:when test="$PolicyFiles = 'FOUND_CROSSDOMAIN_POLICY_FILE'">
                  Found crossdomain.xml at website root.
                </xsl:when>
                <xsl:when test="$PolicyFiles = 'FOUND_CLIENT_ACCESS_POLICY'">
                  Found clientaccesspolicy.xml at website root.
                </xsl:when>
                <xsl:when test="$PolicyFiles = 'UNKNOWN_ERROR'">
                  <xsl:if test="string-length($ErrorInfo) != '0'">
                    Exception Details: <br/><br/><xsl:value-of select="$ErrorInfo" />
                  </xsl:if>
                </xsl:when>
                <xsl:otherwise>None</xsl:otherwise>
              </xsl:choose>
            </td> 
          </tr>
        </table>

        <br />
        <br />

        <h1>Service Configuration</h1>
        <table cellpadding="0" cellspacing="0" border="1">
          <thead>
            <tr>
              <td class="columnDescription">
                <b>Setting</b>
              </td>
              <td class="columnDescription">
                <b>Configured Value</b>
              </td>
            </tr>
          </thead>

          <!-- ScopeName -->
          <tr>
            <td class="columnDescription">
              Scope Name
            </td>
            <td class="columnDescription">
              <xsl:value-of select="Configuration/ScopeName" />
            </td>
          </tr>

          <!-- ConflictResolution -->
          <tr>
            <td class="columnDescription">
              Default Conflict Resolution
            </td>
            <td class="columnDescription">
              <xsl:value-of select="Configuration/ConflictResolution" />
            </td>
          </tr>

          <!-- SerializationFormat -->
          <tr>
            <td class="columnDescription">
              Default Serialization Format
            </td>
            <td class="columnDescription">
              <xsl:value-of select="Configuration/SerializationFormat" />
            </td>
          </tr>

          <!-- VerboseEnabled -->
          <tr>
            <td class="columnDescription">
              Verbose Error Response 
            </td>
            <td class="columnDescription">
              <xsl:value-of select="Configuration/VerboseEnabled" />
            </td>
          </tr>

          <!-- BatchingDirectory -->
          <tr>
            <td class="columnDescription">
              Batching Directory
            </td>
            <td class="columnDescription">
              <xsl:value-of select="Configuration/BatchingDirectory" />
            </td>
          </tr>

          <!-- BatchSize -->
          <tr>
            <td class="columnDescription">
              Download Batch Size
            </td>
            <td class="columnDescription">
              <xsl:value-of select="Configuration/BatchSize" />
            </td>
          </tr>
        </table>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>
