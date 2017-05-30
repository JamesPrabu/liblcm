// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IDataMigrationManager.cs
// Responsibility: FW team

using System;
using SIL.LCModel.Utils;

namespace SIL.LCModel.DomainServices.DataMigration
{
	#region IDataMigrationManager interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for data migration that might be needed after model changes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal interface IDataMigrationManager
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the data migration on the DTOs in the given repository.
		///
		/// The resulting xml in the DTOs must be compatible with the xml of the given
		/// <paramref name="updateToVersion"/> number.
		///
		/// The implementation of this interface will process as many migrations, in sequence,
		/// as are needed to move from whatever version is in the repository to bring it up to
		/// the specified version number in <paramref name="updateToVersion"/>.
		/// </summary>
		/// <param name="domainObjectDtoRepository">Repository of CmObject DTOs.</param>
		/// <param name="updateToVersion">
		/// Model version number the updated repository should have after the migration
		/// </param>
		/// <param name="progressDlg">The dialog to display migration progress</param>
		/// <remarks>
		/// An implementation of this interface is free to define its own migration definitions.
		/// These definitions need not be compatible with any other implementation of this interface.
		/// That is, there is no interface defined for declaring and consuming data migration instructions.
		/// </remarks>
		/// <exception cref="DataMigrationException">
		/// Thrown if the migration cannot be done successfully for the following reasons:
		/// <list type="disc">
		/// <item>
		/// <paramref name="domainObjectDtoRepository"/> is null.
		/// </item>
		/// <item>
		/// Don't know how to upgrade to version: <paramref name="updateToVersion"/>.
		/// </item>
		/// <item>
		/// Version number in <paramref name="domainObjectDtoRepository"/>
		/// is newer than <paramref name="updateToVersion"/>.
		/// </item>
		/// </list>
		/// </exception>
		/// ------------------------------------------------------------------------------------
		void PerformMigration(IDomainObjectDTORepository domainObjectDtoRepository,
			int updateToVersion, IThreadedProgress progressDlg);

		/// <summary>
		/// An optional check for real data migration needs can be performed before calling
		/// PerformMigration by calling this method. If it returns <c>true</c>,
		/// then real data changes will be required to get from
		/// <paramref name="startingVersionNumber"/> to <paramref name="endingVersionNumber"/>.
		/// </summary>
		/// <param name="startingVersionNumber">The starting version number of the data.</param>
		/// <param name="endingVersionNumber">
		/// The desired ending number of the migration.
		/// NB: This should never be higher than the migration manaer can support,
		/// but it may be lower than highest supported migration number.
		/// </param>
		/// <returns><c>true</c> if the migration requires real data migration, otherwise
		/// <c>false</c>, in which case the caller may opt to just update its own
		/// version number to <paramref name="endingVersionNumber"/>.</returns>
		/// <exception cref="DataMigrationException">
		/// Thrown on any of the following conditions:
		///
		/// 1. <paramref name="startingVersionNumber"/> is less than 7000000,
		/// 2. <paramref name="endingVersionNumber"/> is greater than the highest
		///		currently suported migration version number, or
		/// 3. <paramref name="startingVersionNumber"/> is greater than
		///		<paramref name="endingVersionNumber"/>.
		/// </exception>
		bool NeedsRealMigration(int startingVersionNumber, int endingVersionNumber);
	}
	#endregion

	#region DataMigrationException class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exception for any type of problem with data migration. It may contain an inner exception.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DataMigrationException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="message"></param>
		/// ------------------------------------------------------------------------------------
		public DataMigrationException(string message)
			: base(message)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		/// ------------------------------------------------------------------------------------
		public DataMigrationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
	#endregion
}
