// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TsTextPropsHelperTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TsTextPropsHelperTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for equality when props are equal.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PropsAreEqual()
		{
			// test of same int prop: writing system
			ITsPropsBldr tsPropsBldr = TsStringUtils.MakePropsBldr();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, 4565754);
			ITsTextProps ttp1 = tsPropsBldr.GetTextProps();
			ITsTextProps ttp2 = tsPropsBldr.GetTextProps();
			string s;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));

			// test of same int & string props
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"my style"); //add string prop to prior int prop
			ttp1 = tsPropsBldr.GetTextProps();
			ttp2 = tsPropsBldr.GetTextProps();
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));
			Assert.AreEqual("TextProps objects appear to contain the same properties.", s);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for inequality of int props
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PropsDifferByIntProps()
		{
			// test of different int prop: writing system
			ITsPropsBldr tsPropsBldr = TsStringUtils.MakePropsBldr();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, 4565754);
			ITsTextProps ttp1 = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, 4565753);
			ITsTextProps ttp2 = tsPropsBldr.GetTextProps();
			string s;
			Assert.IsFalse(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));
			Assert.AreEqual("Props differ in ktptWs property. Expected ws <4565754> and var <0>, but was ws <4565753> and var <0>.", s);

			// test of different int prop: background color
			tsPropsBldr = TsStringUtils.MakePropsBldr(); // empty builder
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"my style"); //string prop, same for both
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault, 98);
			ttp1 = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault, 99);
			ttp2 = tsPropsBldr.GetTextProps();
			Assert.IsFalse(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));
			Assert.AreEqual("Props differ in intProp type 9. "
				+ "Expected <98,0>, but was <99,0>.", s);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for inequality of string props
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PropsDifferByStrProps()
		{
			// test of different str prop: named style
			ITsPropsBldr tsPropsBldr = TsStringUtils.MakePropsBldr();
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"my style");
			ITsTextProps ttp1 = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				"your style");
			ITsTextProps ttp2 = tsPropsBldr.GetTextProps();
			string s;
			Assert.IsFalse(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));
			Assert.AreEqual("Props differ in ktptNamedStyle property. "
				+ "Expected <my style>, but was <your style>.", s);

			// test of different str prop: named style
			tsPropsBldr = TsStringUtils.MakePropsBldr(); // empty builder
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, 4565754); // int prop, same for both
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
				"my font");
			ttp1 = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
				"your font");
			ttp2 = tsPropsBldr.GetTextProps();
			Assert.IsFalse(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));
			Assert.AreEqual("Props differ in strProp type 1. "
				+ "Expected <my font>, but was <your font>.", s);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for different numbers of string and integer properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PropsDifferByCount()
		{
			// test of different string property counts
			ITsPropsBldr tsPropsBldr = TsStringUtils.MakePropsBldr();
			//note: due to design in PropsAreEqual(), we will get the count message
			// only when all the str props in the first ttp match in the second ttp, and the
			// second ttp has additional str props
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
				"my font");
			ITsTextProps ttp1 = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "my style");
			ITsTextProps ttp2 = tsPropsBldr.GetTextProps();
			string s;
			Assert.IsFalse(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));
			Assert.AreEqual("Props differ in count of strProps. Expected <1>, but was <2>.", s);

			// test of different int property counts
			tsPropsBldr = TsStringUtils.MakePropsBldr(); // empty builder
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault, 123);
			ttp1 = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderTop,
				(int)FwTextPropVar.ktpvDefault, 10);
			ttp2 = tsPropsBldr.GetTextProps();
			Assert.IsFalse(TsTextPropsHelper.PropsAreEqual(ttp1, ttp2, out s));
			Assert.AreEqual("Props differ in count of intProps. Expected <1>, but was <2>.", s);
		}
	}
}
