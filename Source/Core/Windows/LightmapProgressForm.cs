using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
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
				m_WasCancelled = true;

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
				m_WasCancelled = true;
			}
		}

		private void StartZDRay()
		{
			if (!General.Map.ConfigSettings.NodebuilderSave.StartsWith("zdray"))
			{
				OnProcessErrorCallback callback = new OnProcessErrorCallback(OnProcessError);
				Invoke(callback, new object[] { $"NodeBuilder must be set to use ZDRAY in order to build lightmaps, currently set to: {General.Map.ConfigSettings.NodebuilderSave}" });
				return;
			}

			NodebuilderInfo nodebuilder = General.GetNodebuilderByName(General.Map.ConfigSettings.NodebuilderSave);

			string zdrayPath = Path.Combine(nodebuilder.Compiler.Path, nodebuilder.Compiler.ProgramFile);

			if (!File.Exists(zdrayPath))
			{
				OnProcessErrorCallback callback = new OnProcessErrorCallback(OnProcessError);
				Invoke(callback, new object[] { $"Could not find ZDRay at location:\n\t{zdrayPath}" });
				return;
			}

			string arguments = "";

			if (General.Settings.LightmapDeviceIndex == 1)
			{
				arguments += " --cpu-raytrace";
			}

			if (General.Settings.LightmapRenderQuality > 0)
			{
				arguments += $" --downsample={General.Settings.LightmapRenderQuality}";
			}

			arguments += $" --udbmode ";

			arguments += nodebuilder.Parameters;

			arguments = arguments.Replace("-o%FO", ""); // We don't need the output filename for UDBMode
			arguments = arguments.Replace("--nodes-only", ""); // Default configs don't build lightmaps, so make sure we remove that!
			arguments = arguments.Replace("%FI", $"\"{General.Map.FilePathName}\""); // Make sure we have doublequotes around the filename so paths with spaces work

			m_HasErrors = false;
			m_WasCancelled = false;

			m_BuildProcess = new System.Diagnostics.Process();
			m_BuildProcess.StartInfo.FileName = zdrayPath;
			m_BuildProcess.StartInfo.Arguments = arguments;
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

			try
			{
				if (m_BuildProcess.Start())
				{
					m_BuildProcess.BeginOutputReadLine();
				}
				else
				{
					OnProcessErrorCallback callback = new OnProcessErrorCallback(OnProcessError);
					Invoke(callback, new object[] { $"Failed to run {m_BuildProcess.StartInfo.FileName}" });
				}
			}
			catch  (Exception ex) 
			{
				OnProcessErrorCallback callback = new OnProcessErrorCallback(OnProcessError);
				Invoke(callback, new object[] { $"ZDRay exception: {ex.Message}" });
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
			if (m_BuildProcess != null)
			{
				m_BuildProcess.CancelOutputRead();
			}

			if (!m_HasErrors && !m_WasCancelled)
			{
				progressbar.Value = 98;

				if (LoadLightmapData())
				{
					progressbar.Value = 100;
					labelprogress.Text = "Lightmap rendered successfully!";
					PrintOutputMessage("Lightmap rendered successfully!", Color.Green);

					if (General.Settings.LightmapProgressAutoClose)
					{
						Close();
					}
				}
				else
				{
					m_HasErrors = true;
				}
			}

			if (m_HasErrors)
			{
				PrintOutputMessage("\nBuild exited with errors", Color.Red);
			}

			if (m_HasErrors || m_WasCancelled)
			{
				progressbar.Value = 0;
			}

			buttoncancel.Text = "Close";
		}

		private void OnProcessError(string message)
		{
			PrintOutputMessage(message, Color.Red);
			m_HasErrors = true;
			OnProcessExited();
		}

		private bool LoadLightmapData()
		{
			labelprogress.Text = "Copying data to UDB...";
			PrintOutputMessage("Copying data to UDB...");

			string lightmapPath = General.Map.FilePathName + ".lightmap.lmp";
			string lightgroupPath = General.Map.FilePathName + ".lightgrp.lmp";

			try
			{
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

				string lightgroupthingsPath = General.Map.FilePathName + ".lgroupthings.lmp";

				Byte[] lightmapData = File.ReadAllBytes(lightmapPath);
				Byte[] lightgroupData = File.ReadAllBytes(lightgroupPath);

				if (!LoadLightGroupThings(lightgroupthingsPath))
				{
					return false;
				}

				// Copy the lumps into our currently loaded map file
				General.Map.SetLumpData("LIGHTMAP", new MemoryStream(lightmapData));
				General.Map.SetLumpData("LIGHTGRP", new MemoryStream(lightgroupData));

				// The .lmp files are only supposed to be temporary, so delete them now that we're done
				File.Delete(lightmapPath);
				File.Delete(lightgroupPath);
				File.Delete(lightgroupthingsPath);

				labelprogress.Text = "Saving map file...";
				PrintOutputMessage("Saving map file...");

				General.Map.SaveMap(General.Map.FilePathName, SavePurpose.NoAutoSave); // Don't make an autosave on this step, since we already have one from the initial save

				return true;
			}
			catch (Exception ex)
			{
				PrintOutputMessage($"ERROR: Unhandled exception loading lightmap data: {ex.Message}");
				return false;
			}
		}

		private bool LoadLightGroupThings(string path)
		{
			if (!File.Exists(path))
			{
				ProcessOutput($"ERROR: Failed to load LightGroupThings, file not found: {path}");
				return false;
			}

			string[] data = File.ReadAllLines(path);

			if (data.Length == 0)
			{
				// Empty file, probably no light groups
				return true;
			}

			UniversalParser textmap = new UniversalParser();
			textmap.InputConfiguration(data);

			// Check for errors
			if (textmap.ErrorResult != 0)
			{
				ProcessOutput($"ERROR: Error on line {textmap.ErrorLine} while parsing LightGroupThings data:\n" + textmap.ErrorDescription);
			}

			if (textmap.HasWarnings)
			{
				foreach (string warning in textmap.Warnings)
				{
					ProcessOutput($"WARNING: {warning}");
				}
			}

			List<Thing> oldLightGroups = new List<Thing>();

			// Delete all existing LightGroups
			foreach (Thing thing in General.Map.Map.Things)
			{
				if (thing.Type == 9899)
				{
					oldLightGroups.Add(thing);
				}
			}

			General.Map.Map.BeginAddRemove();

			foreach (Thing lightGroup in oldLightGroups)
			{
				lightGroup.Dispose();
			}

			oldLightGroups = null;

			// Make list
			foreach (UniversalEntry e in textmap.Root)
			{
				UniversalCollection uc = e.Value as UniversalCollection;
				if (uc != null && e.Key == "thing")
				{
					string error = null;

					int[] args = new int[Linedef.NUM_ARGS];
					int tag = GetEntry<int>(uc, "id", ref error);
					int type = GetEntry<int>(uc, "type", ref error);
					int score = GetEntry<int>(uc, "score", ref error);
					args[0] = GetEntry<int>(uc, "arg0", ref error);
					args[1] = GetEntry<int>(uc, "arg1", ref error);
					args[2] = GetEntry<int>(uc, "arg2", ref error);
					bool dormant = GetEntry<bool>(uc, "dormant", ref error);
					int animationType = GetEntry<int>(uc, "user_animationtype", ref error);
					int animationInterval = GetEntry<int>(uc, "user_animationinterval", ref error);
					double primaryIntensity = GetEntry<double>(uc, "user_primaryintensity", ref error);
					double secondaryIntensity = GetEntry<double>(uc, "user_secondaryintensity", ref error);
					bool allowColorChange = GetEntry<bool>(uc, "user_allowcolorchange", ref error);

					if (error != null)
					{
						ProcessOutput($"ERROR: {error}");
						General.Map.Map.EndAddRemove();
						return false;
					}

					// Create new item
					Thing t = General.Map.Map.CreateThing();
					if (t != null)
					{
						General.Settings.ApplyDefaultThingSettings(t);
						Dictionary<string, bool> flags = new Dictionary<string, bool>
						{
							{ "enabled", true },
							{ "dormant", dormant },
							{ "skill1", true },
							{ "skill2", true },
							{ "skill3", true },
							{ "skill4", true },
							{ "skill5", true },
							{ "skill6", true },
							{ "single", true },
							{ "coop", true },
						};

						t.Update(type, 0, 0, 0, 0, 0, 0, 0, 0, flags, tag, 0, args);
						AddUserField(t, "score", score);
						AddUserField(t, "skill1", true);
						AddUserField(t, "skill2", true);
						AddUserField(t, "skill3", true);
						AddUserField(t, "skill4", true);
						AddUserField(t, "skill5", true);
						AddUserField(t, "skill6", true);
						AddUserField(t, "single", true);
						AddUserField(t, "coop", true);
						AddUserField(t, "user_animationType", animationType);
						AddUserField(t, "user_animationInterval", animationInterval);
						AddUserField(t, "user_primaryIntensity", primaryIntensity);
						AddUserField(t, "user_secondaryIntensity", secondaryIntensity);
						AddUserField(t, "allowColorChange", allowColorChange);
						t.UpdateConfiguration();
					}
				}
			}

			General.Map.Map.EndAddRemove();
			General.Map.Map.Update();
			General.Map.ThingsFilter.Update();

			return true;
		}

		private static T GetEntry<T>(UniversalCollection c, string entryname, ref string error)
		{
			if (!string.IsNullOrEmpty(error))
			{
				return default;
			}

			// Find the entry
			foreach (UniversalEntry e in c)
			{
				// Check if matches
				if (e.Key == entryname)
				{
					// Let's be kind and cast any int to a float if needed
					if ((typeof(T) == typeof(double)) && (e.Value is int))
					{
						// Make it a float
						object fvalue = (double)(int)e.Value;
						return (T)fvalue;
					}
					else
					{
						// Verify type
						e.ValidateType(typeof(T));

						// Found it!
						return (T)e.Value;
					}
				}
			}

			error = $"Error while reading LightGroupThings data: Missing required field \"{entryname}\"";
			return default;
		}

		private static void AddUserField<T>(Thing t, string field, T value)
		{
			if (typeof(T) == typeof(int))
			{
				t.Fields[field] = new UniValue(Types.UniversalType.Integer, value);
			}
			else if (typeof(T) == typeof(double))
			{
				t.Fields[field] = new UniValue(Types.UniversalType.Float, value);
			}
			else if (typeof(T) == typeof(bool))
			{
				t.Fields[field] = new UniValue(Types.UniversalType.Boolean, value);
			}
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
			textboxoutput.Select(textboxoutput.Text.Length, 0);
			textboxoutput.SelectionColor = color == default(Color) ? textboxoutput.ForeColor : color;
			textboxoutput.SelectedText = message + Environment.NewLine;
			textboxoutput.ScrollToCaret();
		}

		private Int32 TaskPercent(UInt64 tasksComplete, UInt64 tasksCount, int fromPercent, int toPercent)
		{
			return (Int32)((tasksComplete / (double)tasksCount) * (toPercent - fromPercent)) + fromPercent;
		}

		delegate void ProcessOutputCallback(string message);
		delegate void OnProcessExitedCallback();
		delegate void OnProcessErrorCallback(string message);

		private System.Diagnostics.Process m_BuildProcess;
		private List<string> m_Messages = new List<string>();

		private UInt64 m_GatherTasks = 0;
		private UInt64 m_RaytraceTasks = 0;
		private bool m_HasErrors = false;
		private bool m_WasCancelled = false;

		const int GatherTasksStartPercent = 5;
		const int GatherTasksEndPercent = 20;
		const int RaytraceStartPercent = GatherTasksEndPercent;
		const int RaytraceEndPercent = 90;
	}
}
