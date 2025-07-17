using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class LightmapProgressForm : Form
	{
		public LightmapProgressForm()
		{
			InitializeComponent();
		}

		private void LightmapProgressForm_Load(object sender, EventArgs e)
		{
 			textboxoutput.Clear();
			labelprogress.Text = "Starting ZDRay...";

			Task.Run(() => StartZDRay());
		}

		private void buttoncancel_Click(object sender, EventArgs e)
		{
			if (m_BuildProcess != null && !m_BuildProcess.HasExited)
			{
				m_BuildProcess.CancelOutputRead();
				m_BuildProcess.Kill();
				m_BuildProcess.WaitForExit();
				m_BuildProcess.Close();
				m_BuildProcess = null;

				labelprogress.Text = "Cancelled";
				ProcessOutput("ERROR: Lightmap Build cancelled by user");
			}
			else
			{
				Close();
			}
		}

		private void LightmapProgressForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			// Kill the build process if it's still happening
			if (m_BuildProcess != null && !m_BuildProcess.HasExited)
			{
				m_BuildProcess.CancelOutputRead();
				m_BuildProcess.Kill();
				m_BuildProcess.WaitForExit();
				m_BuildProcess.Close();
				m_BuildProcess = null;
			}
		}

		private void StartZDRay()
		{
			m_BuildProcess = new System.Diagnostics.Process();
			m_BuildProcess.StartInfo.FileName = "C:\\Projects\\ZDRay\\out\\build\\x64-Release\\zdray.exe"; // TODO: The location of zdray should come from settings
			m_BuildProcess.StartInfo.Arguments = $"--udbmode \"{General.Map.FilePathName}\"";
			m_BuildProcess.StartInfo.CreateNoWindow = true;
			m_BuildProcess.StartInfo.UseShellExecute = false;
			m_BuildProcess.StartInfo.RedirectStandardOutput = true;
			m_BuildProcess.EnableRaisingEvents = true;
			
			m_BuildProcess.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) =>
			{
				ProcessOutputCallback callback = new ProcessOutputCallback(ProcessOutput);
				Invoke(callback, new object[] { e.Data });
			});
			
			m_BuildProcess.Exited += new System.EventHandler((sender, e) =>
			{
				OnProcessExitedCallback callback = new OnProcessExitedCallback(OnProcessExited);
				Invoke(callback, new object[] { });
			});

			if (m_BuildProcess.Start())
			{
				m_BuildProcess.BeginOutputReadLine();
			}
		}

		private void ProcessOutput(string message)
		{
			if (String.IsNullOrEmpty(message))
			{
				return;
			}

			// Just throw the message directly to the output
			if (message.StartsWith("ERROR: "))
			{
				PrintOutputMessage(message, Color.Red);
			}
			else if (message.StartsWith("WARNING: "))
			{
				PrintOutputMessage(message, Color.Yellow);
			}
			else if (message.StartsWith("STAT:"))
			{
				string statMessage = message.Substring("STAT:".Length);
				string[] args = statMessage.Split('|');
				ProcessStatMessage(args[0], args.Skip(1).ToArray());
				return;
			}
		}

		private void OnProcessExited()
		{
			if (!m_HasErrors)
			{
				progressbar.Value = 98;

				if (LoadLightmapData())
				{
					progressbar.Value = 100;
					labelprogress.Text = "Lightmap rendered successfully!";
					PrintOutputMessage("Lightmap rendered successfully!", Color.Green);

					Close();
				}
				else
				{
					m_HasErrors = true;
				}
			}

			if (m_HasErrors)
			{
				progressbar.Value = 0;
				PrintOutputMessage("\nBuild exited with errors", Color.Red);
				buttoncancel.Text = "Close";
			}
		}

		private bool LoadLightmapData()
		{
			labelprogress.Text = "Copying data to UDB...";
			PrintOutputMessage("Copying data to UDB...");

			string lightmapPath = General.Map.FilePathName + ".lightmap.lmp";
			string lightgroupPath = General.Map.FilePathName + ".lightgrp.lmp";

			if (!File.Exists(lightmapPath))
			{
				ProcessOutput($"ERROR: Failed to load LIGHTMAP lump from file: {lightmapPath}");
				return false;
			}

			if (!File.Exists(lightgroupPath))
			{
				ProcessOutput($"ERROR: Failed to load LIGHTGRP lump from file: {lightgroupPath}");
				return false;
			}

			Byte[] lightmapData = File.ReadAllBytes(lightmapPath);
			Byte[] lightgroupData = File.ReadAllBytes(lightgroupPath);

			// Copy the lumps into our currently loaded map file
			General.Map.SetLumpData("LIGHTMAP", new MemoryStream(lightmapData));
			General.Map.SetLumpData("LIGHTGRP", new MemoryStream(lightgroupData));

			// The .lmp files are only supposed to be temporary, so delete them now that we're done
			File.Delete(lightmapPath);
			File.Delete(lightgroupPath);

			labelprogress.Text = "Saving map file...";
			PrintOutputMessage("Saving map file...");

			General.Map.SaveMap(General.Map.FilePathName, SavePurpose.NoAutoSave); // Don't make an autosave on this step, since we already have one from the initial save

			return true;
		}

		private void ProcessStatMessage(string message, string[] args)
		{
			if (message == "GatherTasksStart" && args.Length == 1)
			{
				m_GatherTasks = UInt64.Parse(args[0]);
				progressbar.Value = GatherTasksStartPercent;
			}
			else if (message == "GatherTasksUpdate" && args.Length == 2)
			{
				UInt64 tasksComplete = UInt64.Parse(args[0]);
				progressbar.Value = TaskPercent(tasksComplete, m_GatherTasks, GatherTasksStartPercent, GatherTasksEndPercent);

				labelprogress.Text = $"Gathering tasks: {tasksComplete} / {m_GatherTasks}";
			}
			else if (message == "GatherTasksComplete")
			{
				progressbar.Value = GatherTasksEndPercent;
			}
			else if (message == "RaytraceStart" && args.Length == 1)
			{
				m_RaytraceTasks = UInt64.Parse(args[0]);
				progressbar.Value = RaytraceStartPercent;

				labelprogress.Text = $"Raytracing: 0 / {m_RaytraceTasks} tasks complete";
			}
			else if (message == "RaytraceUpdate" && args.Length == 2)
			{
				UInt64 tasksComplete = UInt64.Parse(args[0]);
				progressbar.Value = TaskPercent(tasksComplete, m_RaytraceTasks, RaytraceStartPercent, RaytraceEndPercent);

				labelprogress.Text = $"Raytracing: {tasksComplete} / {m_RaytraceTasks} tasks complete";
			}
			else if (message == "RaytraceComplete")
			{
				progressbar.Value = RaytraceEndPercent;

				labelprogress.Text = "Saving output...";
			}
			else if (message == "Section" && args.Length == 1)
			{
				labelprogress.Text = args[0];

				PrintOutputMessage(args[0]);
			}
			else if (message == "Message" && args.Length == 1)
			{
				PrintOutputMessage(args[0]);
			}
			else
			{
				string debugOutput = $"STAT:{message}";

				foreach (string str in args)
				{
					debugOutput += "|" + str;
				}

				PrintOutputMessage(debugOutput, Color.Cyan);
			}
		}

		void PrintOutputMessage(string message, Color color = default(Color))
		{
			textboxoutput.SelectionColor = color == default(Color) ? textboxoutput.ForeColor : color;
			textboxoutput.SelectedText = message + Environment.NewLine;
		}

		private Int32 TaskPercent(UInt64 tasksComplete, UInt64 tasksCount, int fromPercent, int toPercent)
		{
			return (Int32)((tasksComplete / (double)tasksCount) * (toPercent - fromPercent)) + fromPercent;
		}

		delegate void ProcessOutputCallback(string message);
		delegate void OnProcessExitedCallback();

		private System.Diagnostics.Process m_BuildProcess;
		private List<string> m_Messages = new List<string>();

		private UInt64 m_GatherTasks = 0;
		private UInt64 m_RaytraceTasks = 0;
		private bool m_HasErrors = false;

		const int GatherTasksStartPercent = 5;
		const int GatherTasksEndPercent = 20;
		const int RaytraceStartPercent = GatherTasksEndPercent;
		const int RaytraceEndPercent = 90;
	}
}
