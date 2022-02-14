﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FluentAssertions;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.BinaryParsers.ProgramDatabase;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;

using Moq;

using Xunit;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    public class BuildWithSecureToolsTests
    {
        private static readonly Version MinVersion = new Version();

        [Fact]
        public void RetrieveMinimumCompilerVersionByLanguage_ShouldNotRetrieveMinVersionWhenLanguagesDoNotExistInTheMap()
        {
            var buildWithSecureTools = new BuildWithSecureTools();
            using var context = new BinaryAnalyzerContext
            {
                Policy = GeneratePolicyOptions(empty: true),
                Binary = GeneratePEBinary()
            };

            buildWithSecureTools.Initialize(context);

            Version version = BuildWithSecureTools.RetrieveMinimumCompilerVersion(context, Language.C);
            version.Should().NotBe(MinVersion);
        }

        [Fact]
        public void RetrieveMinimumCompilerVersionByLanguage_ShouldRetrieveVersionWhenLanguagesExistInTheMap()
        {
            var buildWithSecureTools = new BuildWithSecureTools();
            using var context = new BinaryAnalyzerContext
            {
                Policy = GeneratePolicyOptions(empty: false),
                Binary = GeneratePEBinary()
            };

            buildWithSecureTools.Initialize(context);

            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                Version version = BuildWithSecureTools.RetrieveMinimumCompilerVersion(context, language);

                if (new[] { Language.C, Language.Cxx }.Contains(language))
                {
                    version.Should().NotBe(MinVersion);
                }
                else
                {
                    version.Should().Be(MinVersion);
                }
            }
        }

        [Fact]
        public void GenerateMessageParametersAndLog_ShouldAlwaysGenerateValidParametersAndLog()
        {
            var buildWithSecureTools = new BuildWithSecureTools();
            var logger = new Mock<IAnalysisLogger>();
            using var context = new BinaryAnalyzerContext
            {
                TargetUri = new Uri(@"c:/file.dll"),
                Policy = GeneratePolicyOptions(empty: true),
                Binary = GeneratePEBinary(),
                Logger = logger.Object,
                Rule = buildWithSecureTools
            };

            buildWithSecureTools.Initialize(context);

            var omDetails = new ObjectModuleDetails(name: "file.obj",
                                                    library: "lib.lib",
                                                    compilerName: "Compiler name",
                                                    compilerFrontEndVersion: new Version(1, 0, 0, 0),
                                                    backEndVersion: new Version(1, 0, 0, 0),
                                                    commandLine: "",
                                                    language: Language.C,
                                                    hasSecurityChecks: true,
                                                    hasDebugInfo: true);

            var languageToBadModules = new SortedDictionary<Language, List<ObjectModuleDetails>>();
            languageToBadModules.Add(Language.C, new List<ObjectModuleDetails> { omDetails });

            buildWithSecureTools.GenerateMessageParametersAndLog(context, languageToBadModules);
            logger.Verify(l => l.Log(It.IsAny<ReportingDescriptor>(), It.IsAny<Result>()), Moq.Times.Once);
        }

        private static PEBinary GeneratePEBinary()
        {
            string fileName = Path.Combine(PEBinaryTests.BaselineTestsDataDirectory, "Native_x64_VS2013_Default.dll");
            return new PEBinary(new Uri(fileName));
        }

        private static PropertiesDictionary GeneratePolicyOptions(bool empty)
        {
            var allOptions = new PropertiesDictionary();
            var buildWithSecureTools = new BuildWithSecureTools();
            string ruleOptionsKey = $"{buildWithSecureTools.Id}.{buildWithSecureTools.Name}.Options";

            allOptions[ruleOptionsKey] = new PropertiesDictionary();

            var properties = (PropertiesDictionary)allOptions[ruleOptionsKey];
            foreach (IOption option in buildWithSecureTools.GetOptions())
            {
                object values = option.DefaultValue;
                if (empty)
                {
                    values = new StringToVersionMap();
                }

                properties.SetProperty(option, values, cacheDescription: true, persistToSettingsContainer: false);
            }

            return allOptions;
        }
    }
}
