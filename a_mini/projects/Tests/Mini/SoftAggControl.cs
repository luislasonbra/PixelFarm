﻿//MIT 2016, WinterDev

using System;
using System.Drawing;
using System.Windows.Forms;
using PixelFarm.Agg;
using PixelFarm.Drawing.Fonts;

namespace Mini
{
    public partial class SoftAggControl : UserControl
    {
        bool isMouseDown;
        DemoBase exampleBase;
        int myWidth = 800;
        int myHeight = 600;
        WindowsFormsBitmapBackBuffer bitmapBackBuffer;
        CanvasPainter painter;
        bool _useGdiPlusOutput;
        bool _gdiAntiAlias;
        Graphics thisGfx;//for output
        Bitmap bufferBmp = null;
        Rectangle bufferBmpRect;
        public SoftAggControl()
        {
            bitmapBackBuffer = new WindowsFormsBitmapBackBuffer(Program._winGdiPlatForm);
            _useGdiPlusOutput = false;
            InitializeComponent();
            this.Load += new EventHandler(SoftAggControl_Load);
        }

        public bool UseGdiPlus
        {
            get { return _useGdiPlusOutput; }
            set { _useGdiPlusOutput = value; }
        }
        public bool UseGdiAntiAlias
        {
            get { return _gdiAntiAlias; }
            set { _gdiAntiAlias = value; }
        }

        void SoftAggControl_Load(object sender, EventArgs e)
        {
            if (_useGdiPlusOutput)
            {
                // This example assumes the existence of a form called Form1.
                // Gets a reference to the current BufferedGraphicsContext
                //BufferedGraphicsContext currentContext = BufferedGraphicsManager.Current;
                //_myBuffGfx = currentContext.Allocate(this.CreateGraphics(),
                //   this.DisplayRectangle);

                // Creates a BufferedGraphics instance associated with Form1, and with 
                // dimensions the same size as the drawing surface of Form1. 
                thisGfx = this.CreateGraphics();  //for render to output
                bufferBmpRect = this.DisplayRectangle;
                bufferBmp = new Bitmap(bufferBmpRect.Width, bufferBmpRect.Height);
                var p = new PixelFarm.Drawing.WinGdi.GdiPlusCanvasPainter(Program._winGdiPlatForm, bufferBmp);
                p.SmoothingMode = _gdiAntiAlias ? PixelFarm.Drawing.SmoothingMode.AntiAlias : PixelFarm.Drawing.SmoothingMode.HighSpeed;

                painter = p;

            }
            else
            {
                painter = new AggCanvasPainter(bitmapBackBuffer.Initialize(myWidth, myHeight, 32));
            }

            painter.CurrentFont = new PixelFarm.Drawing.Font("tahoma", 10);
            painter.Clear(PixelFarm.Drawing.Color.White);
        }

        public void LoadExample(DemoBase exBase)
        {
            this.exampleBase = exBase;
            //exBase.RequestNewGfx2d += () => this.bitmapBackBuffer.CreateNewGraphic2D();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.isMouseDown = true;
            exampleBase.MouseDown(e.X, myHeight - e.Y, e.Button == System.Windows.Forms.MouseButtons.Right);
            base.OnMouseDown(e);
            if (!_useGdiPlusOutput)
            {
                Invalidate();
            }
            else
            {
                UpdateOutput();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            this.isMouseDown = false;
            exampleBase.MouseUp(e.X, myHeight - e.Y);
            base.OnMouseUp(e);
            if (!_useGdiPlusOutput)
            {
                Invalidate();
            }
            else
            {
                UpdateOutput();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (this.isMouseDown)
            {
                exampleBase.MouseDrag(e.X, myHeight - e.Y);
                if (!_useGdiPlusOutput)
                {
                    Invalidate();
                }
                else
                {
                    UpdateOutput();
                }
            }
            base.OnMouseMove(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.exampleBase == null)
            {
                base.OnPaint(e);
                return;
            }
            if (!_useGdiPlusOutput)
            {
                exampleBase.Draw(painter);
                bitmapBackBuffer.UpdateToHardwareSurface(e.Graphics);
            }
            else
            {
                UpdateOutput();
            }
            base.OnPaint(e);
        }
        void UpdateOutput()
        {
            exampleBase.Draw(painter);
            if (_useGdiPlusOutput)
            {
                //_myBuffGfx.Render();
                thisGfx.DrawImageUnscaledAndClipped(bufferBmp, bufferBmpRect);
            }
        }
    }
}
