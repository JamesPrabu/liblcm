// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LangProjectTests.cs
// Responsibility: TE Team

using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for <see cref="ILangProject"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LangProjectTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests LangProject.AllWritingSystems, a property that returns writing
		/// systems either in the cache or in a file list that is
		/// passed in.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WritingSystemsLists()
		{
			List<CoreWritingSystemDefinition> list = new List<CoreWritingSystemDefinition>();
			foreach (var x in Cache.LangProject.AllWritingSystems)
				list.Add(x);
			Assert.AreEqual(2, list.Count);

			ILgWritingSystemFactory factWs = Cache.ServiceLocator.GetInstance<ILgWritingSystemFactory>();
			Assert.LessOrEqual(list.Count, factWs.NumberOfWs, "factory list is at least as large as AllWritingSystems");
			Set<int> set = new Set<int>();
			using (ArrayPtr rgwsT = MarshalEx.ArrayToNative<int>(factWs.NumberOfWs))
			{
				factWs.GetWritingSystems(rgwsT, factWs.NumberOfWs);
				set.AddRange(MarshalEx.NativeToArray<int>(rgwsT, factWs.NumberOfWs));
			}
			int wsEn = factWs.GetWsFromStr("en");
			Assert.AreNotEqual(0, wsEn, "factory should contain English WS");
			int wsFr = factWs.GetWsFromStr("fr");
			Assert.AreNotEqual(0, wsFr, "factory should contain French WS");
			CoreWritingSystemDefinition eng = null;
			CoreWritingSystemDefinition frn = null;
			foreach (var x in list)
			{
				Assert.IsTrue(set.Contains(x.Handle), "AllWritingSystems should be a subset of the factory list");
				if (x.Handle == wsEn)
					eng = x;
				else if (x.Handle == wsFr)
					frn = x;
			}
			Assert.IsNotNull(eng, "AllWritingSystems should contain English");
			Assert.AreEqual("English", factWs.get_EngineOrNull(wsEn).LanguageName);
			Assert.AreEqual("English", eng.LanguageName);

			Assert.IsNotNull(frn, "AllWritingSystems should contain French");
			Assert.AreEqual("French", frn.LanguageName);
			Assert.AreEqual("French", factWs.get_Engine("fr").LanguageName);
		}

		/// <summary>
		/// Tests the various lists of writing systems represented by strings.
		/// </summary>
		[Test]
		public void MoreWritingSystemLists()
		{
			ILgWritingSystemFactory factWs = Cache.ServiceLocator.GetInstance<ILgWritingSystemFactory>();
			char[] rgchSplit = new char[] { ' ' };

			string sVern = factWs.GetStrFromWs(Cache.DefaultVernWs);
			Assert.IsTrue(Cache.LangProject.CurVernWss.Contains(sVern));
			Assert.IsTrue(Cache.LangProject.VernWss.Contains(sVern));
			Set<string> setVern = new Set<string>();
			setVern.AddRange(Cache.LangProject.VernWss.Split(rgchSplit));
			Assert.Less(0, setVern.Count, "should be at least one Vernacular WS");
			Set<string> setCurVern = new Set<string>();
			setCurVern.AddRange(Cache.LangProject.CurVernWss.Split(rgchSplit));
			Assert.Less(0, setCurVern.Count, "should be at least one Current Vernacular WS");
			Assert.LessOrEqual(setCurVern.Count, setVern.Count, "at least as many Current Vernacular as Vernacular");
			foreach (string x in setCurVern)
			{
				Assert.IsTrue(setVern.Contains(x), "Vernacular contains everything in Current Vernacular");
				int ws = factWs.GetWsFromStr(x);
				Assert.AreNotEqual(0, ws, "factory should contain everything in Current Vernacular");
			}
			foreach (string x in setVern)
			{
				int ws = factWs.GetWsFromStr(x);
				Assert.AreNotEqual(0, ws, "factory should contain everything in Vernacular");
			}

			string sAnal = factWs.GetStrFromWs(Cache.DefaultAnalWs);
			Assert.IsTrue(Cache.LangProject.CurAnalysisWss.Contains(sAnal));
			Assert.IsTrue(Cache.LangProject.AnalysisWss.Contains(sAnal));
			Set<string> setAnal = new Set<string>();
			setAnal.AddRange(Cache.LangProject.AnalysisWss.Split(rgchSplit));
			Assert.Less(0, setAnal.Count, "should be at least one Analysis WS");
			Set<string> setCurAnal = new Set<string>();
			setCurAnal.AddRange(Cache.LangProject.CurAnalysisWss.Split(rgchSplit));
			Assert.Less(0, setCurAnal.Count, "should be at least one Current Analysis WS");
			Assert.LessOrEqual(setCurAnal.Count, setAnal.Count, "at least as many Current Analysis as Analysis");
			foreach (string x in setCurAnal)
			{
				Assert.IsTrue(setAnal.Contains(x), "Analysis contains everything in Current Analysis");
				int ws = factWs.GetWsFromStr(x);
				Assert.AreNotEqual(0, ws, "factory should contain everything in Current Analysis");
			}
			foreach (string x in setAnal)
			{
				int ws = factWs.GetWsFromStr(x);
				Assert.AreNotEqual(0, ws, "factory should contain everything in Analysis");
			}
		}

		/// <summary>
		/// Test adding a ws to the standard vern/anal lists.
		/// </summary>
		[Test]
		public void AddingWritingSystems()
		{
			Assert.AreEqual(1, Cache.LangProject.CurrentVernacularWritingSystems.Count, "Only one current vernacular WS");
			Assert.AreEqual(1, Cache.LangProject.VernacularWritingSystems.Count, "Only one vernacular WS");
			Assert.IsFalse(Cache.LangProject.VernWss.Contains("es"), "Vernacular WSs do not include Spanish");
			Assert.IsFalse(Cache.LangProject.CurVernWss.Contains("es"), "Current Vernacular WSs do not include Spanish");
			Assert.IsFalse(Cache.LangProject.AnalysisWss.Contains("es"), "Analysis WSs do not include Spanish");
			Assert.IsFalse(Cache.LangProject.CurAnalysisWss.Contains("es"), "Current Analysis WSs do not include Spanish");

			Assert.AreEqual(1, Cache.LangProject.CurrentAnalysisWritingSystems.Count, "Only one current analysis WS");
			Assert.AreEqual(1, Cache.LangProject.AnalysisWritingSystems.Count, "Only one analysis WS");
			Assert.IsFalse(Cache.LangProject.VernWss.Contains("de"), "Vernacular WSs do not include German");
			Assert.IsFalse(Cache.LangProject.CurVernWss.Contains("de"), "Current Vernacular WSs do not include German");
			Assert.IsFalse(Cache.LangProject.AnalysisWss.Contains("de"), "Analysis WSs do not include German");
			Assert.IsFalse(Cache.LangProject.CurAnalysisWss.Contains("de"), "Current Analysis WSs do not include German");

			ILgWritingSystemFactory factWs = Cache.ServiceLocator.GetInstance<ILgWritingSystemFactory>();
			int wsEs = factWs.GetWsFromStr("es");
			if (wsEs != 0)
			{
				Cache.LangProject.VernWss = Cache.LangProject.VernWss + " es";
				Cache.LangProject.CurVernWss = Cache.LangProject.CurVernWss + " es";
				Assert.AreEqual(2, Cache.LangProject.CurrentVernacularWritingSystems.Count, "Now two current vernacular WS");
				Assert.AreEqual(2, Cache.LangProject.VernacularWritingSystems.Count, "Now two vernacular WS");
			}
			int wsDe = factWs.GetWsFromStr("de");
			if (wsDe != 0)
			{
				var german = factWs.get_EngineOrNull(wsDe) as CoreWritingSystemDefinition;
				Assert.IsNotNull(german, "IWritingSystem and ILgWritingSystem are the same now");
				Cache.LangProject.AnalysisWritingSystems.Add(german);
				Cache.LangProject.CurrentAnalysisWritingSystems.Add(german);
				Assert.IsTrue(Cache.LangProject.AnalysisWss.Contains("de"), "Analysis WSs now include German");
				Assert.IsTrue(Cache.LangProject.CurAnalysisWss.Contains("de"), "Current Analysis WSs now include German");
			}
		}

		/// <summary>
		/// Test LinkedFilesRootDir
		/// </summary>
		[Test]
		public void LinkedFilesRootDirTests()
		{
			//test when LinkedFiles is in the project's root folder
			var projectFolder = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "TestProjectName");
			var linkedFilesFullPath = Path.Combine(projectFolder, "LinkedFiles");
			Cache.LanguageProject.LinkedFilesRootDir = linkedFilesFullPath;
			var outputLinkedFilesFullPath = Cache.LanguageProject.LinkedFilesRootDir;
			Assert.True(linkedFilesFullPath.Equals(outputLinkedFilesFullPath));

			//test when linked files is in FW Projects folder
			linkedFilesFullPath = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "LinkedFiles");
			Cache.LanguageProject.LinkedFilesRootDir = linkedFilesFullPath;
			outputLinkedFilesFullPath = Cache.LanguageProject.LinkedFilesRootDir;
			Assert.True(linkedFilesFullPath.Equals(outputLinkedFilesFullPath));

			//test when linked files is in the CommonApplicationData shared folder
			linkedFilesFullPath = Path.Combine(DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LinkedFiles");
			Cache.LanguageProject.LinkedFilesRootDir = linkedFilesFullPath;
			outputLinkedFilesFullPath = Cache.LanguageProject.LinkedFilesRootDir;
			Assert.True(linkedFilesFullPath.Equals(outputLinkedFilesFullPath));

			//test when the linked files is in the User's MyDocuments folder
			linkedFilesFullPath = Path.Combine(DirectoryFinder.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LinkedFiles");
			Cache.LanguageProject.LinkedFilesRootDir = linkedFilesFullPath;
			outputLinkedFilesFullPath = Cache.LanguageProject.LinkedFilesRootDir;
			Assert.True(linkedFilesFullPath.Equals(outputLinkedFilesFullPath));

			//test when the linked files is in some other location and therefore is just stored as an absolute full path.
			linkedFilesFullPath = Path.Combine(DirectoryFinder.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LinkedFiles");
			Cache.LanguageProject.LinkedFilesRootDir = linkedFilesFullPath;
			outputLinkedFilesFullPath = Cache.LanguageProject.LinkedFilesRootDir;
			Assert.True(linkedFilesFullPath.Equals(outputLinkedFilesFullPath));
		}
	}
}
