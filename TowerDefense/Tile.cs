using System;
using System.Collections.Generic;
using System.Text;

namespace TowerDefense
{
	public enum TileType
	{
		Path, Free, Blocked, Spawn, Goal
	}

	class Tile
	{
		public Tower tower;
		public TileType type;
	}
}
