/// <copyright file="glBase.cs" company="Baker Hughes Incorporated">
/// Copyright (c) 2015 All Rights Reserved
/// </copyright>
/// <author>Odd Marthon Lende</author>
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpGL;
using SharpGL.WPF;
using SharpGL.Shaders;
using SharpGL.Enumerations;
using SharpGL.Version;
using System.Windows.Media;
using PSMViewer.ViewModels;
using System.Windows;
using System.Windows.Controls;
using PSMViewer.Models;
using System.Collections.ObjectModel;
using PSMonitor.Stores;
using PSMonitor;

namespace PSMViewer.Visualizations
{

    /// <summary>
    /// Base class for SharpGL based visualizations
    /// </summary>
    [Visible(false)]
    public class glBase : UserControl, IReplicator
    {
        
        protected OpenGL GL { get; private set; }

        protected List<uint> Programs { get; private set; } = new List<uint>();

        protected bool _initialized = false;
        
        protected OpenGLVersion _version;

        protected float[] _foreground = new float[4];

        protected float[] _background = new float[4];

        private OpenGLControl _control;
        
        public glBase(OpenGLVersion version) : base()
        {

            Content = _control = new OpenGLControl() { RenderContextType = RenderContextType.FBO };
            Background = System.Windows.Media.Brushes.Transparent;
            GL = _control.OpenGL;

            _control.OpenGLInitialized += GlBase_OpenGLInitialized;
            _control.OpenGLDraw += _control_OpenGLDraw;
            _version = version;

            DataContextChanged += GlBase_DataContextChanged;

        }

        private void GlBase_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            
            if(e.NewValue is MultiControl) {
                
               ((MultiControl)e.NewValue).DataChanged += GlBase_DataChanged;
            }

            if (e.OldValue is MultiControl)
            {
                ((MultiControl)e.OldValue).DataChanged -= GlBase_DataChanged;
            }
        }

        protected virtual void GlBase_DataChanged(object sender)
        {

            MultiControl ctrl = (MultiControl)DataContext;
            ObservableCollection<EntryItem> data = ctrl.Entries;

            switch(data.Count)
            {

                case 1:

                    Update(ctrl.Key.Units.Convert<double>((Entry)data[0]));
                    break;

                case 0:

                    break;

                default:

                    Update(data.Average((e) => {
                        return ctrl.Key.Units.Convert<double>((Entry)e);
                    }));
                    break;
            }

        }

        private void _control_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            Draw(true);            
        }

        private void GlBase_OpenGLInitialized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {            

            Initialize();
            Refresh();
        }

        public virtual void Update(object value)
        {
            throw new NotImplementedException();
        }                

        /// <summary>
        /// Drawing should occur in this method
        /// </summary>
        /// <param name="auto"><c>True</c> if triggered from the <see cref="OpenGLControl.OpenGLDraw"/> event </param>
        protected virtual void Draw(bool auto = false)
        {
            GL.Flush();
        }

        protected virtual void Initialize()
        {

            if (!GL.Create(_version, RenderContextType.FBO, (int)Math.Max(ActualWidth, 1), (int)Math.Max(ActualHeight, 1), 32, null ))
            {
                
                uint code = GL.GetError();
                string desc = GL.GetErrorDescription(code);

                throw new Exception(desc);              

            }
            
            _initialized = true;
        }

        protected uint Add(string vs, string fs)
        {
            
            uint prog = GL.CreateProgram();
            uint vertex = GL.CreateShader(OpenGL.GL_VERTEX_SHADER);
            uint frag   = GL.CreateShader(OpenGL.GL_FRAGMENT_SHADER);

            StringBuilder log = new StringBuilder(2048);         
                
            GL.ShaderSource(frag, fs);
            GL.ShaderSource(vertex, vs);

            foreach (uint s in new uint[] {vertex, frag})
            {
                GL.CompileShader(s);
                GL.GetShaderInfoLog(s, 2048, IntPtr.Zero, log);

                if (log.Length > 0)
                    throw new Exception(log.ToString());
                
                GL.AttachShader(prog, s);
            }

            log.Clear();
            
            GL.LinkProgram(prog);
            GL.GetProgramInfoLog(prog, 2048, IntPtr.Zero, log);

            if (log.Length > 0)
                throw new Exception(log.ToString());

            Programs.Add(prog);               

            return prog;

        }

        protected void Remove(uint prog)
        {
            GL.DeleteProgram(prog);
            Programs.Remove(prog);
        }

        public virtual void Refresh()
        {

            Color bg = Brushes.White.Color;
            Color fg = Brushes.Black.Color;
            
            try
            {
                bg = ((SolidColorBrush)Background).Color;
                _control.Background = Background;
            }
            catch(NullReferenceException) { }
            finally
            {
                _background = new float[4] { bg.R / 255f, bg.G / 255f, bg.B / 255f, bg.A / 255f };
                GL.ClearColor(_background[0], _background[1], _background[2], _background[3]);
            }

            try
            {
                fg = ((SolidColorBrush)Foreground).Color;
                _control.Foreground = Foreground;
            }
            catch (NullReferenceException) { }
            finally
            {
                _foreground = new float[4] { fg.R / 255f, fg.G / 255f, fg.B / 255f, fg.A / 255f };
            }            

            GL.SetDimensions((int)ActualWidth, (int)ActualHeight);
            GL.Viewport(0, 0, (int)ActualWidth, (int)ActualHeight);           

        }
        
    }
}
