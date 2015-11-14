using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Collections;

using SWGLib;

/*
	This tool was created to assist SWGReborn in recovering items from SOE's original Gold DB.

	SOE had a cluster referred to as Gold which was used to store results of world building,
	these results were exported to what we know of as 'snapshots', snapshots consist of all
	world objects in the gold DB that were marked as client-cached.

	We rely on SWGLib provided via TREExplorer due to the convenience of already existing code
	to deserialze the client's snapshot tables.

	I have added limited comments for those interested in whats going on in this code.

	- Seefo
*/

namespace SnapshotDumper
{
	class Program
	{
		static void Main(string[] args)
		{
			string[] files = Directory.GetFiles("snapshot/");

			for(int i = 0; i < files.Length; i++)
			{
				Console.WriteLine("Processing {0}...", files[i]);

				string planetName = Path.GetFileNameWithoutExtension(files[i]);

				World world = new World(new SWGLib.WSFile(files[i]), planetName);
				world.DumpToFile("new/" + planetName + "/");
			}	
		}	
	}
}