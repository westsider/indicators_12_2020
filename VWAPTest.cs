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
	public class VWAPTest : Indicator
	{
		private OrderFlowVWAP OrderFlowVWAP1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "VWAPTest";
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
				AddPlot(Brushes.Red, "VWAP");
			}
			else if (State == State.Configure)
			{
				OrderFlowVWAP1 = OrderFlowVWAP(Close, NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 1, 2, 3);
			}
		}

		protected override void OnBarUpdate()
		{
//				var RTHopen						= DateTime.Parse("06:31", System.Globalization.CultureInfo.InvariantCulture);
//				var RTHclose					= DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
//				var startTime = long.Parse(RTHopen.ToString("HHmmss"));
//			 	var endTime = long.Parse(RTHclose.ToString("HHmmss"));
			
				//var th = Bars.TradingHours.Sessions.temp;
			//Print(th);
			//Add your custom indicator logic here.
			//if (ToTime(Time[0]) >= startTime  && ToTime(Time[0]) <= endTime  ) {
				VWAP[0] = OrderFlowVWAP1[0]; //OrderFlowVWAP(Close, NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 1, 2, 3)[0];
			//}
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VWAP
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VWAPTest[] cacheVWAPTest;
		public VWAPTest VWAPTest()
		{
			return VWAPTest(Input);
		}

		public VWAPTest VWAPTest(ISeries<double> input)
		{
			if (cacheVWAPTest != null)
				for (int idx = 0; idx < cacheVWAPTest.Length; idx++)
					if (cacheVWAPTest[idx] != null &&  cacheVWAPTest[idx].EqualsInput(input))
						return cacheVWAPTest[idx];
			return CacheIndicator<VWAPTest>(new VWAPTest(), input, ref cacheVWAPTest);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VWAPTest VWAPTest()
		{
			return indicator.VWAPTest(Input);
		}

		public Indicators.VWAPTest VWAPTest(ISeries<double> input )
		{
			return indicator.VWAPTest(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VWAPTest VWAPTest()
		{
			return indicator.VWAPTest(Input);
		}

		public Indicators.VWAPTest VWAPTest(ISeries<double> input )
		{
			return indicator.VWAPTest(input);
		}
	}
}

#endregion
