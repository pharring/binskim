﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Microsoft.CodeAnalysis.Sarif.Writers;

using Newtonsoft.Json;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.IL
{
    public class BuiltInRuleFunctionalTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public BuiltInRuleFunctionalTests(ITestOutputHelper output)
        {
            this.testOutputHelper = output;
        }

        [Fact]
        public void Driver_BuiltInRuleFunctionalTests()
        {
            if (PlatformSpecificHelpers.RunningOnWindows())
            {
                AnalyzeCommand.s_UnitTestOutputVersion = Sarif.SarifVersion.Current;

                for (int i = 0; i < 6; i++)
                {
                    this.BatchRuleRules(string.Empty, 1, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 2, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 3, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 4, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 5, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 6, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 7, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 8, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 9, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 10, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 11, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 12, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 16, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 20, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 24, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 32, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 50, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 64, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 99, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 100, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 500, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 1000, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                    this.BatchRuleRules(string.Empty, 99999, "*.dll", "*.exe", "gcc.*", "clang.*", "macho.*");
                }
            }
        }

        private void BatchRuleRules(string ruleName, int threads, params string[] inputFilters)
        {
            var sb = new StringBuilder();
            string testDirectory = PEBinaryTests.BaselineTestsDataDirectory + Path.DirectorySeparatorChar + ruleName;

            foreach (string inputFilter in inputFilters)
            {
                string[] testFiles = Directory.GetFiles(testDirectory, inputFilter);

                foreach (string file in testFiles)
                {
                    this.RunRules(sb, file, threads);
                }
            }

            if (sb.Length == 0)
            {
                // Test passes
                return;
            }

            string rebaselineMessage = "If the actual output is expected, generate new baselines by executing `UpdateBaselines.ps1` from a PS command prompt.";
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, rebaselineMessage));

            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Run the following to all test baselines vs. actual results:");
                sb.AppendLine(this.GenerateDiffCommand(
                    Path.Combine(testDirectory, "Expected"),
                    Path.Combine(testDirectory, "Actual")));
                this.testOutputHelper.WriteLine(sb.ToString());
            }

            Assert.Equal(0, sb.Length);
        }

        private void RunRules(StringBuilder sb, string inputFileName, int threads)
        {
            string fileName = Path.GetFileName(inputFileName);
            string actualDirectory = Path.Combine(Path.GetDirectoryName(inputFileName), "Actual");
            string expectedDirectory;
            if (PlatformSpecificHelpers.RunningOnWindows())
            {
                expectedDirectory = Path.Combine(Path.GetDirectoryName(inputFileName), "Expected");
            }
            else
            {
                expectedDirectory = Path.Combine(Path.GetDirectoryName(inputFileName), "NonWindowsExpected");
            }
            if (!Directory.Exists(actualDirectory))
            {
                Directory.CreateDirectory(actualDirectory);
            }

            string expectedFileName = Path.Combine(expectedDirectory, fileName + ".sarif");
            string actualFileName = Path.Combine(actualDirectory, fileName + ".sarif");

            var command = new AnalyzeCommand();
            var options = new AnalyzeOptions
            {
                Force = true,
                Recurse = false,
                PrettyPrint = true,
                DataToInsert = new[] { OptionallyEmittedData.Hashes },
                DataToRemove = new[] { OptionallyEmittedData.NondeterministicProperties },
                OutputFilePath = actualFileName,
                ConfigurationFilePath = "default",
                SarifOutputVersion = Sarif.SarifVersion.Current,
                TargetFileSpecifiers = new string[] { inputFileName },
                Traces = Array.Empty<string>(),
                Level = new List<FailureLevel> { FailureLevel.Error, FailureLevel.Warning, FailureLevel.Note, FailureLevel.None },
                Kind = new List<ResultKind> { ResultKind.Fail, ResultKind.NotApplicable, ResultKind.Pass },
                Threads = threads
            };

            int result = command.Run(options);

            // Note that we don't ensure a success code. That is because we
            // are running end-to-end tests for valid and invalid files

            var settings = new JsonSerializerSettings()
            {
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            string expectedText = File.ReadAllText(expectedFileName);
            string actualText = File.ReadAllText(actualFileName);

            // Replace repository root absolute path with Z:\ for machine and enlistment independence
            string repoRoot = Path.GetFullPath(Path.Combine(actualDirectory, "..", "..", "..", ".."));
            actualText = actualText.Replace(repoRoot.Replace(@"\", @"\\"), @"Z:");
            actualText = actualText.Replace(repoRoot.Replace(@"\", @"/"), @"Z:");

            // Remove stack traces as they can change due to inlining differences by configuration and runtime.
            actualText = Regex.Replace(actualText, @"\\r\\n   at [^""]+", "");

            actualText = actualText.Replace(@"""Sarif""", @"""BinSkim""");
            actualText = actualText.Replace(@"        ""fileVersion"": ""15.0.0""," + Environment.NewLine, string.Empty);

            actualText = Regex.Replace(actualText, @"\s*""fullName""[^\n]+?\n", Environment.NewLine);
            actualText = Regex.Replace(actualText, @"\s*""semanticVersion""[^\n]+?\n", Environment.NewLine);
            actualText = Regex.Replace(actualText, @"      ""id""[^,]+,\s+""tool""", @"      ""tool""", RegexOptions.Multiline);

            // Write back the normalized actual text so that the diff command given on failure shows what was actually compared.

            Encoding utf8encoding = new UTF8Encoding(true);
            using (var textWriter = new StreamWriter(actualFileName, false, utf8encoding))
            {
                textWriter.Write(actualText);
            }

            // Make sure we can successfully deserialize what was just generated
            SarifLog expectedLog = PrereleaseCompatibilityTransformer.UpdateToCurrentVersion(
                                    expectedText,
                                    settings.Formatting,
                                    out expectedText);

            SarifLog actualLog = JsonConvert.DeserializeObject<SarifLog>(actualText, settings);

            var visitor = new ResultDiffingVisitor(expectedLog);

            if (!visitor.Diff(actualLog.Runs[0].Results))
            {
                string errorMessage = "The output of the tool did not match for input {0}.";
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, errorMessage, inputFileName));
                sb.AppendLine("Check differences with:");
                sb.AppendLine(this.GenerateDiffCommand(expectedFileName, actualFileName));
            }
        }

        private string GenerateDiffCommand(string expected, string actual)
        {
            expected = Path.GetFullPath(expected);
            actual = Path.GetFullPath(actual);

            string beyondCompare = TryFindBeyondCompare();
            if (beyondCompare != null)
            {
                return string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\" \"{2}\" /title1=Expected /title2=Actual", beyondCompare, expected, actual);
            }

            if (PlatformSpecificHelpers.RunningOnWindows())
            {
                return string.Format(CultureInfo.InvariantCulture, "windiff \"{0}\" \"{1}\"", expected, actual);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "diff \"{0}\", \"{1}\"", expected, actual);
            }
        }

        private static string TryFindBeyondCompare()
        {
            var directories = new List<string>();
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            directories.Add(programFiles);
            directories.Add(programFiles.Replace(" (x86)", ""));

            foreach (string directory in directories)
            {
                for (int idx = 4; idx >= 3; --idx)
                {
                    string beyondComparePath = string.Format(CultureInfo.InvariantCulture, "{0}\\Beyond Compare {1}\\BComp.exe", directory, idx);
                    if (File.Exists(beyondComparePath))
                    {
                        return beyondComparePath;
                    }
                }

                string beyondCompare2Path = programFiles + "\\Beyond Compare 2\\BC2.exe";
                if (File.Exists(beyondCompare2Path))
                {
                    return beyondCompare2Path;
                }
            }

            return null;
        }
    }
}
