using Kitware.VTK;
using PSMViewer.Models;
using PSMViewer.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSMViewer.Visualizations
{
    public class vtkChartBase : vtkBase
    {

        protected vtkContextView _view = vtkContextView.New();

        protected vtkChartXY _chart = vtkChartXY.New();
        
        protected enum CommandType
        {
            RESET_VIEW
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public vtkChartBase()
        {
            
            _chart.GetLegend().SetDragEnabled(true);
            
            _chart.SetAutoAxes(true);
            _chart.SetAutoSize(true);
            _chart.SetShowLegend(true);
            
            _view.GetScene().AddItem(_chart);

            _renderControl.Load += delegate
            {
                _view.SetRenderWindow(_renderWindow);
                _view.ResetCamera();

            };
                       
            CommandsSource.Add("ResetView", new RelayCommand(ExecuteCommand, canExecute, CommandType.RESET_VIEW));

            RegisterUserCommand();
            RegisterUserCommand("Reset View", CommandsSource["ResetView"]);
            
        }

        protected override void ExecuteCommand(object sender, object parameter)
        {

            RelayCommand cmd = (RelayCommand)sender;

            switch ((CommandType)cmd.Arguments[0].Value)
            {
                case CommandType.RESET_VIEW:
                    
                    _view.ResetCamera();

                    return;
            }

            base.ExecuteCommand(sender, parameter);
        }

        public override void Remove(KeyItem key)
        {

            MultiControl m = GetControlsFor(key);

            if (m != null)
            {
                Collector collector;

                if (_collectors.TryGetValue(m, out collector))
                {

                    _chart.RemovePlot(_chart.GetPlotIndex((vtkPlot)collector.Variables.Plot));

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

                vtkSplitField split = vtkSplitField.New();
                vtkDataObjectToTable table = vtkDataObjectToTable.New();
                vtkPassArrays pass = vtkPassArrays.New();
                
                pass.SetInputDataObject(0, _source);
                pass.AddArray((int)vtkDataObject.AttributeTypes.FIELD, path);

                split.SetInputConnection(pass.GetOutputPort());
                split.SetInputField(path, (int)vtkSplitField.FieldLocations.DATA_OBJECT);

                split.Split(1, "Index");
                split.Split(0, m.Key.Name);
                
                table.SetInputConnection(split.GetOutputPort());
                table.SetFieldType((int)vtkDataObjectToTable.CELL_DATA_WrapperEnum.FIELD_DATA);

                table.Update();                

                collector.Variables.Plot = null;
                collector.Variables.Algorithm = table;
                collector.Variables.Reset = true;

                _chart.AddPlot(Add(table, m));
            }

            return m;
        }
                
        protected override void UpdateView()
        {

            while (_dirty.Count > 0)
            {
                Collector collector = _dirty.Dequeue();
                dynamic v = collector.Variables;

                v.Algorithm.Update();

                if(v.Reset == true)
                {
                    
                    v.Reset = false;
                }
            }
                        
            _view.Render();

            base.UpdateView();

        }

        protected virtual vtkPlot Add(vtkTableAlgorithm table, MultiControl control)
        {
            throw new NotImplementedException();
        }

        public override void Refresh()
        {            
            base.Refresh();
        }
    }
}
