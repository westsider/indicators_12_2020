//
// Copyright (C) 2019, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//

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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX.DirectWrite;
using SharpDX;
using SharpDX.Direct2D1;
using Point = System.Windows.Point;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	public class TickCounter2 : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description			= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionTickCounter;
				Name				= "TickCounter 2";
				Calculate			= Calculate.OnEachTick;
				CountDown			= true;
				DisplayInDataBox	= false;
				DrawOnPricePanel	= false;
				IsChartOnly			= true;
				IsOverlay			= true;
				ShowPercent			= false;
			}
		}

		protected override void OnBarUpdate()
		{
			double periodValue = (BarsPeriod.BarsPeriodType == BarsPeriodType.Tick) ? BarsPeriod.Value : BarsPeriod.BaseBarsPeriodValue;
			double tickCount = ShowPercent ? CountDown ? (1 - Bars.PercentComplete) * 100 : Bars.PercentComplete * 100 : CountDown ? periodValue - Bars.TickCount : Bars.TickCount;

			string tick1 = (BarsPeriod.BarsPeriodType == BarsPeriodType.Tick 
						|| ((BarsPeriod.BarsPeriodType == BarsPeriodType.HeikenAshi || BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric) && BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Tick) ? ((CountDown
										? NinjaTrader.Custom.Resource.TickCounterTicksRemaining + tickCount : NinjaTrader.Custom.Resource.TickCounterTickCount + tickCount) + (ShowPercent ? "%" : ""))
										: NinjaTrader.Custom.Resource.TickCounterBarError);
			string fuckyou = String.Format("{0.#}#", tick1);
			string sb = String.Format("{0:0.00}", tick1);
			Draw.TextFixed(this, "NinjaScriptInfo", sb, TextPosition.BottomRight, ChartControl.Properties.ChartText, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "CountDown", Order = 1, GroupName = "NinjaScriptParameters")]
		public bool CountDown
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof (Custom.Resource), Name = "ShowPercent", Order = 2, GroupName = "NinjaScriptParameters")]
		public bool ShowPercent
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TickCounter2[] cacheTickCounter2;
		public TickCounter2 TickCounter2(bool countDown, bool showPercent)
		{
			return TickCounter2(Input, countDown, showPercent);
		}

		public TickCounter2 TickCounter2(ISeries<double> input, bool countDown, bool showPercent)
		{
			if (cacheTickCounter2 != null)
				for (int idx = 0; idx < cacheTickCounter2.Length; idx++)
					if (cacheTickCounter2[idx] != null && cacheTickCounter2[idx].CountDown == countDown && cacheTickCounter2[idx].ShowPercent == showPercent && cacheTickCounter2[idx].EqualsInput(input))
						return cacheTickCounter2[idx];
			return CacheIndicator<TickCounter2>(new TickCounter2(){ CountDown = countDown, ShowPercent = showPercent }, input, ref cacheTickCounter2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TickCounter2 TickCounter2(bool countDown, bool showPercent)
		{
			return indicator.TickCounter2(Input, countDown, showPercent);
		}

		public Indicators.TickCounter2 TickCounter2(ISeries<double> input , bool countDown, bool showPercent)
		{
			return indicator.TickCounter2(input, countDown, showPercent);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TickCounter2 TickCounter2(bool countDown, bool showPercent)
		{
			return indicator.TickCounter2(Input, countDown, showPercent);
		}

		public Indicators.TickCounter2 TickCounter2(ISeries<double> input , bool countDown, bool showPercent)
		{
			return indicator.TickCounter2(input, countDown, showPercent);
		}
	}
}

#endregion
