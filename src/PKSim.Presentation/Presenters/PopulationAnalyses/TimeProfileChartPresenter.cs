﻿using System;
using System.Windows.Forms;
using OSPSuite.Utility.Extensions;
using PKSim.Core.Chart;
using PKSim.Core.Model.PopulationAnalyses;
using PKSim.Presentation.Views.PopulationAnalyses;
using OSPSuite.Presentation.Presenters;

namespace PKSim.Presentation.Presenters.PopulationAnalyses
{
   public interface ITimeProfileChartPresenter : IPresenter<ITimeProfileChartView>, IPopulationAnalysisChartPresenter<TimeProfileXValue, TimeProfileYValue>
   {
      /// <summary>
      ///    Event is fired when some data are dragged over view
      /// </summary>
      event DragEventHandler DragOver;

      /// <summary>
      ///    Event is fired when some data are dropped onto view
      /// </summary>
      event DragEventHandler DragDrop;

      event Action ObservedDataSettingsChanged;
   }

   internal class TimeProfileChartPresenter : PopulationAnalysisChartPresenter<ITimeProfileChartView, ITimeProfileChartPresenter, TimeProfileXValue, TimeProfileYValue>, ITimeProfileChartPresenter
   {
      private readonly ITimeProfileChartSettingsPresenter _timeProfileChartSettingsPresenter;
      public event Action ObservedDataSettingsChanged = delegate { };

      public TimeProfileChartPresenter(ITimeProfileChartView view, ITimeProfileChartSettingsPresenter timeProfileChartSettingsPresenter)
         : base(view, timeProfileChartSettingsPresenter)
      {
         view.DragDropEnabled = true;
         _timeProfileChartSettingsPresenter = timeProfileChartSettingsPresenter;
         _timeProfileChartSettingsPresenter.ObservedDataSettingsChanged += () => ObservedDataSettingsChanged();
      }

      public event DragEventHandler DragOver
      {
         add { _view.DragOver += value; }
         remove { _view.DragOver -= value; }
      }

      public event DragEventHandler DragDrop
      {
         add { _view.DragDrop += value; }
         remove { _view.DragDrop -= value; }
      }

      public override void Show(ChartData<TimeProfileXValue, TimeProfileYValue> chartsData, PopulationAnalysisChart populationAnalysisChart)
      {
         base.Show(chartsData, populationAnalysisChart);
         _timeProfileChartSettingsPresenter.BindTo(populationAnalysisChart.DowncastTo<TimeProfileAnalysisChart>().ObservedDataCollection);
      }
   }
}