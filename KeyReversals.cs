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
#endregion

//This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Returns a value of 1 when the current close is greater than the prior close after penetrating the lowest low of the last n bars.
	/// </summary>
	public class KeyReversals : Indicator
	{
		private MIN min;
		private MAX max;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionKeyReversalUp;
				Name						= "Key Reversals";
				IsSuspendedWhileInactive	= true;
				Period						= 3;
			}
			else if (State == State.DataLoaded)
				min = MIN(Low, Period);
				max = MAX(High, Period);
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period + 1)
				return;

			double up = Low[0] < min[1] && Close[0] > Close[1] ? 1: 0;
			if (up == 1) {
				Draw.Dot(this, "up"+CurrentBar, false, 0, Low[0] - 1 * TickSize, Brushes.DodgerBlue);
			}
			double down = High[0] > max[1] && Close[0] < Close[1] ? 1: 0;
			if (down == 1) {
				Draw.Dot(this, "down"+CurrentBar, false, 0, High[0] + 1 * TickSize, Brushes.Red);
			}
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private KeyReversals[] cacheKeyReversals;
		public KeyReversals KeyReversals(int period)
		{
			return KeyReversals(Input, period);
		}

		public KeyReversals KeyReversals(ISeries<double> input, int period)
		{
			if (cacheKeyReversals != null)
				for (int idx = 0; idx < cacheKeyReversals.Length; idx++)
					if (cacheKeyReversals[idx] != null && cacheKeyReversals[idx].Period == period && cacheKeyReversals[idx].EqualsInput(input))
						return cacheKeyReversals[idx];
			return CacheIndicator<KeyReversals>(new KeyReversals(){ Period = period }, input, ref cacheKeyReversals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.KeyReversals KeyReversals(int period)
		{
			return indicator.KeyReversals(Input, period);
		}

		public Indicators.KeyReversals KeyReversals(ISeries<double> input , int period)
		{
			return indicator.KeyReversals(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.KeyReversals KeyReversals(int period)
		{
			return indicator.KeyReversals(Input, period);
		}

		public Indicators.KeyReversals KeyReversals(ISeries<double> input , int period)
		{
			return indicator.KeyReversals(input, period);
		}
	}
}

#endregion
