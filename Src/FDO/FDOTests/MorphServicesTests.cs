// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SandboxBase.cs
// Responsibility: pyle
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// </summary>
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_matchingMorphs gets disposed in FixtureTearDown()")]
	public class MatchingMorphsLogicTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private SetupMatchingMorphs m_matchingMorphs;

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
			   DoSetupFixture);
		}

		/// <summary>
		///
		/// </summary>
		public override void FixtureTeardown()
		{
			if (m_matchingMorphs != null)
				m_matchingMorphs.Dispose();
			m_matchingMorphs = null;
			base.FixtureTeardown();
		}

		void DoSetupFixture()
		{

			m_matchingMorphs = new SetupMatchingMorphs(this);
		}

		/// <summary>
		/// We initially tried to do SetupMatchingMorphs on a per test basis,
		/// but discovered that Undo wasn't undoing everything we expected.
		/// (See UndoAllIssueTest)
		/// </summary>
		internal class SetupMatchingMorphsDisabled : SetupMatchingMorphs
		{
			internal SetupMatchingMorphsDisabled(MatchingMorphsLogicTests testFixture)
			{

			}

			protected override void DoSetup()
			{
				// don't do any data setup.
			}

		}

		// REVIEW (TomB): There is no reason to derive from FwDisposableBase because neither
		// Dispose method is being overriden. Either override one of those methods or get rid
		// of all the using statements where objects of this class are instantiated.
		internal class SetupMatchingMorphs : FwDisposableBase
		{
			internal SetupMatchingMorphs()
			{

			}
			internal SetupMatchingMorphs(MatchingMorphsLogicTests testFixture)
			{
				Cache = testFixture.Cache;
				DoSetup();
			}

			protected virtual void DoSetup()
			{
				ILexEntry newEntry;
				ILexSense newSense;
				var msa1 = new SandboxGenericMSA();
				msa1.MsaType = MsaType.kInfl;
				SetupLexEntryAndSense(Cache, "ipr-", "inflectional prefix", msa1, out newEntry, out newSense);
				SetupLexEntryAndSense(Cache, "-isu", "inflectional suffix", msa1, out newEntry, out newSense);
				SetupLexEntryAndSense(Cache, "-ii-", "inflectional infix", msa1, out newEntry, out newSense);
				var msa2 = new SandboxGenericMSA();
				msa2.MsaType = MsaType.kStem;
				SetupLexEntryAndSense(Cache, "s", "stem1", msa1, out newEntry, out newSense);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "sa", newEntry.LexemeFormOA.MorphTypeRA);
				SetupLexEntryAndSense(Cache, "s", "stem2", msa1, out newEntry, out newSense);
				var morphTypeEnclitic =
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(
						MoMorphTypeTags.kguidMorphEnclitic);
				var morphTypeProclitic =
					Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(
						MoMorphTypeTags.kguidMorphProclitic);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "pro", morphTypeProclitic);
				AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newEntry, "enc", morphTypeEnclitic);
			}

			FdoCache Cache { get; set; }

			private void SetupPartsOfSpeech()
			{
				// setup language project parts of speech
				var partOfSpeechFactory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
				var adjunct = partOfSpeechFactory.Create();
				var noun = partOfSpeechFactory.Create();
				var verb = partOfSpeechFactory.Create();
				var transitiveVerb = partOfSpeechFactory.Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(adjunct);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(noun);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(verb);
				verb.SubPossibilitiesOS.Add(transitiveVerb);
				adjunct.Name.set_String(Cache.DefaultAnalWs, "adjunct");
				noun.Name.set_String(Cache.DefaultAnalWs, "noun");
				verb.Name.set_String(Cache.DefaultAnalWs, "verb");
				transitiveVerb.Name.set_String(Cache.DefaultAnalWs, "transitive verb");
			}

			static internal void SetupLexEntryAndSense(FdoCache cache, string fullForm, string senseGloss, SandboxGenericMSA msa,
				out ILexEntry lexEntry1_Entry, out ILexSense lexEntry1_Sense1)
			{
				lexEntry1_Entry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(fullForm, senseGloss, msa);
				lexEntry1_Sense1 = lexEntry1_Entry.SensesOS[0];
			}

			static internal void AddAllomorph<TAllomorphFactory, TItem>(ILexEntry entry, string allomorphForm, IMoMorphType morphType)
				where TAllomorphFactory : IFdoFactory<TItem>
				where TItem : IMoForm
			{
				var Cache = entry.Cache;
				ITsString tssAllomorphForm = TsStringUtils.MakeTss(allomorphForm, Cache.DefaultVernWs);
				TAllomorphFactory stemFactory = Cache.ServiceLocator.GetInstance<TAllomorphFactory>();
				var allomorph = stemFactory.Create();
				entry.AlternateFormsOS.Add(allomorph);
				allomorph.MorphTypeRA = morphType;
				allomorph.Form.set_String(TsStringUtils.GetWsAtOffset(tssAllomorphForm, 0), tssAllomorphForm);
			}
		}

		private void CheckExpected<TMorphExpected>(int expectedMatchCount,
			string prefixMarker, string form, string postfixMarker)
		{
			var targetForm = TsStringUtils.MakeTss(form, Cache.DefaultVernWs);
			var matches = MorphServices.GetMatchingMorphs(Cache,
				prefixMarker, targetForm, postfixMarker);
			Assert.AreEqual(expectedMatchCount, matches.Count());
			foreach (var match in matches)
				Assert.IsTrue(match is TMorphExpected, "Expected class " + typeof(TMorphExpected).Name);
		}

		private void CheckEmpty(string prefixMarker, string form, string postfixMarker)
		{
			ITsString tssForm = TsStringUtils.MakeTss(form, Cache.DefaultVernWs);
			var emptySet = MorphServices.GetMatchingMorphs(Cache,
																		prefixMarker, tssForm, postfixMarker);
			Assert.AreEqual(0, emptySet.Count());
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Stems()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				// match stem morphs
				var stems = MorphServices.GetMatchingMorphs(Cache,
					"", TsStringUtils.MakeTss("s", Cache.DefaultVernWs), "");
				Assert.AreEqual(2, stems.Count());
				foreach (var stem in stems)
					Assert.IsTrue(stem is IMoStemAllomorph, "Expected stems");
				// make sure we don't get any results matching prefix/postfix markers.
				CheckEmpty("-", "s", "");
				CheckEmpty("",  "s", "-");
				CheckEmpty("-", "s", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_StemAllomorphs()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				// match stem morphs
				var stemAllomorphs = MorphServices.GetMatchingMorphs(Cache,
					"", TsStringUtils.MakeTss("sa", Cache.DefaultVernWs), "");
				Assert.AreEqual(1, stemAllomorphs.Count());
				foreach (var stem in stemAllomorphs)
					Assert.IsTrue(stem is IMoStemAllomorph, "Expected stem allomorph");
				// make sure we don't get any results matching prefix/postfix markers.
				CheckEmpty("-", "sa", "");
				CheckEmpty("", "sa", "-");
				CheckEmpty("-", "sa", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Prefixes()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var prefixes = MorphServices.GetMatchingMorphs(Cache,
					"", TsStringUtils.MakeTss("ipr", Cache.DefaultVernWs), "-");
				Assert.AreEqual(1, prefixes.Count());
				foreach (var prefix in prefixes)
					Assert.IsTrue(prefix is IMoAffixAllomorph, "Expected affix");
				CheckEmpty("",  "ipr", "");
				CheckEmpty("-", "ipr", "");
				CheckEmpty("-", "ipr", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Suffixes()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var suffixes = MorphServices.GetMatchingMorphs(Cache,
					"-", TsStringUtils.MakeTss("isu", Cache.DefaultVernWs), "");
				Assert.AreEqual(1, suffixes.Count());
				foreach (var suffix in suffixes)
					Assert.IsTrue(suffix is IMoAffixAllomorph, "Expected affix");
				CheckEmpty("",  "isu", "");
				CheckEmpty("",  "isu", "-");
				CheckEmpty("-", "isu", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Proclitic()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var procliticForm = TsStringUtils.MakeTss("pro", Cache.DefaultVernWs);
				var proclitics = MorphServices.GetMatchingMorphs(Cache,
					"", procliticForm, "=");
				Assert.AreEqual(1, proclitics.Count());
				foreach (var proclitic in proclitics)
					Assert.IsTrue(proclitic is IMoStemAllomorph, "Expected proclitic");
				// check that it's okay to match without postfix marker
				CheckExpected<IMoStemAllomorph>(1, "", "pro", "");
				CheckEmpty("-", "pro", "-");
				CheckEmpty("-", "pro", "");
				CheckEmpty("",  "pro", "-");
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MatchingMorphs_Enclitic()
		{
			using (new SetupMatchingMorphsDisabled(this))
			{
				var encliticForm = TsStringUtils.MakeTss("enc", Cache.DefaultVernWs);
				var enclitics = MorphServices.GetMatchingMorphs(Cache,
					"=", encliticForm, "");
				Assert.AreEqual(1, enclitics.Count());
				foreach (var enclitic in enclitics)
					Assert.IsTrue(enclitic is IMoStemAllomorph, "Expected enclitic");
				// check that it's okay to match without postfix marker
				CheckExpected<IMoStemAllomorph>(1, "", "enc", "");
				CheckEmpty("-", "enc", "-");
				CheckEmpty("-", "enc", "");
				CheckEmpty("", "enc", "-");
			}
		}

	}

	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class InflectionalVariantTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{
		static internal ILexEntryInflType InsertInflType<TOwner>(TOwner owningObj, IFdoServiceLocator sl, ITsString variantTypeName, ITsString reverseAbbr,
			string glossPrepend, string glossAppend)
			where TOwner : ICmObject
		{
			var leitfFactory = sl.GetInstance<ILexEntryInflTypeFactory>();

			ILexEntryInflType let = null;
			if (typeof(TOwner) is ICmPossibilityList)
			{
				var owningList = sl.GetInstance<ILexDb>().VariantEntryTypesOA;
				owningList.PossibilitiesOS.Insert(0, leitfFactory.Create());
				let = owningList.PossibilitiesOS[0] as ILexEntryInflType;
			}
			else if (owningObj is ICmPossibility)
			{
				(owningObj as ICmPossibility).SubPossibilitiesOS.Insert(0, leitfFactory.Create());
				let = (owningObj as ICmPossibility).SubPossibilitiesOS[0] as ILexEntryInflType;
			}

			let.Name.set_String(TsStringUtils.GetWsAtOffset(variantTypeName, 0), variantTypeName);
			let.ReverseAbbr.set_String(TsStringUtils.GetWsAtOffset(reverseAbbr, 0), reverseAbbr);

			var wsGlossDefault = TsStringUtils.GetWsAtOffset(variantTypeName, 0);
			let.GlossPrepend.set_String(wsGlossDefault, glossPrepend);
			let.GlossAppend.set_String(wsGlossDefault, glossAppend);

			return let;
		}

		static internal ILexEntryType LookupLexEntryTypeByName(IFdoServiceLocator sl, string variantTypeName, IWritingSystem ws)
		{
			var eng = sl.WritingSystemManager.UserWritingSystem;
			var letRepo = sl.GetInstance<ILexEntryTypeRepository>();
			ILexEntryType let = letRepo.AllInstances().Where(
				someposs => someposs.Name.get_String(ws.Handle).Text == variantTypeName).FirstOrDefault();

			return let;
		}

		static internal ILexEntryType LookupLexEntryType(IFdoServiceLocator sl, Guid guidVariantType)
		{
			var letRepo = sl.GetInstance<ILexEntryInflTypeRepository>();
			return letRepo.GetObject(guidVariantType);
		}

		static internal void SetupLexEntryVariant(FdoCache cache, string morphForm, IVariantComponentLexeme componentLexeme, ILexEntryType let, out ILexEntryRef lef)
		{
			ITsStrFactory sf = TsStrFactoryClass.Create();
			ITsString mf = sf.MakeString(morphForm, cache.DefaultVernWs);
			lef = componentLexeme.CreateVariantEntryAndBackRef(let, mf);
		}

		private void SetupBoundStemAndMainEntryWithVariant()
		{
			ILexEntry newBoundStem;
			ILexEntry newDummyEntry; ILexSense newDummySense;
			var msaStem = new SandboxGenericMSA() {MsaType = MsaType.kStem};
			MatchingMorphsLogicTests.SetupMatchingMorphs.SetupLexEntryAndSense(Cache, "boundStem", "boundStemSense", msaStem, out newBoundStem, out newDummySense);
			newBoundStem.LexemeFormOA.MorphTypeRA = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphBoundStem);
			var newMainEntry = GetNewMainEntry();

			// Setup variant data
			ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);
			ILexEntryRef newLer;
			SetupLexEntryVariant(Cache, "vaIrrInflVar", newBoundStem, letIrrInflVariantType, out newLer);
			AddVariantOf(newLer, newMainEntry, letIrrInflVariantType);
		}

		private static void AddVariantOf(ILexEntryRef newLer, IVariantComponentLexeme newMainEntryOrSense, ILexEntryType letIrrInflVariantType)
		{
			var newVariant = newLer.Owner as ILexEntry;
			newVariant.MakeVariantOf(newMainEntryOrSense, letIrrInflVariantType);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetVariantRef()
		{
			// Setup bound stem before mainEntry, and have varIrrInfVar pointing to both.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, SetupBoundStemAndMainEntryWithVariant);

			/*
			 * Test for bound stem
			 */
			var variant = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaIrrInflVar").FirstOrDefault();
			{
				var variantRef1 = DomainObjectServices.GetVariantRef(variant, false);
				var entry1 = variantRef1.ComponentLexemesRS[0] as ILexEntry;
				Assert.That(entry1, Is.Not.Null);
				Assert.That(entry1.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphBoundStem));
			}

			/*
			 * Test for excluding bound stem
			 */
			{
				var variantRef2 = DomainObjectServices.GetVariantRef(variant, true);
				var entry2 = variantRef2.ComponentLexemesRS[0] as ILexEntry;
				Assert.That(entry2, Is.Not.Null);
				Assert.That(entry2.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphStem));
			}
		}


		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetVariantRefs()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, SetupBoundStemAndMainEntryWithVariant);

			/*
			 * Test for order of variant refs
			 */
			var variant = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaIrrInflVar").FirstOrDefault();
			{
				var variantRefs = DomainObjectServices.GetVariantRefs(variant);
				Assert.That(variantRefs.Count(), Is.EqualTo(2));
				var varRef1 = variantRefs.ElementAt(0);
				var varRef2 = variantRefs.ElementAt(1);

				// First ref should point to bound stem
				{
					var entry1 = varRef1.ComponentLexemesRS[0] as ILexEntry;
					Assert.That(entry1, Is.Not.Null);
					Assert.That(entry1.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphBoundStem));
				}

				// Second should point to stem
				{
					var entry2 = varRef2.ComponentLexemesRS[0] as ILexEntry;
					Assert.That(entry2, Is.Not.Null);
					Assert.That(entry2.LexemeFormOA.MorphTypeRA.Guid, Is.EqualTo(MoMorphTypeTags.kguidMorphStem));
				}
			}

		}

		private IList<ILexEntryType> GetNonInflVariantEntryTypes(ILexEntryRef variantRef)
		{
			return (from variantType in variantRef.VariantEntryTypesRS
					where !(variantType is ILexEntryInflType)
					select variantType).ToList();
		}

		/// <summary>
		///
		/// </summary>
		private void DoSetupNewInflVariantTypes_OneTypePerEntryRef()
		{
			ILexEntry newMainEntry = GetNewMainEntry();

			// Setup variant data
			ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

			ITsStrFactory sf = TsStrFactoryClass.Create();
			// Create new variantType
			var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
			var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
			ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
														sf.MakeString("NewPlural", eng.Handle),
														sf.MakeString("NPl.", eng.Handle),
														"", ".PL");
			ILexEntryType letNewPast = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
													  sf.MakeString("NewPast", eng.Handle),
													  sf.MakeString("NPst.", eng.Handle),
													  "", ".PST");
			ILexEntryRef newLerPlural;
			SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			ILexEntryRef newLerPast;
			SetupLexEntryVariant(Cache, "vaNewPast", newMainEntry, letNewPast, out newLerPast);
		}

		private ILexEntry GetNewMainEntry(string mainEntryForm = "mainEntry", string mainEntrySenseGloss = "mainEntrySense1")
		{
			ILexEntry newMainEntry;
			ILexEntry newDummyEntry;
			ILexSense newDummySense;
			var msaStem = new SandboxGenericMSA();
			msaStem.MsaType = MsaType.kStem;
			MatchingMorphsLogicTests.SetupMatchingMorphs.SetupLexEntryAndSense(Cache, mainEntryForm, mainEntrySenseGloss, msaStem, out newMainEntry, out newDummySense);
			MatchingMorphsLogicTests.SetupMatchingMorphs.AddAllomorph<IMoStemAllomorphFactory, IMoStemAllomorph>(newMainEntry, "mainEntryAllomorph1", newMainEntry.LexemeFormOA.MorphTypeRA);
			return newMainEntry;
		}


		/// <summary>
		///
		/// </summary>
		[Test]
		public void GetNewInflVariantTypesOfMainEntryVariants()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DoSetupNewInflVariantTypes_OneTypePerEntryRef);

			var mainEntry = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("mainEntry").FirstOrDefault();
			var variantRefsOfmainEntry = mainEntry.VariantFormEntryBackRefs;
			Assert.That(variantRefsOfmainEntry.Count(), Is.EqualTo(2), "Not enough variant refs created.");
			CollectionAssert.AllItemsAreInstancesOfType(variantRefsOfmainEntry, typeof(ILexEntryRef), "Variant reference was of an unexpected type.");
			var newInflVariantType1 = variantRefsOfmainEntry.ElementAt(0).VariantEntryTypesRS[0] as ILexEntryInflType;
			var newInflVariantType2 = variantRefsOfmainEntry.ElementAt(1).VariantEntryTypesRS[0] as ILexEntryInflType;

			CollectionAssert.Contains(new [] {"NewPlural", "NewPast"}, newInflVariantType1.Name.UserDefaultWritingSystem.Text,
				"One of the inflectional variant types didn't have any expected contents");
			CollectionAssert.Contains(new[] { "NewPlural", "NewPast" }, newInflVariantType2.Name.UserDefaultWritingSystem.Text,
				"One of the inflectional variant types didn't have any expected contents");
		}

		private static void GetMainVariantGloss(ILexEntryRef variantRef, out IMultiUnicode gloss)
		{
			ILexSense mainOrFirstSense = MorphServices.GetMainOrFirstSenseOfVariant(variantRef);
			gloss = null;
			if (mainOrFirstSense != null)
				gloss = mainOrFirstSense.Gloss;
		}

		/// <summary>
		/// TODO: move +.pl or +.pst to LexGloss (see LT-9681).
		/// </summary>
		[Test]
		public void GlossOfNonInflVariantOfSense_SingleType()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, ()=>
				{
					ILexEntry newMainEntry = GetNewMainEntry();
					newMainEntry.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
					newMainEntry.SensesOS[1].Gloss.set_String(Cache.DefaultAnalWs, "mainEntrySense2");

					ILexEntryType letDialectalVariantType = Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypDialectalVar);
					// For some reason the reverse abbr isn't already set up in the test cache
					const string variantReverseAbbr = "dial. var. of";
					var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
					letDialectalVariantType.ReverseAbbr.set_String(eng.Handle, variantReverseAbbr);
					ILexEntryRef newLer;
					SetupLexEntryVariant(Cache, "vaDialVar", newMainEntry.SensesOS[0], letDialectalVariantType, out newLer);
					AddVariantOf(newLer, newMainEntry.SensesOS[1], letDialectalVariantType);
				});

			var variant = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaDialVar").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variant);
			Assert.That(variantRefs.Count(), Is.EqualTo(2));
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				IMultiUnicode gloss1;
				GetMainVariantGloss(variantRefs.ElementAt(0), out gloss1);

				var variantGloss = MorphServices.MakeGlossWithReverseAbbrs(gloss1, analWs, variantRefs.ElementAt(0).VariantEntryTypesRS);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("mainEntrySense1+dial. var. of"));
			}
			{
				IMultiUnicode gloss2;
				GetMainVariantGloss(variantRefs.ElementAt(1), out gloss2);
				var variantGloss = MorphServices.MakeGlossWithReverseAbbrs(gloss2, analWs, variantRefs.ElementAt(1).VariantEntryTypesRS);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("mainEntrySense2+dial. var. of"));
			}
		}

		/// <summary>
		/// TODO: If the variant itself has a glosss use it? (Ask Beth Bryson)
		/// TODO: What do we do if there is no sense, or no gloss? Use lexEntry form instead? (or just put the info on the LexEntry line?)
		/// TODO: should default separator for variants using reverse abbrs still be '+' rather than '.' because reverse abbr uses '.'
		/// TODO: handle case where GlossAppend/GlossPrepend does not exist
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_DefaultToReverseAbbr()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, ()=>
				{
					ILexEntry newMainEntry = GetNewMainEntry();

					// Setup variant data
					ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

					ITsStrFactory sf = TsStrFactoryClass.Create();
					// Create new variantType
					var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
					var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
					ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
																sf.MakeString("NewPlural", eng.Handle),
																sf.MakeString("NPl.", eng.Handle),
																"", "");
					ILexEntryRef newLerPlural;
					SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
				});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("mainEntrySense1+NPl."));
			}
		}

		/// <summary>
		/// Construct "GP.mainEntrySense1" with GlossPrepend "GP"
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_GlossPrepend_UnSpecifiedDivider()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry();

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("NPl.", eng.Handle),
															"GP", "");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("GP.mainEntrySense1"));
			}
		}

		/// <summary>
		/// Construct @"GP\mainEntrySense1" with GlossPrepend @"GP\"
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_GlossPrepend_SpecifiedDivider()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry();

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("NPl.", eng.Handle),
															@"GP\", "");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo(@"GP\mainEntrySense1"));
			}
		}


		/// <summary>
		/// Construct "mainEntrySense1.GA" with GlossAppend "GA"
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_GlossAppend_UnSpecifiedDivider()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry();

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("NPl.", eng.Handle),
															"", "GA");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("mainEntrySense1.GA"));
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExtractDivider_AtEnd()
		{
			{
				string extractedDivider = MorphServices.ExtractDivider("+S", 0);
				Assert.That(extractedDivider, Is.EqualTo("+"));
			}
			{
				string extractedDivider2 = MorphServices.ExtractDivider("S", 0);
				Assert.That(extractedDivider2, Is.EqualTo(""));
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExtractDivider_AtBeginning()
		{
			{
				string extractedDivider = MorphServices.ExtractDivider("S+", -1);
				Assert.That(extractedDivider, Is.EqualTo("+"));
			}
			{
				string extractedDivider2 = MorphServices.ExtractDivider("S", -1);
				Assert.That(extractedDivider2, Is.EqualTo(""));
			}
		}

		/// <summary>
		/// Construct "mainEntrySense1/GA" with GlossAppend "/GA"
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_GlossAppend_SpecifiedDivider()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry();

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("NPl.", eng.Handle),
															"", "/GA");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("mainEntrySense1/GA"));
			}
		}

		/// <summary>
		/// Construct "***.GA" with GlossAppend "GA"
		/// TODO: Ask Andy Black what to show in this case.
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_SenseHasEmptyGloss()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss:"");

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("NPl.", eng.Handle),
															"", "GA");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("***.GA"));
			}
		}

		/// <summary>
		/// Construct "***+***" with no sense gloss and no variant reverse abbr.
		/// TODO: Ask Andy Black what to show in this case.
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_EmptyGlossAndVariantTypeHasEmptyReverseAbbr()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss: "");

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("", eng.Handle),
															"", "");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("***+***"));
			}
		}

		/// <summary>
		/// TODO: how to avoid infinite recursion if user specifies that a variant1 is a variant of variant2 is a variant of variant1? (Or detect this situation.)
		/// TODO: should we recurse in a way that is depth first or bredth first?
		/// TODO: Ask Andy Black what to show in this case.
		/// </summary>
		[Ignore("find the first available gloss.")]
		[Test]
		public void GlossOfInflVariantOfVariant_FindFirstGloss()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss: "mainEntrySense");

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("", eng.Handle),
															"", "PL");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
				ILexEntryRef newLerPlural2;
				SetupLexEntryVariant(Cache, "vaVaNewPlural", newLerPlural.Owner as ILexEntry, letNewPlural, out newLerPlural2);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaVaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("mainEntrySense.PL"));
			}
		}

		/// <summary>
		/// Construct "***+***" with no sense gloss and no variant reverse abbr.
		/// TODO: Ask Andy Black what to show in this case.
		/// </summary>
		[Ignore("find the first available gloss.")]
		[Test]
		public void GlossOfInflVariantOfVariant_MissingSense()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss: "");

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("", eng.Handle),
															"", "PL");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaNewPlural", newMainEntry, letNewPlural, out newLerPlural);
				ILexEntryRef newLerPlural2;
				SetupLexEntryVariant(Cache, "vaVaNewPlural", newLerPlural.Owner as ILexEntry, letNewPlural, out newLerPlural2);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaVaNewPlural").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var variantTypePl = variantRefPl.VariantEntryTypesRS[0];
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(variantTypePl, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("***+***"));
			}
		}

		/// <summary>
		/// join each gloss append together (".pl.pst").
		///
		/// NOTE: This is a special case where the user has not yet made a choice between .pst and .pl
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_MultipleVariantTypesPerEntryRef_GlossAppend()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss: "mainEntrySense");

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("", eng.Handle),
															"", ".pl");
				ILexEntryType letNewPst = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPast", eng.Handle),
															sf.MakeString("", eng.Handle),
															"", "pst");
				ILexEntryRef newLerPluralPast;
				SetupLexEntryVariant(Cache, "vaNewPluralPast", newMainEntry, letNewPlural, out newLerPluralPast);
				newLerPluralPast.VariantEntryTypesRS.Add(letNewPst);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPluralPast").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var glossWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				ITsIncStrBldr sbPrepend;
				ITsIncStrBldr sbAppend;
				MorphServices.JoinGlossAffixesOfInflVariantTypes(variantRefPl.VariantEntryTypesRS, glossWs, out sbPrepend, out sbAppend);
				Assert.That(sbAppend.Text, Is.EqualTo(".pl.pst"));
				Assert.That(sbPrepend.Text, Is.Null);
			}
		}

		/// <summary>
		///
		/// join each gloss prepend together ("pst.pl.")
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_MultipleVariantTypesPerEntryRef_GlossPrepend()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss: "mainEntrySense");

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("", eng.Handle),
															"pl", "");
				ILexEntryType letNewPst = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPast", eng.Handle),
															sf.MakeString("", eng.Handle),
															"pst.", "");
				ILexEntryRef newLerPluralPast;
				SetupLexEntryVariant(Cache, "vaNewPluralPast", newMainEntry, letNewPlural, out newLerPluralPast);
				newLerPluralPast.VariantEntryTypesRS.Add(letNewPst);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPluralPast").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var glossWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				ITsIncStrBldr sbPrepend;
				ITsIncStrBldr sbAppend;
				MorphServices.JoinGlossAffixesOfInflVariantTypes(variantRefPl.VariantEntryTypesRS, glossWs, out sbPrepend, out sbAppend);
				Assert.That(sbPrepend.Text, Is.EqualTo("pl.pst."));
				Assert.That(sbAppend.Text, Is.Null);
			}
		}

		/// <summary>
		/// join each gloss append together ("pl. (gloss) .pst")
		/// </summary>
		[Test]
		public void GlossOfInflVariantOfEntry_MultipleVariantTypesPerEntryRef_MixGlossPrependAppend()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss: "mainEntrySense");

				// Setup variant data
				ILexEntryType letIrrInflVariantType = LookupLexEntryType(Cache.ServiceLocator, LexEntryTypeTags.kguidLexTypIrregInflectionVar);

				ITsStrFactory sf = TsStrFactoryClass.Create();
				// Create new variantType
				var variantTypesList = Cache.LanguageProject.LexDbOA.VariantEntryTypesOA;
				var eng = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;
				ILexEntryType letNewPlural = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPlural", eng.Handle),
															sf.MakeString("", eng.Handle),
															"pl.", "");
				ILexEntryType letNewPst = InsertInflType(letIrrInflVariantType, Cache.ServiceLocator,
															sf.MakeString("NewPast", eng.Handle),
															sf.MakeString("", eng.Handle),
															"", ".pst");
				ILexEntryRef newLerPluralPast;
				SetupLexEntryVariant(Cache, "vaNewPluralPast", newMainEntry, letNewPlural, out newLerPluralPast);
				newLerPluralPast.VariantEntryTypesRS.Add(letNewPst);
			});

			var variantPl = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaNewPluralPast").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantPl);
			var variantRefPl = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRefPl, out gloss);
			var glossWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				ITsIncStrBldr sbPrepend;
				ITsIncStrBldr sbAppend;
				MorphServices.JoinGlossAffixesOfInflVariantTypes(variantRefPl.VariantEntryTypesRS, glossWs, out sbPrepend, out sbAppend);
				Assert.That(sbPrepend.Text, Is.EqualTo("pl."));
				Assert.That(sbAppend.Text, Is.EqualTo(".pst"));
			}
		}

		/// <summary>
		///
		/// TODO: Ask Beth to review this case
		/// </summary>
		[Ignore("find the first available gloss.")]
		[Test]
		public void GlossOfInflVariantOfEntry_UseGlossOfVariant()
		{
		}

		/// <summary>
		///
		/// TODO: Ask Andy Black what to show in this case.
		/// </summary>
		[Ignore("pick the first lexeme component? Currently we skip these.")]
		[Test]
		public void GlossOfInflVariantOfEntry_MultipleLexemeComponents()
		{
		}

		/// <summary>
		///
		/// TODO: Ask Andy Black what to show in this case.
		/// </summary>
		[Ignore("pick the first lexeme component? Currently we skip these.")]
		[Test]
		public void GlossOfInflVariantOfEntry_NoLexemeComponents()
		{
		}


		/// <summary>
		/// Currently I think we just display the gloss without any +***"
		/// TODO: Ask Andy Black what to show in this case.
		/// </summary>
		[Ignore("consider deleting this test. Probably don't need to support just a gloss with this interface.")]
		[Test]
		public void GlossOfVariantOfEntry_MissingVariantType()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				ILexEntry newMainEntry = GetNewMainEntry(mainEntrySenseGloss: "mainEntrySense1");
				ILexEntryRef newLerPlural;
				SetupLexEntryVariant(Cache, "vaMissingType", newMainEntry, null, out newLerPlural);
			});

			var variantMissingType = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs("vaMissingType").FirstOrDefault();
			var variantRefs = DomainObjectServices.GetVariantRefs(variantMissingType);
			var variantRef = variantRefs.First();
			IMultiUnicode gloss;
			GetMainVariantGloss(variantRef, out gloss);
			var analWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultAnalWs);
			{
				var variantGloss = MorphServices.MakeGlossOptionWithInflVariantTypes(null, gloss, analWs);
				Assert.That(variantGloss, Is.Not.Null);
				Assert.That(variantGloss.Text, Is.EqualTo("mainEntrySense1"));
			}
		}
	}

	/// <summary>
	/// We expect Undo to undo all the data created during a test.
	/// To verify this, we run two tests, both creating a new entry and testing the same preconditions and postconditions
	/// </summary>
	[TestFixture]
	public class UndoAllIssueTest : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private void MakeSureEverythingIsUndoneAfterTests()
		{
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count, "Expected no initial entries");
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<ILexSenseRepository>().Count, "Expected no initial senses");
			Assert.AreEqual(0, Cache.ServiceLocator.GetInstance<IMoFormRepository>().Count, "Expected no initial moForms");

			ITsString tssFullForm = TsStringUtils.MakeTss("entryToUndo", Cache.DefaultVernWs);
			var entryComponents = MorphServices.BuildEntryComponents(Cache,
																			tssFullForm);
			entryComponents.GlossAlternatives.Add(TsStringUtils.MakeTss("senseToUndo", Cache.DefaultVernWs));
			ILexEntry newEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);

			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count);
			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<ILexSenseRepository>().Count);
			Assert.AreEqual(1, Cache.ServiceLocator.GetInstance<IMoFormRepository>().Count);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NewEntry()
		{
			MakeSureEverythingIsUndoneAfterTests();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NewEntry2()
		{
			MakeSureEverythingIsUndoneAfterTests();
		}
	}
}
