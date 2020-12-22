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
	public class FastPivotVwap : Indicator
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
		private bool debug = false;
		
		/// <summary>
		///  vars for public access
		/// </summary>
//		private int lastHighBarnum;
//		private int lastLowBarnum;
//		private int prevHighBarnum;
//		private int prevLowBarnum;
//		private double prevHigh;
//		private double prevLow;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Fast Pivot VWAP";
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
				ColorBars					= false;
				MinBarsToLastSwing			= 5;
				SwingPct					= 0.0005;
				MinPlotCount				= 2;
			}
			else if (State == State.Configure)
			{
			}
			else if(State == State.DataLoaded)
			{
				  ClearOutputWindow(); 
			} 
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 20 ) {return; }
			
			lastBar = CurrentBar -1;
			findNewHighs(upCount: edgeCount(up: true, plot: PlotCount ), minSwing: MinBarsToLastSwing );
			findNewLows(dnCount: edgeCount(up:false, plot: PlotCount ), minSwing: MinBarsToLastSwing );
			
			if( swingData.lastHigh != 0) { 
				bool removeLast = swingData.lastHighBarnum != CurrentBar;
				int length = CurrentBar - swingData.lastHighBarnum; 
				PivotLines(length: length, name: "pivotHighline", upper: true, price: swingData.lastHigh, removeLast: removeLast, dominant: swingData.highDominant);
			}
				
			if( swingData.lastLow != 0) { 
				bool removeLast = swingData.lastLowBarnum != CurrentBar;
				int length = CurrentBar - swingData.lastLowBarnum; 
				PivotLines(length: length, name: "pivotLowline", upper: false, price: swingData.lastLow, removeLast: removeLast, dominant: swingData.lowDominant);
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
			double hiVwap = OrderFlowVWAP(Close, NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 1, 2, 3).StdDev2Upper[0];
			if ( High[0] >= hiVwap ) {
				if ( debug ) {Draw.Dot(this, "hiVWAP"+CurrentBar, false, 0, hiVwap, Brushes.White);	}
				return true;
			} else { return false; }
		}
		
		private bool GetVWAPlower(bool debug) { 
			// thin line becomes thisk if true
			double lowVwap = OrderFlowVWAP(Close, NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 1, 2, 3).StdDev2Lower[0];
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
				/// remove higher low at lows
				int distanceToLastLow = swingData.lastLowBarnum - swingData.prevLowBarnum;
				if(Low[0] > swingData.prevLow && distanceToLastLow < minSwing ) {
					swingData.lastLow = swingData.prevLow;
					swingData.lastLowBarnum = swingData.prevLowBarnum;
				} 
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Plot Count", Order=1, GroupName="Parameters")]
		public bool PlotCount
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Color Bars", Order=2, GroupName="Parameters")]
		public bool ColorBars
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

//		[Browsable(false)]
//		[XmlIgnore]
//		public Series<double> LastHigh
//		{
//			get { return Values[0]; }
//		}

//		[Browsable(false)]
//		[XmlIgnore]
//		public Series<double> LastLow
//		{
//			get { return Values[1]; }
//		}
		
//		public int LastHighBarnum
//        {
//			// We need to call the Update() method to ensure our exposed variable is in up-to-date.
//            get { Update(); return lastHighBarnum; }
//        }
		
//		public int LastLowBarnum
//        {
//			// We need to call the Update() method to ensure our exposed variable is in up-to-date.
//            get { Update(); return lastLowBarnum; }
//        }
		
//		public int PrevHighBarnum
//        {
//			// We need to call the Update() method to ensure our exposed variable is in up-to-date.
//            get { Update(); return prevHighBarnum; }
//        }
		
//		public int PrevLowBarnum
//        {
//			// We need to call the Update() method to ensure our exposed variable is in up-to-date.
//            get { Update(); return prevLowBarnum; }
//        }
		
		
//		public double PrevHigh
//        {
//			// We need to call the Update() method to ensure our exposed variable is in up-to-date.
//            get { Update(); return prevHigh; }
//        }
		
//		public double PrevLow
//        {
//			// We need to call the Update() method to ensure our exposed variable is in up-to-date.
//            get { Update(); return prevLow; }
//        }
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private FastPivotVwap[] cacheFastPivotVwap;
		public FastPivotVwap FastPivotVwap(bool plotCount, bool colorBars, int minBarsToLastSwing, double swingPct, int minPlotCount)
		{
			return FastPivotVwap(Input, plotCount, colorBars, minBarsToLastSwing, swingPct, minPlotCount);
		}

		public FastPivotVwap FastPivotVwap(ISeries<double> input, bool plotCount, bool colorBars, int minBarsToLastSwing, double swingPct, int minPlotCount)
		{
			if (cacheFastPivotVwap != null)
				for (int idx = 0; idx < cacheFastPivotVwap.Length; idx++)
					if (cacheFastPivotVwap[idx] != null && cacheFastPivotVwap[idx].PlotCount == plotCount && cacheFastPivotVwap[idx].ColorBars == colorBars && cacheFastPivotVwap[idx].MinBarsToLastSwing == minBarsToLastSwing && cacheFastPivotVwap[idx].SwingPct == swingPct && cacheFastPivotVwap[idx].MinPlotCount == minPlotCount && cacheFastPivotVwap[idx].EqualsInput(input))
						return cacheFastPivotVwap[idx];
			return CacheIndicator<FastPivotVwap>(new FastPivotVwap(){ PlotCount = plotCount, ColorBars = colorBars, MinBarsToLastSwing = minBarsToLastSwing, SwingPct = swingPct, MinPlotCount = minPlotCount }, input, ref cacheFastPivotVwap);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.FastPivotVwap FastPivotVwap(bool plotCount, bool colorBars, int minBarsToLastSwing, double swingPct, int minPlotCount)
		{
			return indicator.FastPivotVwap(Input, plotCount, colorBars, minBarsToLastSwing, swingPct, minPlotCount);
		}

		public Indicators.FastPivotVwap FastPivotVwap(ISeries<double> input , bool plotCount, bool colorBars, int minBarsToLastSwing, double swingPct, int minPlotCount)
		{
			return indicator.FastPivotVwap(input, plotCount, colorBars, minBarsToLastSwing, swingPct, minPlotCount);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.FastPivotVwap FastPivotVwap(bool plotCount, bool colorBars, int minBarsToLastSwing, double swingPct, int minPlotCount)
		{
			return indicator.FastPivotVwap(Input, plotCount, colorBars, minBarsToLastSwing, swingPct, minPlotCount);
		}

		public Indicators.FastPivotVwap FastPivotVwap(ISeries<double> input , bool plotCount, bool colorBars, int minBarsToLastSwing, double swingPct, int minPlotCount)
		{
			return indicator.FastPivotVwap(input, plotCount, colorBars, minBarsToLastSwing, swingPct, minPlotCount);
		}
	}
}

#endregion
