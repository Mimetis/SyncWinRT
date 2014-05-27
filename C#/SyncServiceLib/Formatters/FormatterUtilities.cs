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
using System.Linq;
using System.Text;
using System.Xml;
using System.Globalization;

namespace Microsoft.Synchronization.Services.Formatters
{
    static class FormatterUtilities
    {
        static CultureInfo USCultureInfo = new CultureInfo("en-US");

        public static string GetEdmType(Type type)
        {
            if (type.IsGenericType)
            {
                return GetEdmType(type.GetGenericArguments()[0]);
            }
            switch (type.Name)
            {
                case "Boolean":
                    return "Edm.Boolean";
                case "Byte":
                    return "Edm.Byte";
                case "Char":
                case "String":
                    return "Edm.String";
                case "DBNull":
                    return "null";
                case "DateTime":
                    return "Edm.DateTime";
                case "Decimal":
                    return "Edm.Decimal";
                case "Double":
                    return "Edm.Double";
                case "Int16":
                    return "Edm.Int16";
                case "Int32":
                    return "Edm.Int32";
                case "Int64":
                    return "Edm.Int64";
                case "SByte":
                    return "Edm.SByte";
                case "Single":
                    return "Edm.Single";
                case "Byte[]":
                    return "Edm.Binary";
                case "Guid":
                    return "Edm.Guid";
                case "TimeSpan":
                    return "Edm.Time";
                case "DateTimeOffset":
                    return "Edm.DateTimeOffset";
                default:
                    throw new NotSupportedException("TypeCode " + Type.GetTypeCode(type) + " is not a supported type.");
            }
        }

        /// <summary>
        /// Looks at passed in Type and calls the appropriate Date functions for Json
        /// </summary>
        /// <param name="objValue">Actual value</param>
        /// <param name="type">Type coverting from</param>
        /// <returns>Json representation</returns>
        public static object ConvertDateTimeForType_Json(object objValue, Type type)
        {
            if (type == FormatterConstants.DateTimeType)
            {
                return ConvertDateTimeToJson((DateTime)objValue);
            }
            else if (type == FormatterConstants.TimeSpanType)
            {
                return ConvertTimeToJson((TimeSpan)objValue);
            }
            else
            {
                return ConvertDateTimeOffsetToJson((DateTimeOffset)objValue);
            }
        }

        /// <summary>
        /// Looks at passed in Type and calls the appropriate Date functions for Atom
        /// </summary>
        /// <param name="objValue">Actual value</param>
        /// <param name="type">Type coverting from</param>
        /// <returns>Atom representation</returns>
        public static object ConvertDateTimeForType_Atom(object objValue, Type type)
        {
            if (type == FormatterConstants.DateTimeType)
            {
                return ConvertDateTimeToAtom((DateTime)objValue);
            }
            else if (type == FormatterConstants.TimeSpanType)
            {
                return ConvertTimeToAtom((TimeSpan)objValue);
            }
            else
            {
                return ConvertDateTimeOffsetToAtom((DateTimeOffset)objValue);
            }
        }

        /// <summary>
        /// Converts DateTime to OData Atom format as specified in http://www.odata.org/developers/protocols/atom-format#PrimitiveTypes for DateTime
        /// Format is"yyyy-MM-ddThh:mm:ss.fffffff"
        /// <param name="date">DateTime to convert</param>
        /// </summary>
        /// <returns>Atom representation of DateTime</returns>
        public static string ConvertDateTimeToAtom(DateTime date)
        {
            return date.ToString(FormatterConstants.AtomDateTimeLexicalRepresentation, USCultureInfo);
        }

        /// <summary>
        /// Converts a TimeSpan to OData atom format as specified in http://www.odata.org/developers/protocols/atom-format#PrimitiveTypes for Time
        /// Actual lexical representation is time'hh:mm:ss.fffffff'
        /// </summary>
        /// <param name="t">Timespan to convert</param>
        /// <returns>Atom representation of Timespan</returns>
        public static string ConvertTimeToAtom(TimeSpan t)
        {
            return t.ToString();
        }

        /// <summary>
        /// Converts a DateTimeOffset to OData Atom format as specified in http://www.odata.org/developers/protocols/atom-format#PrimitiveTypes for DateTimeOffset
        /// Actual lexical representation is datetimeoffset'yyyy-MM-ddThh:mm:ss.fffffffzzz'
        /// </summary>
        /// <param name="dto">Timespan to convert</param>
        /// <returns>Atom representation of Timespan</returns>
        public static string ConvertDateTimeOffsetToAtom(DateTimeOffset dto)
        {
            return dto.ToString(FormatterConstants.AtomDateTimeOffsetLexicalRepresentation, USCultureInfo);
        }

        /// <summary>
        /// Converts DateTime to OData Json format as specified in http://www.odata.org/developers/protocols/json-format#PrimitiveTypes for DateTime
        /// Format is"\/Date(&lt;ticks&gt;["+" | "-" &lt;offset&gt;)\/"
        /// &lt;ticks&gt; = number of milliseconds since midnight Jan 1, 1970
        /// &lt;offset&gt; = number of minutes to add or subtract
        /// </summary>
        /// <param name="date">DateTime to convert</param>
        /// <returns>Json representation of DateTime</returns>
        public static string ConvertDateTimeToJson(DateTime date)
        {
            // Ticks returns the nanoseconds so to get milliseconds divide by 10,000
            return string.Format(USCultureInfo, 
                FormatterConstants.JsonDateTimeFormat, 
                (date.Ticks - FormatterConstants.JsonDateTimeStartTime.Ticks) / FormatterConstants.JsonNanoToMilliSecondsFactor);
        }

