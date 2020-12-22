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
	public class SqaureOfNine : Indicator
	{
		
			private int LineId=0,i;
		
			private SolidColorBrush Color0  , Color90 , Color180 , Color270;  //Changed Color to SolidColorBrush.
		
			private double MaxLevelUp=double.MinValue, MinLevelDown=double.MaxValue;
			private int[] Angle0 = new int[] {0,1,10,27,52,85,126,175,232,297,370,451,540,637,742,855,976,1105,1242,1387,1540,1701,1870,2047,2232,2425,2626,2835,3052,3277,3510,3751,4000,4257,4522,4795,5076,5365,5662,5967,6280,6601,6930,7267,7612,7965,8326,8695,9072,9457,9850,10251,10660,11077,11502,11935,12376,12825,13282,13747,14220,14701,15190,15687,16192,16705,17226,17755,18292,18837,19390,19951,20520,21097,21682,22275,22876,23485,24102,24727,25360,26001,26650,27307,27972,28645,29326,30015,30712};
			private int[] Angle90 = new int [] {0,3,14,33,60,95,138,189,248,315,390,473,564,663,770,885,1008,1139,1278,1425,1580,1743,1914,2093,2280,2475,2678,2889,3108,3335,3570,3813,4064,4323,4590,4865,5148,5439,5738,6045,6360,6683,7014,7353,7700,8055,8418,8789,9168,9555,9950,10353,10764,11183,11610,12045,12488,12939,13398,13865,14340,14823,15314,15813,16320,16835,17358,17889,18428,18975,19530,20093,20664,21243,21830,22425,23028,23639,24258,24885,25520,26163,26814,27473,28140,28815,29498,30189,30888};
			private int[] Angle180 = new int [] {0,5,18,39,68,105,150,203,264,333,410,495,588,689,798,915,1040,1173,1314,1463,1620,1785,1958,2139,2328,2525,2730,2943,3164,3393,3630,3875,4128,4389,4658,4935,5220,5513,5814,6123,6440,6765,7098,7439,7788,8145,8510,8883,9264,9653,10050,10455,10868,11289,11718,12155,12600,13053,13514,13983,14460,14945,15438,15939,16448,16965,17490,18023,18564,19113,19670,20235,20808,21389,21978,22575,23180,23793,24414,25043,25680,26325,26978,27639,28308,28985,29670,30363,31064};
			private int[] Angle270 = new int [] {0,7,22,45,76,115,162,217,280,351,430,517,612,715,826,945,1072,1207,1350,1501,1660,1827,2002,2185,2376,2575,2782,2997,3220,3451,3690,3937,4192,4455,4726,5005,5292,5587,5890,6201,6520,6847,7182,7525,7876,8235,8602,8977,9360,9751,10150,10557,10972,11395,11826,12265,12712,13167,13630,14101,14580,15067,15562,16065,16576,17095,17622,18157,18700,19251,19810,20377,20952,21535,22126,22725,23332,23947,24570,25201,25840,26487,27142,27805,28476,29155,29842,30537,31240};
			private double MAX_HIGH = Double.MinValue, MIN_LOW = Double.MaxValue;
			private int ZuluBar, NextBar0,NextBar90,NextBar180,NextBar270, PriceDigits;
			private int lastBar,firstBar;
			private double HighestPaintedPrice,LowestPaintedPrice;
			private string OutString;
			
			
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "SqaureOfNine";
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
				
				MultiplierForPriceScale =1;
				InitialPrice =1;
				
				pOrT =  PriceOrTime.Price;
				
				DisplayCountDown = true;
				
				//NT7, when you set the Date, it will default to midnight, while NT8 will reflect that date plus the current time..
				//Thus, the NT7 version of this indicator will have its time plots drawn slightly off from the NT8 one.
		
				//The line below forces the time associated with the date entered, to midnight.  Using this to conform to NT7 figures.
				//Might be best to be able to specify a time as well as date.
				ZuluTime = DateTime.Now.AddMinutes(-DateTime.Now.Minute).AddHours(-DateTime.Now.Hour).AddSeconds(-DateTime.Now.Second);
								
				Angle000Flag = true;
				Angle090Flag = true;
				Angle180Flag = true;
				Angle270Flag = true;
				
				xColor0x = xColor0.Red;
				xColor90x = xColor90.Blue;
				xColor180x = xColor180.Lime;
				xColor270x = xColor270.Purple;
				
				NumberOfLines = 10;

			}
			else if (State == State.Configure)
			{
				ZuluBar = -1;
			;
			}
			else if( State ==State.DataLoaded)
			{
				
				PriceDigits = Math.Max(0,TickSize.ToString().Length-2);		
				
				#region xColor0xSwitch
				switch (xColor0x)
				{
					case xColor0.Blue:
					{
						 Color0 = (SolidColorBrush)Brushes.Blue;
						break;
					}
					case xColor0.Red:
					{
						 Color0 = (SolidColorBrush)Brushes.Red;
						break;
					}
					case xColor0.Yellow:
					{
						 Color0 = (SolidColorBrush)Brushes.Yellow;
						break;
					}
						case xColor0.Lime:
					{
						 Color0 = (SolidColorBrush)Brushes.Lime;
						break;
					}
						case xColor0.Orange:
					{
						 Color0 = (SolidColorBrush)Brushes.Orange;
						break;
					}
						case xColor0.Purple:
					{
						 Color0 = (SolidColorBrush)Brushes.Purple;
						break;
					}
						case xColor0.Gray:
					{
						 Color0 = (SolidColorBrush)Brushes.Gray;
						break;
					}
				}
				
				#endregion
				
				#region xColor90xSwitch
				switch (xColor90x)
				{
					case xColor90.Blue:
					{
						Color90 = (SolidColorBrush)Brushes.Blue;
						break;
					}
					case xColor90.Red:
					{
					
						Color90 = (SolidColorBrush)Brushes.Red;
						break;
					}
					case xColor90.Yellow:
					{
						Print("Picked Yellow");
					
						Color90 = (SolidColorBrush)Brushes.Yellow;
						break;
					}
					case xColor90.Lime:
					{
						Color90 = (SolidColorBrush)Brushes.Lime;
						break;
					}
					case xColor90.Orange:
					{
						Color90 = (SolidColorBrush)Brushes.Orange;
						break;
					}
					case xColor90.Purple:
					{
						Color90 = (SolidColorBrush)Brushes.Purple;
						break;
					}
					case xColor90.Gray:
					{
						Color90 = (SolidColorBrush)Brushes.Gray;
						break;
					}
				}
				
				#endregion
				
				#region xColor180xSwitch
				
				switch (xColor180x)
				{
					case xColor180.Blue:
					{
						 Color180 = (SolidColorBrush)Brushes.Blue;
						break;
					}
					case xColor180.Red:
					{
						 Color180 = (SolidColorBrush)Brushes.Red;
						break;
					}
					case xColor180.Yellow:
					{
						 Color180 = (SolidColorBrush)Brushes.Yellow;
						break;
					}
					case xColor180.Lime:
					{
						 Color180 = (SolidColorBrush)Brushes.Lime;
						break;
					}
					case xColor180.Orange:
					{
						 Color180 = (SolidColorBrush)Brushes.Orange;
						break;
					}
					case xColor180.Purple:
					{
						 Color180 = (SolidColorBrush)Brushes.Purple;
						break;
					}
					case xColor180.Gray:
					{
						 Color180 = (SolidColorBrush)Brushes.Gray;
						break;
					}
				}
				
				#endregion
				
				#region xColor270xSwitch
				switch (xColor270x)
				{
					case xColor270.Blue:
					{
						Color270 = (SolidColorBrush)Brushes.Blue;
						break;
					}
					case xColor270.Red:
					{
						Color270 = (SolidColorBrush)Brushes.Red;
						break;
					}
					case xColor270.Yellow:
					{
						Color270 = (SolidColorBrush)Brushes.Yellow;
						break;
					}
					case xColor270.Lime:
					{
						Color270 = (SolidColorBrush)Brushes.Lime;
						break;
					}
					case xColor270.Orange:
					{
						Color270 = (SolidColorBrush)Brushes.Orange;
						break;
					}
					case xColor270.Purple:
					{
						Color270 = (SolidColorBrush)Brushes.Purple;
						break;
					}
					case xColor270.Gray:
					{
						Color270 = (SolidColorBrush)Brushes.Gray;
						break;
					}
				}
				#endregion
			}
		}

		protected override void OnBarUpdate()
		{
					
		
			switch (pOrT)
			{	
				case PriceOrTime.Price:
				{
									
					MAX_HIGH = Math.Max(MAX_HIGH,High[0]);
					MIN_LOW = Math.Min(MIN_LOW,Low[0]);
					
					if(CurrentBar == Bars.Count-2)
					{
						MAX_HIGH = Math.Round(MAX_HIGH*1.05,PriceDigits);
						MIN_LOW = Math.Round(MIN_LOW*0.95,PriceDigits);
						
						if(InitialPrice<MIN_LOW || InitialPrice>MAX_HIGH) 
						{	
							Log("InitialPrice parameter MUST be between the high price and low price on this chart",NinjaTrader.Cbi.LogLevel.Information);
							if(InitialPrice>MAX_HIGH) InitialPrice = MAX_HIGH;
							if(InitialPrice<MIN_LOW) InitialPrice = MIN_LOW;
						}
						
						
						double LevelUp=-1.0,LevelDown=-1.0;
						i=1;
						
						do
						{
							if(Angle000Flag){ LevelUp=InitialPrice+Angle0[i]*TickSize*MultiplierForPriceScale;    LevelDown=InitialPrice-Angle0[i]*TickSize*MultiplierForPriceScale;   MakePriceLine(LevelUp,LevelDown,Color0);}
						 	if(Angle090Flag){ LevelUp=InitialPrice+Angle90[i]*TickSize*MultiplierForPriceScale;   LevelDown=InitialPrice-Angle90[i]*TickSize*MultiplierForPriceScale;  MakePriceLine(LevelUp,LevelDown,Color90);}
					 		if(Angle180Flag){ LevelUp=InitialPrice+Angle180[i]*TickSize*MultiplierForPriceScale;  LevelDown=InitialPrice-Angle180[i]*TickSize*MultiplierForPriceScale; MakePriceLine(LevelUp,LevelDown,Color180);}
						 	if(Angle270Flag){ LevelUp=InitialPrice+Angle270[i]*TickSize*MultiplierForPriceScale;  LevelDown=InitialPrice-Angle270[i]*TickSize*MultiplierForPriceScale; MakePriceLine(LevelUp,LevelDown,Color270);}
							i++;
							if(LevelUp<0.0 || LevelDown<0.0) break;
							if(i>=Angle0.Length) break;
							if(i>=Angle90.Length) break;
							if(i>=Angle180.Length) break;
							if(i>=Angle270.Length) break;

							if(LevelUp > MaxLevelUp)     MaxLevelUp   = LevelUp;
							if(LevelDown < MinLevelDown) MinLevelDown = LevelDown;
						}
						while (1==1);
	
					}
					break;
				}
				
				case PriceOrTime.Time:
				{
	
					if(ZuluTime.CompareTo(Time[0])<0 && ZuluBar<0)  ZuluBar=CurrentBar; //set ZuluBar to time the user selected
					
					Print("Zulubar"+ ZuluBar.ToString());
					
					if(ZuluBar>0)
					{	
						OutString=null;
						if(Angle000Flag) 
						{
							i = Angle0[NextBar0]+ZuluBar;
							if(i==CurrentBar) { MakeVerticalLine(0,Color0); NextBar0++;}
							OutString="Zero line coming in "+(i-CurrentBar).ToString()+" bars"+Environment.NewLine;
						}
						if(Angle090Flag) 
						{	
							i = Angle90[NextBar90]+ZuluBar;
							if(i==CurrentBar) { MakeVerticalLine(0,Color90); NextBar90++;}
							OutString=OutString+"90 line coming in "+(i-CurrentBar).ToString()+" bars"+Environment.NewLine;
						}
						if(Angle180Flag) 
						{	
							i = Angle180[NextBar180]+ZuluBar;
							if(i==CurrentBar) { MakeVerticalLine(0,Color180); NextBar180++;}
							OutString=OutString+"180 line coming in "+(i-CurrentBar).ToString()+" bars"+Environment.NewLine;
						}
						if(Angle270Flag) 
							
						{	
							i = Angle270[NextBar270]+ZuluBar;
							if(i==CurrentBar) { MakeVerticalLine(0,Color270); NextBar270++;}
							OutString=OutString+"270 line coming in "+(i-CurrentBar).ToString()+" bars";
						}
												
						if(OutString.Length > 0 && DisplayCountDown) 
						{	
						
							lastBar		= Math.Min(ChartControl.LastSlotPainted, Bars.Count - 1);
							firstBar	= (lastBar - ChartControl.SlotsPainted) + 1;
							int i;
						// Find highest and lowest price points
							HighestPaintedPrice = double.MinValue;
							LowestPaintedPrice  = double.MaxValue;
							for (i = firstBar; i <= lastBar && i >= 0; i++)
							{
								HighestPaintedPrice = Math.Max(HighestPaintedPrice, Bars.GetHigh(i));
								LowestPaintedPrice  = Math.Min(LowestPaintedPrice , Bars.GetLow(i));
							}
							
							double Outprice = (HighestPaintedPrice+LowestPaintedPrice)/2.0;
							if(Bars.GetClose(lastBar-1)< Outprice) Outprice = HighestPaintedPrice;
							if(Bars.GetClose(lastBar-1) >= Outprice) Outprice = (Outprice+LowestPaintedPrice)/2.0;

							Print(OutString);
							
							NinjaTrader.Gui.Tools.SimpleFont myFont = new NinjaTrader.Gui.Tools.SimpleFont("Arial", 10) { Size = 20, Bold = false };
							
							if(CurrentBar>10) Draw.Text(this,"Info",true,OutString,10,Outprice,0,Brushes.Gray , myFont, TextAlignment.Justify, Brushes.White, Brushes.Transparent,50);
						
						}				
						
					}				
					
					break;	
				}
				
			}
			
			
		}
		#region Properties
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "InitialPrice", GroupName = "NinjaScriptParameters", Order = 0)]
		public double InitialPrice
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "MultiplierForPriceScale", GroupName = "NinjaScriptParameters", Order = 0)]
		public int MultiplierForPriceScale
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display( Name = "NumberOfLines", GroupName = "NinjaScriptParameters", Order = 0)]
		public int NumberOfLines
		{ get; set; }
		
		
		//Enum	
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PriceOrTime", GroupName = "NinjaScriptParameters", Order = 0)]
		public PriceOrTime pOrT
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ZuluTime", GroupName = "NinjaScriptParameters", Order = 0)]
		public DateTime ZuluTime
		{ get; set; }
		
		//Display Count	
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "DisplayCountDown", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool DisplayCountDown
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Angle000Flag", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool Angle000Flag
		{ get; set; }
	
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Angle090Flag", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool Angle090Flag
		{ get; set; }
	
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Angle180Flag", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool Angle180Flag
		{ get; set; }
	
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Angle270Flag", GroupName = "NinjaScriptParameters", Order = 0)]
		public bool Angle270Flag
		{ get; set; }

		
		//Color Enums
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "xColor0x", GroupName = "NinjaScriptParameters", Order = 0)]
		public xColor0 xColor0x
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "xColor90x", GroupName = "NinjaScriptParameters", Order = 0)]
		public xColor90 xColor90x  
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "xColor180", GroupName = "NinjaScriptParameters", Order = 0)]
		public xColor180 xColor180x
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "xColor270", GroupName = "NinjaScriptParameters", Order = 0)]
		public xColor270 xColor270x
		{ get; set; }
		
		#endregion
		
		
		private void MakePriceLine(double Up, double Down, SolidColorBrush  C)
		{	
			if(Up>0.0 && Up < MAX_HIGH) {Draw.HorizontalLine(this, "Sq9P_"+LineId.ToString(),true,Up,C,DashStyleHelper.Dash,1);	LineId++;}
			if(Down>0.0 && Down > MIN_LOW) {Draw.HorizontalLine(this, "Sq9P_"+LineId.ToString(),true,Down,C,DashStyleHelper.Dash,1); LineId++;}
			if(LineId > NumberOfLines) LineId = 0;
		}
		
		private void MakeVerticalLine(int BarNum, SolidColorBrush C)
			
		{	Draw.VerticalLine(this, "Sq9T_"+LineId.ToString(),BarNum,C,DashStyleHelper.Dash,1);
			LineId++;
			if(LineId > NumberOfLines) LineId = 0;
		}
			
	}
}


