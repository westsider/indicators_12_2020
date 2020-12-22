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
	public class Holidays : Indicator
	{
		private string[] holidays = new string[] { "1/1/2020", "1/20/2020", "2/17/2020", "4/10/2020", "5/25/2020", 
			"7/3/2020", "9/7/2020", "11/26/2020", "12/25/2020"};
		private bool todayHoliday = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Holidays";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
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
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 5 ) { return; }
			
			//todayHoliday =  isHoliday();
		}
		
		private void isHoliday() {
			if (BarsInProgress == 0 && Bars.IsFirstBarOfSession) {
				
    			Print(string.Format("Bar number {0} was the first bar processed of the session at {1}.", CurrentBar, Time[0]));
			
				foreach(string holiday in holidays)
				{
					if ( Time[0].ToShortDateString() == holiday) {
						Print("\t\t\tFound Holiday on " + Time[0].ToShortDateString() 	);
						
					} 
				}
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Holidays[] cacheHolidays;
		public Holidays Holidays()
		{
			return Holidays(Input);
		}

		public Holidays Holidays(ISeries<double> input)
		{
			if (cacheHolidays != null)
				for (int idx = 0; idx < cacheHolidays.Length; idx++)
					if (cacheHolidays[idx] != null &&  cacheHolidays[idx].EqualsInput(input))
						return cacheHolidays[idx];
			return CacheIndicator<Holidays>(new Holidays(), input, ref cacheHolidays);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Holidays Holidays()
		{
			return indicator.Holidays(Input);
		}

		public Indicators.Holidays Holidays(ISeries<double> input )
		{
			return indicator.Holidays(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Holidays Holidays()
		{
			return indicator.Holidays(Input);
		}

		public Indicators.Holidays Holidays(ISeries<double> input )
		{
			return indicator.Holidays(input);
		}
	}
}

#endregion
