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
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TimerTest : Indicator
	{
		private bool timeSpanelapsed = false; 
		private System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();	
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "TimerTest";
				Calculate									= Calculate.OnBarClose;
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
				myTimer.Tick += new EventHandler(TimerEventProcessor);
				myTimer.Interval = 60000; // 1 min, 2000 = 2 sec
				myTimer.Start();
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
		
//		private void LaunchTimer() {
//			var startTimeSpan = TimeSpan.Zero;
//			var periodTimeSpan = TimeSpan.FromMinutes(5);
//			timeSpanelapsed = false; 
//			Print("Timer start, Alerts are now " + timeSpanelapsed);
//			Draw.TextFixed(this, "Alerts", "Alerts are paused for 5 minutes", TextPosition.BottomLeft);
			
//			var timer = new System.Threading.Timer((e) =>
//			{
//				Print("Time up, Alerts are now " + timeSpanelapsed);
//			    timeSpanelapsed = true; 
				
//			}, null, startTimeSpan, periodTimeSpan);
//		}
		
		private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
		{
			TriggerCustomEvent(MyCustomHandler, 0, myTimer.Interval);
		}
		
		private void MyCustomHandler(object state)
		{
			Print("\tTime: " + DateTime.Now);
			Print("\tTimer Interval: " + state.ToString() + "ms");
			
		}
		
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TimerTest[] cacheTimerTest;
		public TimerTest TimerTest()
		{
			return TimerTest(Input);
		}

		public TimerTest TimerTest(ISeries<double> input)
		{
			if (cacheTimerTest != null)
				for (int idx = 0; idx < cacheTimerTest.Length; idx++)
					if (cacheTimerTest[idx] != null &&  cacheTimerTest[idx].EqualsInput(input))
						return cacheTimerTest[idx];
			return CacheIndicator<TimerTest>(new TimerTest(), input, ref cacheTimerTest);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TimerTest TimerTest()
		{
			return indicator.TimerTest(Input);
		}

		public Indicators.TimerTest TimerTest(ISeries<double> input )
		{
			return indicator.TimerTest(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TimerTest TimerTest()
		{
			return indicator.TimerTest(Input);
		}

		public Indicators.TimerTest TimerTest(ISeries<double> input )
		{
			return indicator.TimerTest(input);
		}
	}
}

#endregion
