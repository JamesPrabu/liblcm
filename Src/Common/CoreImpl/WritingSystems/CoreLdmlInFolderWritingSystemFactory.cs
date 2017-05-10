﻿using SIL.WritingSystems;

namespace SIL.CoreImpl.WritingSystems
{
	internal class CoreLdmlInFolderWritingSystemFactory : LdmlInFolderWritingSystemFactory<CoreWritingSystemDefinition>
	{
		public CoreLdmlInFolderWritingSystemFactory(CoreLdmlInFolderWritingSystemRepository writingSystemRepository)
			: base(writingSystemRepository)
		{
		}

		protected override CoreWritingSystemDefinition ConstructDefinition()
		{
			return new CoreWritingSystemDefinition();
		}

		protected override CoreWritingSystemDefinition ConstructDefinition(string ietfLanguageTag)
		{
			return new CoreWritingSystemDefinition(ietfLanguageTag);
		}

		protected override CoreWritingSystemDefinition ConstructDefinition(CoreWritingSystemDefinition ws)
		{
			return new CoreWritingSystemDefinition(ws);
		}
	}
}
