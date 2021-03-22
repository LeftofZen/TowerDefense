using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace TowerDefense
{
	class Tower
	{
		public int dps = 25;
		public int dps_variation = 5;
		public float cooldown = 1403f;
		public float lastFireTime;
		public float range = 256;
		public float bulletDrawFadeTime = 1003f;
		public int tileSize;
		public int level => 1 + (int)Math.Log(Math.Max(1, experience - 100));
		public int experience = 1;

		public (Vector2, Vector2)? BulletLine { get; set; }
		public Point Tile { get; set; }

		public Vector2 Center => (Tile.ToVector2() * new Vector2(tileSize)) + new Vector2(tileSize / 2);

		public Tower(Point tile) => Tile = tile;

		public float distToEnemy(Enemy e) => Vector2.Distance(e.position, Center);

		public int AverageDPS => (dps + (int)(dps_variation / 2f)) * level;

		public int EffectiveDPS => (dps + (int)(GameServices.Random.NextDouble() * dps_variation)) * level;

		public void ShootAt(Enemy e)
		{
			var dmg = EffectiveDPS;
			e.health -= dmg;
			experience += dmg;

			if (e.IsDead)
			{
				experience += e.value;
			}
		}

		public void Draw(SpriteBatch sb, GameTime gameTime)
		{
			var centreOfTile = new Vector2(
				Tile.X * tileSize + tileSize / 2,
				Tile.Y * tileSize + tileSize / 2);

			sb.FillRectangle(
				new Rectangle(
					Tile.X * tileSize + 8,
					Tile.Y * tileSize + 8,
					tileSize - 16,
					tileSize - 16),
				Color.Purple);

			if (BulletLine.HasValue)
			{
				var src = BulletLine.Value.Item1;
				var dst = BulletLine.Value.Item2;
				var decay = (float)gameTime.TotalGameTime.TotalMilliseconds - lastFireTime;
				var decayPer = decay / bulletDrawFadeTime;
				var color = new Color(Color.Aqua, MathHelper.SmoothStep(1, 0, (float)Math.Sqrt(decayPer)));
				//_spriteBatch.DrawString(GameServices.Fonts["SegoeUI"], decayPer.ToString() + "_" + color.ToString(), dst, Color.Black);
				sb.DrawLine(src, dst, color, 5);
			}

			// range
			var c = new CircleF(Center.ToPoint(), range);
			sb.DrawCircle(c, tileSize, Color.MediumPurple, 2);

			// tower details
			var str = $"Lvl={level}\nDPS={AverageDPS}\nExp={experience}";
			var font = GameServices.Fonts["SegoeUI"];
			var strDetails = font.MeasureString(str);
			sb.DrawString(font, str, centreOfTile - strDetails / 2f - new Vector2(0, strDetails.Y / 2), Color.Black);
		}
	}
}
