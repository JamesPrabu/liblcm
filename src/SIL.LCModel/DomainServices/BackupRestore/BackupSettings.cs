﻿// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using SIL.LCModel.Utils;
using SIL.Lexicon;

namespace SIL.LCModel.DomainServices.BackupRestore
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Abstract base class used for the settings classes that mask the UI to store user choices
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public abstract class BackupSettings : IBackupSettings
	{
		#region Constants
		/// <summary>Temp folder where spelling dictionary files are copied</summary>
		internal const string ksSpellingDictionariesDir = "SpellingDictionaries";

		/// <summary>
		/// The format string used for formatting and parsing datetime strings in backup file names
		/// </summary>
		public const string ksBackupDateFormat = "yyyy-MM-dd HHmm";
		#endregion

		#region Member variables
		private DateTime m_backupTime;
		private string m_comment;
		private string m_projectName;
		private bool m_includeConfigurationSettings;
		private bool m_includeMediaFiles;
		private bool m_includeSupportingFiles;
		private bool m_includeSpellCheckAdditions;

		/// <summary>
		/// The root folder for projects (typically the default, but if these settings represent
		/// a project elsewhere, then this will be the root folder for that project)
		/// </summary>
		private readonly string m_projectsRootFolder;

		/// <summary>
		/// Setting this variable allows us to set the LinkedFiles path to something other than
		/// the default location which is a subfoldre of the project folder.
		/// </summary>
		private string m_linkedFilesPath;


		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BackupSettings"/> class.
		/// </summary>
		/// <param name="projectsRootFolder">The root folder for projects (typically the
		/// default, but if these setings represent a project elsewhere, then this will be the
		/// root folder for that project).</param>
		/// <param name="linkedFilesPath">The linked files path.</param>
		/// ------------------------------------------------------------------------------------
		protected BackupSettings(string projectsRootFolder, string linkedFilesPath)
		{
			m_projectsRootFolder = projectsRootFolder;
			m_linkedFilesPath = linkedFilesPath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the filename of the actual FW data file or the XML copy of it (i.e.,
		/// ProjectName with a .fwdata extension)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DbFilename
		{
			get { return LcmFileHelper.GetXmlDataFileName(ProjectName); }
		}

		#region IBackupSettings implementation
		/// <summary>
		/// The date and time of the backup
		/// </summary>
		public DateTime BackupTime
		{
			get { return m_backupTime.ToTheMinute(); }
			set { m_backupTime = value; }
		}

		/// <summary>
		/// User's description of a particular back-up instance
		/// </summary>
		public string Comment
		{
			get { return m_comment; }
			set { m_comment = value; }
		}

		/// <summary>
		/// This is the name of the project being (or about to be) backed up or restored.
		/// </summary>
		public string ProjectName
		{
			get { return m_projectName; }
			set { m_projectName = value; }
		}

		///<summary>
		/// Whether or not field visibilities, columns, dictionary layout, interlinear, etc.
		/// settings are included in the backup/restore.
		///</summary>
		public bool IncludeConfigurationSettings
		{
			get { return m_includeConfigurationSettings; }
			set { m_includeConfigurationSettings = value; }
		}

		///<summary>
		/// Whether or not externally linked files (pictures, media and other) are included in the backup/restore.
		///</summary>
		public bool IncludeLinkedFiles
		{
			get { return m_includeMediaFiles; }
			set { m_includeMediaFiles = value; }
		}

		/// <summary>
		/// Whether or not the files in the SupportingFiles folder are included in the backup/restore.
		/// </summary>
		public bool IncludeSupportingFiles
		{
			get { return m_includeSupportingFiles; }
			set { m_includeSupportingFiles = value; }
		}

		///<summary>
		/// Whether or not spell checking additions are included in the backup/restore.
		///</summary>
		public bool IncludeSpellCheckAdditions
		{
			get { return m_includeSpellCheckAdditions; }
			set { m_includeSpellCheckAdditions = value; }
		}
		#endregion

		#region Paths

		/// <summary>
		/// Gets the projects root folder.
		/// </summary>
		public string ProjectsRootFolder
		{
			get { return m_projectsRootFolder; }
		}

		/// <summary>
		/// This is the path of the project being (or about to be) backed up or restored.
		/// </summary>
		public string ProjectPath
		{
			get { return Path.Combine(m_projectsRootFolder, ProjectName); }
		}

		private const string s_chorusNotesFilename = "Lexicon.fwstub.ChorusNotes";

		/// <summary>
		/// This is the name of the ChorusNotes file that implements the Questions slice.
		/// </summary>
		public string QuestionNotesFilename
		{
			get { return s_chorusNotesFilename; }
		}

		/// <summary>
		/// Directory where Flex Configuration Files are stored.
		/// </summary>
		public string FlexConfigurationSettingsPath
		{
			get { return LcmFileHelper.GetConfigSettingsDir(ProjectPath); }
		}

		/// <summary>
		/// This is the path of the LinkedFiles Folder for this project.
		/// </summary>
		public string LinkedFilesPath
		{
			get
			{
				if (String.IsNullOrEmpty(m_linkedFilesPath))
				{
					return Path.Combine(ProjectPath, LcmFileHelper.ksLinkedFilesDir);
				}
				return m_linkedFilesPath;
			}
			set
			{
				m_linkedFilesPath = value;
			}
		}

		/// <summary>
		/// Path to the standard pictures directory for the specified project
		/// </summary>
		public string PicturesPath
		{
			get { return LcmFileHelper.GetPicturesDir(LinkedFilesPath); }
		}

		/// <summary>
		/// Path to the standard media files directory for the specified project
		/// </summary>
		public string MediaPath
		{
			get { return LcmFileHelper.GetMediaDir(LinkedFilesPath); }
		}

		/// <summary>
		/// Path to the standard directory for other externally linked project files
		/// </summary>
		public string OtherExternalFilesPath
		{
			get { return LcmFileHelper.GetOtherExternalFilesDir(LinkedFilesPath); }
		}

		/// <summary>
		/// Directory where FieldWorks project specific WritingSystem Files are stored.
		/// </summary>
		public string WritingSystemStorePath
		{
			get
			{
				return LcmFileHelper.GetWritingSystemDir(ProjectPath);
			}
		}

		/// <summary>
		/// Path to the standard directory for shared lexicon settings.
		/// </summary>
		public string SharedSettingsPath
		{
			get { return LexiconSettingsFileHelper.GetSharedSettingsPath(ProjectPath); }
		}

		/// <summary>
		/// Directory where Spelling Dictionary files are copied to for backup and restore.
		/// It should be a subfolder of the project folder.
		/// </summary>
		public string SpellingDictionariesPath
		{
			get { return Path.Combine(ProjectPath, ksSpellingDictionariesDir); }
		}

		/// <summary>
		/// Directory where the user can put supporting filed for FieldWorks (e.g. keyman installers, Fonts).
		/// </summary>
		public string ProjectSupportingFilesPath
		{
			get { return LcmFileHelper.GetSupportingFilesDir(ProjectPath); }
		}
		#endregion
	}
}
