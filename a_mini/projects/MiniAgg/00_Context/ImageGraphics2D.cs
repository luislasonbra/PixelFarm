//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.Transform;
using MatterHackers.VectorMath;

namespace MatterHackers.Agg
{
    class ImageGraphics2D : Graphics2D
    {

        Scanline scanline;
        PathStorage drawImageRectPath = new PathStorage();
        ScanlinePacked8 drawImageScanlineCache = new ScanlinePacked8();
        ScanlineRenderer scanlineRenderer = new ScanlineRenderer();

        public ImageGraphics2D(IImage destImage,
            ScanlineRasterizer rasterizer,
            ScanlinePacked8 scanline)
            : base(destImage, rasterizer)
        {
            this.scanline = scanline;
        }



        public override void SetClippingRect(RectangleDouble clippingRect)
        {
            Rasterizer.SetVectorClipBox(clippingRect);
        }

        public override RectangleDouble GetClippingRect()
        {
            return Rasterizer.GetVectorClipBox();
        }

        public override void Render(VertexStoreSnap vertextSnap, ColorRGBA color)
        {
            rasterizer.Reset();
            Affine transform = GetTransform();
            if (!transform.IsIdentity())
            {   
                var s1 = new VertexStoreSnap(transform.Tranform(vertextSnap));
                rasterizer.AddPath(s1);
            }
            else
            {
                rasterizer.AddPath(vertextSnap);
            }

            if (destImageByte != null)
            {
                scanlineRenderer.RenderScanlineSolidAA(destImageByte, rasterizer, scanline, color);
                DestImage.MarkImageChanged();
            }
            else
            {
                //scanlineRenderer.RenderSolid(destImageFloat, rasterizer, m_ScanlineCache, colorBytes.GetAsRGBA_Floats());
                //destImageFloat.MarkImageChanged();
            }
        }


        void DrawImageGetDestBounds(IImage sourceImage,
            double destX, double destY,
            double hotspotOffsetX, double hotSpotOffsetY,
            double scaleX, double scaleY,
            double angleRad, out Affine destRectTransform)
        {

            AffinePlan[] plan = new AffinePlan[4];
            int i = 0;
            if (hotspotOffsetX != 0.0f || hotSpotOffsetY != 0.0f)
            {
                plan[i] = AffinePlan.Translate(-hotspotOffsetX, -hotSpotOffsetY);
                i++;
            }

            if (scaleX != 1 || scaleY != 1)
            {

                plan[i] = AffinePlan.Scale(scaleX, scaleY);
                i++;
            }

            if (angleRad != 0)
            {

                plan[i] = AffinePlan.Rotate(angleRad);
                i++;
            }

            if (destX != 0 || destY != 0)
            {
                plan[i] = AffinePlan.Translate(destX, destY);
                i++;
            }

            destRectTransform = Affine.NewMatix(plan);

            int srcBuffWidth = sourceImage.Width;
            int srcBuffHeight = sourceImage.Height;

            drawImageRectPath.Clear();

            drawImageRectPath.MoveTo(0, 0);
            drawImageRectPath.LineTo(srcBuffWidth, 0);
            drawImageRectPath.LineTo(srcBuffWidth, srcBuffHeight);
            drawImageRectPath.LineTo(0, srcBuffHeight);
            drawImageRectPath.ClosePolygon();
        }

        void DrawImage(IImage sourceImage, ISpanGenerator spanImageFilter, Affine destRectTransform)
        {
            if (destImageByte.OriginOffset.x != 0 || destImageByte.OriginOffset.y != 0)
            {
                destRectTransform *= Affine.NewTranslation(-destImageByte.OriginOffset.x, -destImageByte.OriginOffset.y);
            }

            var sp1 = destRectTransform.TransformToVertexSnap(drawImageRectPath);
            Rasterizer.AddPath(sp1);
            {


                scanlineRenderer.GenerateAndRender(
                    new ChildImage(destImageByte, destImageByte.GetRecieveBlender()),
                    Rasterizer,
                    drawImageScanlineCache,
                    spanImageFilter);
            }
        }

