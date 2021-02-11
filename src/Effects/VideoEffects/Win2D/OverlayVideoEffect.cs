using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Microsoft.Graphics.Canvas;
using VideoEffects.Helpers;

namespace VideoEffects.Win2D
{
    public sealed class OverlayVideoEffect : IBasicVideoEffect
    {
        private int crashCount = 0;
        private VideoEncodingProperties _currentEncodingProperties;
        private CanvasDevice _canvasDevice;
        private IPropertySet _configuration;
        private string _imageFileName;
        private CanvasBitmap _overlay;

        public string ImageFileName
        {
            get
            {
                object val;
                if (_configuration != null && _configuration.TryGetValue("ImageFileName", out val))
                {
                    _imageFileName = (string)_configuration["ImageFileName"];
                    return _imageFileName;
                }

                return "images/BlendEffectImages/Overlay.png";
            }
        }

        public void SetProperties(IPropertySet configuration)
        {
            this._configuration = configuration;
        }

        public async void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            this._currentEncodingProperties = encodingProperties;
            _canvasDevice = device != null ? CanvasDevice.CreateFromDirect3D11Device(device) : CanvasDevice.GetSharedDevice();
            
            this._overlay = await CanvasBitmap.LoadAsync(this._canvasDevice, new Uri($"ms-appdata:///Local/{ImageFileName}"));
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            try
            {
                // When using SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu we need to check if we're using GPU or CPU for the frame

                // If we're on GPU, use InputFrame.Direct3DSurface
                if (context.InputFrame.SoftwareBitmap == null)
                {
                    using (var inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                    using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        ds.DrawImage(inputBitmap);
                        ds.DrawImage(this._overlay, inputBitmap.Bounds);
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
                        (float)context.OutputFrame.SoftwareBitmap.DpiX,
                        context.OutputFrame.SoftwareBitmap.BitmapPixelFormat.ToDirectXPixelFormat(),
                        CanvasAlphaMode.Premultiplied))
                    {
                        using (var ds = renderTarget.CreateDrawingSession())
                        {
                            ds.DrawImage(inputBitmap);
                            ds.DrawImage(this._overlay, inputBitmap.Bounds);

                        }
                    }
                }
            }
            catch (Exception)
            {
                if (crashCount < 20)
                {
                    crashCount++;
                    Debug.WriteLine($"ProcessFrame Exception: #{crashCount}");
                }
                else
                {
                    //System.Exception HResult = 0x88990012
                    //Message = Objects used together must be created from the same factory instance. (Exception from HRESULT: 0x88990012)
                    //Source = System.Private.CoreLib
                    //StackTrace:
                    //at System.Runtime.InteropServices.WindowsRuntime.IClosable.Close()
                    //at System.Runtime.InteropServices.WindowsRuntime.IClosableToIDisposableAdapter.Dispose()
                    //at VideoEffects.Win2D.OverlayVideoEffect.ProcessFrame(ProcessVideoFrameContext context) in D:\GitHub\VideoDiary\src\VideoDiary.EffectsLibrary\Win2dEffects\OverlayVideoEffect.cs:line 66
                     
                    throw;
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
