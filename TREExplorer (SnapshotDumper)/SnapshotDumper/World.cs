using SWGLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnapshotDumper
{
	public class World
	{
		private struct ScriptObjvarPair
		{
			public string scripts, objvars;
			public ScriptObjvarPair(string scripts, string objvars)
			{
				this.scripts = scripts;
				this.objvars = objvars;
			}
		}

		private const string columnNames = "objid	container	server_template_crc	cell_index	px	py	pz	qw	qx	qy	qz	scripts	objvars";
		private const string columnTypes = "i	i	h	i	f	f	f	f	f	f	f	s	p";

		private WSFile ws;
		private string planetName;
		private List<WSFile.WSNode>[,] quadtree;
		private Hashtable oldObjects;

		public World(WSFile ws, string planetName)
		{
			this.ws = ws;
			this.planetName = planetName;
			this.quadtree = new List<WSFile.WSNode>[8, 8];

			oldObjects = new Hashtable();
			ParseOldObjects();

			for (int x = 0; x < 8; x++)
				for (int z = 0; z < 8; z++)
					quadtree[x, z] = new List<WSFile.WSNode>();

			foreach (WSFile.WSNode n in ws.Nodes)
				AddNode(n);
		}

		private void ParseOldObjects()
		{
			string[] files = Directory.GetFiles("old/" + planetName + "/");
			for(int i = 0; i < files.Length; i++)
			{
				StreamReader s = new StreamReader(File.OpenRead(files[i]));
				while(s.Peek() > 0) 
				{
					try
					{
						string[] values = s.ReadLine().Split('\t');

						long objid = long.Parse(values[0]);
						string scripts = values[11];
						string objvars = values[12];

						oldObjects[objid] = new ScriptObjvarPair(scripts, objvars);
					}
					catch (Exception ex) { }
				}
			}
		} 

		private void AddNode(WSFile.WSNode n)
		{
			quadtree[Math.Abs((int)((n.X + 8192) / 2048)), Math.Abs((int)((n.Z + 8192) / 2048))].Add(n);
		}

		private string BuildEntry(WSFile ws, WSFile.WSNode n, int cellIndex)
		{
			StringBuilder b = new StringBuilder();

			// Append values of WS node matching format of buildout
			// tables
			b.Append(n.ID + "\t");
			b.Append(n.ParentID + "\t");
			b.Append(ws.Types[n.ObjectIndex].Replace("shared_", "") + "\t");
			b.Append(cellIndex + "\t");

			// Child objects have local (to parent) coordinates
			bool isContained = n.ParentID != 0;

			b.Append((isContained ? n.X : (n.X + 8192) % 2048) + "\t");
			b.Append((isContained ? n.Y : (n.Y + 8192) % 2048) + "\t");
			b.Append((isContained ? n.Z : (n.Z + 8192) % 2048) + "\t");

			// SWGLib has W and Y quarternion axis switched
			b.Append(n.oY + "\t");
			b.Append(n.oX + "\t");
			b.Append(n.oW + "\t");
			b.Append(n.oZ + "\t");

			// Pull objvars and scripts from old exported snapshots
			if(oldObjects.ContainsKey((long) n.ID))
			{
				ScriptObjvarPair pair = (ScriptObjvarPair) oldObjects[(long) n.ID];
				b.Append(pair.scripts + "\t");
				b.Append(pair.objvars);
			}
			else
			{
				b.Append("\t"); // scripts
				b.Append("$|"); // objvars
			}

			// We've finished building entry for current object
			b.AppendLine();

			// Process children if they exist
			if (n.Nodes.Count > 0)
			{
				int _cellIndex = 1;

				foreach (WSFile.WSNode c in n.Nodes)
				{
					bool isCell = ws.Types[c.ObjectIndex] == "object/cell/shared_cell.iff";
					b.Append(BuildEntry(ws, c, isCell ? _cellIndex++ : 0));
				}
			}

			// We've finished building the entry for this WSNode
			// object
			return b.ToString();
		}

		public void DumpToFile(string path)
		{
			// Create the directory incase it doesn't already exist
			Directory.CreateDirectory(path + "/");

			// Each core planet is made of 8x8 regions
			for (int x = 0; x < 8; x++)
			{
				for (int z = 0; z < 8; z++)
				{
					// We only want to create an export if the region
					// has atleast one object
					if (quadtree[x, z].Count == 0)
						continue;

					StringBuilder b = new StringBuilder();

					b.AppendLine(columnNames);
					b.AppendLine(columnTypes);

					foreach (SWGLib.WSFile.WSNode n in quadtree[x, z])
						b.Append(BuildEntry(ws, n, 0));

					// Generate our snapshot table file name
					String fileName = String.Format("{0}_{1}_{2}_ws.tab", planetName, (x + 1), (z + 1));

					// Go ahead and dump it all to file
					File.WriteAllText(path + "/" + fileName, b.ToString());
				}
			}
		}
	}
}