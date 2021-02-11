using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.UI;
using VideoEffects.Helpers;

namespace VideoEffects.Win2D
{
    public sealed class WaterVideoEffect : IBasicVideoEffect
    {
        CanvasDevice _canvasDevice;
        private IPropertySet _configuration;

        /// <summary>
        /// From 0 to 100
        /// </summary>
        public float Amount
        {
            get
            {
                if (_configuration != null && _configuration.ContainsKey("Amount"))
                    return (float)_configuration["Amount"];

                return 100f;
            }
            set => _configuration["Amount"] = value;
        }

        public void SetProperties(IPropertySet configuration)
        {
            _configuration = configuration;
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _canvasDevice = device != null ? CanvasDevice.CreateFromDirect3D11Device(device) : CanvasDevice.GetSharedDevice();
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            //using (var input = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
            //using (var output = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
            //using (var ds = output.CreateDrawingSession())
            //{
            //    var time = context.InputFrame.RelativeTime ?? new TimeSpan();

            //    var dispX = (float)Math.Cos(time.TotalSeconds) * 75f;
            //    var dispY = (float)Math.Sin(time.TotalSeconds) * 75f;

            //    ds.Clear(Colors.Black);

            //    var dispMap = new DisplacementMapEffect()
            //    {
            //        Source = input,
            //        XChannelSelect = EffectChannelSelect.Red,
            //        YChannelSelect = EffectChannelSelect.Green,
            //        Amount = this.Amount,
            //        Displacement = new Transform2DEffect()
            //        {
            //            TransformMatrix = Matrix3x2.CreateTranslation(dispX, dispY),
            //            Source = new BorderEffect()
            //            {
            //                ExtendX = CanvasEdgeBehavior.Mirror,
            //                ExtendY = CanvasEdgeBehavior.Mirror,
            //                Source = new TurbulenceEffect()
            //                {
            //                    Octaves = 3
            //                }
            //            }
            //        }
            //    };

            //    ds.DrawImage(dispMap, -25f, -25f);
            //}

            // When using SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu we need to check if we're using GPU or CPU for the frame

            // If we're on GPU, use InputFrame.Direct3DSurface
            if (context.InputFrame.SoftwareBitmap == null)
            {
                using (var inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var time = context.InputFrame.RelativeTime ?? new TimeSpan();

                    var dispX = (float)Math.Cos(time.TotalSeconds) * 75f;
                    var dispY = (float)Math.Sin(time.TotalSeconds) * 75f;

                    ds.Clear(Colors.Black);

                    var dispMap = new DisplacementMapEffect()
                    {
                        Source = inputBitmap,
                        XChannelSelect = EffectChannelSelect.Red,
                        YChannelSelect = EffectChannelSelect.Green,
                        Amount = this.Amount,
                        Displacement = new Transform2DEffect()
                        {
                            TransformMatrix = Matrix3x2.CreateTranslation(dispX, dispY),
                            Source = new BorderEffect()
                            {
                                ExtendX = CanvasEdgeBehavior.Mirror,
                                ExtendY = CanvasEdgeBehavior.Mirror,
                                Source = new TurbulenceEffect()
                                {
                                    Octaves = 3
                                }
                            }
                        }
                    };

                    ds.DrawImage(dispMap, -25f, -25f);
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
                        var time = context.InputFrame.RelativeTime ?? new TimeSpan();

                        var dispX = (float)Math.Cos(time.TotalSeconds) * 75f;
                        var dispY = (float)Math.Sin(time.TotalSeconds) * 75f;

                        ds.Clear(Colors.Black);

                        var dispMap = new DisplacementMapEffect()
                        {
                            Source = inputBitmap,
                            XChannelSelect = EffectChannelSelect.Red,
                            YChannelSelect = EffectChannelSelect.Green,
                            Amount = this.Amount,
                            Displacement = new Transform2DEffect()
                            {
                                TransformMatrix = Matrix3x2.CreateTranslation(dispX, dispY),
                                Source = new BorderEffect()
                                {
                                    ExtendX = CanvasEdgeBehavior.Mirror,
                                    ExtendY = CanvasEdgeBehavior.Mirror,
                                    Source = new TurbulenceEffect()
                                    {
                                        Octaves = 3
                                    }
                                }
                            }
                        };

                        ds.DrawImage(dispMap, -25f, -25f);
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

        }

        public bool IsReadOnly { get; }
        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => Constants.SupportedEncodingProperties;
        public MediaMemoryTypes SupportedMemoryTypes => Constants.GlobalSupportedMemoryTypes;
        public bool TimeIndependent => true;
    }
}
