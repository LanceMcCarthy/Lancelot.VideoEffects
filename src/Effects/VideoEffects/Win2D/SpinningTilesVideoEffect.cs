using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.UI;
using VideoEffects.Helpers;

namespace VideoEffects.Win2D
{
    public sealed class SpinningTilesVideoEffect : IBasicVideoEffect
    {
        CanvasDevice _canvasDevice;
        private IPropertySet _configuration;
        uint _numColumns, _numRows;
        const uint PixelsPerTile = 60;

        Transform2DEffect[,] _transforms;
        CropEffect[,] _crops;

        // Scales a number from SinCos range [-1, 1] to range [outputMin, outputMax].
        private float Rescale(float input, float outputMin, float outputMax)
        {
            return outputMin + (input + 1) / 2 * (outputMax - outputMin);
        }

        public void SetProperties(IPropertySet configuration)
        {
            _configuration = configuration;
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _canvasDevice = device != null ? CanvasDevice.CreateFromDirect3D11Device(device) : CanvasDevice.GetSharedDevice();

            _numColumns = (uint)(encodingProperties.Width / PixelsPerTile);
            _numRows = (uint)(encodingProperties.Height / PixelsPerTile);
            _transforms = new Transform2DEffect[_numColumns, _numRows];
            _crops = new CropEffect[_numColumns, _numRows];

            for (uint i = 0; i < _numColumns; i++)
            {
                for (uint j = 0; j < _numRows; j++)
                {
                    _crops[i, j] = new CropEffect();
                    _crops[i, j].SourceRectangle = new Rect(i * PixelsPerTile, j * PixelsPerTile, PixelsPerTile, PixelsPerTile);
                    _transforms[i, j] = new Transform2DEffect();
                    _transforms[i, j].Source = _crops[i, j];
                }
            }
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            //using (var input = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
            //using (var output = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
            //using (var ds = output.CreateDrawingSession())
            //{
            //    var time = context.InputFrame.RelativeTime ?? new TimeSpan();

            //    ds.Clear(Colors.Black);

            //    for (uint i = 0; i < _numColumns; i++)
            //    {
            //        for (uint j = 0; j < _numRows; j++)
            //        {
            //            _crops[i, j].Source = input;
            //            float scale = Rescale((float)(Math.Cos(time.TotalSeconds * 2f + 0.2f * (i + j))), 0.6f, 0.95f);
            //            float rotation = (float)time.TotalSeconds * 1.5f + 0.2f * (i + j);

            //            Vector2 centerPoint = new Vector2((i + 0.5f) * PixelsPerTile, (j + 0.5f) * PixelsPerTile);

            //            _transforms[i, j].TransformMatrix =
            //                Matrix3x2.CreateRotation(rotation, centerPoint) *
            //                Matrix3x2.CreateScale(scale, centerPoint);

            //            ds.DrawImage(_transforms[i, j]);
            //        }
            //    }
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

                    ds.Clear(Colors.Black);

                    for (uint i = 0; i < _numColumns; i++)
                    {
                        for (uint j = 0; j < _numRows; j++)
                        {
                            _crops[i, j].Source = inputBitmap;
                            float scale = Rescale((float)(Math.Cos(time.TotalSeconds * 2f + 0.2f * (i + j))), 0.6f, 0.95f);
                            float rotation = (float)time.TotalSeconds * 1.5f + 0.2f * (i + j);

                            Vector2 centerPoint = new Vector2((i + 0.5f) * PixelsPerTile, (j + 0.5f) * PixelsPerTile);

                            _transforms[i, j].TransformMatrix =
                                Matrix3x2.CreateRotation(rotation, centerPoint) *
                                Matrix3x2.CreateScale(scale, centerPoint);

                            ds.DrawImage(_transforms[i, j]);
                        }
                    }
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

                        ds.Clear(Colors.Black);

                        for (uint i = 0; i < _numColumns; i++)
                        {
                            for (uint j = 0; j < _numRows; j++)
                            {
                                _crops[i, j].Source = inputBitmap;
                                float scale = Rescale((float)(Math.Cos(time.TotalSeconds * 2f + 0.2f * (i + j))), 0.6f, 0.95f);
                                float rotation = (float)time.TotalSeconds * 1.5f + 0.2f * (i + j);

                                Vector2 centerPoint = new Vector2((i + 0.5f) * PixelsPerTile, (j + 0.5f) * PixelsPerTile);

                                _transforms[i, j].TransformMatrix =
                                    Matrix3x2.CreateRotation(rotation, centerPoint) *
                                    Matrix3x2.CreateScale(scale, centerPoint);

                                ds.DrawImage(_transforms[i, j]);
                            }
                        }
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

        public bool IsReadOnly => true;
        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => Constants.SupportedEncodingProperties;
        public MediaMemoryTypes SupportedMemoryTypes => Constants.GlobalSupportedMemoryTypes;
        public bool TimeIndependent => false;
    }
}