        /// <summary>
        /// Converts a TimeSpan to OData Json format as specified in http://www.odata.org/developers/protocols/json-format#PrimitiveTypes for Time
        /// Actual lexical representation is time'hh:mm:ss.fffffff'
        /// </summary>
        /// <param name="t">Timespan to convert</param>
        /// <returns>Json representation of Timespan</returns>
        public static string ConvertTimeToJson(TimeSpan t)
        {
            return string.Format(USCultureInfo, FormatterConstants.JsonTimeFormat, t.ToString());
        }

        /// <summary>
        /// Converts a DateTimeOffset to OData Json format as specified in http://www.odata.org/developers/protocols/json-format#PrimitiveTypes for DateTimeOffset
        /// Actual lexical representation is datetimeoffset'yyyy-MM-ddThh:mm:ss.fffffffzzz'
        /// </summary>
        /// <param name="dto">Timespan to convert</param>
        /// <returns>Json representation of Timespan</returns>
        public static string ConvertDateTimeOffsetToJson(DateTimeOffset dto)
        {
            return string.Format(USCultureInfo, 
                                 FormatterConstants.JsonDateTimeOffsetFormat, 
                                 dto.ToString(FormatterConstants.JsonDateTimeOffsetLexicalRepresentation, USCultureInfo));
        }

        internal static object ParseDateTimeFromString(string value, Type type)
        {
            try
            {
                // Check to see if its Json or Atom. i.e if it contains string date or datetime or time
                if (value.IndexOf("date", 0, StringComparison.OrdinalIgnoreCase) >= 0 || value.Contains("time"))
                {
                    // Its a Json string.
                    return ParseJsonString(value, type);
                }
                else
                {
                    // Its an Atom string
                    return ParseAtomString(value, type);
                }
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(string.Format(USCultureInfo, "Invalid Date/Time value received. Unable to parse value {0} to type {1}.", value, type.Name));
            }
        }

        private static object ParseAtomString(string value, Type type)
        {
            if (FormatterConstants.DateTimeType.IsAssignableFrom(type))
            {
                return XmlConvert.ToDateTime(value, FormatterConstants.AtomDateTimeLexicalRepresentation);
            }
            else if (FormatterConstants.DateTimeOffsetType.IsAssignableFrom(type))
            {
                return XmlConvert.ToDateTimeOffset(value, FormatterConstants.AtomDateTimeOffsetLexicalRepresentation);
            }
            else
            {
                // Its a TimeSpan
                return TimeSpan.Parse(value);
            }
        }

        private static object ParseJsonString(string value, Type type)
        {
            if (FormatterConstants.DateTimeType.IsAssignableFrom(type))
            {
                try
                {
                    int index1 = value.IndexOf(FormatterConstants.LeftBracketString) + 1;
                    int index2 = value.IndexOf(FormatterConstants.RightBracketString);
                    string ticksStr = value.Substring(index1, index2 - index1);
                    // Format is in Date(Ticks). Read the ticks which is in milliseconds and convert it to nanoseconds
                    // and add the starting time ticks as .NET DateTime ticks starts from 0001 year while OData starts from 1970.
                    long ticks = long.Parse(ticksStr, USCultureInfo) * FormatterConstants.JsonNanoToMilliSecondsFactor +
                                FormatterConstants.JsonDateTimeStartTime.Ticks;

                    // Check that ticks is a valid datetime.
                    if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                    {
                        throw new InvalidOperationException(string.Format("Invalid JSON DateTime value received. Value '{0}' is not a valid DateTime", ticks));
                    }
                    
                    return new DateTime(ticks);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException(string.Format(USCultureInfo, @"Invalid Json DateTime value received. Value {0} is not in format '\/Date(ticks)\/'.", value));
                }
            }
            else if (FormatterConstants.DateTimeOffsetType.IsAssignableFrom(type))
            {
                try
                {
                    int index1 = value.IndexOf(FormatterConstants.SingleQuoteString) + 1;
                    int index2 = value.LastIndexOf(FormatterConstants.SingleQuoteString);
                    string dateTimeStr = value.Substring(index1, index2 - index1);
                    return XmlConvert.ToDateTimeOffset(dateTimeStr, FormatterConstants.AtomDateTimeOffsetLexicalRepresentation);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException(string.Format(USCultureInfo, @"Invalid Json DateTimeOffset value received. Value {0} is not in format 'datetimeoffset'yyyy-MM-ddTHH:mm:ss.fffffffzzz''.", value));
                }
            }
            else
            {
                // Its a TimeSpan
                try
                {
                    int index1 = value.IndexOf(FormatterConstants.SingleQuoteString) + 1;
                    int index2 = value.LastIndexOf(FormatterConstants.SingleQuoteString);
                    string timeStr = value.Substring(index1, index2 - index1);
                    return TimeSpan.Parse(timeStr);
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException(string.Format(USCultureInfo, @"Invalid Json TimeSpan value received. Value {0} is not in format 'time'HH:mm:ss''.", value));
                }

            }
        }
    }
}