public enum PriceOrTime
{
		Price,
		Time,
}

#region ColorEnums
public enum xColor0
{
		
		Red,
		Blue,
		Yellow,
		Purple,
		Orange,
		Gray,
		Lime,
	
}
public enum xColor90
{
		
		Red,
		Blue,
		Yellow,
		Purple,
		Orange,
		Gray,
		Lime,
	
}

public enum xColor180
{
		
		Red,
		Blue,
		Yellow,
		Purple,
		Orange,
		Gray,
		Lime,

}

public enum xColor270
{
		
		Red,
		Blue,
		Yellow,
		Purple,
		Orange,
		Gray,
		Lime,
	
}
	#endregion

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SqaureOfNine[] cacheSqaureOfNine;
		public SqaureOfNine SqaureOfNine(double initialPrice, int multiplierForPriceScale, int numberOfLines, PriceOrTime pOrT, DateTime zuluTime, bool displayCountDown, bool angle000Flag, bool angle090Flag, bool angle180Flag, bool angle270Flag, xColor0 xColor0x, xColor90 xColor90x, xColor180 xColor180x, xColor270 xColor270x)
		{
			return SqaureOfNine(Input, initialPrice, multiplierForPriceScale, numberOfLines, pOrT, zuluTime, displayCountDown, angle000Flag, angle090Flag, angle180Flag, angle270Flag, xColor0x, xColor90x, xColor180x, xColor270x);
		}

		public SqaureOfNine SqaureOfNine(ISeries<double> input, double initialPrice, int multiplierForPriceScale, int numberOfLines, PriceOrTime pOrT, DateTime zuluTime, bool displayCountDown, bool angle000Flag, bool angle090Flag, bool angle180Flag, bool angle270Flag, xColor0 xColor0x, xColor90 xColor90x, xColor180 xColor180x, xColor270 xColor270x)
		{
			if (cacheSqaureOfNine != null)
				for (int idx = 0; idx < cacheSqaureOfNine.Length; idx++)
					if (cacheSqaureOfNine[idx] != null && cacheSqaureOfNine[idx].InitialPrice == initialPrice && cacheSqaureOfNine[idx].MultiplierForPriceScale == multiplierForPriceScale && cacheSqaureOfNine[idx].NumberOfLines == numberOfLines && cacheSqaureOfNine[idx].pOrT == pOrT && cacheSqaureOfNine[idx].ZuluTime == zuluTime && cacheSqaureOfNine[idx].DisplayCountDown == displayCountDown && cacheSqaureOfNine[idx].Angle000Flag == angle000Flag && cacheSqaureOfNine[idx].Angle090Flag == angle090Flag && cacheSqaureOfNine[idx].Angle180Flag == angle180Flag && cacheSqaureOfNine[idx].Angle270Flag == angle270Flag && cacheSqaureOfNine[idx].xColor0x == xColor0x && cacheSqaureOfNine[idx].xColor90x == xColor90x && cacheSqaureOfNine[idx].xColor180x == xColor180x && cacheSqaureOfNine[idx].xColor270x == xColor270x && cacheSqaureOfNine[idx].EqualsInput(input))
						return cacheSqaureOfNine[idx];
			return CacheIndicator<SqaureOfNine>(new SqaureOfNine(){ InitialPrice = initialPrice, MultiplierForPriceScale = multiplierForPriceScale, NumberOfLines = numberOfLines, pOrT = pOrT, ZuluTime = zuluTime, DisplayCountDown = displayCountDown, Angle000Flag = angle000Flag, Angle090Flag = angle090Flag, Angle180Flag = angle180Flag, Angle270Flag = angle270Flag, xColor0x = xColor0x, xColor90x = xColor90x, xColor180x = xColor180x, xColor270x = xColor270x }, input, ref cacheSqaureOfNine);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SqaureOfNine SqaureOfNine(double initialPrice, int multiplierForPriceScale, int numberOfLines, PriceOrTime pOrT, DateTime zuluTime, bool displayCountDown, bool angle000Flag, bool angle090Flag, bool angle180Flag, bool angle270Flag, xColor0 xColor0x, xColor90 xColor90x, xColor180 xColor180x, xColor270 xColor270x)
		{
			return indicator.SqaureOfNine(Input, initialPrice, multiplierForPriceScale, numberOfLines, pOrT, zuluTime, displayCountDown, angle000Flag, angle090Flag, angle180Flag, angle270Flag, xColor0x, xColor90x, xColor180x, xColor270x);
		}

		public Indicators.SqaureOfNine SqaureOfNine(ISeries<double> input , double initialPrice, int multiplierForPriceScale, int numberOfLines, PriceOrTime pOrT, DateTime zuluTime, bool displayCountDown, bool angle000Flag, bool angle090Flag, bool angle180Flag, bool angle270Flag, xColor0 xColor0x, xColor90 xColor90x, xColor180 xColor180x, xColor270 xColor270x)
		{
			return indicator.SqaureOfNine(input, initialPrice, multiplierForPriceScale, numberOfLines, pOrT, zuluTime, displayCountDown, angle000Flag, angle090Flag, angle180Flag, angle270Flag, xColor0x, xColor90x, xColor180x, xColor270x);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SqaureOfNine SqaureOfNine(double initialPrice, int multiplierForPriceScale, int numberOfLines, PriceOrTime pOrT, DateTime zuluTime, bool displayCountDown, bool angle000Flag, bool angle090Flag, bool angle180Flag, bool angle270Flag, xColor0 xColor0x, xColor90 xColor90x, xColor180 xColor180x, xColor270 xColor270x)
		{
			return indicator.SqaureOfNine(Input, initialPrice, multiplierForPriceScale, numberOfLines, pOrT, zuluTime, displayCountDown, angle000Flag, angle090Flag, angle180Flag, angle270Flag, xColor0x, xColor90x, xColor180x, xColor270x);
		}

		public Indicators.SqaureOfNine SqaureOfNine(ISeries<double> input , double initialPrice, int multiplierForPriceScale, int numberOfLines, PriceOrTime pOrT, DateTime zuluTime, bool displayCountDown, bool angle000Flag, bool angle090Flag, bool angle180Flag, bool angle270Flag, xColor0 xColor0x, xColor90 xColor90x, xColor180 xColor180x, xColor270 xColor270x)
		{
			return indicator.SqaureOfNine(input, initialPrice, multiplierForPriceScale, numberOfLines, pOrT, zuluTime, displayCountDown, angle000Flag, angle090Flag, angle180Flag, angle270Flag, xColor0x, xColor90x, xColor180x, xColor270x);
		}
	}
}

#endregion
