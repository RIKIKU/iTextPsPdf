/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Collections.ObjectModel;
#region CSV conversion

#region ExportHelperConversion

/// <summary>
/// Helper class for Export-Csv and ConvertTo-Csv.
/// </summary>
internal class ExportCsvHelper : IDisposable
{
    private char _delimiter;
    readonly private BaseCsvWritingCommand.QuoteKind _quoteKind;
    readonly private HashSet<string> _quoteFields;
    readonly private StringBuilder _outputString;

    /// <summary>
    /// Create ExportCsvHelper instance.
    /// </summary>
    /// <param name="delimiter">Delimiter char.</param>
    /// <param name="quoteKind">Kind of quoting.</param>
    /// <param name="quoteFields">List of fields to quote.</param>
    internal ExportCsvHelper(char delimiter, BaseCsvWritingCommand.QuoteKind quoteKind, string[] quoteFields)
    {
        _delimiter = delimiter;
        _quoteKind = quoteKind;
        _quoteFields = quoteFields == null ? null : new HashSet<string>(quoteFields, StringComparer.OrdinalIgnoreCase);
        _outputString = new StringBuilder(128);
    }

    // Name of properties to be written in CSV format

    /// <summary>
    /// Get the name of properties from source PSObject and add them to _propertyNames.
    /// </summary>
    internal static IList<string> BuildPropertyNames(PSObject source, IList<string> propertyNames)
    {
        if (propertyNames != null)
        {
            throw new InvalidOperationException(CsvCommandStrings.BuildPropertyNamesMethodShouldBeCalledOnlyOncePerCmdletInstance);
        }

        // serialize only Extended and Adapted properties..
        PSMemberInfoCollection<PSPropertyInfo> srcPropertiesToSearch = 
            /*new PSMemberInfoIntegratingCollection<PSPropertyInfo>(
                source,
                PSObject.GetPropertyCollection(PSMemberViewTypes.Extended | PSMemberViewTypes.Adapted));*/
/*
        propertyNames = new Collection<string>();
        foreach (PSPropertyInfo prop in srcPropertiesToSearch)
        {
            propertyNames.Add(prop.Name);
        }

        return propertyNames;
    }
    /*
    /// <summary>
    /// Converts PropertyNames in to a CSV string.
    /// </summary>
    /// <returns>Converted string.</returns>
    internal string ConvertPropertyNamesCSV(IList<string> propertyNames)
    {
        if (propertyNames == null)
        {
            throw new ArgumentNullException(nameof(propertyNames));
        }

        _outputString.Clear();
        bool first = true;

        foreach (string propertyName in propertyNames)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                _outputString.Append(_delimiter);
            }

            if (_quoteFields != null)
            {
                if (_quoteFields.TryGetValue(propertyName, out _))
                {
                    AppendStringWithEscapeAlways(_outputString, propertyName);
                }
                else
                {
                    _outputString.Append(propertyName);
                }
            }
            else
            {
                switch (_quoteKind)
                {
                    case BaseCsvWritingCommand.QuoteKind.Always:
                        AppendStringWithEscapeAlways(_outputString, propertyName);
                        break;
                    case BaseCsvWritingCommand.QuoteKind.AsNeeded:
                        if (propertyName.Contains(_delimiter))
                        {
                            AppendStringWithEscapeAlways(_outputString, propertyName);
                        }
                        else
                        {
                            _outputString.Append(propertyName);
                        }

                        break;
                    case BaseCsvWritingCommand.QuoteKind.Never:
                        _outputString.Append(propertyName);
                        break;
                }
            }
        }

        return _outputString.ToString();
    }

    /// <summary>
    /// Convert PSObject to CSV string.
    /// </summary>
    /// <param name="mshObject">PSObject to convert.</param>
    /// <param name="propertyNames">Property names.</param>
    /// <returns></returns>
    internal string ConvertPSObjectToCSV(PSObject mshObject, IList<string> propertyNames)
    {
        if (propertyNames == null)
        {
            throw new ArgumentNullException(nameof(propertyNames));
        }

        _outputString.Clear();
        bool first = true;

        foreach (string propertyName in propertyNames)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                _outputString.Append(_delimiter);
            }

            // If property is not present, assume value is null and skip it.
            if (mshObject.Properties[propertyName] is PSPropertyInfo property)
            {
                var value = GetToStringValueForProperty(property);

                if (_quoteFields != null)
                {
                    if (_quoteFields.TryGetValue(propertyName, out _))
                    {
                        AppendStringWithEscapeAlways(_outputString, value);
                    }
                    else
                    {
                        _outputString.Append(value);
                    }
                }
                else
                {
                    switch (_quoteKind)
                    {
                        case BaseCsvWritingCommand.QuoteKind.Always:
                            AppendStringWithEscapeAlways(_outputString, value);
                            break;
                        case BaseCsvWritingCommand.QuoteKind.AsNeeded:
                            if (value.Contains(_delimiter))
                            {
                                AppendStringWithEscapeAlways(_outputString, value);
                            }
                            else
                            {
                                _outputString.Append(value);
                            }

                            break;
                        case BaseCsvWritingCommand.QuoteKind.Never:
                            _outputString.Append(value);
                            break;
                        default:
                            Diagnostics.Assert(false, "BaseCsvWritingCommand.QuoteKind has new item.");
                            break;
                    }
                }
            }
        }

        return _outputString.ToString();
    }

    /// <summary>
    /// Get value from property object.
    /// </summary>
    /// <param name="property"> Property to convert.</param>
    /// <returns>ToString() value.</returns>
    internal static string GetToStringValueForProperty(PSPropertyInfo property)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        string value = null;
        try
        {
            object temp = property.Value;
            if (temp != null)
            {
                value = temp.ToString();
            }
        }
        catch (Exception)
        {
            // If we cannot read some value, treat it as null.
        }

        return value;
    }

    /// <summary>
    /// Prepares string for writing type information.
    /// </summary>
    /// <param name="source">PSObject whose type to determine.</param>
    /// <returns>String with type information.</returns>
    internal static string GetTypeString(PSObject source)
    {
        string type = null;

        // get type of source
        Collection<string> tnh = source.TypeNames;
        if (tnh == null || tnh.Count == 0)
        {
            type = "#TYPE";
        }
        else
        {
            if (tnh[0] == null)
            {
                throw new InvalidOperationException(CsvCommandStrings.TypeHierarchyShouldNotHaveNullValues);
            }

            string temp = tnh[0];

            // If type starts with CSV: remove it. This would happen when you export
            // an imported object. import-csv adds CSV. prefix to the type.
            if (temp.StartsWith(ImportExportCSVHelper.CSVTypePrefix, StringComparison.OrdinalIgnoreCase))
            {
                temp = temp.Substring(4);
            }

            type = string.Format(System.Globalization.CultureInfo.InvariantCulture, "#TYPE {0}", temp);
        }

        return type;
    }

    /// <summary>
    /// Escapes the " in string if necessary.
    /// Encloses the string in double quotes if necessary.
    /// </summary>
    internal static void AppendStringWithEscapeAlways(StringBuilder dest, string source)
    {
        if (source == null)
        {
            return;
        }

        // Adding Double quote to all strings
        dest.Append('"');
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];

            // Double quote in the string is escaped with double quote
            if (c == '"')
            {
                dest.Append('"');
            }

            dest.Append(c);
        }

        dest.Append('"');
    }

    #region IDisposable Members

    /// <summary>
    /// Set to true when object is disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Public dispose method.
    /// </summary>
    public void Dispose()
    {
        if (_disposed == false)
        {
            GC.SuppressFinalize(this);
        }

        _disposed = true;
    }

    #endregion IDisposable Members
}

#endregion ExportHelperConversion
*/