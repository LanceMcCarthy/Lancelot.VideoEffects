﻿using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using VideoEffects.Helpers;

//Invert docs: http://microsoft.github.io/Win2D/html/T_Microsoft_Graphics_Canvas_Effects_InvertEffect.htm

namespace VideoEffects.Win2D
{
    public sealed class InvertVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties _currentEncodingProperties;
        private CanvasDevice _canvasDevice;
        private IPropertySet _configuration;

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            //using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
            //using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
            //using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
            //{
            //    var invert = new InvertEffect()
            //    {
            //        Source = inputBitmap
            //    };
            //    ds.DrawImage(invert);

            //}

            // When using SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu we need to check if we're using GPU or CPU for the frame

            // If we're on GPU, use InputFrame.Direct3DSurface
            if (context.InputFrame.SoftwareBitmap == null)
            {
                using (var inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var invert = new InvertEffect()
                    {
                        Source = inputBitmap
                    };
                    ds.DrawImage(invert);
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
                    context.InputFrame.SoftwareBitmap.BitmapPixelFormat.ToDirectXPixelFormat()))

                using (var renderTarget = new CanvasRenderTarget(
                    _canvasDevice,
                    context.OutputFrame.SoftwareBitmap.PixelWidth,
                    context.OutputFrame.SoftwareBitmap.PixelHeight,
                    (float) context.OutputFrame.SoftwareBitmap.DpiX,
                    context.OutputFrame.SoftwareBitmap.BitmapPixelFormat.ToDirectXPixelFormat(),
                    CanvasAlphaMode.Premultiplied))
                {
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        var invert = new InvertEffect()
                        {
                            Source = inputBitmap
                        };
                        ds.DrawImage(invert);

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
            //NOT USED, Invert has no properties to configure, but leaving propset instance in place to prevent breaking
            _configuration = configuration;
        }

        public bool IsReadOnly => false;
        public MediaMemoryTypes SupportedMemoryTypes => Constants.GlobalSupportedMemoryTypes;
        public bool TimeIndependent => false;

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => Constants.SupportedEncodingProperties;

        public void Close(MediaEffectClosedReason reason)
        {
            _canvasDevice?.Dispose();
        }

        public void DiscardQueuedFrames()
        {
            //no cached frames
        }
    }
}
