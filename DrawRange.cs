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
	public class DrawRange : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Draw Range";
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
				RangeSize					= 9;
				RangeSizeMax					= 15;
				OffsetToRight					= 5;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			double rangeHigh = Close[0] + RangeSize * 0.5;
			double rangeLow = Close[0] - RangeSize * 0.5;
			int width = (-OffsetToRight * 2)  +1;
			Draw.Rectangle(this, "min", true, 
				-OffsetToRight, rangeLow, width, rangeHigh, 
				Brushes.DimGray, Brushes.Transparent,50);
			
			
			double rangeHighMax = Close[0] + RangeSizeMax * 0.5;
			double rangeLowMax = Close[0] - RangeSizeMax * 0.5;
			Draw.Rectangle(this, "max", true, 
				-OffsetToRight, rangeLowMax, width, rangeHighMax, 
				Brushes.DimGray, Brushes.Transparent,50);
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RangeSize", Order=1, GroupName="Parameters")]
		public int RangeSize
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Range Size Max", Order=2, GroupName="Parameters")]
		public int RangeSizeMax

		{ get; set; }
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="OffsetToRight", Order=3, GroupName="Parameters")]
		public int OffsetToRight
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DrawRange[] cacheDrawRange;
		public DrawRange DrawRange(int rangeSize, int rangeSizeMax, int offsetToRight)
		{
			return DrawRange(Input, rangeSize, rangeSizeMax, offsetToRight);
		}

		public DrawRange DrawRange(ISeries<double> input, int rangeSize, int rangeSizeMax, int offsetToRight)
		{
			if (cacheDrawRange != null)
				for (int idx = 0; idx < cacheDrawRange.Length; idx++)
					if (cacheDrawRange[idx] != null && cacheDrawRange[idx].RangeSize == rangeSize && cacheDrawRange[idx].RangeSizeMax == rangeSizeMax && cacheDrawRange[idx].OffsetToRight == offsetToRight && cacheDrawRange[idx].EqualsInput(input))
						return cacheDrawRange[idx];
			return CacheIndicator<DrawRange>(new DrawRange(){ RangeSize = rangeSize, RangeSizeMax = rangeSizeMax, OffsetToRight = offsetToRight }, input, ref cacheDrawRange);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DrawRange DrawRange(int rangeSize, int rangeSizeMax, int offsetToRight)
		{
			return indicator.DrawRange(Input, rangeSize, rangeSizeMax, offsetToRight);
		}

		public Indicators.DrawRange DrawRange(ISeries<double> input , int rangeSize, int rangeSizeMax, int offsetToRight)
		{
			return indicator.DrawRange(input, rangeSize, rangeSizeMax, offsetToRight);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DrawRange DrawRange(int rangeSize, int rangeSizeMax, int offsetToRight)
		{
			return indicator.DrawRange(Input, rangeSize, rangeSizeMax, offsetToRight);
		}

		public Indicators.DrawRange DrawRange(ISeries<double> input , int rangeSize, int rangeSizeMax, int offsetToRight)
		{
			return indicator.DrawRange(input, rangeSize, rangeSizeMax, offsetToRight);
		}
	}
}

#endregion
