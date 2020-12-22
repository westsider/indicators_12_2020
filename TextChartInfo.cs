#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;

using System.Windows.Media.Imaging;
using System.Windows.Controls;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	// Based on script at 
	// https://ninjatrader.com/support/helpGuides/nt8/en-us/?using_bitmapimage_objects_with_buttons.htm
	
	// Demo Tool Bar Button by: 
	// Add button to tool bar, attach on click event. 
	// http://nigel-forex.blogspot.co.uk/
	
	public class TextChartInfo : Indicator
	{
		bool showLabel = false; 
		
		// Define a Chart object to refer to the chart on which the indicator resides
		private Chart chartWindow;

		// Define a Button
		private System.Windows.Controls.Button myButton = null;

		private bool IsToolBarButtonAdded;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Demo Tool Bar Button";
				Name										= "Text Chart Info";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.Historical)
			{
				//Call the custom addButtonToToolbar method in State.Historical to ensure it is only done when applied to a chart

				// -- not when loaded in the Indicators window
				if (!IsToolBarButtonAdded) AddButtonToToolbar();
			}
			else if (State == State.Terminated)
			{
				//Call a custom method to dispose of any leftover objects in State.Terminated
				DisposeCleanUp();
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
		
		private void AddButtonToToolbar()
		{
		  // Use this.Dispatcher to ensure code is executed on the proper thread
		  ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		  {
		      //Obtain the Chart on which the indicator is configured
		      chartWindow = Window.GetWindow(this.ChartControl.Parent) as Chart;
		      if (chartWindow == null)
		      {
		          Print("chartWindow == null");
		          return;
		      }

		      // Create a style to apply to the button
		      Style s = new Style();
		      s.TargetType = typeof(System.Windows.Controls.Button);
		      s.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
		      s.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.DimGray));
		      s.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.WhiteSmoke));
		      s.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
		      s.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));

		      // Instantiate the Button
		      myButton = new System.Windows.Controls.Button();

		      //Set Button Style            
		      myButton.Style = s;

		      myButton.Content = " info ";
		      myButton.IsEnabled = true;
		      myButton.HorizontalAlignment = HorizontalAlignment.Left;
			  
			  // Assign the Click event 
			  myButton.Click += myButton_Click;

		      // Add the Button to the Chart's Toolbar
		      chartWindow.MainMenu.Add(myButton);

		      //Prevent the Button From Displaying when WorkSpace Opens if it is not in an active tab
		      myButton.Visibility = Visibility.Collapsed;
		      foreach (TabItem tab in this.chartWindow.MainTabControl.Items)
		      {
		          if ((tab.Content as ChartTab).ChartControl == this.ChartControl

		               && tab == this.chartWindow.MainTabControl.SelectedItem)
		          {
		              myButton.Visibility = Visibility.Visible;
		          }
		      }
		      IsToolBarButtonAdded = true;
		  }));
		}
		
		protected void myButton_Click(object sender, RoutedEventArgs e)
		{
			// Add Click event code here or call a custom function. 
			
			// Toggle the bool value and text
			if(showLabel)
			{
				RemoveDrawObject("txtInstrName");
				RemoveDrawObject("txtInstrDesc");
				showLabel = false;
			}
			else 
			{
				Draw.TextFixed(this, "txtInstrName", Instrument.MasterInstrument.Name, TextPosition.TopLeft, Brushes.Gray, new Gui.Tools.SimpleFont("Arial", 72), Brushes.Transparent, Brushes.Transparent, 20);
				Draw.TextFixed(this, "txtInstrDesc", Instrument.MasterInstrument.Description, TextPosition.BottomRight, Brushes.Gray, new Gui.Tools.SimpleFont("Arial", 24), Brushes.Transparent, Brushes.Transparent, 10);
				showLabel = true; 
			}

			// required.
			ChartControl.InvalidateVisual();
		}		
		
		private void DisposeCleanUp()
		{
		  //ChartWindow Null Check
		  if (chartWindow != null)
		  {
		      //Dispatcher used to Assure Executed on UI Thread
		      ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		      {
		          //Button Null Check
		          if (myButton != null)
		          {
					  // remove click event 
					  myButton.Click -= myButton_Click;
					  
		              //Remove Button from Indicator's Chart ToolBar
		              chartWindow.MainMenu.Remove(myButton);
		          }
		      }));
		  }
		}		

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TextChartInfo[] cacheTextChartInfo;
		public TextChartInfo TextChartInfo()
		{
			return TextChartInfo(Input);
		}

		public TextChartInfo TextChartInfo(ISeries<double> input)
		{
			if (cacheTextChartInfo != null)
				for (int idx = 0; idx < cacheTextChartInfo.Length; idx++)
					if (cacheTextChartInfo[idx] != null &&  cacheTextChartInfo[idx].EqualsInput(input))
						return cacheTextChartInfo[idx];
			return CacheIndicator<TextChartInfo>(new TextChartInfo(), input, ref cacheTextChartInfo);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TextChartInfo TextChartInfo()
		{
			return indicator.TextChartInfo(Input);
		}

		public Indicators.TextChartInfo TextChartInfo(ISeries<double> input )
		{
			return indicator.TextChartInfo(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TextChartInfo TextChartInfo()
		{
			return indicator.TextChartInfo(Input);
		}

		public Indicators.TextChartInfo TextChartInfo(ISeries<double> input )
		{
			return indicator.TextChartInfo(input);
		}
	}
}

#endregion
