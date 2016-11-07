using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Lexicon;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	[TestFixture]
	[SetCulture("en-US")]
	public class WritingSystemManagerTests // can't derive from BaseTest, but instantiate DebugProcs instead
	{
		private class TestSettingsStore : ISettingsStore
		{
			private XElement m_settings;

			public XElement GetSettings()
			{
				return m_settings;
			}

			public void SaveSettings(XElement settingsElem)
			{
				m_settings = settingsElem;
			}
		}

		private class TestCoreLdmlInFolderWritingSystemRepository : CoreLdmlInFolderWritingSystemRepository
		{
			public TestCoreLdmlInFolderWritingSystemRepository(string path, ISettingsStore projectSettingsStore, ISettingsStore userSettingsStore,
				CoreGlobalWritingSystemRepository globalRepository = null) : base(path, projectSettingsStore, userSettingsStore, globalRepository)
			{
			}

			protected override IWritingSystemFactory<CoreWritingSystemDefinition> CreateWritingSystemFactory()
			{
				return new CoreWritingSystemFactory();
			}
		}

		private DebugProcs m_debugProcs;

		/// <summary>
		/// If a test overrides this, it should call this base implementation.
		/// </summary>
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{
			// This needs to be set for ICU
			RegistryHelper.CompanyName = "SIL";
			Icu.InitIcuDataDir();
			m_debugProcs = new DebugProcs();
		}

		/// <summary>
		/// Cleans up some resources that were used during the test
		/// </summary>
		[TestFixtureTearDown]
		public virtual void FixtureTeardown()
		{
			m_debugProcs.Dispose();
			m_debugProcs = null;
		}

		private static string PrepareTempStore(string name)
		{
			string path = Path.Combine(Path.GetTempPath(), name);
			if (Directory.Exists(path))
				Directory.Delete(path, true);
			Directory.CreateDirectory(path);
			return path;
		}

		/// <summary>
		/// Tests serialization and deserialization of writing systems.
		/// </summary>
		[Test]
		public void SerializeDeserialize()
		{
			string storePath = PrepareTempStore("Store");

			var projectSettingsStore = new TestSettingsStore();
			var userSettingsStore = new TestSettingsStore();
			// serialize
			var wsManager = new WritingSystemManager(new TestCoreLdmlInFolderWritingSystemRepository(storePath, projectSettingsStore, userSettingsStore));
			CoreWritingSystemDefinition ws = wsManager.Set("en-US");
			ws.SpellCheckingId = "en_US";
			ws.MatchedPairs.Add(new MatchedPair("(", ")", true));
			ws.WindowsLcid = 0x409.ToString(CultureInfo.InvariantCulture);
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}});
			ws.LegacyMapping = "legacy mapping";
			wsManager.Save();

			// deserialize
			wsManager = new WritingSystemManager(new TestCoreLdmlInFolderWritingSystemRepository(storePath, projectSettingsStore, userSettingsStore));
			Assert.IsTrue(wsManager.Exists("en-US"));
			ws = wsManager.Get("en-US");
			Assert.AreEqual("Eng", ws.Abbreviation);
			Assert.AreEqual("English", ws.Language.Name);
			Assert.AreEqual("en_US", ws.SpellCheckingId);
			Assert.AreEqual("United States", ws.Region.Name);
			Assert.That(ws.MatchedPairs, Is.EqualTo(new[] {new MatchedPair("(", ")", true)}));
			Assert.AreEqual(0x409.ToString(CultureInfo.InvariantCulture), ws.WindowsLcid);
			Assert.That(ws.CharacterSets.Count, Is.EqualTo(1));
			Assert.That(ws.CharacterSets[0].ValueEquals(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}}), Is.True);
			Assert.AreEqual("legacy mapping", ws.LegacyMapping);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_Engine method.
		/// </summary>
		[Test]
		public void get_Engine()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs = wsManager.Set("en-US");
			ILgWritingSystem enLgWs = wsManager.get_Engine("en-US");
			Assert.AreSame(enWs, enLgWs);

			Assert.IsFalse(wsManager.Exists("en-Latn-US"));
			// this should create a new writing system, since it doesn't exist
			ILgWritingSystem enUsLgWs = wsManager.get_Engine("en-US-fonipa");
			Assert.IsTrue(wsManager.Exists("en-US-fonipa"));
			Assert.IsTrue(wsManager.Exists(enUsLgWs.Handle));
			CoreWritingSystemDefinition enUsWs = wsManager.Get("en-US-fonipa");
			Assert.AreSame(enUsWs, enUsLgWs);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_EngineOrNull method.
		/// </summary>
		[Test]
		public void get_EngineOrNull()
		{
			var wsManager = new WritingSystemManager();
			Assert.IsNull(wsManager.get_EngineOrNull(1));
			CoreWritingSystemDefinition ws = wsManager.Set("en-US");
			Assert.AreSame(ws, wsManager.get_EngineOrNull(ws.Handle));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the GetWsFromStr method.
		/// </summary>
		[Test]
		public void GetWsFromStr()
		{
			var wsManager = new WritingSystemManager();
			Assert.AreEqual(0, wsManager.GetWsFromStr("en-US"));
			CoreWritingSystemDefinition ws = wsManager.Set("en-US");
			Assert.AreEqual(ws.Handle, wsManager.GetWsFromStr("en-US"));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the GetStrFromWs method.
		/// </summary>
		[Test]
		public void GetStrFromWs()
		{
			var wsManager = new WritingSystemManager();
			Assert.IsNull(wsManager.GetStrFromWs(1));
			CoreWritingSystemDefinition ws = wsManager.Set("en-US");
			Assert.AreEqual("en-US", wsManager.GetStrFromWs(ws.Handle));
			wsManager.Save();
		}

		/// <summary>
		/// Tests the Create method.
		/// </summary>
		[Test]
		public void Create()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWS = wsManager.Create("en-Latn-US-fonipa");
			Assert.That(enWS.Abbreviation, Is.EqualTo("Eng"));
			Assert.That(enWS.Language, Is.EqualTo((LanguageSubtag) "en"));
			Assert.That(enWS.Script, Is.EqualTo((ScriptSubtag) "Latn"));
			Assert.That(enWS.Region, Is.EqualTo((RegionSubtag) "US"));
			Assert.That(enWS.Variants, Is.EqualTo(new VariantSubtag[] {"fonipa"}));
			Assert.That(enWS.DefaultFontName, Is.EqualTo("Charis SIL"));
			Assert.That(enWS.DefaultCollation.ValueEquals(new IcuRulesCollationDefinition("standard")), Is.True);
			Assert.That(enWS.LanguageTag, Is.EqualTo("en-US-fonipa"));
			Assert.That(string.IsNullOrEmpty(enWS.WindowsLcid), Is.True);

			CoreWritingSystemDefinition chWS = wsManager.Create("zh-CN");
			Assert.That(chWS.Abbreviation, Is.EqualTo("Chi"));
			Assert.That(chWS.Language, Is.EqualTo((LanguageSubtag) "zh"));
			Assert.That(chWS.Script, Is.EqualTo((ScriptSubtag) "Hans"));
			Assert.That(chWS.Region, Is.EqualTo((RegionSubtag) "CN"));
			Assert.That(chWS.DefaultFontName, Is.EqualTo("Charis SIL"));
			Assert.That(chWS.DefaultCollation.ValueEquals(new SystemCollationDefinition {LanguageTag = "zh-CN"}), Is.True);
		}

		/// <summary>
		/// Tests the InterpretChrp method.
		/// </summary>
		[Test]
		public void InterpretChrp()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition ws = wsManager.Create("en-US");
			var chrp = new LgCharRenderProps
				{
					ws = ws.Handle,
					szFaceName = new ushort[32],
					dympHeight = 10000,
					ssv = (int) FwSuperscriptVal.kssvSuper
				};
			MarshalEx.StringToUShort("<default font>", chrp.szFaceName);
			ws.InterpretChrp(ref chrp);

			Assert.AreEqual(ws.DefaultFontName, MarshalEx.UShortToString(chrp.szFaceName));
			Assert.AreEqual(10000 / 3, chrp.dympOffset);
			Assert.AreEqual((10000 * 2) / 3, chrp.dympHeight);
			Assert.AreEqual((int) FwSuperscriptVal.kssvOff, chrp.ssv);

			chrp.ssv = (int) FwSuperscriptVal.kssvSub;
			chrp.dympHeight = 10000;
			chrp.dympOffset = 0;
			ws.InterpretChrp(ref chrp);

			Assert.AreEqual(-(10000 / 5), chrp.dympOffset);
			Assert.AreEqual((10000 * 2) / 3, chrp.dympHeight);
			Assert.AreEqual((int)FwSuperscriptVal.kssvOff, chrp.ssv);
			wsManager.Save();
		}

		/// <summary>
		/// Tests the get_WordForming method
		/// </summary>
		[Test]
		public void get_IsWordForming()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition ws = wsManager.Set("zh-CN");
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"e", "f", "g", "h", "'"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {",", "!", "*"}});
			Assert.IsTrue(ws.get_IsWordForming('\''));
			Assert.IsFalse(ws.get_IsWordForming('"'));

			ws.CharacterSets.Clear();
			Assert.IsFalse(ws.get_IsWordForming('\''));
			Assert.IsFalse(ws.get_IsWordForming('"'));
			wsManager.Save();
		}

		[Test]
		public void GetOrSetWorksRepeatedlyOnIdNeedingModification()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition ws;
			Assert.That(wsManager.GetOrSet("en-Latn", out ws), Is.False);
			Assert.That(ws.Id, Is.EqualTo("en"));
			CoreWritingSystemDefinition ws2;
			Assert.That(wsManager.GetOrSet("en-Latn", out ws2), Is.True);
			Assert.That(ws2, Is.EqualTo(ws));

			// By the way it should work the same for one where it does not have to modify the ID.
			Assert.That(wsManager.GetOrSet("fr", out ws), Is.False);
			Assert.That(wsManager.GetOrSet("fr", out ws), Is.True);
		}

		// ENHANCE: Ideally, we would want to test incrementing the middle and first character,
		// but that would require at least 677 (26^2 + 1) writing systems be created.

		[Test]
		public void CreateAudioWritingSystemScriptFirst()
		{
			var wsManager = new WritingSystemManager();

			CoreWritingSystemDefinition newWs = wsManager.Create(WellKnownSubtags.UnlistedLanguage, null, null, Enumerable.Empty<VariantSubtag>());

			Assert.DoesNotThrow(() =>
			{
				newWs.Script = WellKnownSubtags.AudioScript;
				newWs.Variants.Add(WellKnownSubtags.AudioPrivateUse);
			});
		}

		[Test]
		public void CreateAudioWritingSystemVariantFirst()
		{
			var wsManager = new WritingSystemManager();

			CoreWritingSystemDefinition newWs = wsManager.Create(WellKnownSubtags.UnlistedLanguage, null, null, Enumerable.Empty<VariantSubtag>());

			Assert.DoesNotThrow(() =>
			{
				newWs.Variants.Add(WellKnownSubtags.AudioPrivateUse);
				newWs.Script = WellKnownSubtags.AudioScript;
			});
		}
	}
}
