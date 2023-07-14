using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace ComputerGraphicsFromScratch;

internal sealed class Rasterizer
{
	private const int NumTextures = 2;
	private int TextureIndex;
	private readonly Texture2D[] Textures;
	private readonly uint[] TextureData;

	private readonly int CanvasW;
	private readonly int CanvasH;
	private readonly float ViewportW;
	private readonly float ViewportH;
	private readonly float ViewportZ = 1.0f;

	private readonly Color BackgroundColor = Color.White;
	private readonly Vector3 CameraPosition = Vector3.Zero;

	public Rasterizer(GraphicsDevice graphicsDevice, int w, int h)
	{
		CanvasW = w;
		CanvasH = h;

		ViewportW = (float)w / h;
		ViewportH = 1.0f;

		// Set up the textures.
		Textures = new Texture2D[NumTextures];
		for (var i = 0; i < NumTextures; i++)
		{
			Textures[i] = new Texture2D(graphicsDevice, w, h, false, SurfaceFormat.Color);
		}

		TextureData = new uint[CanvasW * CanvasH];
	}

	public void Update(GameTime _)
	{
		var texture = Textures[TextureIndex];

		var halfCanvasW = CanvasW >> 1;
		var halfCanvasH = CanvasH >> 1;

		for (var x = -halfCanvasW; x < halfCanvasW; x++)
		{
			for (var y = -halfCanvasH; y < halfCanvasH; y++)
			{
				var color = Color.BlueViolet;
				PutPixel(x, y, color);
			}
		}

		texture.SetData(TextureData);
	}

	private void PutPixel(int canvasX, int canvasY, Color color)
	{
		var x = canvasX + (CanvasW >> 1);
		var y = (CanvasH >> 1) - canvasY - 1;
		TextureData[y * CanvasW + x] = ToRgba(color);
	}

	private Vector3 CanvasToViewport(int canvasX, int canvasY)
	{
		return new Vector3(canvasX * ViewportW / CanvasW, canvasY * ViewportH / CanvasH, ViewportZ);
	}
	private static uint ToRgba(Color color)
	{
		return color.R
		       | ((uint)color.G << 8)
		       | ((uint)color.B << 16)
		       | ((uint)color.A << 24);
	}

	public void Draw(SpriteBatch spriteBatch)
	{
		spriteBatch.Draw(Textures[TextureIndex], new Vector2(0, 0), null, Color.White);
		TextureIndex = (TextureIndex + 1) % NumTextures;
	}
}
