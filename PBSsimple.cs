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
	public class PBSsimple : Indicator
	{
        private int hhCount = 0;
        private int llCount = 0;
        private int trend = 0;
        private SMA smaFast;
        private SMA smaSlow;
        private bool showTrend = false;
        private bool showCount = false;
        private bool showTradeApproaching = true;
        private string name = "";
        private int lastBar = 0;
        private int startTime = 930;
        private int endTime = 1600;
        private int ninja_Start_Time;
        private int ninja_End_Time;

        /// trade management
        private double entryPrice = 0.0;
        private double targetPrice = 0.0;
        private double breakevenPrice = 0.0;
        private double stopPrice = 0.0;
        private double entryPriceS = 0.0;
        private double targetPriceS = 0.0;
        private double breakevenPriceS = 0.0;
        private double stopPriceS = 0.0;

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

        struct ListStats
        {
            public double ListMin;
            public double ListMax;
            public double ListLast;
            public double ListAvg;

            public ListStats(double listMin, double listMax, double listLast, double listAvg)
            {
                ListMin = listMin;
                ListMax = listMax;
                ListLast = listLast;
                ListAvg = listAvg;
            }
        };

        /// daily profit loss
        private double dailyGain = 0;
        private int dayCount = 0;
        List<double> dailyGainList = new List<double>();

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Enter the description for your new custom Indicator here.";
                Name = "PBS Simple";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right; 
                IsSuspendedWhileInactive = true;
                Longs = true;
                Shorts = true;
                MAFilter = true;
                showBkg = true;
                Opacity = 0.1;
                AlertPBS = @"ES_EnteringLongZone.wav";
                AlertPSS = @"ES_EnteringShortZone.wav";
                AlertApproach = @"ES_ZoneTrigger.wav";
                showGreen = false;
                maxLoss = 500.0;
                maxGain = 500.0;
                targetTicks = 8;
                stopTicks = 8;
                breakEvenTicks = 7;
                useBreakEven = false;
                deBug = false;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                smaFast = SMA(20);
                smaSlow = SMA(40);
                ninja_Start_Time = startTime * 100;
                ninja_End_Time = endTime * 100;
                ClearOutputWindow();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 20) { return; }
            name = Bars.Instrument.MasterInstrument.Name;
            setDayLossMax();
            if (dailyGain <= -maxLoss || dailyGain >= maxGain) { return; }
            showTargets();
            setTrend();
            setPSS();
            setPBS();
            manageLongTrade();
            manageShortTrade();
            stats();
        }

        private void setDayLossMax()
        {
            if (Bars.IsFirstBarOfSession)
            {
                dayCount += 1;
                dailyGainList.Add(dailyGain);
                dailyGain = 0;
            }
        }

        private void setPSS()
        {
            if (!Shorts) { return; }
            /// setup
            if ((High[0] > High[1] || Low[0] > Low[1]) && trend <= 0)
            {
                hhCount += 1;
                if (trend <= 0 && showCount)
                    Draw.Text(this, "HH" + CurrentBar, hhCount.ToString(), 0, Low[0] - 1 * TickSize, Brushes.Red);
                if (showTradeApproaching && hhCount >= 3 && trend <= 0)
                {
                    RemoveDrawObject("shortClose" + lastBar);
                    Draw.TriangleDown(this, "shortClose" + CurrentBar, true, 0, High[0], Brushes.Gray);
                    sendAlert(message: "Setting Up Sell on " + name, sound: AlertPSS);
                }
            }
            /// entry 
            if (!inTradeShort && !inTradeLong && Low[0] < Low[1])
            {
                if (hhCount >= 3 && trend <= 0)
                {
                    hhCount = 0;
                    llCount = 0;
                    Draw.ArrowDown(this, "PSSb" + CurrentBar, false, 0, High[0] + 1 * TickSize, Brushes.Crimson);
                    tradeCount += 1;
                    entryPriceS = Low[1];
                    inTradeShort = true;
                    targetPriceS = entryPriceS - (targetTicks * TickSize);
                    stopPriceS = entryPriceS + (stopTicks * TickSize);
                    breakevenPriceS = entryPriceS - (breakEvenTicks * TickSize);
                    if (deBug) { Print("Short Entry " + entryPriceS + " \tTarget " + targetPriceS + " \tStop " + stopPriceS); }
                    sendAlert(message: "Sell " + name, sound: AlertApproach);
                }
            }
        }

        private void setPBS()
        {
            if (!Longs) { return; }
            /// setup
            if ((High[0] < High[1] || Low[0] < Low[1]) && trend >= 0)
            {
                llCount += 1;
                if (trend >= 0 && showCount)
                    Draw.Text(this, "LL" + CurrentBar, llCount.ToString(), 0, High[0] + 1 * TickSize, Brushes.DodgerBlue);
                if (showTradeApproaching && llCount >= 3 && trend >= 0)
                {
                    RemoveDrawObject("longClose" + lastBar);
                    Draw.TriangleUp(this, "longClose" + CurrentBar, true, 0, Low[0], Brushes.Gray);
                    sendAlert(message: "Setting Up Buy on " + name, sound: AlertPBS);
                }
            }
            /// entry
            if (!inTradeShort && !inTradeLong && High[0] > High[1])
            {
                if (llCount >= 3 && trend >= 0)
                {
                    llCount = 0;
                    hhCount = 0;
                    Draw.ArrowUp(this, "PBSb" + CurrentBar, false, 0, Low[0] - 1 * TickSize, Brushes.LimeGreen);
                    tradeCount += 1;
                    entryPrice = High[1];
                    inTradeLong = true;
                    targetPrice = entryPrice + (targetTicks * TickSize);
                    stopPrice = entryPrice - (stopTicks * TickSize);
                    breakevenPrice = entryPrice + (breakevenPrice * TickSize);
                    if (deBug) { Print("Long Entry " + entryPrice + " \tTarget " + targetPrice + " \tStop " + stopPrice); }
                    sendAlert(message: "Buy " + name, sound: AlertApproach);
                }
            }
        }

        private void manageLongTrade()
        {
            if (!Longs) { return; }
            if (!inTradeLong) { return; }
            /// target hit
            if (High[0] >= targetPrice)
            {
                inTradeLong = false;
                llCount = 0;
                winCount += 1;
                double profit = (targetTicks * TickSize) * Bars.Instrument.MasterInstrument.PointValue;
                profits += profit;
                dailyGain += profit;
                profitList.Add(profits);
                profitsL += profit;
                profitListL.Add(profitsL);
                if (deBug) { Print(Time[0] + "\t Long gain of " + profit + " \tTotal profit " + profits); }
                Draw.Dot(this, "targetHit" + CurrentBar, false, barsinTradeLong, targetPrice, Brushes.DodgerBlue);
                return;
            }
            /// break even hit
            if (High[0] >= breakevenPrice && useBreakEven)
            {
                stopPrice = entryPrice;
                if (deBug) { Print(Time[0] + "\t Long Break Even Hit "); }
                Draw.Dot(this, "breakeven" + CurrentBar, false, barsinTradeLong, breakevenPrice, Brushes.White);
                return;
            }
            /// stop hit
            if (Low[0] <= stopPrice && barsinTradeLong > 0)
            {
                inTradeLong = false;
                llCount = 0;
                double profit = ((stopPrice - entryPrice)) * Bars.Instrument.MasterInstrument.PointValue;
                if (deBug) { Print("--> \tEntry " + entryPrice + " - stop " + stopPrice + " =  loss of $" + profit); }
                if (profit == 0) { winCount += 1; }
                profits += profit;
                dailyGain += profit;
                profitList.Add(profits);
                profitsL += profit;
                profitListL.Add(profitsL);
                if (deBug) { Print(Time[0] + "\t Long loss of " + profit + " \tTotal profit " + profits); }
                Draw.Dot(this, "stopHit" + CurrentBar, false, barsinTradeLong, stopPrice, Brushes.Crimson);
            }
        }

        private void manageShortTrade()
        {
            if (!Shorts) { return; }
            if (!inTradeShort) { return; }
            /// target hit
            if (Low[0] <= targetPriceS)
            {
                inTradeShort = false;
                hhCount = 0;
                winCount += 1;
                double profit = (targetTicks * TickSize) * Bars.Instrument.MasterInstrument.PointValue;
                profits += profit;
                dailyGain += profit;
                profitList.Add(profits);
                profitsS += profit;
                profitListS.Add(profitsS);
                if (deBug) { Print(Time[0] + "\t Short gain of " + profit + " \tTotal profit " + profits); }
                Draw.Dot(this, "targetHitS" + CurrentBar, false, barsinTradeShort, targetPriceS, Brushes.DodgerBlue);
                return;
            }
            /// break even hit
            if (Low[0] <= breakevenPriceS && useBreakEven)
            {
                stopPriceS = entryPriceS;
                if (deBug) { Print(Time[0] + "\t Short Break Even Hit "); }
                Draw.Dot(this, "breakevenS" + CurrentBar, false, 0, breakevenPriceS, Brushes.White);
                return;
            }
            /// stop hit
            if (High[0] >= stopPriceS && barsinTradeShort > 0)
            {
                inTradeShort = false;
                hhCount = 0;
                double profit = ((entryPriceS - stopPriceS)) * Bars.Instrument.MasterInstrument.PointValue;
                if (deBug) { Print("--> \tEntry " + entryPrice + " - stop " + stopPrice + " =  loss of $" + profit); }
                if (profit == 0) { winCount += 1; }
                profits += profit;
                dailyGain += profit;
                profitList.Add(profits);
                profitsS += profit;
                profitListS.Add(profitsS);
                if (deBug) { Print(Time[0] + "\t Short loss of " + profit + " \tTotal profit " + profits); }
                Draw.Dot(this, "stopHitS" + CurrentBar, false, 0, stopPriceS, Brushes.Crimson);
            }
        }

        private void setTrend()
        {
            if (smaFast[0] >= smaSlow[0])
            {
                trend = 1;
                if (showBkg && showGreen)
                    BackBrush = new SolidColorBrush(Colors.DarkGreen) { Opacity = Opacity };
            }
            if (smaFast[0] < smaSlow[0])
            {
                trend = -1;
                if (showBkg)
                    BackBrush = new SolidColorBrush(Colors.DarkRed) { Opacity = Opacity };
            }
            if (!MAFilter)
            {
                trend = 0;
            }
        }

        private void showTargets()
        {
            lastBar = CurrentBar - 1;
            if (inTradeLong && Longs)
            {
                barsinTradeLong += 1;
                RemoveDrawObject("targetPrice" + lastBar);
                Draw.Line(this, "targetPrice" + CurrentBar, barsinTradeLong, targetPrice, 0, targetPrice, Brushes.DodgerBlue);
                RemoveDrawObject("stopPrice" + lastBar);
                Draw.Line(this, "stopPrice" + CurrentBar, barsinTradeLong, stopPrice, 0, stopPrice, Brushes.Crimson);
            }
            else
            {
                barsinTradeLong = 0;
            }
            if (inTradeShort && Shorts)
            {
                barsinTradeShort += 1;
                RemoveDrawObject("targetPriceS" + lastBar);
                Draw.Line(this, "targetPriceS" + CurrentBar, barsinTradeShort, targetPriceS, 0, targetPriceS, Brushes.DodgerBlue);
                RemoveDrawObject("stopPriceS" + lastBar);
                Draw.Line(this, "stopPriceS" + CurrentBar, barsinTradeShort, stopPriceS, 0, stopPriceS, Brushes.Crimson);
            }
            else
            {
                barsinTradeShort = 0;
            }
        }

        private ListStats CalcFor(List<double> list)
        {
            ListStats stats = new ListStats(0.0, 0.0, 0.0, 0.0);
            if (list.Count > 0)
            {
                stats.ListMin = list.Min();
                stats.ListMax = list.Max();
                stats.ListLast = list.Last();
                stats.ListAvg = list.Average();
                return stats;
            }
            else { return stats; }
        }

        private void stats()
        {
            /*
             When CalculateOnBarClose = true: and not RTH
            if (Count - 2 == CurrentBar) 
            When CalculateOnbarClose = false:
            if (Count - 1 == CurrentBar)
            */
            if (Count - 2 != CurrentBar) { return; }
            double minProfit = CalcFor(list: profitList).ListMin;
            double maxProfit = CalcFor(list: profitList).ListMax;
            double minProfitS = CalcFor(list: profitListS).ListMin;
            double maxProfitS = CalcFor(list: profitListS).ListMax;
            double allProfitS = CalcFor(list: profitListS).ListLast;
            double minProfitL = CalcFor(list: profitListL).ListMin;
            double maxProfitL = CalcFor(list: profitListL).ListMax;
            double allProfitL = CalcFor(list: profitListL).ListLast;
            double minProfitD = CalcFor(list: dailyGainList).ListMin;
            double maxProfitD = CalcFor(list: dailyGainList).ListMax;
            double avgProfitD = CalcFor(list: dailyGainList).ListAvg;
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
        }

        private void sendAlert(string message, string sound)
        {
            if (CurrentBar < Count - 2) return;
            message += " " + Bars.Instrument.MasterInstrument.Name;
            Alert("myAlert" + CurrentBar, Priority.High, message, NinjaTrader.Core.Globals.InstallDir + @"\sounds\" + sound, 10, Brushes.Black, Brushes.Yellow);
            SendMail("13103824522@tmomail.net", "Trade Alert", message);
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
		[Display(Name="De Bug", Order=4, GroupName="Parameters")]
		public bool deBug
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
		[Range(1, int.MaxValue)]
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
		private PBSsimple[] cachePBSsimple;
		public PBSsimple PBSsimple(bool longs, bool shorts, bool mAFilter, bool deBug, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return PBSsimple(Input, longs, shorts, mAFilter, deBug, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}

		public PBSsimple PBSsimple(ISeries<double> input, bool longs, bool shorts, bool mAFilter, bool deBug, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			if (cachePBSsimple != null)
				for (int idx = 0; idx < cachePBSsimple.Length; idx++)
					if (cachePBSsimple[idx] != null && cachePBSsimple[idx].Longs == longs && cachePBSsimple[idx].Shorts == shorts && cachePBSsimple[idx].MAFilter == mAFilter && cachePBSsimple[idx].deBug == deBug && cachePBSsimple[idx].showBkg == showBkg && cachePBSsimple[idx].Opacity == opacity && cachePBSsimple[idx].showGreen == showGreen && cachePBSsimple[idx].AlertPBS == alertPBS && cachePBSsimple[idx].AlertPSS == alertPSS && cachePBSsimple[idx].AlertApproach == alertApproach && cachePBSsimple[idx].maxGain == maxGain && cachePBSsimple[idx].maxLoss == maxLoss && cachePBSsimple[idx].targetTicks == targetTicks && cachePBSsimple[idx].stopTicks == stopTicks && cachePBSsimple[idx].breakEvenTicks == breakEvenTicks && cachePBSsimple[idx].useBreakEven == useBreakEven && cachePBSsimple[idx].EqualsInput(input))
						return cachePBSsimple[idx];
			return CacheIndicator<PBSsimple>(new PBSsimple(){ Longs = longs, Shorts = shorts, MAFilter = mAFilter, deBug = deBug, showBkg = showBkg, Opacity = opacity, showGreen = showGreen, AlertPBS = alertPBS, AlertPSS = alertPSS, AlertApproach = alertApproach, maxGain = maxGain, maxLoss = maxLoss, targetTicks = targetTicks, stopTicks = stopTicks, breakEvenTicks = breakEvenTicks, useBreakEven = useBreakEven }, input, ref cachePBSsimple);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PBSsimple PBSsimple(bool longs, bool shorts, bool mAFilter, bool deBug, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBSsimple(Input, longs, shorts, mAFilter, deBug, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}

		public Indicators.PBSsimple PBSsimple(ISeries<double> input , bool longs, bool shorts, bool mAFilter, bool deBug, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBSsimple(input, longs, shorts, mAFilter, deBug, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PBSsimple PBSsimple(bool longs, bool shorts, bool mAFilter, bool deBug, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBSsimple(Input, longs, shorts, mAFilter, deBug, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}

		public Indicators.PBSsimple PBSsimple(ISeries<double> input , bool longs, bool shorts, bool mAFilter, bool deBug, bool showBkg, double opacity, bool showGreen, string alertPBS, string alertPSS, string alertApproach, double maxGain, double maxLoss, int targetTicks, int stopTicks, int breakEvenTicks, bool useBreakEven)
		{
			return indicator.PBSsimple(input, longs, shorts, mAFilter, deBug, showBkg, opacity, showGreen, alertPBS, alertPSS, alertApproach, maxGain, maxLoss, targetTicks, stopTicks, breakEvenTicks, useBreakEven);
		}
	}
}

#endregion
