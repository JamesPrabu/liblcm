## --------------------------------------------------------------------------------------------
## Copyright (c) 2007-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the LcmGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
## This will generate the various methods used by the LCM aware ISilDataAccess implementation.
## Using these methods avoids using Reflection in that implementation.
##

		/// <summary>
		/// Get a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		protected override byte[] GetBinaryPropertyInternal(int flid)
		{
			switch (flid)
			{
				default:
					return base.GetBinaryPropertyInternal(flid);
#foreach( $prop in $class.BinaryProperties )
				case $prop.Number:
#if( $prop.IsHandGenerated)
					return $prop.NiuginianPropName;
#else
					return m_$prop.NiuginianPropName;
#end
#end
			}
		}

		/// <summary>
		/// Set a Binary type property.
		/// </summary>
		/// <param name="flid">The property to read.</param>
		/// <param name="newValue">The property to read.</param>
		/// <param name="useAccessor"></param>
		protected override void SetPropertyInternal(int flid, byte[] newValue, bool useAccessor)
		{
			switch (flid)
			{
				default:
					base.SetPropertyInternal(flid, newValue, useAccessor);
					break;
#foreach( $prop in $class.BinaryProperties )
				case $prop.Number:
					if (useAccessor)
						$prop.NiuginianPropName = newValue;
					else
						m_$prop.NiuginianPropName = newValue;
					break;
#end
			}
		}