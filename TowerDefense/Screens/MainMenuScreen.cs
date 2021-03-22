using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Screens;
using System;
using System.Collections.Generic;

namespace TowerDefense.Screens
{
	class MainMenuScreen : ScreenBase
	{
		private readonly SpriteBatch _spriteBatch;

		Type[] screens = new Type[] { typeof(MainGameScreen), typeof(SettingsScreen) };
		List<Link> screenLinks;

		public MainMenuScreen(Game game) : base(game)
		{
			_spriteBatch = new SpriteBatch(Game.GraphicsDevice);

			screenLinks = new List<Link>();
			foreach (var v in screens)
			{
				var link = new Link(v.Name, GameServices.Fonts["SegoeUI"]);
				link.ClickedEvent += (a, b) => DoClick((Screen)Activator.CreateInstance(v, new object[] { Game }));
				screenLinks.Add(link);
			}
		}

		void DoClick(Screen screen)
		{
			Game.Services.GetService<ScreenManager>().LoadScreen(screen);
		}

		public override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Game.Services.GetService<InputManager>().IsNewKeyPress(Keys.Escape))
			{
				// would you like to quit?
				Game.Exit();
			}

			foreach (var v in screenLinks)
			{
				v.Update(gameTime);
			}
		}

		public override void Draw(GameTime gameTime)
		{
			Game.GraphicsDevice.Clear(Color.BlueViolet);

			_spriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.PointClamp);

			var centre = Game.GraphicsDevice.Viewport.Width / 2f;
			var font = GameServices.Fonts["SegoeUI"];
			var heading = "Tower Defense";
			var ms = font.MeasureString(heading);
			var fontWidth = ms.X;
			var expectedWidth = Game.GraphicsDevice.Viewport.Width * 0.5f;
			var headingScale = expectedWidth / fontWidth;
			var headingFontSize = ms * headingScale;
			var tenthViewportHeight = Game.GraphicsDevice.Viewport.Height / 10f;
			var fontDrawPos = new Vector2(centre - (headingFontSize.X / 2), tenthViewportHeight);
			_spriteBatch.DrawString(font, heading, fontDrawPos, Color.White, 0, Vector2.Zero, headingScale, SpriteEffects.None, 0);

			var yStart = fontDrawPos.Y + headingFontSize.Y + tenthViewportHeight;
			foreach (var v in screenLinks)
			{
				v.Draw(gameTime, _spriteBatch, new Vector2(centre, yStart), headingScale * 0.35f);
				yStart += (int)(v.Bounds.Height + (v.Bounds.Height / 2f));
			}

			_spriteBatch.End();
		}
	}

	class Link
	{
		public RectangleF Bounds;

		string text;
		SpriteFont font;

		public bool Clicked;
		public bool Clicking;
		public bool MouseOver;

		public event EventHandler ClickedEvent;


		public Link(string text, SpriteFont font)
		{
			this.text = text;
			this.font = font;
		}
		public void Update(GameTime gameTime)
		{
			var mouse = Mouse.GetState();
			MouseOver = Bounds.Contains(mouse.Position);
			Clicking = MouseOver && mouse.LeftButton == ButtonState.Pressed;

			if (Clicking)
			{
				Clicked = true;
				ClickedEvent.Invoke(this, new EventArgs());
			}
		}

		public void Draw(GameTime gameTime, SpriteBatch _spriteBatch, Vector2 drawCentreAt, float scale)
		{
			var fontDimensions = font.MeasureString(text) * scale;
			var drawPos = drawCentreAt - new Vector2(fontDimensions.X / 2, fontDimensions.Y / 2);

			var color = Color.DarkGray;
			if (MouseOver)
				color = Color.Gray;
			if (Clicking)
				color = Color.DarkOrchid;
			_spriteBatch.DrawString(font, text, drawPos, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

			Bounds = new RectangleF(drawPos, fontDimensions);
			Bounds.Inflate(2, 2);

			// debug
			//_spriteBatch.DrawRectangle(Bounds, Color.White);
		}
	}
}
