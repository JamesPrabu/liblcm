// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyProgressDlg.cs

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple implementation for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_threadHelper is a singleton and disposed by the SingletonsContainer")]
	public class DummyProgressDlg : IThreadedProgress
	{
		private readonly ThreadHelper m_threadHelper = SingletonsContainer.Get<ThreadHelper>();

		#region IProgress and IThreadedProgress Members
		/// <summary></summary>
		event CancelEventHandler IProgress.Canceling
		{
			add { throw new NotImplementedException(); }
			remove { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Cause the progress indicator to advance by the specified amount.
		/// </summary>
		/// <param name="amount">Amount of progress.</param>
		public void Step(int amount)
		{
			Position += amount == 0 ? StepSize : amount;
		}

		/// <summary>
		/// Get the title of the progress display window.
		/// </summary>
		/// <value>
		/// The title.
		/// </value>
		public string Title { get; set; }

		/// <summary>
		/// Get the message within the progress display window.
		/// </summary>
		/// <value>
		/// The message.
		/// </value>
		public string Message { get; set; }

		/// <summary>
		/// Gets or sets the current position of the progress bar. This should be within the limits set by
		///             SetRange, or returned by GetRange.
		/// </summary>
		/// <value>
		/// The position.
		/// </value>
		public int Position { get; set; }

		/// <summary>
		/// Gets or sets the size of the step increment used by Step.
		/// </summary>
		/// <value>
		/// The size of the step.
		/// </value>
		public int StepSize { get; set; }

		/// <summary>
		/// Gets or sets the minimum value of the progress bar.
		/// </summary>
		/// <value>
		/// The minimum.
		/// </value>
		public int Minimum { get; set; }

		/// <summary>
		/// Gets or sets the maximum value of the progress bar.
		/// </summary>
		/// <value>
		/// The maximum.
		/// </value>
		public int Maximum { get; set; }

		/// <summary>
		/// Gets an object to be used for ensuring that required tasks are invoked on the main
		/// UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_threadHelper; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the task has been canceled.
		/// </summary>
		/// <value>
		/// <c>true</c> if canceled; otherwise, <c>false</c>.
		/// </value>
		public bool Canceled
		{
			get { return false; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this progress is indeterminate.
		/// </summary>
		public bool IsIndeterminate
		{
			get { return false; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the opertation executing on the separate thread
		/// can be cancelled by a different thread (typically the main UI thread).
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool AllowCancel { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCanceling
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the progress dialog as a modal dialog and starts the background task.
		/// </summary>
		/// <param name="fDisplayUi">set to <c>true</c> to display the progress dialog,
		/// <c>false</c> to run without UI.</param>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task</param>
		/// <returns>
		/// The return value from the background thread.
		/// </returns>
		/// <exception cref="NotImplementedException">Wraps any exception thrown by the background
		/// task</exception>
		/// ------------------------------------------------------------------------------------
		public object RunTask(bool fDisplayUi, Func<IThreadedProgress, object[], object> backgroundTask, params object[] parameters)
		{
			return backgroundTask(this, parameters);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If progress dialog is already showing, we run the background task using it (without
		/// creating a separate thread). Otherwise we display a new progress dialog as a modal
		/// dialog and start the background task in a separate thread.
		/// </summary>
		/// <param name="backgroundTask">The background task.</param>
		/// <param name="parameters">The paramters that will be passed to the background task</param>
		/// <returns>
		/// The return value from the background thread.
		/// </returns>
		/// <exception cref="T:SIL.FieldWorks.Common.FwUtils.WorkerThreadException">Wraps any exception thrown by the background
		/// task</exception>
		/// ------------------------------------------------------------------------------------
		public object RunTask(Func<IThreadedProgress, object[], object> backgroundTask, params object[] parameters)
		{
			return backgroundTask(this, parameters);
		}

		#endregion
	}
}
