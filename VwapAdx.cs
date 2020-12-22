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
	public class VwapAdx : Indicator
	{
		private OrderFlowVWAP OrderFlowVWAP1;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "VwapAdx";
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
				ColorBands					= true;
				RangeColor					= Brushes.DimGray;
				TrendColor					= Brushes.DarkGoldenrod;
				AddPlot(RangeColor, "StdDev2");
				AddPlot(RangeColor, "StdDev1");
				AddPlot(RangeColor, "StdDevM1");
				AddPlot(RangeColor, "StdDevM2");
			}
			else if (State == State.Configure)
			{ 
				OrderFlowVWAP1 = OrderFlowVWAP(Close, NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 1, 2, 3);
			}
			else if(State == State.DataLoaded)
			{ 
				ClearOutputWindow(); 
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 10) { return; }
			
			StdDev2[0] = OrderFlowVWAP1.StdDev3Upper[0];
			StdDev1[0] = OrderFlowVWAP1.StdDev2Upper[0];
			StdDevM1[0] = OrderFlowVWAP1.StdDev2Lower[0];
			StdDevM2[0] = OrderFlowVWAP1.StdDev3Lower[0];
			Draw.Region(this, "topBand", CurrentBar, 0, StdDev2, StdDev1, null, RangeColor, 20);
			Draw.Region(this, "BototomBand", CurrentBar, 0, StdDevM2, StdDevM1, null, RangeColor, 20);
			
			if ( ADX(14)[0] > 25 ) {
				//Print("> 25");
				PlotBrushes[0][0] = TrendColor;
				PlotBrushes[1][0] = TrendColor;
				PlotBrushes[2][0] = TrendColor;
				PlotBrushes[3][0] = TrendColor;
				
				
			} else {
				//Print("range");
				PlotBrushes[0][0] = RangeColor;
				PlotBrushes[1][0] = RangeColor;
				PlotBrushes[2][0] = RangeColor;
				PlotBrushes[3][0] = RangeColor;
				
			}
			
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="ColorBands", Order=1, GroupName="Parameters")]
		public bool ColorBands
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="RangeColor", Order=2, GroupName="Parameters")]
		public Brush RangeColor
		{ get; set; }

		[Browsable(false)]
		public string RangeColorSerializable
		{
			get { return Serialize.BrushToString(RangeColor); }
			set { RangeColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="TrendColor", Order=3, GroupName="Parameters")]
		public Brush TrendColor
		{ get; set; }

		[Browsable(false)]
		public string TrendColorSerializable
		{
			get { return Serialize.BrushToString(TrendColor); }
			set { TrendColor = Serialize.StringToBrush(value); }
		}			


		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StdDev1
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StdDev2
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StdDevM1
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> StdDevM2
		{
			get { return Values[3]; }
		}
		
		
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VwapAdx[] cacheVwapAdx;
		public VwapAdx VwapAdx(bool colorBands, Brush rangeColor, Brush trendColor)
		{
			return VwapAdx(Input, colorBands, rangeColor, trendColor);
		}

		public VwapAdx VwapAdx(ISeries<double> input, bool colorBands, Brush rangeColor, Brush trendColor)
		{
			if (cacheVwapAdx != null)
				for (int idx = 0; idx < cacheVwapAdx.Length; idx++)
					if (cacheVwapAdx[idx] != null && cacheVwapAdx[idx].ColorBands == colorBands && cacheVwapAdx[idx].RangeColor == rangeColor && cacheVwapAdx[idx].TrendColor == trendColor && cacheVwapAdx[idx].EqualsInput(input))
						return cacheVwapAdx[idx];
			return CacheIndicator<VwapAdx>(new VwapAdx(){ ColorBands = colorBands, RangeColor = rangeColor, TrendColor = trendColor }, input, ref cacheVwapAdx);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VwapAdx VwapAdx(bool colorBands, Brush rangeColor, Brush trendColor)
		{
			return indicator.VwapAdx(Input, colorBands, rangeColor, trendColor);
		}

		public Indicators.VwapAdx VwapAdx(ISeries<double> input , bool colorBands, Brush rangeColor, Brush trendColor)
		{
			return indicator.VwapAdx(input, colorBands, rangeColor, trendColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VwapAdx VwapAdx(bool colorBands, Brush rangeColor, Brush trendColor)
		{
			return indicator.VwapAdx(Input, colorBands, rangeColor, trendColor);
		}

		public Indicators.VwapAdx VwapAdx(ISeries<double> input , bool colorBands, Brush rangeColor, Brush trendColor)
		{
			return indicator.VwapAdx(input, colorBands, rangeColor, trendColor);
		}
	}
}

#endregion
