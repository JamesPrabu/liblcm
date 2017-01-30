﻿// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// <summary>
	/// This class is a message filter which can be installed in order to track when the user last
	/// pressed a key or did any mouse action, including moving the mouse.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithNativeFieldsShouldBeDisposableRule", Justification="No unmanaged resources to release")]
	public class UserActivityMonitor : IMessageFilter
	{
		private const int KEYDOWN = 128;
		private IntPtr m_lastMousePosition;
		private IntPtr m_keyboardHook;
		private DateTime m_lastActivityTime;

		/// <summary>
		/// Starts monitoring user activity.
		/// </summary>
		public void StartMonitoring()
		{
			Application.AddMessageFilter(this);
		}

		/// <summary>
		/// Stops monitoring user activity.
		/// </summary>
		public void StopMonitoring()
		{
			Application.RemoveMessageFilter(this);
		}

#if !__MonoCS__
		/// <summary>
		/// WinAPI get keyboard state method to detect keyDown events
		/// </summary>
		[DllImport("user32.dll")]
		public static extern int GetKeyboardState(byte[] keystate);
#endif

		/// <summary>
		/// Gets the last user activity time.
		/// </summary>
		/// <value>
		/// The last activity user time.
		/// </value>
		public DateTime LastActivityTime {
			get
			{
#if !__MonoCS__ // Keyboard doesn't exist in Mono. Linux users must prefer voice-to-text. If this leaves a bug in mono, it needs its own solution
				var isKeyDown = false;

				byte[] keys = new byte[256];
				if (GetKeyboardState(keys) >= 0)
				{
					if (keys.Any(k => (k & KEYDOWN) > 0))
					{
						isKeyDown = true;
					}
				}
				if (isKeyDown)
					return DateTime.Now; // If the user is holding down e.g. the Backspace key, that counts as current activity
#endif
				return m_lastActivityTime;
			}
		}

		bool IMessageFilter.PreFilterMessage(ref Message m)
		{
			if(m.Msg == (int) Win32.WinMsgs.WM_MOUSEMOVE)
			{
				// For mouse move, we get spurious ones when it didn't really move. So check the actual position.
				if (m.LParam != m_lastMousePosition)
				{
					m_lastActivityTime = DateTime.Now;
					m_lastMousePosition = m.LParam;
				}
				return false;
			}
			if ((m.Msg >= (int) Win32.WinMsgs.WM_MOUSE_Min && m.Msg <= (int) Win32.WinMsgs.WM_MOUSE_Max)
				|| (m.Msg >= (int) Win32.WinMsgs.WM_KEY_Min && m.Msg <= (int) Win32.WinMsgs.WM_KEY_Max))
			{
				m_lastActivityTime = DateTime.Now;
			}
			return false; // don't want to block any messages.
		}
	}
}
