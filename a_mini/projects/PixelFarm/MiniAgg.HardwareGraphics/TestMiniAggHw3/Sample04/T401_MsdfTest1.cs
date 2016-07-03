﻿//MIT, 2014-2016,WinterDev

using System;
using Mini;
using PixelFarm.DrawingGL;
namespace OpenTkEssTest
{
    [Info(OrderCode = "401")]
    [Info("T401_MsdfTest1")]
    public class T401_MsdfTest1 : PrebuiltGLControlDemoBase
    {
        CanvasGL2d canvas2d;
        bool resInit;
        GLBitmap msdf_bmp;
        GLBitmap sdf_bmp;
        protected override void OnInitGLProgram(object sender, EventArgs args)
        {
            int max = Math.Max(this.Width, this.Height);
            canvas2d = new CanvasGL2d(max, max);
        }
        protected override void DemoClosing()
        {
            canvas2d.Dispose();
        }
        protected override void OnGLRender(object sender, EventArgs args)
        {
            canvas2d.SmoothMode = CanvasSmoothMode.Smooth;
            canvas2d.StrokeColor = PixelFarm.Drawing.Color.Blue;
            canvas2d.ClearColorBuffer();
            if (!resInit)
            {
                msdf_bmp = LoadTexture(@"..\msdf_65.png");
                sdf_bmp = LoadTexture(@"..\sdf_65.png");
                resInit = true;
            }
            canvas2d.DrawImageWithMsdf(msdf_bmp, 0, 400, 6);
            canvas2d.DrawImageWithMsdf(msdf_bmp, 100, 500, 0.5f);
            canvas2d.DrawImageWithMsdf(msdf_bmp, 100, 520, 0.4f);
            canvas2d.DrawImageWithMsdf(msdf_bmp, 100, 550, 0.3f);
            canvas2d.DrawImage(msdf_bmp, 150, 400);

            //
            canvas2d.DrawImageWithMsdf(sdf_bmp, 200, 400, 6);
            canvas2d.DrawImageWithMsdf(sdf_bmp, 200, 500, 0.5f);
            canvas2d.DrawImageWithMsdf(sdf_bmp, 200, 520, 0.4f);
            canvas2d.DrawImageWithMsdf(sdf_bmp, 200, 550, 0.3f);
            canvas2d.DrawImage(sdf_bmp, 400, 300);

            miniGLControl.SwapBuffers();
        }
    }
}
