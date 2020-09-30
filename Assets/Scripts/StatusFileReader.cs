using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using EDTracking;

using UnityEngine;

public class StatusFileReader
{
//	const string ClientIdFile = "client.id";
//	private string _lastUpdateTime = "";
	private FileStream _statusFileStream = null;
	private System.Timers.Timer _statusTimer = null;
	private string _statusFile = "";
	private DateTime _lastFileWrite = DateTime.MinValue;
	private DateTime _lastStatusRead = DateTime.MinValue;
//	private static string _clientId = null;
//	private JournalReader _journalReader = null;
//	public static EDLocation CurrentLocation { get; private set; } = null;
//	public static int CurrentHeading { get; private set; } = -1;
//	public static double SpeedInMS { get; internal set; } = 0;

	private System.IO.FileSystemWatcher statusFileWatcher;


	public StatusFileReader()  {

//		this.statusFileWatcher = new System.IO.FileSystemWatcher();

//		_statusTimer = new System.Timers.Timer(700);
//		_statusTimer.Stop();
//		_statusTimer.Elapsed += _statusTimer_Elapsed;

//		statusFileWatcher.Changed += new System.IO.FileSystemEventHandler(this.statusFileWatcher_Changed);
	}

	private void _statusTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
		_statusTimer.Stop();

		// If the file has been written, then process it
		DateTime lastWriteTime = File.GetLastWriteTime(_statusFile);

//		if ( (lastWriteTime != _lastFileWrite) || (DateTime.Now.Subtract(_lastStatusRead).TotalSeconds>5) ) {
		if ( lastWriteTime != _lastFileWrite ) {
			ProcessStatusFileUpdate(_statusFile);
			_lastFileWrite = lastWriteTime;
			_lastStatusRead = DateTime.Now;
		}
		_statusTimer.Start();
	}


	private void statusFileWatcher_Changed(object sender, System.IO.FileSystemEventArgs e) {
		if (e.FullPath.ToLower().EndsWith("status.json")) {
			// Create a task to process the status (we return as quickly as possible from the event procedure
			Task.Factory.StartNew(() => ProcessStatusFileUpdate(e.FullPath));
		}
	}


	public string ProcessStatusFileUpdate(string statusFile) {
		DateTime lastWriteTime = File.GetLastWriteTime(_statusFile);

		if ( lastWriteTime == _lastFileWrite ) {
			return "";
		} else {
			_lastFileWrite = lastWriteTime;
			_lastStatusRead = DateTime.Now;
		}

		// Read the status from the file and update the UI
		string status = "";
		try
		{
			// Read the file - we open in file share mode as E: D will be constantly writing to this file
			if (_statusFileStream == null ) {
				_statusFileStream = new FileStream(statusFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			}

			_statusFileStream.Seek(0, SeekOrigin.Begin);

			using (StreamReader sr = new StreamReader(_statusFileStream, Encoding.Default, true, 1000, true))
				status = sr.ReadToEnd();

			if ( (File.Exists(statusFile)) || (!_statusFileStream.CanSeek) ) {
				// We only close the file if we can't seek (no point in continuously reopening)
				_statusFileStream.Close();
				_statusFileStream = null;
			}
		}
		catch  {
			Debug.Log("WARN: Failed to create status file stream. status file:"+statusFile);
		}


		if (String.IsNullOrEmpty(status))
			return "";

//		Debug.Log("status:"+status);
		

		try
		{
			// E: D status.json file does not include milliseconds in the timestamp.  We want milliseconds, so we add our own timestamp
			// This also gives us polling every five seconds in case the commander stops moving (as soon as they move, the new status should be picked up)
			// Turns out milliseconds is pointless as E: D is very unlikely to generate a new status file more than once a second (and/or we won't detect it), but
			// we'll keep them in case this changes in future.
//			UpdateUI(new EDEvent(status,textBoxClientId.Text, DateTime.Now));
		}
		catch { }

		try
		{
			if (mainControl.instance.isSaveToFile)
				File.AppendAllText(mainControl.instance.statusFileMyCopy, status);
		}
		catch (Exception ex)
		{
			MonoBehaviour.print($"ERROR: Failed to save to local log file: {ex.Message}");
//			Action action = new Action(() => { checkBoxSaveToFile.Checked = false; });
//			if (checkBoxSaveToFile.InvokeRequired)
//				checkBoxSaveToFile.Invoke(action);
//			else
//				action();
		}

		return status;
	}

/*
	private void SaveToFile(EDEvent edEvent)
	{
		try
		{
			// This is very inefficient, save to file should only be enabled for debugging
			// I may revisit this at some point if more features are added for local tracking
			string eventData = eventData = edEvent.ToJson();
			System.IO.File.AppendAllText(mainControl.instance.statusFileMyCopy, eventData);
		}
		catch (Exception ex)
		{
			AddLog($"Error saving to tracking log: {ex.Message}");
			checkBoxSaveToFile.Checked = false;
		}
	}
*/


}





/*
	private void InitClientId()
	{
		// Check if we have an Id saved, and if not, generate one
		if (!File.Exists(ClientIdFile))
		{
			// First run, so show splash and prompt for commander name
			using (FormFirstRun formFirstRun = new FormFirstRun())
			{
				formFirstRun.ShowDialog(this);
				_clientId = formFirstRun.textBoxCommanderName.Text;
				if (String.IsNullOrEmpty(_clientId))
				{
					_clientId = ReadCommanderNameFromJournal();
					if (String.IsNullOrEmpty(_clientId))
					{
						AddLog("New client Id generated");
						_clientId = Guid.NewGuid().ToString();
					}
				}
				try
				{
					File.WriteAllText(ClientIdFile, _clientId);
					AddLog($"Saved client Id to file: {ClientIdFile}");
				}
				catch (Exception ex)
				{
					AddLog($"Error saving client Id to file: {ex.Message}");
				}
			}
		}
		else
		{
			try
			{
				// Read the file
				_clientId = File.ReadAllText(ClientIdFile);
				AddLog("Restored client Id");
			}
			catch { }
		}

		if (!String.IsNullOrEmpty(_clientId))
			textBoxClientId.Text = _clientId;
	}

	public static string ClientId
	{
		get
		{
			return _clientId;
		}
	}

	private string ReadCommanderNameFromJournal()
	{
		// E: D writes the commander name to the journal.  We locate the most recent journal and attempt to read it from there.
		string path = EDJournalPath();
		if (!String.IsNullOrEmpty(path))
		{
			string[] files = Directory.GetFiles(path, "Journal*.cache",SearchOption.TopDirectoryOnly);
		}
		return null; // Not currently implemented
	}

	private string EDJournalPath() {
		string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\Saved Games\\Frontier Developments\\Elite Dangerous";
		if (Directory.Exists(path))
			return path;
		return "";
	}
*/
