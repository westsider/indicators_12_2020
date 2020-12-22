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
	public class CycleCounterHarmonic : Indicator
	{
		private Swing Swing1;
		private WTTcRSI2 WTTcRSI21;
		
		private int lastBarNum = 0;
		private List<int> cycleLows = new List<int>();
		private string bellCurve = "";
		private int peakFrequency = 0;
		private int peakValue = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Cycle Counter Harmonic";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				SwingStrength					= 5;
				SmallCycleMin					= 12;
				LargeCycleMin					= 90;
				
				NoteLocation			= TextPosition.TopLeft;
				BackgroundColor			= Brushes.DimGray;
				BackgroundOpacity 		= 90;
				FontColor				= Brushes.WhiteSmoke;
				OutlineColor			= Brushes.DimGray;
				NoteFont				= new SimpleFont("Arial", 12);
				
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				Swing1				= Swing(Close, SwingStrength);
				ClearOutputWindow();
				
				WTTcRSI21				= WTTcRSI2(Close, 20, 10, true, 3, 20, Brushes.DodgerBlue, Brushes.Red);
				WTTcRSI21.Plots[0].Brush = Brushes.Red;
				WTTcRSI21.Plots[1].Brush = Brushes.DodgerBlue;
				WTTcRSI21.Plots[2].Brush = Brushes.DimGray;
				WTTcRSI21.Plots[3].Brush = Brushes.DodgerBlue;
				//AddChartIndicator(WTTcRSI21);
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < SwingStrength + 1 ) { return;}
			populateCycles(debug: false);
			printCycles(arr: cycleLows);
			showHistogram();
			//checkHarmionics();
			 // Set 1
			if (Close[0] == WTTcRSI21.CRSI[0])
			{
			}
			
		}
		
		private void checkHarmionics() {
			for (int seq = 1; seq < 7; seq++) 
			{
				Print("Checking Seq: " + seq);
				List<int> newArr = checkCombinations(arr: cycleLows, forSeq: seq, debug: false);
				newArr.Sort();
				printCycles(arr: newArr);		
			}
		}
		
		private List<int> checkCombinations(List<int> arr, int forSeq, bool debug) {
			
			List<int> newArr = new List<int>();
			int insideLoopSum = 0;
			int counter = 1;
			
			foreach (int a in arr) 
			{
				if ( counter < forSeq ) {
					if ( debug ) { Print(a); }
					insideLoopSum += a;
					counter += 1;
				} else {
					if ( debug ) { Print(a + " then new");}
					insideLoopSum += a;
					newArr.Add(insideLoopSum);
					insideLoopSum = 0;
					counter = 1;
				}
			}
			return newArr;
		}
		
		private void showHistogram() {
			Draw.TextFixed(this, "myStatsFixed", 
					bellCurve, 
					NoteLocation, 
					FontColor,  // text color
					NoteFont, 
					OutlineColor, // outline color
					BackgroundColor, 
					BackgroundOpacity);
		}
		
		private void populateCycles(bool debug ) {
			if (Low[SwingStrength] == Swing1.SwingLow[0]) 
			{
				int length = CurrentBar - lastBarNum;
				if ( length > SmallCycleMin && lastBarNum != 0) {
					//Print("Swing Low on " + CurrentBar + " length = " + length);
					cycleLows.Add(length);
				}
				lastBarNum = CurrentBar;
			}
			if ( cycleLows.Count > 2 ) { cycleLows.Sort(); }
			showArray(debug: false);
		}
		
		private void showArray(bool debug) {
			if (CurrentBar < Count -2) return;
			if ( debug ) {
				Print("\nArray: " + cycleLows.Count() );
				for (int i = 0; i < cycleLows.Count(); i++) 
				{
					Print(cycleLows[i]);
				}
			}
		}
		
		private void printCycles(List<int> arr) 
		{ 
			if (CurrentBar < Count -2) return;
			Dictionary<int, int> ItemCount = new Dictionary<int, int>();
			int[] items =  arr.ToArray(); 
			Print(" " );
	
			foreach (int item in items)
			{
				
			    if (ItemCount.ContainsKey(item))
			    {
			         ItemCount[item]++;
			    }
			    else {
					ItemCount.Add(item,1);
			    }
			}
			
//			Print(" ");
//			int lastItem = 0;
//			foreach (int val in items) {
//				if (val == lastItem + 1) {
//					Print(val + " is next to " + lastItem);
//				} 
//				lastItem = val;
//			}

			Print(" ");
			bellCurve = "\nFrequencies found \nin data set troughs\n\n";
			foreach (KeyValuePair<int,int> res in ItemCount)
			{
				//Print(res.Key + "  " + res.Value);
				string bar = "";
				for (int index = 0; index < res.Value; index++) 
				{
					bar += "X";
				}
				if (res.Value > peakValue) { 
					peakValue = res.Value; 
				}
				string h = res.Key.ToString();
				if (h.Count() == 1) {
					h += "_";
				}
				string message = h +"   |\t"+bar;
				Print(message);
				bellCurve += message + "\n";
			}
			 
			peakFrequency = ItemCount.FirstOrDefault(x => x.Value == peakValue).Key;
			bellCurve += "\nPeak Frequency " + peakFrequency ;
			bellCurve += ", Value " + peakValue + "\n"; // peakFrequency
		} 
		

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SwingStrength", Order=1, GroupName="Parameters")]
		public int SwingStrength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SmallCycleMin", Order=2, GroupName="Parameters")]
		public int SmallCycleMin
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="LargeCycleMin", Order=3, GroupName="Parameters")]
		public int LargeCycleMin
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Background Color", Description="Background Color", Order=10, GroupName="Stats")]
		public Brush BackgroundColor
		{ get; set; }

		[Browsable(false)]
		public string BackgroundColorSerializable
		{
			get { return Serialize.BrushToString(BackgroundColor); }
			set { BackgroundColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Font Color", Description="Font Color", Order=2, GroupName="Stats")]
		public Brush FontColor
		{ get; set; }

		[Browsable(false)]
		public string FontColorSerializable
		{
			get { return Serialize.BrushToString(FontColor); }
			set { FontColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="OutlineColor Color", Description="OutlineColor Color", Order=3, GroupName="Stats")]
		public Brush OutlineColor
		{ get; set; }

		[Browsable(false)]
		public string OutlineColorSerializable
		{
			get { return Serialize.BrushToString(OutlineColor); }
			set { OutlineColor = Serialize.StringToBrush(value); }
		}
		 
		[NinjaScriptProperty]
		[Display(Name="Note Font", Description="Note Font", Order=4, GroupName="Stats")]
		public SimpleFont NoteFont
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Background Opacity", Description="Background Opacity", Order=5, GroupName="Stats")]
		public int BackgroundOpacity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Note1Location", Description="Note Location", Order=6, GroupName="Stats")]
		public TextPosition NoteLocation
		{ get; set; }
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CycleCounterHarmonic[] cacheCycleCounterHarmonic;
		public CycleCounterHarmonic CycleCounterHarmonic(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return CycleCounterHarmonic(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public CycleCounterHarmonic CycleCounterHarmonic(ISeries<double> input, int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			if (cacheCycleCounterHarmonic != null)
				for (int idx = 0; idx < cacheCycleCounterHarmonic.Length; idx++)
					if (cacheCycleCounterHarmonic[idx] != null && cacheCycleCounterHarmonic[idx].SwingStrength == swingStrength && cacheCycleCounterHarmonic[idx].SmallCycleMin == smallCycleMin && cacheCycleCounterHarmonic[idx].LargeCycleMin == largeCycleMin && cacheCycleCounterHarmonic[idx].BackgroundColor == backgroundColor && cacheCycleCounterHarmonic[idx].FontColor == fontColor && cacheCycleCounterHarmonic[idx].OutlineColor == outlineColor && cacheCycleCounterHarmonic[idx].NoteFont == noteFont && cacheCycleCounterHarmonic[idx].BackgroundOpacity == backgroundOpacity && cacheCycleCounterHarmonic[idx].NoteLocation == noteLocation && cacheCycleCounterHarmonic[idx].EqualsInput(input))
						return cacheCycleCounterHarmonic[idx];
			return CacheIndicator<CycleCounterHarmonic>(new CycleCounterHarmonic(){ SwingStrength = swingStrength, SmallCycleMin = smallCycleMin, LargeCycleMin = largeCycleMin, BackgroundColor = backgroundColor, FontColor = fontColor, OutlineColor = outlineColor, NoteFont = noteFont, BackgroundOpacity = backgroundOpacity, NoteLocation = noteLocation }, input, ref cacheCycleCounterHarmonic);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CycleCounterHarmonic CycleCounterHarmonic(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounterHarmonic(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public Indicators.CycleCounterHarmonic CycleCounterHarmonic(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounterHarmonic(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CycleCounterHarmonic CycleCounterHarmonic(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounterHarmonic(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public Indicators.CycleCounterHarmonic CycleCounterHarmonic(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounterHarmonic(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}
	}
}

#endregion
