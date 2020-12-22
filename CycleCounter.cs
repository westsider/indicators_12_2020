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
	public class CycleCounter : Indicator
	{
		private Swing Swing1;
		//private WTTcRSI WTTcRSI1;
		
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
				Name										= "CycleCounter";
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
				
//				WTTcRSI1				= WTTcRSI(Close, 20, 10, 40, 10);
//				WTTcRSI1.Plots[0].Brush = Brushes.Red;
//				WTTcRSI1.Plots[1].Brush = Brushes.Blue;
//				WTTcRSI1.Plots[2].Brush = Brushes.Gray;
//				WTTcRSI1.Plots[3].Brush = Brushes.Blue;
//				//AddChartIndicator(WTTcRSI1);
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < SwingStrength + 1 ) { return;}
			
			if (Low[SwingStrength] == Swing1.SwingLow[0]) 
			{
				int length = CurrentBar - lastBarNum;
				if ( length > SmallCycleMin && lastBarNum != 0) {
					//Print("Swing Low on " + CurrentBar + " length = " + length);
					cycleLows.Add(length);
				}
				lastBarNum = CurrentBar;
			}
			
			calcStats(debug: false);
			printHistogram(arr: cycleLows);
			
			Draw.TextFixed(this, "myStatsFixed", 
					bellCurve, 
					NoteLocation, 
					FontColor,  // text color
					NoteFont, 
					OutlineColor, // outline color
					BackgroundColor, 
					BackgroundOpacity);
		}
		
		private void calcStats(bool debug) {
			if (CurrentBar < Count -2) return;
			cycleLows.Sort();	
			if ( debug ) {
				Print("\nArray: " + cycleLows.Count() );
				for (int i = 0; i < cycleLows.Count(); i++) 
				{
					Print(cycleLows[i]);
				}
			}
		}
		
		private void printHistogram(List<int> arr) 
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
			Print(" ");
			bellCurve = "Frequencies found \nin data set troughs\n";
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
			bellCurve += "Peak Frequency " + peakFrequency ;
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
		private CycleCounter[] cacheCycleCounter;
		public CycleCounter CycleCounter(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return CycleCounter(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public CycleCounter CycleCounter(ISeries<double> input, int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			if (cacheCycleCounter != null)
				for (int idx = 0; idx < cacheCycleCounter.Length; idx++)
					if (cacheCycleCounter[idx] != null && cacheCycleCounter[idx].SwingStrength == swingStrength && cacheCycleCounter[idx].SmallCycleMin == smallCycleMin && cacheCycleCounter[idx].LargeCycleMin == largeCycleMin && cacheCycleCounter[idx].BackgroundColor == backgroundColor && cacheCycleCounter[idx].FontColor == fontColor && cacheCycleCounter[idx].OutlineColor == outlineColor && cacheCycleCounter[idx].NoteFont == noteFont && cacheCycleCounter[idx].BackgroundOpacity == backgroundOpacity && cacheCycleCounter[idx].NoteLocation == noteLocation && cacheCycleCounter[idx].EqualsInput(input))
						return cacheCycleCounter[idx];
			return CacheIndicator<CycleCounter>(new CycleCounter(){ SwingStrength = swingStrength, SmallCycleMin = smallCycleMin, LargeCycleMin = largeCycleMin, BackgroundColor = backgroundColor, FontColor = fontColor, OutlineColor = outlineColor, NoteFont = noteFont, BackgroundOpacity = backgroundOpacity, NoteLocation = noteLocation }, input, ref cacheCycleCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CycleCounter CycleCounter(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounter(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public Indicators.CycleCounter CycleCounter(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounter(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CycleCounter CycleCounter(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounter(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}

		public Indicators.CycleCounter CycleCounter(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation)
		{
			return indicator.CycleCounter(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation);
		}
	}
}

#endregion
