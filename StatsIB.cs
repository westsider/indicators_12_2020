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
using System.IO;
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
	public class StatsIB : Indicator
	{
		private double Open_D = 0.0;
		private double Close_D = 0.0;
		private double Gap_D = 0.0;
		private string message = "no message";
		private long startTime = 0;
		private	long endTime = 0;
		private	long ibTime = 0;
		private int openingBar = 0;
		
		private int RTH_Counter = 0; 
		private double Y_High = 0.0;
		private double Y_Low = 0.0;
		
		private int GX_Counter = 0;
		private double IB_Low = 0.0;
		private double IB_High = 0.0;
        private double IB_Mid = 0.0;
        private double IB_Vol = 0.0;
		private string path;
		private int ibLength = 0;
		private int dayCount = 0;
		private int inRangeCount = 0;
		private int lastBar = 0;
		private bool insideYestRange = false;
		// stats
		private List<double> iBRanges = new List<double>();
		private List<double> sessionRanges = new List<double>();
		private List<double> volumeRanges = new List<double>();
		private bool inRange = false;
		private int [] breaks = new int[2]  { 0,0};
        private int[] breakCount = new int[2] { 0, 0 };
        private List<double> breaksList = new List<double>();
		private Tuple<double, double> ibRangeValue = Tuple.Create(0.0, 0.0);
		private int hiBreakCount = 0;
        private int lowBreakCount = 0;

        // debugging
        private bool debug = false;
		private string stringDate = "";
		private bool isHoliday = false;
		private double ibRange = 0.0;
		private string[] holidays = new string[] { "1/1/2020", "1/20/2020", "2/17/2020", "4/10/2020", "5/25/2020", 
			"7/3/2020", "9/7/2020", "11/26/2020", "12/25/2020", "1/1/2019", "1/21/2019", "2/18/2019", "4/19/2019", "5/27/2019", 
			"7/4/2019", "9/2/2019", "11/28/2019", "12/25/2019"};

        // hit targets
        private bool midTargetHit = false;
        private int midTargetHitCount = 0;

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Stats IB";
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
				RTHopen						= DateTime.Parse("06:31", System.Globalization.CultureInfo.InvariantCulture);
				IB							= DateTime.Parse("07:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHclose					= DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
				path 						= NinjaTrader.Core.Globals.UserDataDir + "JigsawLevels.csv";
				MinIB 						= 2.75;
				MinRange					= 4.75;
				CalcIB						= true;
				CalcRange					= false;
				CalcVolume					= false;
				CurrentDayOnly				= true;
				
				BackgroundColor			= Brushes.DimGray;
				BackgroundOpacity 		= 90;
				FontColor				= Brushes.WhiteSmoke;
				OutlineColor			= Brushes.DimGray;
				NoteFont				= new SimpleFont("Arial", 12);
			}
			else if (State == State.Configure)
			{
				startTime = long.Parse(RTHopen.ToString("HHmmss"));
			 	endTime = long.Parse(RTHclose.ToString("HHmmss"));
				ibTime = long.Parse(IB.ToString("HHmmss"));
				AddDataSeries(Data.BarsPeriodType.Minute, 1);
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{

			if ( CurrentBar < 5 ) { return; }
            if (BarsInProgress == 0) { lastBar = CurrentBar - 1; }
			if ("Sunday"  == Time[0].DayOfWeek.ToString()) { return; }
			holidayCheck(debug: false); 
			openData();
			ibData();
			if (BarsInProgress == 1 && ToTime(Time[0]) == endTime ) { 
				isHoliday = false;
				Close_D = Close[0];
                midTargetHit = false;
			}
			rangeData();
			countIbBreak(debug: true);
            drawIBLines();
            calcStats();

            if (!midTargetHit && breaks[0] == 1 && Low[0] <= IB_Mid)
            {
                Draw.Dot(this, "target" + CurrentBar, false, 0, IB_Mid, Brushes.WhiteSmoke);
                midTargetHitCount += 1;
                midTargetHit = true;
            }
        }

		private void countIbBreak(bool debug) {
			// after 7:30
			if (BarsInProgress == 0 && ToTime(Time[0]) > ibTime  && ToTime(Time[0]) < endTime  ) { 
				if ( High[0] > IB_High ) {
                    if (breaks[0] == 0) {
                        if (debug) { Draw.Dot(this, "breakHi" + CurrentBar, false, 0, IB_High, FontColor); }
                    }
					breaks[0] = 1;
				} 
				if ( Low[0] < IB_Low) {
                    if (breaks[1] == 0)
                    {
                        if (debug) { Draw.Dot(this, "breakLo" + CurrentBar, false, 0, IB_Low, FontColor); }
                    }
					breaks[1] = 1;
				}
			}

            countOneIBCloses(debug: false);

            if (BarsInProgress == 0  && ToTime(Time[0]) == endTime  ) 
			{
				breaksList.Add(breaks.Sum());
				breaks[0] = 0;
				breaks[1] = 0;
			}
			
			if (CurrentBar < Count -2 || Time[0].Date != DateTime.Today) { return; }
			if ( BarsInProgress == 0 && debug ) { 
				printList(arr: breaksList, title: "Breaks"); 
				var zero = breaksList.Where(num => num == 0);
				var one = breaksList.Where(num => num == 1);
				var two = breaksList.Where(num => num == 2); 
				double oneCount = one.Count();
                double twoCount = two.Count();
                double zeroCount = zero.Count();
                double listCount = breaksList.Count();
				double oneBreak = (oneCount / listCount) * 100;
				double twoBreak = (twoCount / listCount) * 100;
                double zeroBreak = (zeroCount / listCount) * 100;
                Print("One IB Break " + oneBreak.ToString("N1") + "%\tTwo IB Break " + twoBreak.ToString("N1") 
                    + "%\tZero IB Break " + zeroBreak.ToString("N1") + "%");

                double hiCloseAndBreakPct = (Convert.ToDouble(breakCount[0]) / Convert.ToDouble(hiBreakCount) ) * 100;
                double lowCloseAndBreakPct = (Convert.ToDouble(breakCount[1]) / Convert.ToDouble(lowBreakCount)) * 100;
                Print("Hi Closes " + breakCount[0] + " / " + " Hi break " + hiBreakCount + " = " + hiCloseAndBreakPct + "\t\tHigh IB break results in Above IB close: " + hiCloseAndBreakPct.ToString("N1") + "%");
                Print("Low Closes " + breakCount[1] + " / " + " Low break " + lowBreakCount + " = " + lowCloseAndBreakPct + "\t\tLow IB break results in Below IB close: " + lowCloseAndBreakPct.ToString("N1") + "%");
                Print("midTargetHitCount " + midTargetHitCount);
            }
			
		}

        private void countOneIBCloses(bool debug)
        {
			
			// ANOTHER Last Bar attempt if ( CurrentBar < Bars.Count - 2 || CurrentBar == Bars.Count - 1 ) 
			
			
            // at close 
            if (BarsInProgress == 0 && ToTime(Time[0]) == endTime)
            {
                
                //if 1 IB break hi
                if (breaks[0] == 1 && breaks[1] == 0)
                {
                    if (debug) { Print("Hi break"); }
                    hiBreakCount += 1;
                    if ( Close[0] > IB_High )
                    {
                        breakCount[0] += 1;
                    }
                }

                //if 1 IB break low
                if (breaks[0] == 0 &&  breaks[1] == 1)
                {
                    if (debug)  { Print("Low break"); }
                    lowBreakCount += 1;
                    if (Close[0] < IB_Low)
                    {
                        breakCount[1] += 1;
                    }
                }
                // where is close
                if (debug) { printArray(arr: breakCount, title: "Hi break = Hi close"); }
            }


        }

        private static double StandardDeviation(IEnumerable<double> values)
	    {
	        double avg = values.Average();
	        return Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));
	    }
	
		private void holidayCheck(bool debug) {
			if (BarsInProgress == 0 && Bars.IsFirstBarOfSession) { 
				foreach(string holiday in holidays)
				{
					if ( Time[0].ToShortDateString() == holiday) {
						if ( debug ) { Print("\t\t\tFound Holiday on " + Time[0].ToShortDateString() 	); }
						isHoliday = true;
					} 
				}
			}
		}
		
		private void openData() {
			// Open
			if (BarsInProgress == 1)
				if (ToTime(Time[0]) == startTime ) { 
					stringDate = Time[0].ToShortDateString();
					//Print(stringDate);
					Open_D = Open[0];
					Gap_D = Open_D - Close_D;
					dayCount += 1;
					openingBar = CurrentBars[0];
					message =  Time[0].ToShortDateString() + "\t"  + Time[0].ToShortTimeString() + "\tOpen: " + Open_D.ToString() +  "\tGap: " + Gap_D.ToString();
					if ( Open_D > Y_Low  && Open_D < Y_High) {
						message += "\tOpen In Range";
						inRangeCount += 1;	
					} else {
						message += "\tOpen Outside Range";	
					}
					message += "\tday " + dayCount + "\tin range\t" + inRangeCount;
					if( debug ) { 
						Print(message); 
					}
					if (Open_D <= Y_High && Open_D >= Y_Low) {
						insideYestRange = true;
					} else {
						insideYestRange = false;	
					}
				}
		}
		
		private void ibData() {
			// IB 
			if ( ToTime(Time[0]) == ibTime ) { 
				if( BarsInProgress == 0 ) {
					ibLength = CurrentBars[0] - openingBar;
					ibVolume(length: ibLength, debug: false);
				}

				if( BarsInProgress == 0 ) {
					if ( ibLength> 0 ) {
						IB_Low = MIN(Low, ibLength)[0];
						IB_High = MAX(High, ibLength)[0];
                        IB_Mid = ((IB_High - IB_Low) / 2) + IB_Low;
                        //Draw.Dot(this, "ibMid" + CurrentBar, false, 0, IB_Mid, Brushes.Yellow);
                        ibRange = IB_High - IB_Low;
						if ( ibRange > MinIB  && ibRange < 50 && !isHoliday) {
							iBRanges.Add(ibRange); 
							//if ( range <= 4 ) { Print("Found " + range + " on " + stringDate + " " + Time[0].DayOfWeek.ToString()); }
						}
					}
					if ( openingBar > 0 ) {
						if (Time[0].Date == DateTime.Today && CurrentDayOnly) {
                            

                            

							if ( insideYestRange ) {
								Draw.Text(this, "insideYestRange"+CurrentBar, "inside Y range", 3, IB_High + 1 * TickSize, Brushes.Black);
                                drawRectLong(name: "IB", Top: IB_High, Bottom: IB_Low, Length: ibLength, colors: Brushes.DimGray);
                            } else {
								Draw.Text(this, "insideYestRange"+CurrentBar, "outside Y range", 3, IB_High + 1 * TickSize, Brushes.Black);
                                drawRectLong(name: "IB", Top: IB_High, Bottom: IB_Low, Length: ibLength, colors: Brushes.Goldenrod);
                            }
						} 
						if ( !CurrentDayOnly) {
							//RemoveDrawObject("IB_Low"+lastBar);
							//Draw.Line(this, "IB_Low"+CurrentBar, false, ibLength, IB_Low, 0, IB_Low, FontColor, DashStyleHelper.Solid, 2);
							//RemoveDrawObject("IB_High"+lastBar);
							//Draw.Line(this, "IB_High"+CurrentBar, false, ibLength, IB_High, 0, IB_High, FontColor, DashStyleHelper.Solid, 2);

                           // drawRectLong(name: "IB", Top: IB_High, Bottom: IB_Low, Length: ibLength, colors: Brushes.DimGray);
                            if ( insideYestRange ) {
								Draw.Text(this, "insideYestRange"+CurrentBar, "inside", 3, IB_High + 1 * TickSize, FontColor);
                                drawRectLong(name: "IB", Top: IB_High, Bottom: IB_Low, Length: ibLength, colors: Brushes.DimGray);
                            } else {
								Draw.Text(this, "insideYestRange"+CurrentBar, "outside", 3, IB_High + 1 * TickSize, FontColor);
                                drawRectLong(name: "IB", Top: IB_High, Bottom: IB_Low, Length: ibLength, colors: Brushes.Goldenrod);
                            }
						}
						
					}
				}
			}
		}

        private void drawRectLong(string name, double Top, double Bottom, int Length, Brush colors)
        {
                RemoveDrawObject("name" + lastBar);
                Draw.Rectangle(this, "name" + CurrentBar, false, Length, Top, 0,
                    Bottom, Brushes.Transparent, colors, 70);
        }

        private void drawIBLines()
        {
            if (BarsInProgress == 0 && ToTime(Time[0]) >= ibTime && ToTime(Time[0]) <= endTime)
            {
                int postIbLength = CurrentBar - openingBar;
                RemoveDrawObject("IB_Low"+lastBar);
                Draw.Line(this, "IB_Low"+CurrentBar, false, postIbLength, IB_Low, 0, IB_Low, FontColor, DashStyleHelper.Solid, 2);
                RemoveDrawObject("IB_High"+lastBar);
                Draw.Line(this, "IB_High"+CurrentBar, false, postIbLength, IB_High, 0, IB_High, FontColor, DashStyleHelper.Solid, 2);
                RemoveDrawObject("IB_Mid" + lastBar);
                Draw.Line(this, "IB_Mid" + CurrentBar, false, postIbLength, IB_Mid, 0, IB_Mid, Brushes.DimGray, DashStyleHelper.Dash, 2);
            }
        }

            private void rangeData() {
			// RTH High - Low
			if (BarsInProgress == 0 && ToTime(Time[0]) >= startTime && ToTime(Time[0]) <= endTime ) {  
				RTH_Counter += 1; 
				if ( CurrentBar > 10) {
					if (Time[0].Date == DateTime.Today && CurrentDayOnly) {
							RemoveDrawObject("yhigh"+lastBar);
							Draw.Line(this, "yhigh"+CurrentBar, false, RTH_Counter, Y_High, 0, Y_High, Brushes.Red, DashStyleHelper.Dot, 4);
							RemoveDrawObject("ylow"+lastBar);
							Draw.Line(this, "ylow"+CurrentBar, false, RTH_Counter, Y_Low, 0, Y_Low, Brushes.DodgerBlue, DashStyleHelper.Dot, 4); 
						} 
					if ( !CurrentDayOnly) {
							RemoveDrawObject("yhigh"+lastBar);
							Draw.Line(this, "yhigh"+CurrentBar, false, RTH_Counter, Y_High, 0, Y_High, Brushes.Red, DashStyleHelper.Dot, 4);
							RemoveDrawObject("ylow"+lastBar);
							Draw.Line(this, "ylow"+CurrentBar, false, RTH_Counter, Y_Low, 0, Y_Low, Brushes.DodgerBlue, DashStyleHelper.Dot, 4); 
						}
					
				}
				if (ToTime(Time[0]) == endTime && RTH_Counter > 0 ) { 
					Y_High = MAX(High, RTH_Counter)[0];
					Y_Low = MIN(Low, RTH_Counter)[0];
					double range = Y_High - Y_Low;
					if ( range > MinRange  && range < 80 && !isHoliday) {
						sessionRanges.Add(range);
					};
					RTH_Counter = 0;
				} 
			}
		}
		
		private void ibVolume(int length, bool debug) {
			
			for (int index = 0; index < length; index++) 
			{
				IB_Vol += Volume[0];
			}
			// round to 10000 then removed last 3 0's converted to k
			decimal rounded = Math.Ceiling((decimal) IB_Vol/10000)*10;// * 10;//*10000;
			volumeRanges.Add(Convert.ToDouble(rounded));
			if ( debug) { Print(Time[0].ToShortDateString() + " " + IB_Vol +  " Rounded "+ rounded); }
			IB_Vol = 0.0;
		}
		
		
		private void calcStats() { 
			if ( CalcIB && BarsInProgress == 0 )  
				printHistogramD(list: iBRanges, title: "IB  Range", location: TextPosition.TopLeft, debug: false);
			
			if ( CalcRange && BarsInProgress == 0 )
				printHistogramD(list: sessionRanges, title: "RTH  Range", location: TextPosition.Center, debug: false);
			
			if ( CalcVolume && BarsInProgress == 0 )
				printHistogramD(list: volumeRanges, title: "IB  Vol", location: TextPosition.TopRight, debug: false);
		}
		
		private void printList(List<double> arr, string title) { 
			string message = "";
			foreach (int i in arr) {  message += i.ToString(); }
			Print("\n----------> " + title + " <----------\n" + message + "\n-------------------------------------");
		}

        private void printArray(int[] arr, string title)
        {
            string message = "";
            foreach (int i in arr) { message += i.ToString(); }
            Print("\n----------> " + title + " <----------\n" + message + "\n-------------------------------------");
        }

        private void printHistogramD(List<double> list, string title, TextPosition location, bool debug) 
		{ 
			if (CurrentBar < Count -2) {
				return;
			}
			List<double> arr =   list;
			//if ( arr.Count < 2 ) { return; }
			if (Time[0].Date != DateTime.Today) { 
				return;
			}
			if ( debug ) { printList(arr: arr, title: title); }
			double lastVlaue = arr.Last();
			arr.Sort();
			Dictionary<double, double> ItemCount = new Dictionary<double, double>();
			double[] items =  arr.ToArray(); 
	
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
	
			string bellCurve = title + "  Histogram\n";
			var modeCount = ItemCount.Values.Max(); 
			var mode = ItemCount.OrderByDescending(x => x.Value).FirstOrDefault().Key;
			double stdDev = StandardDeviation(values: arr);
			double stDevLo = mode - stdDev;
			if ( stDevLo < 3 ) { stDevLo = 3; }
			double stDevHi = mode + stdDev;
			double avg = arr.Average();
			string todaysValues = "";
			
			if ( lastVlaue > stDevLo && lastVlaue < stDevHi ) { 
				inRange = true; 
				todaysValues =  Time[0].ToShortDateString() + "  "+ lastVlaue + "  in range ";
			} else {
				inRange = false;
				todaysValues =  Time[0].ToShortDateString() + "  "+ lastVlaue + "  out range ";	
			}
			
			foreach (KeyValuePair<double,double> res in ItemCount)
			{
				string bar = "";
				if ( Convert.ToInt32( res.Key ) == Convert.ToInt32( avg ) ) {	// average
					for (int index = 0; index < res.Value; index++) 
					{
						bar += "A";
					}
				} else if ( Convert.ToInt32( res.Key ) == Convert.ToInt32( mode ) ) {	// mode
					for (int index = 0; index < res.Value; index++) 
					{
						bar += "M";
					}
				} else if ( res.Key <= stDevLo || res.Key >= stDevHi) {		// value area
					for (int index = 0; index < res.Value; index++) 
					{
						bar += "-";
					} 
				} else {
					for (int index = 0; index < res.Value; index++) 			// all others
					{
						bar += "X";
					}
				}
				
				string h = res.Key.ToString();
				if (h.Count() == 1) {
					h += "_";
				}
				string message = h +"   |\t"+bar;
				if( debug ) { Print(message); }
				bellCurve += message + "\n";
				
				
			}
		
			bellCurve += "Mode " + mode ;
			bellCurve += ", Occurrences " + modeCount + "\n";  
			bellCurve += title + " Average " + avg.ToString("N0") + "\n";
			bellCurve += title + " Std Dev: " + stdDev.ToString("N2") + " L: " + stDevLo.ToString("N2") + " H: " + stDevHi.ToString("N2") + "\n";
			bellCurve += todaysValues;
			if ( debug ) { Print(bellCurve); }
			
			Draw.TextFixed(this, title, 
					bellCurve, 
					location, 
					FontColor,  // text color
					NoteFont, 
					OutlineColor, // outline color
					BackgroundColor, 
					BackgroundOpacity);
			//printList(List<double> arr, string title)
		}  
		

		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHopen", Order=1, GroupName="Parameters")]
		public DateTime RTHopen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="IB", Order=2, GroupName="Parameters")]
		public DateTime IB
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHclose", Order=3, GroupName="Parameters")]
		public DateTime RTHclose
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Minimum IB", Order=4, GroupName="Parameters")]
		public double MinIB
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Minimum Range", Order=5, GroupName="Parameters")]
		public double MinRange
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Line only today", Order=6, GroupName="Parameters")]
		public bool CurrentDayOnly
		{ get; set; } 

		[NinjaScriptProperty]
		[Display(Name="Calc IB", Order=7, GroupName="Parameters")]
		public bool CalcIB
		{ get; set; }
			
		[NinjaScriptProperty]
		[Display(Name="Calc Range", Order=8, GroupName="Parameters")]
		public bool CalcRange
		{ get; set; }
			
		[NinjaScriptProperty]
		[Display(Name="Calc Volume", Order=9, GroupName="Parameters")]
		public bool CalcVolume
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
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private StatsIB[] cacheStatsIB;
		public StatsIB StatsIB(DateTime rTHopen, DateTime iB, DateTime rTHclose, double minIB, double minRange, bool currentDayOnly, bool calcIB, bool calcRange, bool calcVolume, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity)
		{
			return StatsIB(Input, rTHopen, iB, rTHclose, minIB, minRange, currentDayOnly, calcIB, calcRange, calcVolume, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity);
		}

		public StatsIB StatsIB(ISeries<double> input, DateTime rTHopen, DateTime iB, DateTime rTHclose, double minIB, double minRange, bool currentDayOnly, bool calcIB, bool calcRange, bool calcVolume, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity)
		{
			if (cacheStatsIB != null)
				for (int idx = 0; idx < cacheStatsIB.Length; idx++)
					if (cacheStatsIB[idx] != null && cacheStatsIB[idx].RTHopen == rTHopen && cacheStatsIB[idx].IB == iB && cacheStatsIB[idx].RTHclose == rTHclose && cacheStatsIB[idx].MinIB == minIB && cacheStatsIB[idx].MinRange == minRange && cacheStatsIB[idx].CurrentDayOnly == currentDayOnly && cacheStatsIB[idx].CalcIB == calcIB && cacheStatsIB[idx].CalcRange == calcRange && cacheStatsIB[idx].CalcVolume == calcVolume && cacheStatsIB[idx].BackgroundColor == backgroundColor && cacheStatsIB[idx].FontColor == fontColor && cacheStatsIB[idx].OutlineColor == outlineColor && cacheStatsIB[idx].NoteFont == noteFont && cacheStatsIB[idx].BackgroundOpacity == backgroundOpacity && cacheStatsIB[idx].EqualsInput(input))
						return cacheStatsIB[idx];
			return CacheIndicator<StatsIB>(new StatsIB(){ RTHopen = rTHopen, IB = iB, RTHclose = rTHclose, MinIB = minIB, MinRange = minRange, CurrentDayOnly = currentDayOnly, CalcIB = calcIB, CalcRange = calcRange, CalcVolume = calcVolume, BackgroundColor = backgroundColor, FontColor = fontColor, OutlineColor = outlineColor, NoteFont = noteFont, BackgroundOpacity = backgroundOpacity }, input, ref cacheStatsIB);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.StatsIB StatsIB(DateTime rTHopen, DateTime iB, DateTime rTHclose, double minIB, double minRange, bool currentDayOnly, bool calcIB, bool calcRange, bool calcVolume, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity)
		{
			return indicator.StatsIB(Input, rTHopen, iB, rTHclose, minIB, minRange, currentDayOnly, calcIB, calcRange, calcVolume, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity);
		}

		public Indicators.StatsIB StatsIB(ISeries<double> input , DateTime rTHopen, DateTime iB, DateTime rTHclose, double minIB, double minRange, bool currentDayOnly, bool calcIB, bool calcRange, bool calcVolume, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity)
		{
			return indicator.StatsIB(input, rTHopen, iB, rTHclose, minIB, minRange, currentDayOnly, calcIB, calcRange, calcVolume, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.StatsIB StatsIB(DateTime rTHopen, DateTime iB, DateTime rTHclose, double minIB, double minRange, bool currentDayOnly, bool calcIB, bool calcRange, bool calcVolume, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity)
		{
			return indicator.StatsIB(Input, rTHopen, iB, rTHclose, minIB, minRange, currentDayOnly, calcIB, calcRange, calcVolume, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity);
		}

		public Indicators.StatsIB StatsIB(ISeries<double> input , DateTime rTHopen, DateTime iB, DateTime rTHclose, double minIB, double minRange, bool currentDayOnly, bool calcIB, bool calcRange, bool calcVolume, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity)
		{
			return indicator.StatsIB(input, rTHopen, iB, rTHclose, minIB, minRange, currentDayOnly, calcIB, calcRange, calcVolume, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity);
		}
	}
}

#endregion
