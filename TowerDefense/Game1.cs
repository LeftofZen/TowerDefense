using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using MonoGame.Extended.Screens;
using MonoGame.Extended.Screens.Transitions;
using TowerDefense.Screens;

namespace TowerDefense
{
	public static class GameServices
	{
		public static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(); // TextureCollection??
		public static readonly Dictionary<string, SpriteFont> Fonts = new Dictionary<string, SpriteFont>(); //
		public static readonly Dictionary<string, SoundEffect> SoundEffects = new Dictionary<string, SoundEffect>(); // SoundBank??
		public static readonly Dictionary<string, Song> Songs = new Dictionary<string, Song>(); // SongCollection??
		public static readonly Random Random = new Random();
	}

	public class Game1 : Game
	{
		private GraphicsDeviceManager _graphics;
		ScreenManager screenManager;

		public Game1()
		{
			_graphics = new GraphicsDeviceManager(this);

			Content.RootDirectory = "Content";

			Window.AllowUserResizing = true;
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			_graphics.SynchronizeWithVerticalRetrace = false;
			_graphics.PreferredBackBufferWidth = 1920;
			_graphics.PreferredBackBufferHeight = 1080;
			_graphics.ApplyChanges();

			screenManager = new ScreenManager();
			Components.Add(screenManager);
			Services.AddService(typeof(ScreenManager), screenManager);

			var inputManager = new InputManager(this);
			Components.Add(inputManager);
			Services.AddService(typeof(InputManager), inputManager);

			base.Initialize();

			screenManager.LoadScreen(new MainMenuScreen(this), new FadeTransition(GraphicsDevice, Color.Black, 1f));
			//screenManager.LoadScreen(new MainGameScreen(this), new FadeTransition(GraphicsDevice, Color.Black, 1f));
		}

		protected override void LoadContent()
		{
			var fontNames = new List<string> { "SegoeUI" };
			foreach (var v in fontNames)
			{
				GameServices.Fonts.Add(v, Content.Load<SpriteFont>("Fonts\\" + v));
			}

			base.LoadContent();
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.SteelBlue);
			base.Draw(gameTime);
		}
	}
}
