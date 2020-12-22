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

public enum CenTexFishTDivIndicatorMethod
{
    FisherTransform,
	MACDhistOnly,
	
}

public enum CenTexFishTDivPriceType
{
	High_Low,
	Open_Close,
	SMA1,
	SMA2,
	EMA,
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public class CenTexFishTDiv : Indicator

	{
    
	    
		private double             noCrossZero = .05;  ///filter used to keep divergence from drawing near or accrossed the zero line
	  
	
		
		
	   
	    private DashStyleHelper lineStyle2 = DashStyleHelper.Solid;
		private CenTexFishTDivIndicatorMethod method = CenTexFishTDivIndicatorMethod.FisherTransform;
		private CenTexFishTDivPriceType pType = CenTexFishTDivPriceType.High_Low;
		private bool useDefaultPlot = true; 
		private bool showAlerts = false;
		private	MAX				max;
		private	MIN				min;
		private Series<double>	tmpSeries;
  		private int             period = 10; // WAS 10
#region counting stuff		
	    private int counter;
        private bool isCounting;
		private bool isCounting2;
		private TextPosition			textBoxPositionBR	= TextPosition.BottomRight;
		private TextPosition			textBoxPosition		= TextPosition.TopRight;
		private Brush					textColor			= Brushes.Pink;
		private	Gui.Tools.SimpleFont	textFont;
		private int counter2;  
#endregion
		
		private double		foundValue;
		private double		foundAvg;
		private double		foundDiff;
		
		private int			macd_Fast 		= 14;  ///12
		private int 		macd_Slow 		= 26;  ///26
		private int 		macd_Smooth 	= 12;   ///9
		private bool initDone = false;
		private ISeries<double> Indicator;
		private int 		[] HighBarsAgo;
		private int 		[] LowBarsAgo;	
		private int 		ThisHigh;
		private int			ThisLow;
		private int 		QHLength	 		= 0;   // was 0
		private int			QLLength	 		= 0; 
		private int 		queueLength 		= 3;	//was 3
		private int 		scanWidth 			= 10;  //was 10
		private int 		rdivlinelookbackperiod = 100; // was 45 if changed to 100 messes up the count for some reason
		private int 		A 					= 1;		
		private int 		BarsAgo;
		private double 		priceDiffLimit 		= 0.0;
		private double 		indicatorDiffLimit  = 0.0;
		private DashStyleHelper divergenceDashStyle 	= DashStyleHelper.Dot;
		private int divergenceLineWidth 				= 3; 
		private int markerDistanceFactor 				= 1;  // conrolls how far away divergence line hovers aways from candles
		private Brush divergenceColor 					= Brushes.Magenta;
		private string myAlert1 = @"C:\Program Files (x86)\NinjaTrader 8\sounds\Alert1.wav";
		private string myAlert2 = @"C:\Program Files (x86)\NinjaTrader 8\sounds\Alert2.wav";
	
			
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Quality divergence finder using FisherTransform indicator";
				Name						= "CenTexFishTDiv";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= false;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= false;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Left; //Changed from right to left
				IsSuspendedWhileInactive	= false;  // set to false because of alerts
				AddPlot(new Stroke(Brushes.Chartreuse, 1), PlotStyle.Bar, "IndicPlot0");/// the number is = barwidth of histogram 
				AddPlot(Brushes.White, "IndicPlot1");
				AddPlot(Brushes.Yellow, "IndicPlot2");
				AddPlot(new Stroke(Brushes.Yellow, 2), PlotStyle.Bar, "IndicPlot3");/// the number is = barwidth of histogram
			//	AddLine(Brushes.Yellow, fromZero, "IndicLine0");
			//	AddLine(Brushes.Chartreuse, - fromZero, "IndicLine1");
				
				max			= MAX(Input, period);
				min			= MIN(Input, period);
				tmpSeries	= new Series<double>(this);
				counter = 0;
                isCounting = false; 
					
			}
			
