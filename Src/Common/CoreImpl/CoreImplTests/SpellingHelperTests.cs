using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Tests (for now pretty incomplete) of the SpellingHelper class.
	/// </summary>
	/// <remarks>If you get failing tests on Linux make sure that Hunspell package is installed
	/// (currently libhunspell-dev, but eventually we'll need a custom package for FW)</remarks>
	[TestFixture]
	public class SpellingHelperTests
	{
		// TODO-Linux: need slightly modified hunspell package installed!

		/// <summary>
		/// Check how spelling status is set and cleared.
		/// </summary>
		[Test]
		public void BasicSpellingStatus()
		{
			var dictId = MakeEmptyFooDictionary();
			var dict = SpellingHelper.GetSpellChecker(dictId);
			Assert.That(dict, Is.Not.Null);
			Assert.That(dict.Check("nonsense"), Is.False);
			Assert.That(dict.Check("big"), Is.False);
			dict.SetStatus("big", true);
			Assert.That(dict.Check("big"), Is.True);
			// By default Hunspell fails this test; setting "big" to correct makes "Big" correct too.
			// We override this behavior (in a rather tricky way) for vernacular dictionaries.
			Assert.That(dict.Check("Big"), Is.False);
			dict.SetStatus("Big", false);
			Assert.That(dict.Check("Big"), Is.False);
			Assert.That(dict.Check("big"), Is.True);

			// If we set the upper case version only, that is considered correct, but the LC version is not.
			Assert.That(dict.Check("Bother"), Is.False);
			dict.SetStatus("Bother", true);
			Assert.That(dict.Check("Bother"), Is.True);
			Assert.That(dict.Check("bother"), Is.False);

			// Subsequently explicitly setting the LC version to false is not a problem.
			dict.SetStatus("bother", false);
			Assert.That(dict.Check("Bother"), Is.True);
			Assert.That(dict.Check("bother"), Is.False);

			// Now if we set the UC version false, both are.
			dict.SetStatus("Bother", false);
			Assert.That(dict.Check("Bother"), Is.False);
			Assert.That(dict.Check("bother"), Is.False);

			// Now both are explicitly false. Set the LC one to true.
			dict.SetStatus("bother", true);
			Assert.That(dict.Check("Bother"), Is.False);
			Assert.That(dict.Check("bother"), Is.True);

			// Now make the LC one false again.
			dict.SetStatus("bother", false);
			Assert.That(dict.Check("Bother"), Is.False);
			Assert.That(dict.Check("bother"), Is.False);

			// Now make the UC one true again.
			dict.SetStatus("Bother", true);
			Assert.That(dict.Check("Bother"), Is.True);
			Assert.That(dict.Check("bother"), Is.False);

			// There is a special case in the code when the new word is alphabetically after any we already added.
			dict.SetStatus("world", true);
			Assert.That(dict.Check("world"), Is.True);

			// Check normalization
			dict.SetStatus("w\x00f4rld", true); // o with cicrumflex (composed)
			Assert.That(dict.Check("w\x00f4rld"), Is.True);
			Assert.That(dict.Check("wo\x0302rld"), Is.True); // decomposed form.
			dict.SetStatus("bo\x0302lt", true); // set decomposed
			Assert.That(dict.Check("bo\x0302lt"), Is.True);
			Assert.That(dict.Check("b\x00f4lt"), Is.True); // composed form.


			// Check that results were persisted.
			SpellingHelper.ClearAllDictionaries();
			dict = SpellingHelper.GetSpellChecker(dictId);
			Assert.That(dict.Check("Bother"), Is.True);
			Assert.That(dict.Check("world"), Is.True);
		}

		private static string MakeEmptyFooDictionary()
		{
			Directory.CreateDirectory(SpellingHelper.GetSpellingDirectoryPath());
			var dictId = "foo";
			var filePath = SpellingHelper.GetDicPath(SpellingHelper.GetSpellingDirectoryPath(), dictId);
			File.Delete(filePath);
			File.Delete(Path.ChangeExtension(filePath, ".aff"));
			File.Delete(Path.ChangeExtension(filePath, ".exc"));
			SpellingHelper.EnsureDictionary(dictId);
			return dictId;
		}

		/// <summary>
		/// A dictionary created by EnsureDictionary should be recognized as private, and not returned when
		/// we see a dictionary with a shorter name.
		/// </summary>
		[Test]
		public void OurDictionaryIsPrivate()
		{
			var dictId = MakeEmptyFooDictionary();
			var dict = SpellingHelper.GetSpellChecker(dictId);
			dict.SetStatus("big", true);
			var otherDict = SpellingHelper.GetSpellChecker("fo"); // try to get one with a shorter name

			Assert.That(otherDict, Is.Null);
		}

		/// <summary>
		/// A dictionary we did not create should not be private
		/// </summary>
		[Test]
		public void OtherDictionaryIsNotPrivate()
		{
			// Create a dictionary file for "blah" that does NOT look like one of ours.
			MakeNonPrivateBlahDictionary();

			var dict = SpellingHelper.GetSpellChecker("blah");
			dict.SetStatus("big", true);
			Assert.That(dict, Is.Not.Null);
			Assert.That(dict.Check("big"), Is.True);
			var otherDict = SpellingHelper.GetSpellChecker("bl"); // try to get one with a shorter name

			Assert.That(otherDict, Is.EqualTo(dict), "getting a prefix of a non-private dictionary should find the non-private dictionary");
		}

		private static void MakeNonPrivateBlahDictionary()
		{
			string dirPath = SpellingHelper.GetSpellingDirectoryPath();
			if (!Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);
			string filePath = SpellingHelper.GetDicPath(dirPath, "blah");
			File.Delete(filePath);
			File.Delete(Path.ChangeExtension(filePath, ".aff"));
			File.Delete(Path.ChangeExtension(filePath, ".exc"));
			using (var writer = FileUtils.OpenFileForWrite(Path.ChangeExtension(filePath, ".aff"), Encoding.UTF8))
			{
				writer.WriteLine("SET UTF-8");
			}
			using (var writer = FileUtils.OpenFileForWrite(filePath, Encoding.UTF8))
			{
				writer.WriteLine("10");
			}
		}

		/// <summary>
		/// Tests the ResetDictionary method
		/// </summary>
		[Test]
		public void ResetDictionaryAddsAllWords()
		{
			var id = MakeEmptyFooDictionary();
			var dict = SpellingHelper.GetSpellChecker(id);
			dict.SetStatus("old", true);
			Assert.That(dict.Check("old"), Is.True);

			SpellingHelper.ResetDictionary(id, new[] {"hello", "wo\x0302rld", "this", "is", "a", "test"});
			dict = SpellingHelper.GetSpellChecker(id);

			Assert.That(dict.Check("old"), Is.False);
			Assert.That(dict.Check("hello"), Is.True);
			Assert.That(dict.Check("w\x00f4rld"), Is.True);
			Assert.That(dict.Check("wo\x0302rld"), Is.True); // decomposed form.
			Assert.That(dict.Check("is"), Is.True);
			Assert.That(dict.Check("a"), Is.True);
			Assert.That(dict.Check("test"), Is.True);
		}

		/// <summary>
		/// Check that we don't 'reset' (overwrite) dictionaries we didn't make
		/// </summary>
		[Test]
		public void ResetDictionary_ThrowsForNonPrivateDictionary()
		{
			MakeNonPrivateBlahDictionary();
			Assert.Throws<ArgumentException>(() => SpellingHelper.ResetDictionary("blah", new[] {"hello", "world"}));
		}

		/// <summary>
		/// Check that loading the exceptions works for a non-vernacular dictionary.
		/// </summary>
		[Test]
		public void ExceptionListIsLoadedForNonVernacularDictionary()
		{
			MakeNonPrivateBlahDictionary();
			var filePath = SpellingHelper.GetDicPath(SpellingHelper.GetSpellingDirectoryPath(), "blah");
			using (var writer = FileUtils.OpenFileForWrite(Path.ChangeExtension(filePath, ".exc"), Encoding.UTF8))
			{
				writer.WriteLine("good");
			}
			var dict = SpellingHelper.GetSpellChecker("blah");
			Assert.That(dict.Check("good"), Is.True);
		}

		/// <summary>
		/// This guards against a weird case we encountered in real life
		/// </summary>
		[Test]
		public void AddBaddBeforeStarSpellling()
		{
			MakeEmptyFooDictionary();
			var dict = SpellingHelper.GetSpellChecker("foo");
			dict.SetStatus("spellling", false);
			dict.SetStatus("badd", true);
			SpellingHelper.ClearAllDictionaries();
			dict = SpellingHelper.GetSpellChecker("foo");
			Assert.That(dict.Check("spellling"), Is.False);
			Assert.That(dict.Check("badd"), Is.True);
		}

		/// <summary>
		/// See if we can get suggestions.
		/// </summary>
		[Test]
		public void GetSimpleSuggestion()
		{
			MakeEmptyFooDictionary();
			var dict = SpellingHelper.GetSpellChecker("foo");
			dict.SetStatus("bad", true);
			var suggestions = dict.Suggest("badd");
			Assert.That(suggestions.Count, Is.GreaterThanOrEqualTo(1));
			Assert.That(suggestions.First(), Is.EqualTo("bad"));
		}
	}
}
