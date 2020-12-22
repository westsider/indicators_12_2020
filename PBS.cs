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
	public class PBS : Indicator
	{
		private int hhCount  = 0;
		private int llCount  = 0;
		private int trend = 0;
		private SMA smaFast;
		private SMA smaSlow;
		private bool showTrend = false;
		private bool showCount = false;
		private bool showTradeApproaching = true;
		private string name = "";
		private int lastBar = 0;
		
		/// trade management
		private double entryPrice  = 0.0;
		private double targetPrice  = 0.0;
		private double breakevenPrice  = 0.0;
		private double stopPrice  = 0.0;
		private double entryPriceS  = 0.0;
		private double targetPriceS  = 0.0;
		private double breakevenPriceS  = 0.0;
		private double stopPriceS  = 0.0;
		
		/// private bool inTradeLong = false;
		private double profits = 0.0;
		private int barsinTradeLong = 0;
		private int barsinTradeShort = 0;
		private bool inTradeLong = false;
		private bool inTradeShort = false;
		
		/// stats
		private int tradeCount = 0;
		private int winCount = 0;
		List<double> profitList = new List<double>();
		List<double> profitListS = new List<double>();
		List<double> profitListL = new List<double>();
		private double profitsS = 0;
		private double profitsL = 0;
		
		/// daily profit loss
		private double dailyGain = 0;
		private int dayCount = 0;
		List<double> dailyGainList = new List<double>();
		
		///  ranking
		private double dia;
		private double spy;
		private double qqq;
		private double tf;
		private int 	startTime 	= 930; 
        private int	 	endTime 	= 1600;
		private int		ninja_Start_Time;
		private int		ninja_End_Time;
		private int 	requiredBars = 20;
		private double	esClose = 0.0;
		private double	ymClose = 0.0;
		private double	nqClose = 0.0;
		private double	tfClose = 0.0;
		private int indexRank = -1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "PBS";
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
				Longs										= true;
				Shorts										= true;
				MAFilter									= true;
				showBkg 									= true;
				Opacity 									= 0.1;
				AlertPBS									= @"ES_EnteringLongZone.wav";
				AlertPSS									= @"ES_EnteringShortZone.wav";
				AlertApproach 								= @"ES_ZoneTrigger.wav";
				showGreen 									= false;
				maxLoss 									= 500.0;
				maxGain 									= 500.0;
				targetTicks 								= 8;
				stopTicks 									= 8;
				breakEvenTicks 								= 7;
				useBreakEven								= false;
			}
			else if (State == State.Configure)
			{
				AddDataSeries("DIA", Data.BarsPeriodType.Minute, 3, Data.MarketDataType.Last);
				AddDataSeries("SPY", Data.BarsPeriodType.Minute, 3, Data.MarketDataType.Last);
				AddDataSeries("QQQ", Data.BarsPeriodType.Minute, 3, Data.MarketDataType.Last);
				AddDataSeries("IWM", Data.BarsPeriodType.Minute, 3, Data.MarketDataType.Last);
			}
			else if (State == State.DataLoaded)
			{
				smaFast 			= SMA(20);
				smaSlow 			= SMA(40);
				ninja_Start_Time = startTime * 100;
				ninja_End_Time = endTime * 100;
				ClearOutputWindow(); 
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 20 ) { return;}
			name = Bars.Instrument.MasterInstrument.Name;
			setDayLossMax();
			getIndexData(); 
			if (dailyGain <= -maxLoss || dailyGain >= maxGain) { return; }
			if (BarsInProgress == 0)
		    {
				showTargets();
				setTrend();
				setPSS();
				setPBS();
				manageLongTrade();
				manageShortTrade();
				stats(); }
		}
		
		private void getIndexData() {
			if (CurrentBars[0] <= requiredBars || CurrentBars[1] <= requiredBars || 
				CurrentBars[2] <= requiredBars || CurrentBars[3] <= requiredBars) {return;}
			
			if (BarsInProgress == 1) {
				if (ToTime(Time[1]) <= ninja_End_Time && ToTime(Time[0]) >= ninja_End_Time)
	            { ymClose = Close[0]; }
				dia = (Close[0] - ymClose) / ymClose * 100; 
			}
			if (BarsInProgress == 2)  {
				if (ToTime(Time[1]) <= ninja_End_Time && ToTime(Time[0]) >= ninja_End_Time)
	            {  esClose = Close[0]; }
				spy = (Close[0] - esClose) / esClose * 100;
			}
			if (BarsInProgress == 3) {
				if (ToTime(Time[1]) <= ninja_End_Time && ToTime(Time[0]) >= ninja_End_Time)
	            { nqClose = Close[0]; }
				qqq = (Close[0] - nqClose) / nqClose * 100;
			}
			if (BarsInProgress == 4) {
				if (ToTime(Time[1]) <= ninja_End_Time && ToTime(Time[0]) >= ninja_End_Time)
	            { tfClose = Close[0]; }
				tf = (Close[0] - tfClose) / tfClose * 100;
			}
			var dictionary = new Dictionary<string, double>();
			dictionary.Add("YM", dia); dictionary.Add("ES", spy);
	        dictionary.Add("NQ", qqq); dictionary.Add("TF", tf);
        	var items = from pair in dictionary
                    orderby pair.Value ascending
                    select pair;
			//Print("\nSorted:");
			int counter = 1;
			int indexRanking = -1;
			foreach (KeyValuePair<string, double> pair in items)
	        {
				if (pair.Key == "ES") {
					indexRanking = counter;
				}
				//Print("S " + counter + " \t" + pair.Key + " \t" + pair.Value);
				counter += 1;
	        }
			//Print("ES is ranked " + indexRanking + ", 1 is weakest, 4 is strongest\n");
			indexRank = indexRanking;
			
		}
				
		private void setDayLossMax() {
			if (Bars.IsFirstBarOfSession) {
				dayCount += 1;
				dailyGainList.Add(dailyGain);
				dailyGain = 0;
			}
		}
		
		private void setPSS() {
			if (!Shorts) { return;}
			/// setup
			if ((High[0] > High[1] || Low[0] > Low[1]) && trend <= 0 ) {
				hhCount += 1;
				if ( trend <= 0 && showCount)
					Draw.Text(this, "HH"+CurrentBar, hhCount.ToString(), 0, Low[0] - 1 * TickSize, Brushes.Red);
				if ( showTradeApproaching && hhCount >= 3 && trend <= 0 ) {
					RemoveDrawObject("shortClose" + lastBar);
					Draw.TriangleDown(this, "shortClose"+CurrentBar, true, 0, High[0] , Brushes.Gray);
					sendAlert(message: "Setting Up Sell on " + name, sound: AlertPSS );
				}
			}
			/// entry
			//if (  indexRank <= 3 )
			if (!inTradeShort && !inTradeLong && Low[0] < Low[1]) {
				if ( hhCount >= 3 && trend <= 0) {
					hhCount  = 0;
					llCount  = 0;
					Draw.ArrowDown(this, "PSSb"+CurrentBar, false, 0, High[0] + 1 * TickSize, Brushes.Crimson); 
					tradeCount += 1;
					entryPriceS = Low[1];
					inTradeShort = true;
					targetPriceS = entryPriceS - (targetTicks * TickSize);
					stopPriceS = entryPriceS + (stopTicks * TickSize);
					breakevenPriceS = entryPriceS - (breakEvenTicks * TickSize); 
					//Print("Short Entry " + entryPriceS + " \tTarget " + targetPriceS + " \tStop " + stopPriceS);
					sendAlert(message: "Sell " + name, sound: AlertApproach );
					Print("ES is ranked " + indexRank + ", 1 is weakest, 4 is strongest\n");
				}
			}
		}
		
		private void setPBS() {
			if (!Longs) { return;}
			/// setup
			if ((High[0] < High[1] || Low[0] < Low[1]) && trend >= 0) {
				llCount += 1;
				if ( trend >= 0 && showCount)
					Draw.Text(this, "LL"+CurrentBar, llCount.ToString(), 0, High[0] + 1 * TickSize, Brushes.DodgerBlue);
				if ( showTradeApproaching && llCount >= 3 && trend >= 0  ) {
					RemoveDrawObject("longClose" + lastBar);
					Draw.TriangleUp(this, "longClose"+CurrentBar, true, 0, Low[0], Brushes.Gray);
					sendAlert(message: "Setting Up Buy on " + name, sound: AlertPBS );
				}
			}
			/// entry
			//if (  indexRank >= 2 )
			if (!inTradeShort && !inTradeLong && High[0] > High[1]) {
				if ( llCount >= 3 && trend >= 0) {
					llCount  = 0;
					hhCount  = 0;
					Draw.ArrowUp(this, "PBSb"+CurrentBar, false, 0, Low[0] - 1 * TickSize, Brushes.LimeGreen);
					tradeCount += 1;
					entryPrice = High[1];
					inTradeLong = true;
					targetPrice = entryPrice + (targetTicks * TickSize);
					stopPrice = entryPrice - (stopTicks * TickSize);
					breakevenPrice = entryPrice + (breakevenPrice * TickSize);
					Print("Long Entry " + entryPrice + " \tTarget " + targetPrice + " \tStop " + stopPrice);
					sendAlert(message: "Buy " + name, sound: AlertApproach );
					Print("ES is ranked " + indexRank + ", 1 is weakest, 4 is strongest\n");
				}
			}
		}

		private void manageLongTrade() {
			if (!Longs) { return;}
			if (!inTradeLong) { return;}
			/// target hit
			if (High[0] >= targetPrice) {
				inTradeLong = false;
				llCount  = 0;
				winCount += 1;
				double profit = (targetTicks * TickSize) * Bars.Instrument.MasterInstrument.PointValue;
				profits += profit;
				dailyGain += profit;
				profitList.Add(profits);
				profitsL += profit;
				profitListL.Add(profitsL);
				Print(Time[0] + "\t Long gain of " + profit + " \tTotal profit " + profits);
				Draw.Dot(this, "targetHit"+CurrentBar, false, barsinTradeLong, targetPrice, Brushes.DodgerBlue);
				return;
			}
			/// break even hit
			if (High[0] >= breakevenPrice && useBreakEven) {
				stopPrice = entryPrice;
				Print(Time[0] + "\t Long Break Even Hit ");
				Draw.Dot(this, "breakeven"+CurrentBar, false, barsinTradeLong, breakevenPrice, Brushes.White);
				return;
			}
			/// stop hit
			if (Low[0] <= stopPrice && barsinTradeLong > 0) {
				inTradeLong = false;
				llCount  = 0;
				double profit = (( stopPrice - entryPrice ) ) * Bars.Instrument.MasterInstrument.PointValue;
				Print("--> \tEntry " + entryPrice + " - stop " + stopPrice + " =  loss of $" + profit);
				if ( profit == 0 ) { winCount += 1;}
				profits += profit;
				dailyGain += profit;
				profitList.Add(profits);
				profitsL += profit;
				profitListL.Add(profitsL);
				Print(Time[0] + "\t Long loss of " + profit + " \tTotal profit " + profits);
				Draw.Dot(this, "stopHit"+CurrentBar, false, barsinTradeLong, stopPrice, Brushes.Crimson);
			}
			
			
		}
		
		private void manageShortTrade() {
			if (!Shorts) { return;}
			if (!inTradeShort) { return;}
			/// target hit
			if (Low[0] <= targetPriceS) {
				inTradeShort = false;
				hhCount  = 0;
				winCount += 1;
				double profit = (targetTicks * TickSize) * Bars.Instrument.MasterInstrument.PointValue;
				profits += profit;
				dailyGain += profit;
				profitList.Add(profits); 
				profitsS += profit;
				profitListS.Add(profitsS);
				Print(Time[0] + "\t Short gain of " + profit + " \tTotal profit " + profits);
				Draw.Dot(this, "targetHitS"+CurrentBar, false, barsinTradeShort, targetPriceS, Brushes.DodgerBlue);
				return;
			}
			/// break even hit
			if (Low[0] <= breakevenPriceS && useBreakEven) {
				stopPriceS = entryPriceS;
				Print(Time[0] + "\t Short Break Even Hit ");
				Draw.Dot(this, "breakevenS"+CurrentBar, false, 0, breakevenPriceS, Brushes.White);
				return;
			}
			/// stop hit
			if (High[0] >= stopPriceS && barsinTradeShort > 0) {
				inTradeShort = false;
				hhCount  = 0;
				double profit = ((entryPriceS - stopPriceS)) * Bars.Instrument.MasterInstrument.PointValue;
				Print("--> \tEntry " + entryPrice + " - stop " + stopPrice + " =  loss of $" + profit);
				if ( profit == 0 ) { winCount += 1;}
				profits += profit;
				dailyGain += profit;
				profitList.Add(profits);
				profitsS += profit;
				profitListS.Add(profitsS);
				Print(Time[0] + "\t Short loss of " + profit + " \tTotal profit " + profits);
				Draw.Dot(this, "stopHitS"+CurrentBar, false, 0, stopPriceS, Brushes.Crimson);
			}
		}
		
		private void setTrend() {	
			if (smaFast[0] >= smaSlow[0]) {
				trend = 1;
				if ( showBkg && showGreen)
					BackBrush  = new SolidColorBrush(Colors.DarkGreen) {Opacity = Opacity};
			}
			if (smaFast[0] < smaSlow[0]) {
				trend = -1;
				if ( showBkg )
					BackBrush  = new SolidColorBrush(Colors.DarkRed) {Opacity = Opacity};
			}
			if (!MAFilter) {
				trend = 0;
			}
		}
		
		private void showTargets() {
			lastBar = CurrentBar - 1;
			if (inTradeLong && Longs) {
				barsinTradeLong += 1;
				RemoveDrawObject("targetPrice" + lastBar);
				Draw.Line(this, "targetPrice" + CurrentBar, barsinTradeLong, targetPrice, 0, targetPrice, Brushes.DodgerBlue);
				RemoveDrawObject("stopPrice" + lastBar);
				Draw.Line(this, "stopPrice" + CurrentBar, barsinTradeLong, stopPrice, 0, stopPrice, Brushes.Crimson);
			} else {
				barsinTradeLong = 0;	
			}
			if (inTradeShort && Shorts) {
				barsinTradeShort += 1;
				RemoveDrawObject("targetPriceS" + lastBar);
				Draw.Line(this, "targetPriceS" + CurrentBar, barsinTradeShort, targetPriceS, 0, targetPriceS, Brushes.DodgerBlue);
				RemoveDrawObject("stopPriceS" + lastBar);
				Draw.Line(this, "stopPriceS" + CurrentBar, barsinTradeShort, stopPriceS, 0, stopPriceS, Brushes.Crimson);
			} else {
				barsinTradeShort = 0;	
			}
		}
		
		private void stats() {
			
			if (CurrentBar < Count -2) return;
			double maxProfit = 0.0;
			double minProfit = 0.0;
			if (profitList.Count > 0) {
				maxProfit = profitList.Max();
				minProfit = profitList.Min();
			}
		
			double maxProfitS = 0.0;
			double minProfitS = 0.0;
			double allProfitS = 0.0;
			if (profitListS.Count > 0) {
				maxProfitS = profitListS.Max();
				minProfitS = profitListS.Min();
				allProfitS = profitListS.Last();
				//Print(String.Join(", ", profitListS));
			}
			
			double maxProfitL = 0.0;
			double minProfitL = 0.0;
			double allProfitL = 0.0;
			if (profitListL.Count > 0) {
				maxProfitL = profitListL.Max();
				minProfitL = profitListL.Min();
				allProfitL = profitListL.Last();
				//Print(String.Join(", ", profitListL));
			}
			
			double maxProfitD = 0.0;
			double minProfitD = 0.0;
			double avgProfitD = 0.0;
			if (dailyGainList.Count > 0) {
				maxProfitD = dailyGainList.Max();
				minProfitD = dailyGainList.Min();
				avgProfitD = dailyGainList.Average();
			}
			
			double winPct = (Convert.ToDouble(winCount) / Convert.ToDouble(tradeCount)) * 100;
			double roi = (profits / 6500.0) * 100;
			string formattedMoneyValue = String.Format("{0:C}", profits);
			string message = formattedMoneyValue + "\n" 
				+ tradeCount + " trades" 
				+ "\n" + dayCount + " days"
				+ "\n" + winCount + " wins"
				+ "\n" + winPct.ToString("N1") + "% win"  
				+ "\n" + roi.ToString("N1") + "% roi"
				+ "\n$" + maxProfit.ToString("N0") + " : $" + minProfit.ToString("N0")
				+ "\n\nShort $" + allProfitS
				+ "\n$" + maxProfitS.ToString("N0") + " : $" + minProfitS.ToString("N0")
				+ "\n\nLong $" + allProfitL
				+ "\n$" + maxProfitL.ToString("N0") + " : $" + minProfitL.ToString("N0")
				+ "\n\navg day $" + avgProfitD.ToString("N0") 
				+ "\n$" + maxProfitD.ToString("N0") + " : $" + minProfitD.ToString("N0");
			Draw.TextFixed(this, "profits", message, TextPosition.BottomLeft);
			//Print(""); Print(message); Print("");
			
		}
		
		private void sendAlert(string message, string sound ) {
			if (CurrentBar < Count -2) return;
			message += " " + Bars.Instrument.MasterInstrument.Name;
			Alert("myAlert"+CurrentBar, Priority.High, message, NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ sound,10, Brushes.Black, Brushes.Yellow);  
			//if (CurrentBar < Count -2) return;
			//SendMailChart(name + " Alert",message,"ticktrade10@gmail.com","13103824522@tmomail.net","smtp.gmail.com",587,"ticktrade10","WH2403wh");
			//SendMail("13103824522@tmomail.net", "Trade Alert", message);
		}
		
		#region Properties
		
		[NinjaScriptProperty]
		[Display(Name="Longs", Order=1, GroupName="Parameters")]
		public bool Longs
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Shorts", Order=2, GroupName="Parameters")]
		public bool Shorts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="MAFilter", Order=3, GroupName="Parameters")]
		public bool MAFilter
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(Name="Paint Trend On Background", Order=1, GroupName="Display")]
		public bool showBkg
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.1, double.MaxValue)]
		[Display(Name="Background Opacity", Order=1, GroupName="Display")]
		public double Opacity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Bullis Black Background", Order=3, GroupName="Display")]
		public bool showGreen
		
			
		{ get; set; }
		[NinjaScriptProperty]
		[Display(Name="Alert For PBS", Order=1, GroupName="Alerts")]
		public string AlertPBS
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Alert For PSS", Order=2, GroupName="Alerts")]
		public string AlertPSS
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Alert For Trade Approaching", Order=3, GroupName="Alerts")]
		public string AlertApproach
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, double.MaxValue)]
		[Display(Name="Max Daily Gain", Description="Max Daily Gain", Order=1, GroupName="Money Management")]
		public double maxGain
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, double.MaxValue)]
		[Display(Name="Max Daily Loss", Description="Max Daily Loss", Order=2, GroupName="Money Management")]
		public double maxLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, int.MaxValue)]
		[Display(Name="Target Ticks", Description="Target Ticks", Order=3, GroupName="Money Management")]
		public int targetTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, int.MaxValue)]
		[Display(Name="Stop Ticks", Description="Stop Ticks", Order=4, GroupName="Money Management")]
		public int stopTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, int.MaxValue)]
		[Display(Name="BreakEven Ticks", Description="BreakEven Ticks", Order=5, GroupName="Money Management")]
		public int breakEvenTicks
		{ get; set; } 
		
		[NinjaScriptProperty]
		[Display(Name="Use BreakEven", Order=6, GroupName="Money Management")]
		public bool useBreakEven
		{ get; set; }
		
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PBS[] cachePBS;
		public PBS PBS(bool longs, bool shorts, bool mAFilter, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return PBS(Input, longs, shorts, mAFilter, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}

		public PBS PBS(ISeries<double> input, bool longs, bool shorts, bool mAFilter, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			if (cachePBS != null)
				for (int idx = 0; idx < cachePBS.Length; idx++)
					if (cachePBS[idx] != null && cachePBS[idx].Longs == longs && cachePBS[idx].Shorts == shorts && cachePBS[idx].MAFilter == mAFilter && cachePBS[idx].showBkg == showBkg && cachePBS[idx].Opacity == opacity && cachePBS[idx].showGreen == showGreen && cachePBS[idx].AlertPBS == alertPBS && cachePBS[idx].AlertPSS == alertPSS && cachePBS[idx].AlertApproach == alertApproach && cachePBS[idx].maxGain == maxGain && cachePBS[idx].maxLoss == maxLoss && cachePBS[idx].targetTicks == targetTicks && cachePBS[idx].stopTicks == stopTicks && cachePBS[idx].breakEvenTicks == breakEvenTicks && cachePBS[idx].useBreakEven == useBreakEven && cachePBS[idx].EqualsInput(input))
						return cachePBS[idx];
			return CacheIndicator<PBS>(new PBS(){ Longs = longs, Shorts = shorts, MAFilter = mAFilter, showBkg = showBkg, Opacity = opacity, showGreen = showGreen, AlertPBS = alertPBS, AlertPSS = alertPSS, AlertApproach = alertApproach, maxGain = maxGain, maxLoss = maxLoss, targetTicks = targetTicks, stopTicks = stopTicks, breakEvenTicks = breakEvenTicks, useBreakEven = useBreakEven }, input, ref cachePBS);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PBS PBS(bool longs, bool shorts, bool mAFilter, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBS(Input, longs, shorts, mAFilter, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}

		public Indicators.PBS PBS(ISeries<double> input , bool longs, bool shorts, bool mAFilter, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBS(input, longs, shorts, mAFilter, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PBS PBS(bool longs, bool shorts, bool mAFilter, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBS(Input, longs, shorts, mAFilter, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}

		public Indicators.PBS PBS(ISeries<double> input , bool longs, bool shorts, bool mAFilter, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBS(input, longs, shorts, mAFilter, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}
	}
}

#endregion
