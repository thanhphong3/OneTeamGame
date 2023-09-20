using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Visometry.VisionLib.SDK.Core.Details
{
    /// <summary>
    /// VisionLib functions for working with Paths and URIs.
    /// </summary>
    public static class PathHelper
    {
        public static string CombinePaths(string path1, string path2)
        {
            if (path2.Length == 0)
            {
                return path1;
            }

            if (path1.Length == 0 || IsAbsolutePath(path2))
            {
                return path2;
            }

            var directorySeparators = new char[]{'/', '\\'};
            return path1.TrimEnd(directorySeparators) + '/' + path2.TrimStart(directorySeparators);
        }

        public static string CombinePaths(string path1, string path2, string path3)
        {
            return CombinePaths(path1, CombinePaths(path2, path3));
        }

        public static bool IsAbsolutePath(string path)
        {
            return ContainsScheme(path) || IsAbsoluteWindowsPath(path) || IsAbsoluteUnixPath(path);
        }

        private static bool IsAbsoluteWindowsPath(string path)
        {
            return path.Length >= 2 && path[1] == ':' && Char.IsLetter(path[0]);
        }

        private static bool IsAbsoluteUnixPath(string path)
        {
            return path.Length >= 1 && (path[0] == '/' || path[0] == '~');
        }

        public static bool ContainsScheme(string uri)
        {
            // clang-format off
            return uri.StartsWith("streaming-assets-dir:") ||
                uri.StartsWith("local_storage_dir:") ||
                uri.StartsWith("local-storage-dir:") ||
                uri.StartsWith("project_dir:") ||
                uri.StartsWith("project-dir:") ||
                uri.StartsWith("capture_dir:") ||
                uri.StartsWith("capture-dir:") ||
                uri.StartsWith("data:") ||
                uri.StartsWith("http://") ||
                uri.StartsWith("https://") ||
                uri.StartsWith("file://");
            // clang-format on
        }

        public static string AppendQueryToURI(string query, string uri)
        {
            if (String.IsNullOrEmpty(query))
            {
                return uri;
            }

            string seperator = uri.Contains("?") ? "&" : "?";
            return uri + seperator + query;
        }

        public static string AppendQueryToURI(List<string> queryParameters, string uri)
        {
            return AppendQueryToURI(CombineQueryParameters(queryParameters), uri);
        }

        public static string CombineQueryParameters(string parameter1, string parameter2)
        {
            if (String.IsNullOrEmpty(parameter2))
            {
                return parameter1;
            }
            return parameter1 + "&" + parameter2;
        }

        public static string CombineQueryParameters(List<string> newParameters)
        {
            return String.Join("&", newParameters.ToArray());
        }

        public static string RemoveQueryString(string uri)
        {
            int queryStartIndex = uri.LastIndexOf('?');
            if (queryStartIndex >= 0)
            {
                return uri.Substring(0, queryStartIndex);
            }
            return uri;
        }

        public static string GetQueryString(string uri)
        {
            int queryStartIndex = uri.LastIndexOf('?');
            if (queryStartIndex >= 0)
            {
                return uri.Substring(queryStartIndex);
            }
            return "";
        }

        public static Dictionary<string, string> GetQueryMap(string uri)
        {
            string queryString = GetQueryString(uri);

            if (queryString == "")
            {
                return new Dictionary<string, string>();
            }

            List<string> queryParameters =
                queryString.Substring(1)
                    .Split(new char[]{'&'}, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            Dictionary<string, string> queryParameterValuesMap = new Dictionary<string, string>();

            foreach (string queryParameter in queryParameters)
            {
                string[] values = queryParameter.Split(new char[]{'='}, 2);
                queryParameterValuesMap[values[0]] = (values.Length == 2 ? values[1] : "");
            }

            return queryParameterValuesMap;
        }

        public static string RemoveStartingSlashes(string path)
        {
            return path.TrimStart('/');
        }

        public static string UnifySlashes(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}