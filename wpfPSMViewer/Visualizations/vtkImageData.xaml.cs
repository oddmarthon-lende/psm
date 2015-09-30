using Kitware.VTK;
using PSMViewer.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSMViewer.Visualizations
{

    [Visible(true)]
    [DisplayName("ImageData (VTK)")]
    [Icon("../icons/image.png")]
    public sealed partial class vtkImageData : vtkBase
    {
        
        /// <summary>
        /// Constructor
        /// </summary>
        public vtkImageData()
        {
            InitializeComponent();
        }

        public override void Remove(KeyItem key)
        {

            MultiControl m = GetControlsFor(key);

            if (m != null)
            {
                Collector collector;

                if (_collectors.TryGetValue(m, out collector))
                {

                    _renderer.RemoveActor(collector.Variables.Actor);

                }
            }

            base.Remove(key);

        }

        
        public override MultiControl Add(KeyItem key, ObservableCollection<EntryItem> collection = null)
        {
            
            MultiControl m = base.Add(key, collection);

            if (m != null)
            {

                string path = m.Key.Path;

                Collector collector = _collectors[m];
                
                vtkActor actor = vtkActor.New();
                vtkDataSetMapper mapper = vtkDataSetMapper.New();
                vtkMergeDataObjectFilter merge = vtkMergeDataObjectFilter.New();
                vtkPassArrays pass = vtkPassArrays.New();
                
                pass.SetInputDataObject(0, _source);
                pass.AddArray((int)vtkDataObject.AttributeTypes.FIELD, path);

                merge.SetOutputFieldToPointDataField();
                merge.SetDataObjectInputData(_source);                

                mapper.SetInputConnection(merge.GetOutputPort());
                mapper.ImmediateModeRenderingOff();
                mapper.SetScalarModeToUsePointData();
                
                actor.SetMapper(mapper);
                actor.GetProperty().SetRepresentationToSurface();

                dynamic v = collector.Variables;

                v.Actor = actor;
                v.Mapper = mapper;
                
                v.Algorithm = merge;
                v.Path = path;
                v.Added = true;

                //if (_renderer != null)
                //{
                //    _renderer.AddActor(actor);
                //    _renderer.ResetCamera();
                //}
                //else
                //{
                //    v.Added = false;

                //    if (!_dirty.Contains(collector))
                //        _dirty.Enqueue(collector);
                //}
                
            }

            return m;
        }

        protected override void UpdateView()
        {
            while (_dirty.Count > 0)
            {

                Collector collector = _dirty.Dequeue();
                dynamic v = collector.Variables;
                
                //v.Algorithm.Update();

                //Debug.WriteLine((object)v.Algorithm.GetOutput());
                //Debug.WriteLine((object)v.Image);                

                if(v.Added == false)
                {

                    _renderer.AddActor(v.Actor);
                    _renderer.ResetCamera();

                    v.Added = true;
                }

            }

            _renderWindow.Render();

            base.UpdateView();
        }

        public override void Refresh()
        {
            base.Refresh();
        }
    }
}