			else if (State == State.Configure)
			{
				
				HighBarsAgo	= new int[QueueLength];
				LowBarsAgo	= new int[QueueLength];
			
				for (int i=0; i<QueueLength; i++) 
				{
					HighBarsAgo[i] 	= 0;
					LowBarsAgo[i] 	= 0;
				}
			}
			
			else if (State == State.DataLoaded) 
			{				
				Name = "";
				Indicator = new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
			
		}
		
		protected override void OnBarUpdate()
		{
			double PriceDiff, IndicatorDiff;
         
			
			
if (useDefaultPlot == true)
    Indicator  = SMA(Value,1);	
else		
  PlotMACDhistOnly();	
	   if (CurrentBar < ScanWidth) return;
			
			if (initDone == false)
			{
				switch (Method) 
					{
							
						case CenTexFishTDivIndicatorMethod.MACDhistOnly:
							InitMACDhistOnly();
							break;
		             			
						case CenTexFishTDivIndicatorMethod.FisherTransform: 
							InitFisherTransform();
							break;
						
					}
				} 
			
			switch (Method) 
			{
												
				case CenTexFishTDivIndicatorMethod.MACDhistOnly:	InitMACDhistOnly(); PlotMACDhistOnly(); break;		
					
				case CenTexFishTDivIndicatorMethod.FisherTransform:	InitFisherTransform(); PlotFisherTransform(); break;			
											
			}
			
//--------------------------------------------------------------------
			
			switch (PType) 
			{
				case CenTexFishTDivPriceType.High_Low:	
					ThisHigh = HighestBar(High, ScanWidth);
					break;
					
				case CenTexFishTDivPriceType.Open_Close: 
					ThisHigh = HighestBar(Close, ScanWidth);
					break;
				
				case CenTexFishTDivPriceType.SMA1:
					ThisHigh = HighestBar(SMA(High,1), ScanWidth);
					break;
					
				case CenTexFishTDivPriceType.EMA:
					ThisHigh = HighestBar(EMA(High,1), ScanWidth);
					break;
					
			}
			
			if (ThisHigh == A) 
				
				
			{

				if (ShowAlerts == true) 
				{
					Alert("MyAlrt1" + CurrentBar.ToString(), Priority.High, "a: High Found", MyAlert1, 10, Brushes.Black, Brushes.Yellow);
				}
				
				for (int i = QueueLength-1; i >= 1; i--) ///TEST HERE WAS -1
				{
					HighBarsAgo[i] = HighBarsAgo[i-1];
				}
			
				HighBarsAgo[0] = CurrentBar - A;
					
				DrawOnPricePanel = false;
				
				DrawOnPricePanel = true;
				
				if (++QHLength >= 2) 
				{
					for(int i = 0; i < Math.Min(QHLength, QueueLength); i++) 
					{
						BarsAgo = CurrentBar - HighBarsAgo[i];
						
						IndicatorDiff	= Indicator[A] - Indicator[BarsAgo];

						switch (PType) 
						{
							case CenTexFishTDivPriceType.High_Low:	
								PriceDiff	= High[A] - High[BarsAgo];
								break;
								
							case CenTexFishTDivPriceType.Open_Close: 
								PriceDiff	= Close[A] - Close[BarsAgo];
								break;
							
							case CenTexFishTDivPriceType.SMA1:
								PriceDiff	= SMA(High, 1)[A] - SMA(High, 1)[BarsAgo];
								break;
								
							default :
								PriceDiff	= High[A] - High[BarsAgo];
								break;
						}

					//	if (IndicatorDiff < IndicatorDiffLimit && (PriceDiff > PriceDiffLimit || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) 	
							
						if (IndicatorDiff < IndicatorDiffLimit && (PriceDiff > PriceDiffLimit))    // removes horizontal lines that are not divergence ((better version hwh)	
							
						{							

							if ((BarsAgo - A) < rdivlinelookbackperiod) 
							{
								if (ShowAlerts == true) 
								{
									Alert("MyAlrt2" + CurrentBar.ToString(), Priority.High, "a: Divergence Found", MyAlert2, 10, Brushes.Black, Brushes.Red);
								}
								
								 
#region UPPER DIVERGENCE LINES	
								/// DIVER FILTER test area
								/// //////////////////
						if (SMA(Low,1)[A] < High[BarsAgo])
						
								////////////////////	
								/// end of test area
						if (Indicator[A] > - noCrossZero) 	/// UPPER filter working WITH FISHER AND MACD HISTOGRAM !!!!!!!!!!!!!!!!!
						  	
						{	///ADDED FOR STRATEGTY VALUE CONNECTOR							
									
								Draw.Line(this, "high"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, High[BarsAgo] + (TickSize * MarkerDistanceFactor), A, 
									High[A] + (TickSize * MarkerDistanceFactor), divergenceColor, divergenceDashStyle, divergenceLineWidth);	
								
						
						}
						
						
						
						
								{
								if (isCounting == false && ShowAlerts == true);  /// for counting
								}
							
								
						    
								
								
						if (SMA(Low,1)[A] < High[BarsAgo])  ///BINGO !!!!!!!!!!!
								
								
								
						if (Indicator[A] > - noCrossZero) 	/// UPPER filter working WITH FISHER AND MACD HISTOGRAM !!!!!!!!!!!!!!!!!
								
								counter++;
                                isCounting = true;	
						
						        DrawOnPricePanel = false;
								
								Draw.TextFixed(this, "Count", counter +  "\n Short Divergence ", textBoxPosition, textColor, textFont, Brushes.Transparent, Brushes.Transparent, 0);
									
							
								
							//	DrawOnPricePanel = false;
								
								
								
						if (SMA(Low,1)[A] < High[BarsAgo]) 
									
				        if (Indicator[A] > - noCrossZero) 	/// UPPER filter working WITH FISHER AND MACD HISTOGRAM !!!!!!!!!!!!!!!!!	
								
									
						
								////////////////////	
								/// end of test area
								
								
								
										
								Draw.Line(this, "IH"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Indicator[BarsAgo], A, Indicator[A], divergenceColor, 
									divergenceDashStyle, divergenceLineWidth);
								
								DrawOnPricePanel = true;
							
#endregion											
							}	
						}
						else if (IndicatorDiff > IndicatorDiffLimit && (PriceDiff < PriceDiffLimit || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) 
						{							
							
					
							{
				
								{
						
								}
															  												
							}	
						}
					}	
				}	
			}
#region Type of divergence detection
			
			switch (PType) 
			{
				case CenTexFishTDivPriceType.High_Low:	
					ThisLow = LowestBar(Low, ScanWidth);
					break;
					
				case CenTexFishTDivPriceType.Open_Close: 
					ThisLow = LowestBar(Close, ScanWidth);
					break;
					
				case CenTexFishTDivPriceType.SMA1:
					ThisLow = LowestBar(SMA(Low,1), ScanWidth);
					break;
					
				case CenTexFishTDivPriceType.EMA:
					ThisLow = LowestBar(EMA(Low,1), ScanWidth);
					break;
#endregion				

			}
				
			if (ThisLow == A) 
			{					
				for (int i = QueueLength-1; i >= 1; i--) 
				{
					LowBarsAgo[i] = LowBarsAgo[i-1];
				}
			
				LowBarsAgo[0] = CurrentBar - A;
				
	
				DrawOnPricePanel = false;
				

				DrawOnPricePanel = true;

				if (ShowAlerts == true) 
				
				{
					Alert("MyAlrt1" + CurrentBar.ToString(), Priority.High, "a: Low Found", MyAlert1, 10, Brushes.Black, Brushes.Yellow);
				}
				
				if (++QLLength >= 2) 
				{
					for(int i = 0; i < Math.Min(QLLength, QueueLength); i++) 
					{
						BarsAgo = CurrentBar - LowBarsAgo[i];
						
						IndicatorDiff 	= Indicator[A] - Indicator[BarsAgo];
						switch (PType) 
						{
							case CenTexFishTDivPriceType.High_Low:	
								PriceDiff 		= Low[A] - Low[BarsAgo];	
								break;
								
							case CenTexFishTDivPriceType.Open_Close: 
								PriceDiff 		= Close[A] - Close[BarsAgo];	
								break;
							
							case CenTexFishTDivPriceType.SMA1:
								PriceDiff 		= SMA(Close,1)[A] - SMA(Close,1)[BarsAgo];	
								break;
								
							default:
								PriceDiff 		= Low[A] - Low[BarsAgo];	
								break;
						}	
						
						
					//	if (IndicatorDiff > IndicatorDiffLimit && (PriceDiff < PriceDiffLimit || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) 	
							
							if (IndicatorDiff > IndicatorDiffLimit && (PriceDiff < PriceDiffLimit)) // || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) 
						{	

							if ((BarsAgo - A) < rdivlinelookbackperiod)  ////origanal hwh /////////////////
								
						
								
								
							{  
#region  Bottom div Lines	
								
								
							
						 if (SMA(High,1)[A] > Low[BarsAgo])	 ///BINGO !!!!!!!!!!!
									
				         if (Indicator[A] < - noCrossZero) 	/// Lower filter working WITH FISHER AND MACD HISTOGRAM !!!!!!!!!!!!!!!!!	
						 	 
						 {	/////added for strategy value connector			 
							 
									
						        ///SHOWS DIVERGENCE LINES ON BOTTOM OF PRICE CANDLES
								Draw.Line(this, "low"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Low[BarsAgo] - (TickSize * MarkerDistanceFactor), 
									A, Low[A] - (TickSize * MarkerDistanceFactor), DivergenceColor, DivergenceDashStyle, DivergenceLineWidth);
								
							 
							 
						 }
						 
						 
						 
							
								{
								if (isCounting2 == false && ShowAlerts == true);  /// for counting
								}
							    
						    
								
								
					     if (SMA(High,1)[A] > Low[BarsAgo])  ///BINGO !!!!!!!!!!!
						
						 
						 if (Indicator[A] < - noCrossZero) 	/// Lower filter working WITH FISHER AND MACD HISTOGRAM !!!!!!!!!!!!!!!!!				
					
														
								counter2++;
                                isCounting2 = true;	
						 
						        DrawOnPricePanel = false;
						          
								///DRAWS COUNTER ON BOTTOM RIGHT OF PRICE CHART
								Draw.TextFixed(this, "Count2", counter2 +  "\n Long Divergence ", textBoxPositionBR, textColor, textFont, Brushes.Transparent, Brushes.Transparent, 0);
															
								
						
								
			             if (SMA(High,1)[A] > Low[BarsAgo])  ///BINGO !!!!!!!!!!!
	////////						 
							 
			//	if (SMA(High,1)[A] > Low[BarsAgo] && High[A] > - fromZero);	 ////////////////TESTING THIS
	///////								
		     		//	 if (Value[1] > - fromZero) 	/// Lower filter working ONLY WITH FISHER NOT MACD HISTOGRAM !!!!!!!!!!!!!!!!!
						
						 if (Indicator[A] < - noCrossZero) 	/// Lower filter working WITH FISHER AND MACD HISTOGRAM !!!!!!!!!!!!!!!!!	 
									
						        ///SHOWS DIVERGENCE LINES ON BOTTOME OF INDICATOR HISTOGRAM
								Draw.Line(this, "Ilow"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Indicator[BarsAgo],
									A, Indicator[A], divergenceColor, divergenceDashStyle, divergenceLineWidth);
								
														
			
								DrawOnPricePanel = true;
								
#endregion				
								
								if (ShowAlerts == true) {
									Alert("MyAlrt2" + CurrentBar.ToString(), Priority.High, "a: Divergence Found", MyAlert2, 10, Brushes.Black, Brushes.Red);
								}
							}
						}
					}		
				}		
			}		
		}				
							

		public override string ToString()
		{
			return "";
		}		

	
		
#region MACD Indicator
		private void InitMACDhistOnly()
		{

			if (useDefaultPlot == true)
			{
		
			}
		}
		
		private void PlotMACDhistOnly()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "MACDhistogram", "Using: MACDhistogram("+Macd_Fast.ToString()+","+Macd_Slow.ToString()+","+Macd_Smooth.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			
			Indicator = MACD(Macd_Fast, Macd_Slow, Macd_Smooth).Diff;
			foundValue = Indicator[0];

		//	IndicPlot0[0] = foundValue;
			IndicPlot3[0] = foundValue;			
		}
 
		
		private void InitFisherTransform()  
		{

			if (useDefaultPlot == true)
			{
			   
		
		
			}
		}
		
#endregion	
#region FisherReform indicator
		
