using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using VideoEffects.Helpers;

namespace VideoEffects.Win2D
{
    /// <summary>
    /// Gaussian Blur effect - http://microsoft.github.io/Win2D/html/T_Microsoft_Graphics_Canvas_Effects_GaussianBlurEffect.htm
    /// Amount - 0 is off, default is 3, max is 9
    /// </summary>
    public sealed class GaussianBlurVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties _currentEncodingProperties;
        private CanvasDevice _canvasDevice;
        private IPropertySet _configuration;

        public float Amount
        {
            get
            {
                if (_configuration != null && _configuration.ContainsKey("Amount"))
                    return (float)_configuration["Amount"];

                return 3f;
            }
            set => _configuration["Amount"] = value;
        }

        public void SetProperties(IPropertySet configuration)
        {
            _configuration = configuration;
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _currentEncodingProperties = encodingProperties;
            _canvasDevice = device != null ? CanvasDevice.CreateFromDirect3D11Device(device) : CanvasDevice.GetSharedDevice();
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            // When using SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu we need to check if we're using GPU or CPU for the frame

            // If we're on GPU, use InputFrame.Direct3DSurface
            if (context.InputFrame.SoftwareBitmap == null)
            {
                using (var inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var effect = new GaussianBlurEffect
                    {
                        Source = inputBitmap,
                        BlurAmount = Amount,
                        Optimization = EffectOptimization.Speed
                    };

                    ds.DrawImage(effect);
                }

                return;
            }

            // If we're on CPU, use InputFrame.SoftwareBitmap
            if (context.InputFrame.Direct3DSurface == null)
            {
                // InputFrame's raw pixels
                byte[] inputFrameBytes = new byte[4 * context.InputFrame.SoftwareBitmap.PixelWidth * context.InputFrame.SoftwareBitmap.PixelHeight];
                context.InputFrame.SoftwareBitmap.CopyToBuffer(inputFrameBytes.AsBuffer());

                using (var inputBitmap = CanvasBitmap.CreateFromBytes(
                    _canvasDevice,
                    inputFrameBytes,
                    context.InputFrame.SoftwareBitmap.PixelWidth,
                    context.InputFrame.SoftwareBitmap.PixelHeight,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized))

                using (var renderTarget = new CanvasRenderTarget(
                    _canvasDevice,
                    context.OutputFrame.SoftwareBitmap.PixelWidth,
                    context.OutputFrame.SoftwareBitmap.PixelHeight,
                    (float) context.OutputFrame.SoftwareBitmap.DpiX,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    CanvasAlphaMode.Premultiplied))
                {
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        var effect = new GaussianBlurEffect
                        {
                            Source = inputBitmap,
                            BlurAmount = Amount,
                            Optimization = EffectOptimization.Speed
                        };

                        ds.DrawImage(effect);
                    }
                }
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            _canvasDevice?.Dispose();
        }

        public void DiscardQueuedFrames()
        {
            //no frames cached
        }

        public bool IsReadOnly => false;

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => Constants.SupportedEncodingProperties;
        public MediaMemoryTypes SupportedMemoryTypes => Constants.GlobalSupportedMemoryTypes;
        public bool TimeIndependent => false;
    }
}
