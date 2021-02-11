using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using VideoEffects.Helpers;

namespace VideoEffects.Win2D
{
    public sealed class ChromaKeyVideoEffect : IBasicVideoEffect
    {
        private VideoEncodingProperties _currentEncodingProperties;
        private CanvasDevice _canvasDevice;
        private CanvasBitmap _backgroundBitmap;
        private IPropertySet _configuration;
        private string _lastUsedImageFilePath;
        private string _imageFileName;

        public float Tolerance
        {
            get
            {
                if (_configuration != null && _configuration.TryGetValue(nameof(Tolerance), out _))
                {
                    return (float)_configuration[nameof(Tolerance)];
                }

                return 0.1f;
            }
        }

        public Color ChromaColor
        {
            get
            {
                if (_configuration != null && _configuration.TryGetValue(nameof(ChromaColor), out _))
                {
                    return (Color)_configuration[nameof(ChromaColor)];
                }

                return Color.FromArgb(255, 0, 177, 64); //TODO double check with Pantone
            }
        }

        public string ImageFileName
        {
            get
            {
                if (_configuration != null && _configuration.TryGetValue(nameof(ImageFileName), out _))
                {
                    _imageFileName = (string)_configuration[nameof(ImageFileName)];
                    return _imageFileName;
                }

                return "ms-appx:///images/FreshSnow.png";
            }
        }

        public void SetProperties(IPropertySet configuration)
        {
            _configuration = configuration;
        }

        public async void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _currentEncodingProperties = encodingProperties;
            _canvasDevice = device != null ? CanvasDevice.CreateFromDirect3D11Device(device) : CanvasDevice.GetSharedDevice();

            _backgroundBitmap = await CanvasBitmap.LoadAsync(_canvasDevice, new Uri($"ms-appdata:///Local/{ImageFileName}"));
        }

        public async void ProcessFrame(ProcessVideoFrameContext context)
        {
            // When using SupportedMemoryTypes => MediaMemoryTypes.GpuAndCpu we need to check if we're using GPU or CPU for the frame

            // If we're on GPU, use InputFrame.Direct3DSurface
            if (context.InputFrame.SoftwareBitmap == null)
            {
                using (var inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (var renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var chromaKeyEffect = new ChromaKeyEffect
                    {
                        Color = ChromaColor,
                        Source = inputBitmap,
                        Tolerance = Tolerance,
                        Feather = true
                    };

                    //only load bg image if it hasnt already been loaded or if image is different
                    if (_backgroundBitmap == null || _lastUsedImageFilePath != _imageFileName)
                    {
                        _backgroundBitmap = await CanvasBitmap.LoadAsync(ds, new Uri($"ms-appdata:///Local/{ImageFileName}"));
                    }

                    _lastUsedImageFilePath = _imageFileName; //keep track of any incoming image changes

                    var compositeEffect = new CompositeEffect
                    {
                        Sources = { _backgroundBitmap, chromaKeyEffect }
                    };

                    ds.DrawImage(compositeEffect);
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
                    CanvasAlphaMode.Ignore))
                {
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        var chroma = new ChromaKeyEffect
                        {
                            Color = ChromaColor,
                            Source = inputBitmap,
                            Tolerance = Tolerance
                        };

                        //only load bg image if it hasnt already been loaded or if image is different
                        if (_backgroundBitmap == null || _lastUsedImageFilePath != _imageFileName)
                            _backgroundBitmap = await CanvasBitmap.LoadAsync(ds, new Uri($"ms-appdata:///Local/{ImageFileName}"));

                        //keep track of any incoming image changes
                        _lastUsedImageFilePath = _imageFileName;

                        var compositeEffect = new CompositeEffect { Mode = CanvasComposite.Add };
                        compositeEffect.Sources.Add(_backgroundBitmap);
                        compositeEffect.Sources.Add(chroma);

                        ds.DrawImage(compositeEffect);

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

        // official demo's effect properties with tiger on top of checker backaground
        //private ICanvasImage CreateChromaKey()
        //{
        //    var chromaKeyEffect = new ChromaKeyEffect
        //    {
        //        Source = bitmapTiger,
        //        Color = Color.FromArgb(255, 162, 125, 73),
        //        Feather = true
        //    };

        //    // Composite the chromakeyed image on top of a background checker pattern.
        //    var compositeEffect = new CompositeEffect
        //    {
        //        Sources = { CreateCheckeredBackground(), chromaKeyEffect }
        //    };

        //    // Animation changes the chromakey matching tolerance.
        //    animationFunction = elapsedTime =>
        //    {
        //        chromaKeyEffect.Tolerance = 0.1f + (float)Math.Sin(elapsedTime) * 0.1f;
        //    };

        //    return compositeEffect;
        //}
    }
}