		private void PlotFisherTransform()
		{
		
			
		double fishPrev		= 0;
			double tmpValuePrev	= 0;

			if (CurrentBar > 0)
			{
				fishPrev		= Value[1];
				tmpValuePrev	= tmpSeries[1];
			}

			double minLo	= min[0];
			double num1		= max[0] - minLo;

			
			num1			= (num1 < TickSize / 10 ? TickSize / 10 : num1);
			double tmpValue = 0.66 * ((Input[0] - minLo) / num1 - 0.5) + 0.67 * tmpValuePrev;

			if (tmpValue > 0.99)
				tmpValue = 0.999;
			else if (tmpValue < -0.99)
				tmpValue = -0.999;

			tmpSeries[0]	= tmpValue;
			Value[0]		= 0.5 * Math.Log((1 + tmpValue) / (1 - tmpValue)) + 0.5 * fishPrev;	
			///test /////////////////////		
		    DrawOnPricePanel = false;
			Draw.TextFixed(this, "FisherTransform", "Using: FisherTransform("+period.ToString()+")", TextPosition.TopLeft); //, Color.Yellow, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			///test/////////////////////
		}	
		
  
		
#endregion		
		
#region Properties
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndicPlot0
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndicPlot1
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndicPlot2
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IndicPlot3
		{
			get { return Values[3]; }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Indicator Method", Description="Indicator method to use for divergence test", Order=1, GroupName="Control")]
		public CenTexFishTDivIndicatorMethod Method
		{ 
			get {return method;}
		 	set {method = value; }
		}

		[NinjaScriptProperty]
		[Display(Name="Limit Indicator Difference", Description="Indicator Difference Limit for divergence", Order=2, GroupName="Control")]		
		public double IndicatorDiffLimit
		{
			get { return indicatorDiffLimit; }
			set { indicatorDiffLimit = value; }
		}

		[NinjaScriptProperty]
		[Display(Name="Limit Price Difference", Description="Price Difference Limit for divergence", Order=3, GroupName="Control")]		
		public double PriceDiffLimit
		{
			get { return priceDiffLimit; }
			set { priceDiffLimit =value; }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="ScanWidth Lookback Bars", Description="High/Low Lookback Scan Width", Order=4, GroupName="Control")]				
		public int ScanWidth
		{
			get { return scanWidth; }
			set { scanWidth = Math.Max(10, value); }
		}	
		
