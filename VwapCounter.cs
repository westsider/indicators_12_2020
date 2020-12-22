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
using System.IO;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class VwapCounter : Indicator
	{
		private int 	startTime 	= 900; 
        private int	 	endTime 	= 1500;
		
		//private int 	startTime 	= 1200; 
        //private int	 	endTime 	= 1500;
		
		//private int 	startTime 	= 835; 
       // private int	 	endTime 	= 900;
		
		private int		ninja_Start_Time;
		private int		ninja_End_Time;
		
		private OrderFlowVWAP OrderFlowVWAP1;
		private double StopL = 0.0;
		private double StopS = 0.0;
		private double EntryPriceL = 0.0;
		private double EntryPriceS = 0.0;
		private double TargetL = 0.0;
		private double TargetS = 0.0;
		private bool InTradeL = false;
		private bool InTradeS = false;
		
		private int WinCountL = 0;
		private int LooseCountL = 0;
		private int WinCountS = 0;
		private int LooseCountS = 0;
		private double WinPctL = 0.0;
		private double WinPctS = 0.0;
		
		private double lower2 = 0.0;
		private double upper2 = 0.0;
		
		private int DayGain = 0;
		private int WinDayCount = 0;
		private int DayCount = 0;
		private double WinPctDays =0.0;
		
		List<double> dailyGain = new List<double>();
		private string csvPath = @"C:\Users\trade\Documents\_Send_To_Mac\";
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "VwapCounter";
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
			}
			else if (State == State.Configure)
			{ 
				OrderFlowVWAP1 = OrderFlowVWAP(Close, NinjaTrader.NinjaScript.Indicators.VWAPResolution.Standard, Bars.TradingHours, NinjaTrader.NinjaScript.Indicators.VWAPStandardDeviations.Three, 1, 2, 3);
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
			if (CurrentBar < 10) { return; }

			CalcWinDays();
			
			if (ToTime(Time[1]) < ninja_Start_Time || ToTime(Time[0]) > ninja_End_Time) { return;  }
			
			lower2 = OrderFlowVWAP1.StdDev2Lower[0];
			upper2 = OrderFlowVWAP1.StdDev2Upper[0];
			if ( DayGain >= 75 ) { return; }
			LongEntry();
			ShortEntry();
			/// cumulative total
		}
		
		private void LongEntry() {  
			
			if (Close[0] <= lower2 && !InTradeL)
			{
				Draw.ArrowUp(this, "LE"+CurrentBar, false, 0, Low[0] - 0.25, Brushes.DodgerBlue);
				EntryPriceL = Close[0];
				StopL = EntryPriceL - 1.5;
				TargetL = EntryPriceL + 1.5;
				InTradeL = true;
			} 
			/// stop
			if ( InTradeL && Low[0] < StopL ) {
				InTradeL = false;	
				Draw.Dot(this, "minusStop"+CurrentBar, true, 0, StopL, Brushes.Red);
				LooseCountL += 1;
				DayGain -= 75;
				GetStats(longs: true);
			}
			/// target
			if ( InTradeL && High[0] > TargetL ) {
				InTradeL = false;	
				Draw.Dot(this, "minustgt"+CurrentBar, true, 0, TargetL, Brushes.Green);
				WinCountL += 1;
				DayGain += 75;
				GetStats(longs: true);
			}
		}
		
		private void ShortEntry() { 
			if (Close[0] >= upper2 && !InTradeS)
			{
				Draw.ArrowDown(this, "SE"+CurrentBar, false, 0, High[0] + 0.25, Brushes.Red);
				EntryPriceS = Close[0];
				StopS = EntryPriceS + 1.5;
				TargetS = EntryPriceS - 1.5;
				InTradeS = true;
			} 
			/// stop
			if ( InTradeS && Low[0] > StopS ) {
				InTradeS = false;	
				Draw.Dot(this, "shortStop"+CurrentBar, true, 0, StopS, Brushes.Red);
				LooseCountS += 1;
				DayGain -= 75;
				GetStats(longs: false);
			}
			/// target
			if ( InTradeS && High[0] < TargetS ) {
				InTradeS = false;	
				Draw.Dot(this, "LongStop"+CurrentBar, true, 0, TargetS, Brushes.Green);
				WinCountS += 1;
				DayGain += 75;
				GetStats(longs: false);
			}
		}
		
		private void GetStats(bool longs) { 
			
			if ( longs ) {
				int trades = WinCountL + LooseCountL;
				if ( trades > 0 ) { 
				 	WinPctL = ((double)WinCountL / (double)trades) * 100;
					var answer = String.Format("{0:0.00}",WinPctL);
					Print("Long " + Time[0].ToShortDateString() + " " + Time[0].ToShortTimeString() + " " + answer + "% ");
				}
			} else {
				int trades = WinCountS + LooseCountS;
				if ( trades > 0 ) { 
				 	WinPctS = ((double)WinCountS / (double)trades) * 100;
					var answer = String.Format("{0:0.00}",WinPctS);
					Print("Short " + Time[0].ToShortDateString() + " " + Time[0].ToShortTimeString() + " " + answer + "% ");
				}
			}
			
			var answerS = String.Format("{0:0.0}",WinPctS);
			var answerL = String.Format("{0:0.0}",WinPctL);
			var answerWPD = String.Format("{0:0.0}",WinPctDays);
			var winSvsLong = DayCount + " Day Statistics\n"+ answerL + "% Win Long\n" + answerS + "% Win Short\n" +  answerWPD + "% Winning Days"  ;
			Draw.TextFixed(this, "stats", winSvsLong, TextPosition.TopRight);
			
		}

		private void CalcWinDays() { 
			// pct win days
			if ("Sunday"  == Time[0].DayOfWeek.ToString()) { return; }
			if(ToTime(Time[0]) > 151500 && Bars.IsFirstBarOfSession)
			{
				DayCount += 1;
				Draw.Text(this, "day"+CurrentBar, DayCount.ToString(), 0, High[0] + 2 * TickSize, Brushes.White);
				
				if ( DayGain >= 75 ) {
					WinDayCount += 1;
				}
				dailyGain.Add(DayGain);
				WinPctDays = ((double)WinDayCount / (double)DayCount) * 100;
				var answer = String.Format("{0:0.0}",WinPctDays);
				Print(" \t" + answer + "% " + " win days");
				DayGain = 0;
				
				Print(string.Join("\n", dailyGain));
				Print(dailyGain.Sum());
			}
		}
		
		private void WriteFile(string path, string newLine, bool header)
        {
			if ( header ) {
				ClearFile(path: csvPath);
				using (var tw = new StreamWriter(path, true))
	            {
	                tw.WriteLine(newLine); 
	                tw.Close();
	            }
				return;
			}
			
            using (var tw = new StreamWriter(path, true))
            {
                tw.WriteLine(newLine);
                tw.Close();
            }
        }
		
		private void ClearFile(string path)
        {
            try    
			{    
				// Check if file exists with its full path    
				if (File.Exists(path))    
				{    
					// If file found, delete it    
					File.Delete(path);    
					Print("File deleted.");    
				} 
				else  Print("File not found");    
			}    
			catch (IOException ioExp)    
			{    
				Print(ioExp.Message);    
			} 
			
        }
		
	}
	

}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VwapCounter[] cacheVwapCounter;
		public VwapCounter VwapCounter()
		{
			return VwapCounter(Input);
		}

		public VwapCounter VwapCounter(ISeries<double> input)
		{
			if (cacheVwapCounter != null)
				for (int idx = 0; idx < cacheVwapCounter.Length; idx++)
					if (cacheVwapCounter[idx] != null &&  cacheVwapCounter[idx].EqualsInput(input))
						return cacheVwapCounter[idx];
			return CacheIndicator<VwapCounter>(new VwapCounter(), input, ref cacheVwapCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VwapCounter VwapCounter()
		{
			return indicator.VwapCounter(Input);
		}

		public Indicators.VwapCounter VwapCounter(ISeries<double> input )
		{
			return indicator.VwapCounter(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VwapCounter VwapCounter()
		{
			return indicator.VwapCounter(Input);
		}

		public Indicators.VwapCounter VwapCounter(ISeries<double> input )
		{
			return indicator.VwapCounter(input);
		}
	}
}

#endregion
