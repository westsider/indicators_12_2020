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
	public class FastPivotFib : Indicator
	{

		private struct SwingData
		{
			public  double 	lastHigh 		{ get; set; }
			public  bool 	highDominant	{ get; set; }
			public	int 	lastHighBarnum	{ get; set; }
			public	double 	lastLow 		{ get; set; }
			public  bool 	lowDominant		{ get; set; }
			public	int 	lastLowBarnum	{ get; set; }
			public  double 	prevHigh		{ get; set; }
			public	int 	prevHighBarnum	{ get; set; }
			public	double 	prevLow			{ get; set; }
			public	int 	prevLowBarnum	{ get; set; }
		}
		private SwingData swingData = new SwingData{};
		private int lastBar			= 0;
		private bool debug 			= false;
		private bool drawDownFib 	= false;
		private bool drawUpFib 		= false;
		private double entryPrice 	= 0.0;
		private double targetPrice 	= 0.0;
		private bool inLongTrade 	= false;
		private double Vwap 		= 0.0;
		
		private int 	startTime 	= 930; 
        private int	 	endTime 	= 1500;
		private int		ninja_Start_Time;
		private int		ninja_End_Time;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Fast Pivot Fib";
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
				IsSuspendedWhileInactive	= true;
				
				PlotCount					= false;
				MinBarsToLastSwing			= 5;
				SwingPct					= 0.0005;
				MinPlotCount				= 2;
				ShowFibs 					= false;
				RTHonly 					= false;
			}
			else if (State == State.Configure)
			{
			}
			else if(State == State.DataLoaded)
			{
				ninja_Start_Time = startTime * 100;
				ninja_End_Time = endTime * 100;
				ClearOutputWindow(); 
			} 
		}
		
		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 20 ) {return; }
			
			if ((RTHonly) && (ToTime(Time[1]) < ninja_Start_Time || ToTime(Time[0]) > ninja_End_Time)) {
				return;
			}
			Vwap = OrderFlowVWAP(Close, NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 1, 2, 3).VWAP[0];
			lastBar = CurrentBar -1;
			findNewHighs(upCount: edgeCount(up: true, plot: PlotCount ), minSwing: MinBarsToLastSwing );
			findNewLows(dnCount: edgeCount(up:false, plot: PlotCount ), minSwing: MinBarsToLastSwing );
			
			if( swingData.lastHigh != 0) { 
				bool removeLast = swingData.lastHighBarnum != CurrentBar;
				int length = CurrentBar - swingData.lastHighBarnum; 
				PivotLines(length: length, name: "pivotHighline", upper: true, price: swingData.lastHigh, removeLast: removeLast, dominant: swingData.highDominant);
				int distanceToLastLow = CurrentBar - swingData.lastLowBarnum;
				if ( ShowFibs && swingData.highDominant && Close[0] > Vwap ) {
					FibLines(start: swingData.lastLow, end: swingData.lastHigh, length: distanceToLastLow, up: true);
				}
			}
				
			if( swingData.lastLow != 0) { 
				bool removeLast = swingData.lastLowBarnum != CurrentBar;
				int length = CurrentBar - swingData.lastLowBarnum; 
				PivotLines(length: length, name: "pivotLowline", upper: false, price: swingData.lastLow, removeLast: removeLast, dominant: swingData.lowDominant);
				int distanceToLastHigh = CurrentBar - swingData.lastHighBarnum;
				if ( ShowFibs && swingData.lowDominant && Close[0] < Vwap) {
					FibLines(start: swingData.lastHigh, end: swingData.lastLow, length: distanceToLastHigh, up: false);
				}
			}
		}
		
		private void PivotLines(int length, string name, bool upper, double price, bool removeLast, bool dominant) {
			Brush lineColor = Brushes.DimGray; 
			if ( removeLast ) { RemoveDrawObject(name + lastBar);}
			int thickness = 1;
			if ( dominant ) {
				thickness = 3;
				lineColor = Brushes.CornflowerBlue;
				if ( upper ) { lineColor = Brushes.Red; } 
			}
			Draw.Line(this, name+CurrentBar, false, length, price , 0, price , lineColor, DashStyleHelper.Solid, thickness);
		}
		
		private bool GetVWAPupper(bool debug) { 
			// thin line becomes thisk if true
			double hiVwap = OrderFlowVWAP(Close, 
				NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, 
				Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 
				1, 2, 3).StdDev2Upper[0];
			if ( High[0] >= hiVwap ) {
				if ( debug ) {Draw.Dot(this, "hiVWAP"+CurrentBar, false, 0, hiVwap, Brushes.White);	}
				return true;
			} else { return false; }
		}
		
		private bool GetVWAPlower(bool debug) { 
			// thin line becomes thisk if true
			double lowVwap = OrderFlowVWAP(Close, 
				NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, 
				Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 
				1, 2, 3).StdDev2Lower[0];
			if ( Low[0] <= lowVwap ) {
				if ( debug ) {Draw.Dot(this, "lowVWAP"+CurrentBar, false, 0, lowVwap, Brushes.White);}		
				return true;
			} else { return false; }
		}
				
		public int edgeCount(bool up, bool plot){

			int upCount = 0;
			int dnCount = 0;
			int result = 0;
			double upper = Bollinger(2, 20).Upper[0];	
			double lower = Bollinger(2, 20).Lower[0];	
			double  Rsi1	= RSI(14, 1)[0];
			/// rsi section
			if ( Rsi1> 70 ) { upCount ++;}		
			if ( Rsi1 < 30 ) {	dnCount ++;} 

			/// bollinger section			
			if ( High[0] > upper ) {	upCount ++; }	
			if ( Low[0] <  lower ) {	dnCount ++; }
				
			/// highest high section
			if (High[0] >= MAX(High, 20)[1] ) { upCount ++;}
			if (Low[0] <= MIN(Low, 20)[1] ) { dnCount ++; }
			
			/// TODO: use min plot count to fiter active swings
				
			/// plot the count
			if (up) {
				result = upCount;
				if (upCount >= MinPlotCount && plot ) {
					Draw.Text(this, "upCount"+CurrentBar, upCount.ToString(), 0, High[0] + (TickSize * 10));
				}
			} 
			
			if ( up == false ) {
				result = dnCount;
				if (dnCount >= MinPlotCount && !up && plot ) {
					Draw.Text(this, "dnCount"+CurrentBar, dnCount.ToString(), 0, Low[0] - (TickSize * 10));
				}
			}
		    return result;
		}
		
		/// find new highs 
		public void findNewHighs(int upCount, double minSwing){
			/// find min swing as pct of close, old hard coded value is 1.5
			/// 226 * 0.00663 = 1.49
			/// swingPct 0.005 = .9 - 1.2 and much better results
			double minPriceSwing = Math.Abs(Close[0] * SwingPct);

			if ( upCount >= MinPlotCount && High[0] - swingData.lastLow > minPriceSwing ) {
				swingData.prevHigh = swingData.lastHigh;
				swingData.prevHighBarnum = swingData.lastHighBarnum;
				swingData.lastHigh = High[0];
				swingData.highDominant = GetVWAPupper(debug: debug);
				//RemoveDrawObject("pivotHigh" + lastBar);
				//Draw.Dot(this, "pivotHigh"+CurrentBar, false, 0, swingData.lastHigh  + 2 * TickSize, Brushes.Red);
				/// remove lower high at highs
				swingData.lastHighBarnum = CurrentBar;
				int distanceToLastHigh = swingData.lastHighBarnum - swingData.prevHighBarnum;
				if(High[0] < swingData.prevHigh && distanceToLastHigh < minSwing ) { 
					swingData.lastHigh = swingData.prevHigh;
					swingData.lastHighBarnum = CurrentBar - distanceToLastHigh; 
				}
			}			
		}
		
		/// find new lows
		public void findNewLows(int dnCount, double minSwing){
			double minPriceSwing = Math.Abs( Close[0] * SwingPct );
			if ( dnCount >= MinPlotCount  && swingData.lastHigh - Low[0] > minPriceSwing ) {
				swingData.prevLow = swingData.lastLow;
				swingData.prevLowBarnum = swingData.lastLowBarnum;
				swingData.lastLow = Low[0];
				swingData.lastLowBarnum = CurrentBar;
				swingData.lowDominant = GetVWAPlower(debug: debug);
				
				int distanceToLastLow = swingData.lastLowBarnum - swingData.prevLowBarnum;
				if(Low[0] > swingData.prevLow && distanceToLastLow < minSwing ) {
					swingData.lastLow = swingData.prevLow;
					swingData.lastLowBarnum = swingData.prevLowBarnum;
				} 
			}
		}

		private void FibLines(double start, double end, int length, bool up) {
			if ( up ) {
				Draw.FibonacciRetracements(this, "up", true, length, start, 0, end, false, "HansenBear");
			} else {
				drawDownFib = true;
				Draw.FibonacciRetracements(this, "down", true, length, start, 0, end, false, "Hansen");
			}
			
//			if ( drawDownFib ) {
//				// calc entry point
//				entryPrice = ((swingData.lastHigh - swingData.lastLow) * 0.236) + swingData.lastLow;
//				Print(Time[0].ToShortTimeString() + " " + entryPrice.ToString());
//				/// entry
//				if ( High[0] >= entryPrice ) {
//					RemoveDrawObject("longEntry"+ lastBar);
//					Draw.TriangleUp(this, "longEntry"+CurrentBar, false, 0, entryPrice, Brushes.DodgerBlue);
//					inLongTrade = true;
//					targetPrice = ((swingData.lastHigh - swingData.lastLow) * 0.38) + swingData.lastLow;
//				}
//				/// target
//				if ( inLongTrade && High[0] >=  targetPrice) {
//					Draw.Dot(this, "longExit"+CurrentBar, false, 0, targetPrice, Brushes.LimeGreen);
					
//					inLongTrade = false;
//				}
//			}
			
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Plot Count", Order=1, GroupName="Parameters")]
		public bool PlotCount
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Min Bars To Last Swing", Order=3, GroupName="Parameters")]
		public int MinBarsToLastSwing
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Swing Pct", Order=4, GroupName="Parameters")]
		public double SwingPct
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Min Filter Count", Order=5, GroupName="Parameters")]
		public int MinPlotCount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Fibs", Order=6, GroupName="Parameters")]
		public bool ShowFibs
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(Name="RTH only", Order=7, GroupName="Parameters")]
		public bool RTHonly
		{ get; set; }
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FastPivotFib[] cacheFastPivotFib;
		public FastPivotFib FastPivotFib(bool plotCount, int minBarsToLastSwing, double swingPct, int minPlotCount, bool showFibs, bool rTHonly)
		{
			return FastPivotFib(Input, plotCount, minBarsToLastSwing, swingPct, minPlotCount, showFibs, rTHonly);
		}

		public FastPivotFib FastPivotFib(ISeries<double> input, bool plotCount, int minBarsToLastSwing, double swingPct, int minPlotCount, bool showFibs, bool rTHonly)
		{
			if (cacheFastPivotFib != null)
				for (int idx = 0; idx < cacheFastPivotFib.Length; idx++)
					if (cacheFastPivotFib[idx] != null && cacheFastPivotFib[idx].PlotCount == plotCount && cacheFastPivotFib[idx].MinBarsToLastSwing == minBarsToLastSwing && cacheFastPivotFib[idx].SwingPct == swingPct && cacheFastPivotFib[idx].MinPlotCount == minPlotCount && cacheFastPivotFib[idx].ShowFibs == showFibs && cacheFastPivotFib[idx].RTHonly == rTHonly && cacheFastPivotFib[idx].EqualsInput(input))
						return cacheFastPivotFib[idx];
			return CacheIndicator<FastPivotFib>(new FastPivotFib(){ PlotCount = plotCount, MinBarsToLastSwing = minBarsToLastSwing, SwingPct = swingPct, MinPlotCount = minPlotCount, ShowFibs = showFibs, RTHonly = rTHonly }, input, ref cacheFastPivotFib);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FastPivotFib FastPivotFib(bool plotCount, int minBarsToLastSwing, double swingPct, int minPlotCount, bool showFibs, bool rTHonly)
		{
			return indicator.FastPivotFib(Input, plotCount, minBarsToLastSwing, swingPct, minPlotCount, showFibs, rTHonly);
		}

		public Indicators.FastPivotFib FastPivotFib(ISeries<double> input , bool plotCount, int minBarsToLastSwing, double swingPct, int minPlotCount, bool showFibs, bool rTHonly)
		{
			return indicator.FastPivotFib(input, plotCount, minBarsToLastSwing, swingPct, minPlotCount, showFibs, rTHonly);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FastPivotFib FastPivotFib(bool plotCount, int minBarsToLastSwing, double swingPct, int minPlotCount, bool showFibs, bool rTHonly)
		{
			return indicator.FastPivotFib(Input, plotCount, minBarsToLastSwing, swingPct, minPlotCount, showFibs, rTHonly);
		}

		public Indicators.FastPivotFib FastPivotFib(ISeries<double> input , bool plotCount, int minBarsToLastSwing, double swingPct, int minPlotCount, bool showFibs, bool rTHonly)
		{
			return indicator.FastPivotFib(input, plotCount, minBarsToLastSwing, swingPct, minPlotCount, showFibs, rTHonly);
		}
	}
}

#endregion
