using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TowerDefense
{
	public static class SpriteBatchExtensions
	{
		public static void DrawStringCentered(this SpriteBatch sb, SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
		{
			Vector2 scale2 = new Vector2(scale, scale);
			var centre = spriteFont.MeasureString(text);
			var pos = position - new Vector2(centre.X / 2f, centre.Y / 2f);
			sb.DrawString(spriteFont, text, position, color, rotation, origin, scale2, effects, layerDepth);
		}
		public static void DrawStringCentered(this SpriteBatch sb, SpriteFont spriteFont, string text, Vector2 position, Color color)
		{
			var centre = spriteFont.MeasureString(text);
			var pos = position - new Vector2(centre.X / 2f, centre.Y / 2f);
			sb.DrawString(spriteFont, text, pos, color, 0f, Vector2.Zero, 0f, SpriteEffects.None, 0);
		}
	}
}