        public override void Render(IImage source,
            double destX, double destY,
            double angleRadians,
            double inScaleX, double inScaleY)
        {
            {   // exit early if the dest and source bounds don't touch.
                // TODO: <BUG> make this do rotation and scalling
                RectangleInt sourceBounds = source.GetBounds();
                RectangleInt destBounds = this.destImageByte.GetBounds();
                sourceBounds.Offset((int)destX, (int)destY);

                if (!RectangleInt.DoIntersect(sourceBounds, destBounds))
                {
                    if (inScaleX != 1 || inScaleY != 1 || angleRadians != 0)
                    {
                        throw new NotImplementedException();
                    }
                    return;
                }
            }

            double scaleX = inScaleX;
            double scaleY = inScaleY;

            Affine graphicsTransform = GetTransform();
            if (!graphicsTransform.IsIdentity())
            {
                if (scaleX != 1 || scaleY != 1 || angleRadians != 0)
                {
                    throw new NotImplementedException();
                }
                graphicsTransform.Transform(ref destX, ref destY);
            }

#if false // this is an optomization that eliminates the drawing of images that have their alpha set to all 0 (happens with generated images like explosions).
	        MaxAlphaFrameProperty maxAlphaFrameProperty = MaxAlphaFrameProperty::GetMaxAlphaFrameProperty(source);

	        if((maxAlphaFrameProperty.GetMaxAlpha() * color.A_Byte) / 256 <= ALPHA_CHANNEL_BITS_DIVISOR)
	        {
		        m_OutFinalBlitBounds.SetRect(0,0,0,0);
	        }
#endif
            bool isScale = (scaleX != 1 || scaleY != 1);

            bool isRotated = true;
            if (Math.Abs(angleRadians) < (0.1 * MathHelper.Tau / 360))
            {
                isRotated = false;
                angleRadians = 0;
            }

            //bool IsMipped = false;
            double sourceOriginOffsetX = source.OriginOffset.x;
            double sourceOriginOffsetY = source.OriginOffset.y;
            bool canUseMipMaps = isScale;
            if (scaleX > 0.5 || scaleY > 0.5)
            {
                canUseMipMaps = false;
            }

            bool renderRequriesSourceSampling = isScale || isRotated || destX != (int)destX || destY != (int)destY;

            // this is the fast drawing path
            if (renderRequriesSourceSampling)
            {
#if false // if the scalling is small enough the results can be improved by using mip maps
	        if(CanUseMipMaps)
	        {
		        CMipMapFrameProperty* pMipMapFrameProperty = CMipMapFrameProperty::GetMipMapFrameProperty(source);
		        double OldScaleX = scaleX;
		        double OldScaleY = scaleY;
		        const CFrameInterface* pMippedFrame = pMipMapFrameProperty.GetMipMapFrame(ref scaleX, ref scaleY);
		        if(pMippedFrame != source)
		        {
			        IsMipped = true;
			        source = pMippedFrame;
			        sourceOriginOffsetX *= (OldScaleX / scaleX);
			        sourceOriginOffsetY *= (OldScaleY / scaleY);
		        }

			    HotspotOffsetX *= (inScaleX / scaleX);
			    HotspotOffsetY *= (inScaleY / scaleY);
	        }
#endif
                Affine destRectTransform;
                DrawImageGetDestBounds(source, destX, destY, sourceOriginOffsetX, sourceOriginOffsetY, scaleX, scaleY, angleRadians, out destRectTransform);

                Affine sourceRectTransform = destRectTransform.CreateInvert();
                // We invert it because it is the transform to make the image go to the same position as the polygon. LBB [2/24/2004]


                SpanImageFilter spanImageFilter;
                var interpolator = new MatterHackers.Agg.Lines.InterpolatorLinear(sourceRectTransform);
                ImageBufferAccessorClip sourceAccessor = new ImageBufferAccessorClip(source, ColorRGBAf.rgba_pre(0, 0, 0, 0).ToColorRGBA());

                spanImageFilter = new SpanImageFilterRGBA_BilinearClip(sourceAccessor, ColorRGBAf.rgba_pre(0, 0, 0, 0).ToColorRGBA(), interpolator);

                DrawImage(source, spanImageFilter, destRectTransform);
#if false // this is some debug you can enable to visualize the dest bounding box
		        LineFloat(BoundingRect.left, BoundingRect.top, BoundingRect.right, BoundingRect.top, WHITE);
		        LineFloat(BoundingRect.right, BoundingRect.top, BoundingRect.right, BoundingRect.bottom, WHITE);
		        LineFloat(BoundingRect.right, BoundingRect.bottom, BoundingRect.left, BoundingRect.bottom, WHITE);
		        LineFloat(BoundingRect.left, BoundingRect.bottom, BoundingRect.left, BoundingRect.top, WHITE);
#endif
            }
            else // TODO: this can be even faster if we do not use an intermediat buffer
            {
                Affine destRectTransform;
                DrawImageGetDestBounds(source, destX, destY, sourceOriginOffsetX, sourceOriginOffsetY, scaleX, scaleY, angleRadians, out destRectTransform);

                Affine sourceRectTransform = destRectTransform.CreateInvert();
                // We invert it because it is the transform to make the image go to the same position as the polygon. LBB [2/24/2004]


                var interpolator = new MatterHackers.Agg.Lines.InterpolatorLinear(sourceRectTransform);
                ImageBufferAccessorClip sourceAccessor = new ImageBufferAccessorClip(source, ColorRGBAf.rgba_pre(0, 0, 0, 0).ToColorRGBA());

                SpanImageFilter spanImageFilter = null;
                switch (source.BitDepth)
                {
                    case 32:
                        spanImageFilter = new SpanImageFilterRGBA_NN_StepXBy1(sourceAccessor, interpolator);
                        break;

                    case 24:
                        spanImageFilter = new SpanImageFilterRBG_NNStepXby1(sourceAccessor, interpolator);
                        break;

                    case 8:
                        spanImageFilter = new SpanImageFilterGray_NNStepXby1(sourceAccessor, interpolator);
                        break;

                    default:
                        throw new NotImplementedException();
                }
                //spanImageFilter = new span_image_filter_rgba_nn(sourceAccessor, interpolator);

                DrawImage(source, spanImageFilter, destRectTransform);
                DestImage.MarkImageChanged();
            }
        }

