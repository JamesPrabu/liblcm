﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.LCModel.Infrastructure
{
	/// <summary>
	/// Domain to have bulk loaded by the backend provider
	/// </summary>
	public enum BackendBulkLoadDomain
	{
		/// <summary>WFI and WfiWordforms</summary>
		WFI,
		/// <summary>Lexicon and entries.</summary>
		Lexicon,
		/// <summary>Texts and their paragraphs (no wordforms)</summary>
		Text,
		/// <summary></summary>
		Scripture,
		/// <summary>Load everything.</summary>
		All,
		/// <summary>Strictly 'on demand' loading</summary>
		None
	}

	/// <summary>
	/// Enumeration of types or main object properties.
	/// </summary>
	internal enum ObjectPropertyType
	{
		Owning,
		Reference
	} ;

	/// <summary>
	/// Enumeration of DVCS types
	/// </summary>
	public enum DvcsType
	{
		/// <summary>
		/// No DVCS is being used
		/// </summary>
		None = 0,

		/// <summary>
		/// Mercurial
		/// </summary>
		Mercurial = 1, //BackendProviderType.kMercurial,

		/// <summary>
		/// Git
		/// </summary>
		Git = 2, //BackendProviderType.kGit
	} ;


}
