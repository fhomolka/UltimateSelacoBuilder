using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
			m_BuildProcess.StartInfo.Arguments =
				"--from-editor " +
				"--output=\"C:\\Projects\\SelacoData\\Files\\Maps\\LightmapTest.wad\" " +
				"\"C:\\Projects\\SelacoData\\Files\\MAPS\\SE_01b-StaticLights.wad\"";
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
				textboxoutput.SelectionColor = Color.Red;
			}
			else if (message.StartsWith("WARNING: "))
			{
				textboxoutput.SelectionColor = Color.Yellow;
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
			progressbar.Value = 100;
			labelprogress.Text = "Lightmap rendered successfully!";
			buttoncancel.Text = "Close";
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
				progressbar.Value = LerpPercent(tasksComplete, m_GatherTasks, GatherTasksStartPercent, GatherTasksEndPercent);

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
				progressbar.Value = LerpPercent(tasksComplete, m_RaytraceTasks, RaytraceStartPercent, RaytraceEndPercent);

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

				textboxoutput.SelectionColor = textboxoutput.ForeColor;
				textboxoutput.SelectedText = args[0] + Environment.NewLine;
			}
			else if (message == "Message" && args.Length == 1)
			{
				textboxoutput.SelectionColor = textboxoutput.ForeColor;
				textboxoutput.SelectedText = args[0] + Environment.NewLine;
			}
			else
			{
				string debugOutput = $"STAT:{message}";

				foreach (string str in args)
				{
					debugOutput += "|" + str;
				}

				textboxoutput.SelectionColor = Color.Cyan;
				textboxoutput.SelectedText = debugOutput;
			}
		}

		private Int32 LerpPercent(UInt64 value, UInt64 valueMax, int from, int to)
		{
			return (Int32)((value / (double)valueMax) * (to - from)) + from;
		}

		delegate void ProcessOutputCallback(string message);
		delegate void OnProcessExitedCallback();

		private System.Diagnostics.Process m_BuildProcess;
		private List<string> m_Messages = new List<string>();

		private UInt64 m_GatherTasks = 0;
		private UInt64 m_RaytraceTasks = 0;

		const int GatherTasksStartPercent = 5;
		const int GatherTasksEndPercent = 20;
		const int RaytraceStartPercent = GatherTasksEndPercent;
		const int RaytraceEndPercent = 90;
	}
}
