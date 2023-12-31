﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ComputerGraphicsFromScratch;

internal sealed class Game1 : Game
{
	private const int W = 640;
	private const int H = 640;

	private GraphicsDeviceManager Graphics;
	private SpriteBatch SpriteBatch;
	private readonly Rasterizer Rasterizer;
	private SpriteFont MonospaceFont;

	public Game1()
	{
		Graphics = new GraphicsDeviceManager(this);
		Graphics.GraphicsProfile = GraphicsProfile.HiDef;

		Window.AllowUserResizing = false;
		Graphics.SynchronizeWithVerticalRetrace = true;

		Graphics.PreferredBackBufferHeight = H;
		Graphics.PreferredBackBufferWidth = W;
		Graphics.IsFullScreen = false;
		Graphics.ApplyChanges();

		Rasterizer = new Rasterizer(GraphicsDevice, W, H);

		Content.RootDirectory = "Content";
		IsMouseVisible = true;
	}

	protected override void LoadContent()
	{
		SpriteBatch = new SpriteBatch(GraphicsDevice);
		MonospaceFont = Content.Load<SpriteFont>("Fonts/Monospace");
	}

	protected override void Update(GameTime gameTime)
	{
		if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
			Exit();

		Rasterizer.Update(gameTime);

		base.Update(gameTime);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.CornflowerBlue);

		SpriteBatch.Begin();
		Rasterizer.Draw(SpriteBatch, MonospaceFont);
		SpriteBatch.End();

		base.Draw(gameTime);
	}
}
