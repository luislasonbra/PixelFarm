﻿//MIT 2016, WinterDev
//we use concept from https://www.mapbox.com/blog/drawing-antialiased-lines/

using System;
using OpenTK;
using OpenTK.Graphics.ES20;
using Mini;
namespace OpenTkEssTest
{
    [Info(OrderCode = "055")]
    [Info("T55_Lines")]
    public class T55_Lines2 : PrebuiltGLControlDemoBase
    {
        MiniShaderProgram shaderProgram = new MiniShaderProgram();
        ShaderVtxAttrib a_position;
        ShaderVtxAttrib a_normal;
        ShaderVtxAttrib a_color;
        ShaderUniformMatrix4 u_matrix;
        ShaderUniformVar1 u_useSolidColor;
        ShaderUniformVar4 u_solidColor;
        ShaderUniformVar1 u_linewidth;
        MyMat4 orthoView;
        protected override void OnInitGLProgram(object sender, EventArgs args)
        {
            //----------------
            //vertex shader source
            string vs = @"        
           
            attribute vec3 a_position;
            attribute vec2 a_normal;
            attribute vec4 a_color;            

            uniform mat4 u_mvpMatrix;
            uniform vec4 u_solidColor;
            uniform int u_useSolidColor;              
            uniform float u_linewidth;

            varying vec4 v_color;
            varying vec3 v_distance;
            void main()
            {   
                vec4 delta = vec4(a_normal * u_linewidth, 0,1); 
                vec4 pos = vec4(a_position[0],a_position[1],0,0) + delta; 

                gl_Position = u_mvpMatrix* pos;
                v_distance= a_position;

                if(u_useSolidColor !=0)
                {
                    v_color= u_solidColor;
                }
                else
                {
                    v_color = a_color;
                }
            }
            ";
            //fragment source
            string fs = @"
                precision mediump float;
                varying vec4 v_color; 
                varying vec3 v_distance;
                void main()
                {
                    float d0= v_distance[2];
                    float p0= 0.1;
                    float p1= 1.0-p0;
                    float factor= 1.0 /p0;
            
                    if(d0 < p0){                        
                        gl_FragColor =vec4(v_color[0],v_color[1],v_color[2], v_color[3] *(d0 * factor));
                    }else if(d0> p1){                         
                        gl_FragColor =vec4(v_color[0],v_color[1],v_color[2], v_color[3] *((1.0-d0)* factor));
                    }
                    else{
                        gl_FragColor =v_color;
                    } 
                }
            ";
            if (!shaderProgram.Build(vs, fs))
            {
                throw new NotSupportedException();
            }


            a_position = shaderProgram.GetVtxAttrib("a_position");
            a_normal = shaderProgram.GetVtxAttrib("a_normal");
            a_color = shaderProgram.GetVtxAttrib("a_color");
            u_matrix = shaderProgram.GetUniformMat4("u_mvpMatrix");
            u_useSolidColor = shaderProgram.GetUniform1("u_useSolidColor");
            u_solidColor = shaderProgram.GetUniform4("u_solidColor");
            u_linewidth = shaderProgram.GetUniform1("u_linewidth");
            //--------------------------------------------------------------------------------
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.ClearColor(1, 1, 1, 1);
            //setup viewport size
            int max = Math.Max(this.Width, this.Height);
            //square viewport
            GL.Viewport(0, 0, max, max);
            orthoView = MyMat4.ortho(0, max, 0, max, 0, 1);
            //--------------------------------------------------------------------------------

            //load image


        }
        protected override void DemoClosing()
        {
            shaderProgram.DeleteMe();
        }
        protected override void OnGLRender(object sender, EventArgs args)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            shaderProgram.UseProgram();
            //---------------------------------------------------------  
            u_matrix.SetData(orthoView.data);
            //---------------------------------------------------------  
            //DrawLines(50, 0, 50, 150);
            DrawLines(50, 50, 300, 200);
            //--------------------------------------------------------- 
            miniGLControl.SwapBuffers();
        }
        void DrawLines(float x1, float y1, float x2, float y2)
        {
            //find normal
            float dx = x2 - x1;
            float dy = y2 - y1;
            Vector2 n1 = new Vector2(-dy, dx);
            Vector2 n2 = new Vector2(dy, -dx);
            n1.Normalize();
            n2.Normalize();
            float[] vtxs = new float[] {
                x1, y1,0,
                x1, y1,1,
                x2, y2,1,
                //-------
                x2,y2,1,
                x2,y2,0,
                x1,y1,0,
            };
            float[] normals = new float[] { n1.X,n1.Y,
               n2.X,n2.Y,
               n2.X,n2.Y,
               //---------
               n2.X,n2.Y,
               n1.X,n1.Y,
               n1.X,n1.Y,
            };
            u_useSolidColor.SetValue(1);
            u_solidColor.SetValue(0f, 0f, 0f, 1f);//use solid color 
            a_position.LoadV3f(vtxs, 3, 0);
            a_normal.LoadV2f(normals, 2, 0);
            u_linewidth.SetValue(5f);
            GL.DrawArrays(BeginMode.Triangles, 0, 6);
        }
    }
}
