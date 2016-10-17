using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SharpGL;
using SharpGL.VertexBuffers;
using SharpGL.SceneGraph.Assets;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace PSMViewer.Visualizations
{
    
    public class glGauge : glBase
    {

        
        public uint StartAngle { get; set; } = 0; 

        public uint EndAngle { get; set; } = 360;

        public float LineWidth { get; set; } = 1; 

        public bool Fill { get; set; } = false;

        public double InnerRadius { get; set; } = .8;

        public double OuterRadius { get; set; } = 1;

        public PropertyDefinition[] Properties
        {
            get
            {

                return new PropertyDefinition[]
                {
                   new PropertyDefinition()
                   {
                    Category = "Gauge",
                    TargetProperties = new List<object>(new string[] { "StartAngle", "EndAngle", "LineWidth", "Fill", "InnerRadius", "OuterRadius" })
                    }

                };

            }
            
        }        

        private VertexBufferArray _vertexbufferArray = new VertexBufferArray();

        private VertexBuffer _buffer = new VertexBuffer();


        private uint _program;

        private int _count = 0;

        private double _value = 0;                

        private Texture _texture = new Texture();

        
        /// <summary>
        /// Constructor
        /// </summary>
        public glGauge() : base(SharpGL.Version.OpenGLVersion.OpenGL1_5) { }
                
        protected override void Draw(bool auto = false)
        {
            
            if (auto) return;

            GL.UseProgram(_program);
            GL.Uniform1(GL.GetUniformLocation(_program, "value"), (float)(_value));
            GL.Clear(OpenGL.GL_COLOR_BUFFER_BIT);
            GL.DrawArrays(Fill ? OpenGL.GL_TRIANGLE_STRIP : OpenGL.GL_LINES, 0, _count);
            GL.UseProgram(0);
            GL.DrawText((int)ActualWidth / 2, (int)ActualHeight / 2, _foreground[0], _foreground[1], _foreground[2], FontFamily.ToString(), (float)FontSize, _value.ToString());
            
            base.Draw(auto);

        }

        protected override void Initialize()
        {

            base.Initialize();

            Assembly assembly = Assembly.GetExecutingAssembly();

            using (StreamReader fs = new StreamReader(assembly.GetManifestResourceStream("PSMViewer.Shaders.gauge.fs")))
            {
                using (StreamReader vs = new StreamReader(assembly.GetManifestResourceStream("PSMViewer.Shaders.gauge.vs")))
                {
                    _program = Add(vs.ReadToEnd(), fs.ReadToEnd());
                }
            }

            GL.ActiveTexture(0);

            _texture.Create(GL);
            _texture.Bind(GL);

            GL.Uniform1(GL.GetUniformLocation(_program, "colors"), 0);
            
            GL.TexParameterI(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, new int[] { (int)OpenGL.GL_LINEAR});
            GL.TexParameterI(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, new int[] { (int)OpenGL.GL_LINEAR });
            GL.TexParameterI(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, new int[] { (int)OpenGL.GL_CLAMP_TO_EDGE });
            GL.TexParameterI(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, new int[] { (int)OpenGL.GL_CLAMP_TO_EDGE });

            _vertexbufferArray.Create(GL);
            _vertexbufferArray.Bind(GL);

            _buffer.Create(GL);
            _buffer.Bind(GL);

            GL.BindAttribLocation(_program, 0, "position");
            
        }

        public override void Update(object value)
        {
            _value = (double)value;
            Draw();
        }

        public override void Refresh()
        {
            
            List<float> vertices = new List<float>();

            const uint d = 256;

            double start = (StartAngle * (Math.PI / 180));
            double end = (EndAngle * (Math.PI / 180));
            double a;
            double angle = end / d;
            double scale_w, scale_h;
            
            base.Refresh();
            
            if (!_initialized || ActualHeight == 0 || ActualWidth == 0)
                return;

            GL.UseProgram(_program);

            scale_w = Math.Min(1, ActualHeight / ActualWidth);
            scale_h = Math.Min(1, ActualWidth / ActualHeight);            
            
            for (double i = 0; i < d; i++)
            {

                a = (end - start) + (angle * i);

                vertices.AddRange(new float[]
                {
                    (float)(Math.Cos(a) * OuterRadius * scale_w),
                    (float)(Math.Sin(a) * OuterRadius * scale_h),
                    (float)(angle * i),
                    (float)(Math.Cos(a) * InnerRadius * scale_w),
                    (float)(Math.Sin(a) * InnerRadius * scale_h),
                    (float)(angle * i)
                });
            }

            _buffer.Bind(GL);
            _buffer.SetData(GL, 0, vertices.ToArray(), false, 3);

            _count = vertices.Count / 3;

            GL.EnableVertexAttribArray(0);
            GL.LineWidth(LineWidth);

            GL.Uniform1(GL.GetUniformLocation(_program, "startAngle"), (float)start);
            GL.Uniform1(GL.GetUniformLocation(_program, "endAngle"), (float)end);

            GL.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA, 3, 1, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, new byte[] { 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255 });

            Draw();
            
        }
    }
 }
