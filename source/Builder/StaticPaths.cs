using Microsoft.Win32;
using System;
using System.IO;

namespace Builder
{
    public static class StaticPaths
    {
        private const string _projectFolderPath = "projects";
        private const string _commonLanguageFolderPath = "common";
        private const string _dataFolderPath = "data";
        private const string _xmlFolderPath = "xml";
        private const string _mapsFolderPath = "maps";
        private const string _officialMapsFolderPath = "official";
        private const string _toolsFolderPath = "tools";
        private const string _builtIntermediateFolderPath = "builti";
        private const string _builtFolderPath = "built";
        private const string _globalIncludeFile = "global.xml";
        private const string _staticIncludeFile = "static.xml";
        private const string _registryPath = "Software\\WOW6432Node\\Electronic Arts\\Electronic Arts\\Command and Conquer 3";

        private static readonly RegistryKey _registryKey;

        public static string WrathEdPath { get; } = Path.Combine(_toolsFolderPath, "BinaryAssetBuilder.exe");
        public static string ProjectFolderPath => _projectFolderPath;
        public static string CommonLanguageFolderPath => _commonLanguageFolderPath;
        public static string BuiltIntermediatePath { get; } = Path.Combine(Environment.CurrentDirectory, _builtIntermediateFolderPath);
        public static string BuiltPath { get; } = Path.Combine(Environment.CurrentDirectory, _builtFolderPath);

        static StaticPaths()
        {
            try
            {
                if ((_registryKey = Registry.LocalMachine.OpenSubKey(_registryPath)) == null)
                {
                    _registryKey = Registry.CurrentUser.OpenSubKey(_registryPath);
                }
            }
            catch
            {
            }
        }

        public static string ConvertNameToBasePath(string name)
        {
            return Path.Combine(Environment.CurrentDirectory, _projectFolderPath, name);
        }

        public static string ConvertNameToMainPath(string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(ConvertNameToBasePath(name), language);
        }

        public static string ConvertNameToDataPath(string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(ConvertNameToMainPath(name, language), _dataFolderPath);
        }

        public static string ConvertNameToXmlDataPath(string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(ConvertNameToMainPath(name, language), _xmlFolderPath);
        }

        public static string ConvertNameToMapDataPath(string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(ConvertNameToMainPath(name, language), _mapsFolderPath);
        }

        public static string ConvertNameToGlobalXmlPath(string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(ConvertNameToXmlDataPath(name, language), _globalIncludeFile);
        }

        public static string ConvertNameToStaticXmlPath(string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(ConvertNameToXmlDataPath(name, language), _staticIncludeFile);
        }

        public static string ConvertNameToOfficialMapsPath(string name)
        {
            return Path.Combine(ConvertNameToMapDataPath(name), _officialMapsFolderPath);
        }

        public static string GetDefaultGamePath()
        {
            if (_registryKey == null)
            {
                return "null";
            }
            object installpath = _registryKey.GetValue("installpath");
            if (installpath is null)
            {
                return "null";
            }
            return installpath.ToString();
        }

        public static string GetDefaultOutputPath()
        {
            return "out";
        }

        public static string ConvertNameToBuiltOutputPath(string output, string name, string language = _commonLanguageFolderPath)
        {
            output = output.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Path.Combine(Environment.CurrentDirectory, output, name, language);
        }

        public static string ConvertNameToBuiltOutputDataPath(string output, string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(ConvertNameToBuiltOutputPath(output, name, language), _dataFolderPath);
        }

        public static string ConvertNameToRelativePath(string name, string language = _commonLanguageFolderPath)
        {
            return Path.Combine(name, language);
        }
    }
}