        //public override void Render(IImageFloat source,
        //    double x, double y,
        //    double angleDegrees,
        //    double inScaleX, double inScaleY)
        //{
        //    throw new NotImplementedException();
        //}

        public override void Clear(ColorRGBA color)
        {
            RectangleDouble clippingRect = GetClippingRect();
            RectangleInt clippingRectInt = new RectangleInt((int)clippingRect.Left, (int)clippingRect.Bottom, (int)clippingRect.Right, (int)clippingRect.Top);

            IImage destImage = this.DestImage;

            if (destImage != null)
            {

                int width = destImage.Width;
                int height = destImage.Height;
                byte[] buffer = destImage.GetBuffer();
                switch (destImage.BitDepth)
                {
                    case 8:
                        {
                            int bytesBetweenPixels = destImage.GetBytesBetweenPixelsInclusive();
                            byte byteColor = (byte)color.Red0To255;
                            int clipRectLeft = clippingRectInt.Left;
                            for (int y = clippingRectInt.Bottom; y < clippingRectInt.Top; ++y)
                            {
                                int bufferOffset = destImage.GetBufferOffsetXY(clipRectLeft, y);
                                for (int x = 0; x < clippingRectInt.Width; ++x)
                                {
                                    buffer[bufferOffset] = color.blue;
                                    bufferOffset += bytesBetweenPixels;
                                }
                            }
                        }
                        break;

                    case 24:
                        {
                            int bytesBetweenPixels = destImage.GetBytesBetweenPixelsInclusive();
                            int clipRectLeft = clippingRectInt.Left;
                            for (int y = clippingRectInt.Bottom; y < clippingRectInt.Top; y++)
                            {
                                int bufferOffset = destImage.GetBufferOffsetXY(clipRectLeft, y);
                                for (int x = 0; x < clippingRectInt.Width; ++x)
                                {
                                    buffer[bufferOffset + 0] = color.blue;
                                    buffer[bufferOffset + 1] = color.green;
                                    buffer[bufferOffset + 2] = color.red;
                                    bufferOffset += bytesBetweenPixels;
                                }
                            }
                        }
                        break;
                    case 32:
                        {

                            int bytesBetweenPixels = destImage.GetBytesBetweenPixelsInclusive();
                            int clipRectLeft = clippingRectInt.Left;
                            for (int y = clippingRectInt.Bottom; y < clippingRectInt.Top; ++y)
                            {
                                int bufferOffset = destImage.GetBufferOffsetXY(clipRectLeft, y);
                                for (int x = 0; x < clippingRectInt.Width; ++x)
                                {
                                    buffer[bufferOffset + 0] = color.blue;
                                    buffer[bufferOffset + 1] = color.green;
                                    buffer[bufferOffset + 2] = color.red;
                                    buffer[bufferOffset + 3] = color.alpha;
                                    bufferOffset += bytesBetweenPixels;
                                }
                            }
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

        }
    }
}
