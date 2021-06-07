// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration
{
    public class JsonConfigurationTest : FileCleanupTestBase
    {
        private JsonConfigurationProvider LoadProvider(string json)
        {
            var p = new JsonConfigurationProvider(new JsonConfigurationSource { Optional = true });
            p.Load(TestStreamHelpers.StringToStream(json));
            return p;
        }

        [Fact]
        public void CanLoadValidJsonFromStreamProvider()
        {
            var json = @"
{
    ""firstname"": ""test"",
    ""test.last.name"": ""last.name"",
        ""residential.address"": {
            ""street.name"": ""Something street"",
            ""zipcode"": ""12345""
        }
}";
            var config = new ConfigurationBuilder().AddJsonStream(TestStreamHelpers.StringToStream(json)).Build();
            Assert.Equal("test", config["firstname"]);
            Assert.Equal("last.name", config["test.last.name"]);
            Assert.Equal("Something street", config["residential.address:STREET.name"]);
            Assert.Equal("12345", config["residential.address:zipcode"]);
        }

        [Fact]
        public void ReloadThrowsFromStreamProvider()
        {
            var json = @"
{
    ""firstname"": ""test""
}";
            var config = new ConfigurationBuilder().AddJsonStream(TestStreamHelpers.StringToStream(json)).Build();
            Assert.Throws<InvalidOperationException>(() => config.Reload());
        }


        [Fact]
        public void LoadKeyValuePairsFromValidJson()
        {
            var json = @"
{
    ""firstname"": ""test"",
    ""test.last.name"": ""last.name"",
        ""residential.address"": {
            ""street.name"": ""Something street"",
            ""zipcode"": ""12345""
        }
}";
            var jsonConfigSrc = LoadProvider(json);

            Assert.Equal("test", jsonConfigSrc.Get("firstname"));
            Assert.Equal("last.name", jsonConfigSrc.Get("test.last.name"));
            Assert.Equal("Something street", jsonConfigSrc.Get("residential.address:STREET.name"));
            Assert.Equal("12345", jsonConfigSrc.Get("residential.address:zipcode"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
        {
            var json = @"
{
    ""name"": """"
}";
            var jsonConfigSrc = LoadProvider(json);
            Assert.Equal(string.Empty, jsonConfigSrc.Get("name"));
        }

        [Fact]
        public void LoadWithCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                var json = @"
{
    ""number"": 3.14
}";
                var jsonConfigSrc = LoadProvider(json);
                Assert.Equal("3.14", jsonConfigSrc.Get("number"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Fact]
        public void NonObjectRootIsInvalid()
        {
            var json = @"""test""";

            var exception = Assert.Throws<FormatException>(
                () => LoadProvider(json));

            Assert.NotNull(exception.Message);
        }

        [Fact]
        public void SupportAndIgnoreComments()
        {
            var json = @"/* Comments */
                {/* Comments */
                ""name"": /* Comments */ ""test"",
                ""address"": {
                    ""street"": ""Something street"", /* Comments */
                    ""zipcode"": ""12345""
                }
            }";
            var jsonConfigSrc = LoadProvider(json);
            Assert.Equal("test", jsonConfigSrc.Get("name"));
            Assert.Equal("Something street", jsonConfigSrc.Get("address:street"));
            Assert.Equal("12345", jsonConfigSrc.Get("address:zipcode"));
        }

        [Fact]
        public void SupportAndIgnoreTrailingCommas()
        {
            var json = @"
{
    ""firstname"": ""test"",
    ""test.last.name"": ""last.name"",
        ""residential.address"": {
            ""street.name"": ""Something street"",
            ""zipcode"": ""12345"",
        },
}";
            var jsonConfigSrc = LoadProvider(json);

            Assert.Equal("test", jsonConfigSrc.Get("firstname"));
            Assert.Equal("last.name", jsonConfigSrc.Get("test.last.name"));
            Assert.Equal("Something street", jsonConfigSrc.Get("residential.address:STREET.name"));
            Assert.Equal("12345", jsonConfigSrc.Get("residential.address:zipcode"));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50868", TestPlatforms.Android)]
        public void ThrowExceptionWhenUnexpectedEndFoundBeforeFinishParsing()
        {
            var json = @"{
                ""name"": ""test"",
                ""address"": {
                    ""street"": ""Something street"",
                    ""zipcode"": ""12345""
                }
            /* Missing a right brace here*/";
            var exception = Assert.Throws<FormatException>(() => LoadProvider(json));
            Assert.Contains("Could not parse the JSON file.", exception.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50868", TestPlatforms.Android)]
        public void ThrowExceptionWhenMissingCurlyBeforeFinishParsing()
        {
            var json = @"
            {
              ""Data"": {
            ";

            var exception = Assert.Throws<FormatException>(() => LoadProvider(json));
            Assert.Contains("Could not parse the JSON file.", exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(SR.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddJsonFile(path: null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(SR.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddJsonFile(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50868", TestPlatforms.Android)]
        public void JsonConfiguration_Throws_On_Missing_Configuration_File()
        {
            var config = new ConfigurationBuilder().AddJsonFile("NotExistingConfig.json", optional: false);
            var exception = Assert.Throws<FileNotFoundException>(() => config.Build());

            // Assert
            Assert.StartsWith($"The configuration file 'NotExistingConfig.json' was not found and is not optional. The expected physical path was '", exception.Message);
        }

        [Fact]
        public void JsonConfiguration_Does_Not_Throw_On_Optional_Configuration()
        {
            var config = new ConfigurationBuilder().AddJsonFile("NotExistingConfig.json", optional: true).Build();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50868", TestPlatforms.Android)]
        public void ThrowFormatExceptionWhenFileIsEmpty()
        {
            var exception = Assert.Throws<FormatException>(() => LoadProvider(@""));
            Assert.Contains("Could not parse the JSON file.", exception.Message);
        }

        [Theory]
        //[InlineData(false)]
        [InlineData(true)]
        public void JsonConfiguration_ReloadsOnChange(bool useSymbolicLink)
        {
            Debugger.Launch();
            string rootPath = GetTestFilePath();
            string subFolderPath = Directory.CreateDirectory(Path.Combine(rootPath, "subfolder")).FullName;

            string configPath = Path.Combine(subFolderPath, "config.json");
            File.WriteAllBytes(configPath, Encoding.UTF8.GetBytes(@"{""config"":{""message"":""v1.1""}}"));

            if (useSymbolicLink)
            {
                string linkPath = Path.Combine(rootPath, "config.json");
                bool success = CreateSymLink(configPath, linkPath, isDirectory: false);
                Assert.True(success);
                configPath = linkPath;
            }

            var config = new ConfigurationBuilder().AddJsonFile(configPath, optional: false, reloadOnChange: true).Build();
            Assert.Equal("v1.1", config.GetSection("config:message").Value);

            // Verify reloadOnChange.
            File.WriteAllBytes(configPath, Encoding.UTF8.GetBytes(@"{""config"":{""message"":""v1.2""}}"));
            // It takes 250ms by default to reload changes.
            Thread.Sleep(500);
            Assert.Equal("v1.2", config.GetSection("config:message").Value);
        }

        private static bool CreateSymLink(string targetPath, string linkPath, bool isDirectory)
        {
            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst()) // OSes that don't support Process.Start()
            {
                return false;
            }

            Process symLinkProcess = new Process();
            if (OperatingSystem.IsWindows())
            {
                symLinkProcess.StartInfo.FileName = "cmd";
                symLinkProcess.StartInfo.Arguments = string.Format("/c mklink{0} \"{1}\" \"{2}\"", isDirectory ? " /D" : "", Path.GetFullPath(linkPath), Path.GetFullPath(targetPath));
            }
            else
            {
                symLinkProcess.StartInfo.FileName = "/bin/ln";
                symLinkProcess.StartInfo.Arguments = string.Format("-s \"{0}\" \"{1}\"", Path.GetFullPath(targetPath), Path.GetFullPath(linkPath));
            }
            symLinkProcess.StartInfo.RedirectStandardOutput = true;
            symLinkProcess.Start();

            if (symLinkProcess != null)
            {
                symLinkProcess.WaitForExit();
                return (0 == symLinkProcess.ExitCode);
            }
            else
            {
                return false;
            }
        }
    }
}
