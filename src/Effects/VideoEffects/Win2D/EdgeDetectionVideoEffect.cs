using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using VideoEffects.Helpers;

namespace VideoEffects.Win2D
{
    public sealed class EdgeDetectionVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties _currentEncodingProperties;
        private CanvasDevice _canvasDevice;
        private IPropertySet _configuration;

#if DEBUG
        private bool debugOutputFlag;
#endif

        private float Amount
        {
            get
            {
                if (_configuration != null && _configuration.ContainsKey("Amount"))
                    return (float)_configuration["Amount"];

                return 0.1f;
            }
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            //using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
            //using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
            //using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            //{

            //    var effect = new EdgeDetectionEffect
            //    {
            //        Source = inputBitmap,
            //        Amount = this.Amount
            //    };

            //    ds.DrawImage(effect);
            //}

            // When using SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu we need to check if we're using GPU or CPU for the frame

            // If we're on GPU, use InputFrame.Direct3DSurface
            if (context.InputFrame.SoftwareBitmap == null)
            {
                using (var inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var effect = new EdgeDetectionEffect
                    {
                        Source = inputBitmap,
                        Amount = this.Amount
                    };

                    ds.DrawImage(effect);
                }

                return;
            }

            // If we're on CPU, use InputFrame.SoftwareBitmap
            if (context.InputFrame.Direct3DSurface == null)
            {
#if DEBUG
                if (!debugOutputFlag)
                {
                    Debug.WriteLine($"PixelFormat: {context.InputFrame.SoftwareBitmap.BitmapPixelFormat}");
                    debugOutputFlag = true;
                }
#endif

                // ********** Test for using SoftwareBitmap.Convert ********* //
                //using (var inputBitmap = CanvasBitmap.CreateFromSoftwareBitmap(
                //    _canvasDevice,
                //    SoftwareBitmap.Convert(context.InputFrame.SoftwareBitmap, context.InputFrame.SoftwareBitmap.BitmapPixelFormat))) //PixelFormat: Bgra8

                //using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                //using (var ds = renderTarget.CreateDrawingSession())
                //{
                //    var effect = new EdgeDetectionEffect
                //    {
                //        Source = inputBitmap,
                //        Amount = this.Amount
                //    };

                //    ds.DrawImage(effect);
                //}

                // ********************************************************** //

                // InputFrame's raw pixels
                byte[] inputFrameBytes = new byte[4 * context.InputFrame.SoftwareBitmap.PixelWidth * context.InputFrame.SoftwareBitmap.PixelHeight];
                context.InputFrame.SoftwareBitmap.CopyToBuffer(inputFrameBytes.AsBuffer());

                using (var inputBitmap = CanvasBitmap.CreateFromBytes(
                    _canvasDevice,
                    inputFrameBytes,
                    context.InputFrame.SoftwareBitmap.PixelWidth,
                    context.InputFrame.SoftwareBitmap.PixelHeight,
                    context.InputFrame.SoftwareBitmap.BitmapPixelFormat.ToDirectXPixelFormat()))

                using (var renderTarget = new CanvasRenderTarget(
                    _canvasDevice,
                    context.OutputFrame.SoftwareBitmap.PixelWidth,
                    context.OutputFrame.SoftwareBitmap.PixelHeight,
                    (float)context.OutputFrame.SoftwareBitmap.DpiX,
                    context.OutputFrame.SoftwareBitmap.BitmapPixelFormat.ToDirectXPixelFormat(),
                    CanvasAlphaMode.Premultiplied))
                {
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        var effect = new EdgeDetectionEffect
                        {
                            Source = inputBitmap,
                            Amount = this.Amount
                        };

                        ds.DrawImage(effect);

                    }
                }

            }
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _currentEncodingProperties = encodingProperties;
            _canvasDevice = device != null ? CanvasDevice.CreateFromDirect3D11Device(device) : CanvasDevice.GetSharedDevice();
        }

        public void SetProperties(IPropertySet configuration)
        {
            _configuration = configuration;
        }

        public bool IsReadOnly => false;
        public MediaMemoryTypes SupportedMemoryTypes => Constants.GlobalSupportedMemoryTypes;
        public bool TimeIndependent => false;

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => Constants.SupportedEncodingProperties;

        public void Close(MediaEffectClosedReason reason)
        {
            // Clean up devices
            _canvasDevice?.Dispose();
        }

        public void DiscardQueuedFrames()
        {
            // No cached frames to discard
        }
    }
}
