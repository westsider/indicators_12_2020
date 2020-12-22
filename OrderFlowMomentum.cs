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
	public class OrderFlowMomentum : Indicator
	{
		private OrderFlowCumulativeDelta cumulativeDelta;
		private double cumDeltaValue = 0.0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "OrderFlow Momentum";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= false;
				DrawVerticalGridLines						= false;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				AddPlot(new Stroke(Brushes.LawnGreen, 3), PlotStyle.Bar, "Momo");
				//AddPlot(new Stroke(Brushes.Red, 3), PlotStyle.Bar, "DownMomo");
				AddPlot(Brushes.Transparent, "DeltaMomo");
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{				
			      // Instantiate the indicator
			      cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Bar, 0);

			}
			
		}

		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			
		}

		protected override void OnMarketDepth(MarketDepthEventArgs marketDepthUpdate)
		{
			
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < 10 ) return;
			
			if (BarsInProgress == 0)
			{
			      // Print the close of the cumulative delta bar with a delta type of Bid Ask and with a Session period
			     // Print("Delta Close: " + cumulativeDelta.DeltaClose[0]);
//				if ( IsFirstTickOfBar )
//				Print(FormatDateTime() + "\t" + cumDeltaValue);
			}
			else if (BarsInProgress == 1)
			{
			      // We have to update the secondary series of the hosted indicator to make sure the values we get in BarsInProgress == 0 are in sync
			      cumulativeDelta.Update(cumulativeDelta.BarsArray[1].Count - 1, 1);

				//Add your custom indicator logic here.
				cumDeltaValue = cumulativeDelta.DeltaClose[0];

			
			if (cumulativeDelta.DeltaClose[0] > 0)
			{
				Momo[0] = 0;
				if (DeltaMomo[1] > 0)
					DeltaMomo[0] = DeltaMomo[1] + cumulativeDelta.DeltaClose[0];
				else
					DeltaMomo[0] = cumulativeDelta.DeltaClose[0];
				
				Momo[0] = DeltaMomo[0];
				PlotBrushes[0][0] = Brushes.DodgerBlue;
			}

			if (cumulativeDelta.DeltaClose[0] < 0)
			{
				Momo[0] = 0;
				if (DeltaMomo[1] < 0)
					DeltaMomo[0] = DeltaMomo[1] + cumulativeDelta.DeltaClose[0];
				else
					DeltaMomo[0] = cumulativeDelta.DeltaClose[0];
				
				Momo[0] = DeltaMomo[0];
				PlotBrushes[0][0] = Brushes.Red;
			}
			
			}
			
		}
		
		private string FormatDateTime() {
			DateTime myDate = Time[0];  // DateTime type
			string prettyDate = myDate.ToString("M/d/yyyy") + " " + myDate.ToString("hh:mm");
			return prettyDate;
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Momo
		{
			get { return Values[0]; }
		}

//		[Browsable(false)]
//		[XmlIgnore]
//		public Series<double> DownMomo
//		{
//			get { return Values[1]; }
//		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DeltaMomo
		{
			get { return Values[1]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrderFlowMomentum[] cacheOrderFlowMomentum;
		public OrderFlowMomentum OrderFlowMomentum()
		{
			return OrderFlowMomentum(Input);
		}

		public OrderFlowMomentum OrderFlowMomentum(ISeries<double> input)
		{
			if (cacheOrderFlowMomentum != null)
				for (int idx = 0; idx < cacheOrderFlowMomentum.Length; idx++)
					if (cacheOrderFlowMomentum[idx] != null &&  cacheOrderFlowMomentum[idx].EqualsInput(input))
						return cacheOrderFlowMomentum[idx];
			return CacheIndicator<OrderFlowMomentum>(new OrderFlowMomentum(), input, ref cacheOrderFlowMomentum);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrderFlowMomentum OrderFlowMomentum()
		{
			return indicator.OrderFlowMomentum(Input);
		}

		public Indicators.OrderFlowMomentum OrderFlowMomentum(ISeries<double> input )
		{
			return indicator.OrderFlowMomentum(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrderFlowMomentum OrderFlowMomentum()
		{
			return indicator.OrderFlowMomentum(Input);
		}

		public Indicators.OrderFlowMomentum OrderFlowMomentum(ISeries<double> input )
		{
			return indicator.OrderFlowMomentum(input);
		}
	}
}

#endregion