		[Range(1, int.MaxValue)]  
		[NinjaScriptProperty]
		[Display(Name="Queue Length", Description="The number of consecutive candidates to look back at", Order=5, GroupName="Control")]				
		public int QueueLength
		{
			get { return queueLength; }
			set { queueLength = Math.Max(3, value); }
		}	
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="rDiv Line Lookback Bars", Description="regular divergence line Lookback", Order=6, GroupName="Control")]	//WH Added			
		public int Rdivlinelookbackperiod
		{
			get { return rdivlinelookbackperiod; }
			set { rdivlinelookbackperiod = Math.Max(10, value); }
		}
		

		[NinjaScriptProperty]
		[Display(Name="Price Type", Description="Price Type", Order=8, GroupName="Control")]						
		public CenTexFishTDivPriceType PType
		{
			get { return pType; }
			set { pType = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Show Alerts", Description="Show Alerts", Order=9, GroupName="Control")] 
		public bool ShowAlerts
        {
            get { return showAlerts; }
            set { showAlerts = value; }
        }
		
		[NinjaScriptProperty]
		[Display(Name="High/Low sound", Description="Sound for detected High/Low", Order=10, GroupName="Control")] 		
		public string MyAlert1
		{
			get { return myAlert1; }
			set { myAlert1 = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Divergence sound", Description="Sound for divergence", Order=11, GroupName="Control")] 		
		public string MyAlert2
		{
			get { return myAlert2; }
			set { myAlert2 = value; }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Use default plots", Description="Use default plots", Order=12, GroupName="Control")] 		
        public bool UseDefaultPlot
        {
            get { return useDefaultPlot; }
            set { useDefaultPlot = value; }
        }

		[XmlIgnore]
		[Display(Name="Color", Description="Color of divergence line plotted", Order=1, GroupName="Divergence")]		
		public Brush DivergenceColor 
		{
			get { return divergenceColor; }
			set { divergenceColor = value;; }
		}	
		[Browsable(false)]
		public string divergenceColorSerializable
		{
			get { return Serialize.BrushToString(divergenceColor); }
			set { divergenceColor = Serialize.StringToBrush(value); }
		}			
				
		[NinjaScriptProperty]
		[Display(Name="Dash style", Description="Dashstyle of the divergence line", Order=2, GroupName="Divergence")]
		public DashStyleHelper DivergenceDashStyle
		{
			get { return divergenceDashStyle; }
			set { divergenceDashStyle = value; }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Line width", Description="Divergence Line Width", Order=3, GroupName="Divergence")]		
		public int DivergenceLineWidth

		{
			get { return divergenceLineWidth; }
			set { divergenceLineWidth = Math.Max(1,value);}
		}
			
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Marker Distance", Description="Marker distance (ticks) above/below", Order=6, GroupName="Divergence")]		
		public int MarkerDistanceFactor
		{
			get { return markerDistanceFactor; }
			set { markerDistanceFactor = Math.Max(1,value);}
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="MACD Fast", Description="Macd Fast period", Order=6, GroupName="Indicators")]			
        public int Macd_Fast
        {
            get { return macd_Fast; }
            set { macd_Fast = Math.Max(1, value); }
        }

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="MACD Slow", Description="Macd slow period", Order=7, GroupName="Indicators")]			
        public int Macd_Slow
        {
            get { return macd_Slow; }
            set { macd_Slow = Math.Max(1, value); }
        }
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="MACD Smooth", Description="Macd smoothing period", Order=8, GroupName="Indicators")]			
        public int Macd_Smooth
        {
            get { return macd_Smooth; }
            set { macd_Smooth = Math.Max(1, value); }
        }
       
	

        //////////////////////////////////////////
		#endregion

	}	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CenTexFishTDiv[] cacheCenTexFishTDiv;
		public CenTexFishTDiv CenTexFishTDiv(CenTexFishTDivIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, CenTexFishTDivPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int macd_Fast, int macd_Slow, int macd_Smooth)
		{
			return CenTexFishTDiv(Input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, macd_Fast, macd_Slow, macd_Smooth);
		}

		public CenTexFishTDiv CenTexFishTDiv(ISeries<double> input, CenTexFishTDivIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, CenTexFishTDivPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int macd_Fast, int macd_Slow, int macd_Smooth)
		{
			if (cacheCenTexFishTDiv != null)
				for (int idx = 0; idx < cacheCenTexFishTDiv.Length; idx++)
					if (cacheCenTexFishTDiv[idx] != null && cacheCenTexFishTDiv[idx].Method == method && cacheCenTexFishTDiv[idx].IndicatorDiffLimit == indicatorDiffLimit && cacheCenTexFishTDiv[idx].PriceDiffLimit == priceDiffLimit && cacheCenTexFishTDiv[idx].ScanWidth == scanWidth && cacheCenTexFishTDiv[idx].QueueLength == queueLength && cacheCenTexFishTDiv[idx].Rdivlinelookbackperiod == rdivlinelookbackperiod && cacheCenTexFishTDiv[idx].PType == pType && cacheCenTexFishTDiv[idx].ShowAlerts == showAlerts && cacheCenTexFishTDiv[idx].MyAlert1 == myAlert1 && cacheCenTexFishTDiv[idx].MyAlert2 == myAlert2 && cacheCenTexFishTDiv[idx].UseDefaultPlot == useDefaultPlot && cacheCenTexFishTDiv[idx].DivergenceDashStyle == divergenceDashStyle && cacheCenTexFishTDiv[idx].DivergenceLineWidth == divergenceLineWidth && cacheCenTexFishTDiv[idx].MarkerDistanceFactor == markerDistanceFactor && cacheCenTexFishTDiv[idx].Macd_Fast == macd_Fast && cacheCenTexFishTDiv[idx].Macd_Slow == macd_Slow && cacheCenTexFishTDiv[idx].Macd_Smooth == macd_Smooth && cacheCenTexFishTDiv[idx].EqualsInput(input))
						return cacheCenTexFishTDiv[idx];
			return CacheIndicator<CenTexFishTDiv>(new CenTexFishTDiv(){ Method = method, IndicatorDiffLimit = indicatorDiffLimit, PriceDiffLimit = priceDiffLimit, ScanWidth = scanWidth, QueueLength = queueLength, Rdivlinelookbackperiod = rdivlinelookbackperiod, PType = pType, ShowAlerts = showAlerts, MyAlert1 = myAlert1, MyAlert2 = myAlert2, UseDefaultPlot = useDefaultPlot, DivergenceDashStyle = divergenceDashStyle, DivergenceLineWidth = divergenceLineWidth, MarkerDistanceFactor = markerDistanceFactor, Macd_Fast = macd_Fast, Macd_Slow = macd_Slow, Macd_Smooth = macd_Smooth }, input, ref cacheCenTexFishTDiv);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CenTexFishTDiv CenTexFishTDiv(CenTexFishTDivIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, CenTexFishTDivPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int macd_Fast, int macd_Slow, int macd_Smooth)
		{
			return indicator.CenTexFishTDiv(Input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, macd_Fast, macd_Slow, macd_Smooth);
		}

		public Indicators.CenTexFishTDiv CenTexFishTDiv(ISeries<double> input , CenTexFishTDivIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, CenTexFishTDivPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int macd_Fast, int macd_Slow, int macd_Smooth)
		{
			return indicator.CenTexFishTDiv(input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, macd_Fast, macd_Slow, macd_Smooth);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CenTexFishTDiv CenTexFishTDiv(CenTexFishTDivIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, CenTexFishTDivPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int macd_Fast, int macd_Slow, int macd_Smooth)
		{
			return indicator.CenTexFishTDiv(Input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, macd_Fast, macd_Slow, macd_Smooth);
		}

		public Indicators.CenTexFishTDiv CenTexFishTDiv(ISeries<double> input , CenTexFishTDivIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, CenTexFishTDivPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int macd_Fast, int macd_Slow, int macd_Smooth)
		{
			return indicator.CenTexFishTDiv(input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, macd_Fast, macd_Slow, macd_Smooth);
		}
	}
}

#endregion
