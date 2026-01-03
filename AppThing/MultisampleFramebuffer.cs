using GLCS;
using GLCS.Managed;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace AppThing;

internal sealed class MultisampleFramebuffer : IDisposable
{
	private readonly GLFramebuffer _framebuffer = new();
	private readonly GLTexture _colorBuffer = new(TextureTarget.Texture2dMultisample);
	private readonly GLRenderbuffer? _depthBuffer = null;

	private Size _size;
	private int _samples;

	public MultisampleFramebuffer(bool supportDepthBuffer)
	{
		if (supportDepthBuffer)
			_depthBuffer = new();
	}

	public void Dispose()
	{
		_framebuffer.Dispose();
		_colorBuffer.Dispose();
		_depthBuffer?.Dispose();
	}

	public void Setup(Size size, int samples)
	{
		var maxSamples = Renderer.Gl.GetInteger(GetPName.MaxFramebufferSamples);
		if (samples > maxSamples)
		{
			Console.WriteLine($"[MultisampleFramebuffer] Requested {samples} samples, {maxSamples} maximum supported, reducing.");
			samples = maxSamples;
		}

		if (size == _size && samples == _samples)
			return;

		if (size.Width == 0 || size.Height == 0)
			return;

		_size = size;
		_samples = samples;

		if (samples <= 0)
			return;

		_colorBuffer.Image2DMultisample(samples, InternalFormat.Rgba, size, true);
		_framebuffer.Texture2D(FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2dMultisample, _colorBuffer, 0);

		if (_depthBuffer != null)
		{
			_depthBuffer.StorageMultisample(samples, InternalFormat.Depth24Stencil8, size);
			_framebuffer.Renderbuffer(FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _depthBuffer);
		}

		if (_framebuffer.CheckStatus() != FramebufferStatus.FramebufferComplete)
			throw new("Framebuffer not complete.");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BeginFrame() => _framebuffer.BindDraw();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EndFrame() => _framebuffer.UnbindDraw();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Blit(GLFramebuffer? target, Rectangle sourceRect, Rectangle destRect) => _framebuffer.Blit(target, sourceRect, destRect, ClearBufferMask.ColorBufferBit | (_depthBuffer != null ? ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit : 0), BlitFramebufferFilter.Nearest);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Blit() => Blit(null, new(Point.Empty, _size), new(Point.Empty, _size));
}
