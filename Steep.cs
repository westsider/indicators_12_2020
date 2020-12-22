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
	public class Steep : Indicator
	{
		private LinRegSlope LinRegSlope1;
		private int lastBar = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Steep";
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
				
				Level			= 0.5;
				Smoothing 		= 14;
				Spacer 		= 12;
				ShowText = true;
				ShowDiamonds = true;
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				LinRegSlope1				= LinRegSlope(Close, Smoothing);
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < Smoothing ) { return; }
			lastBar = CurrentBar - 1;
			//double level = 0.5;
			// Steep down
			if ( LinRegSlope1[0] <= -Level) {
				if ( ShowDiamonds ) {
					Draw.Diamond(this, "SteepDown"+ CurrentBar, false, 0, Low[0] , Brushes.Red);
					Draw.Diamond(this, "SteepDownH"+ CurrentBar, false, 0, High[0] , Brushes.Black);
					Draw.Diamond(this, "SteepDownM"+ CurrentBar, false, 0, Median[0] , Brushes.Red);
				}
				
				if ( ShowText ) {
					RemoveDrawObject("words"+lastBar);
					Draw.Text(this, "words"+CurrentBar, "STEEP", Spacer, Median[1], Brushes.Red);
				}
			}
			if ( LinRegSlope1[0] >= Level) {
				if ( ShowDiamonds ) {
					Draw.Diamond(this, "SteepUp"+ CurrentBar, false, 0, Low[0] , Brushes.Black);
					Draw.Diamond(this, "SteepUp"+ CurrentBar, false, 0, High[0] , Brushes.DodgerBlue);
					Draw.Diamond(this, "SteepUp"+ CurrentBar, false, 0, Median[0] , Brushes.DodgerBlue);
				}
				
				if ( ShowText ) {
					RemoveDrawObject("words"+lastBar);
					Draw.Text(this, "words"+CurrentBar, "STEEP", Spacer, Median[1], Brushes.DodgerBlue);
				}
			}
			
		}
		
		#region Properties

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Smoothing", Order=1, GroupName="Parameters")]
		public int Smoothing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Thresh Hold", Order=2, GroupName="Parameters")]
		public double Level
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Spacer", Order=3, GroupName="Parameters")]
		public int Spacer
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Text", Order=4, GroupName="Parameters")]
		public bool ShowText
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Diamonds", Order=5, GroupName="Parameters")]
		public bool ShowDiamonds
		{ get; set; }
		
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Steep[] cacheSteep;
		public Steep Steep(int smoothing, double level, int spacer, bool showText, bool showDiamonds)
		{
			return Steep(Input, smoothing, level, spacer, showText, showDiamonds);
		}

		public Steep Steep(ISeries<double> input, int smoothing, double level, int spacer, bool showText, bool showDiamonds)
		{
			if (cacheSteep != null)
				for (int idx = 0; idx < cacheSteep.Length; idx++)
					if (cacheSteep[idx] != null && cacheSteep[idx].Smoothing == smoothing && cacheSteep[idx].Level == level && cacheSteep[idx].Spacer == spacer && cacheSteep[idx].ShowText == showText && cacheSteep[idx].ShowDiamonds == showDiamonds && cacheSteep[idx].EqualsInput(input))
						return cacheSteep[idx];
			return CacheIndicator<Steep>(new Steep(){ Smoothing = smoothing, Level = level, Spacer = spacer, ShowText = showText, ShowDiamonds = showDiamonds }, input, ref cacheSteep);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Steep Steep(int smoothing, double level, int spacer, bool showText, bool showDiamonds)
		{
			return indicator.Steep(Input, smoothing, level, spacer, showText, showDiamonds);
		}

		public Indicators.Steep Steep(ISeries<double> input , int smoothing, double level, int spacer, bool showText, bool showDiamonds)
		{
			return indicator.Steep(input, smoothing, level, spacer, showText, showDiamonds);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Steep Steep(int smoothing, double level, int spacer, bool showText, bool showDiamonds)
		{
			return indicator.Steep(Input, smoothing, level, spacer, showText, showDiamonds);
		}

		public Indicators.Steep Steep(ISeries<double> input , int smoothing, double level, int spacer, bool showText, bool showDiamonds)
		{
			return indicator.Steep(input, smoothing, level, spacer, showText, showDiamonds);
		}
	}
}

#endregion
