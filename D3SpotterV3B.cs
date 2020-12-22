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

public enum D3SpotIndicatorMethod
{
	CCI,
//	CCI_JMA_MASM,
//	DO,
	MACD,
	MACDdiff,
	MACDhistOnly, 
	MFI,
	Momentum,
	ROC,
	RSI,
	RVI,
//	SMI,
	StochasticsD,
	StochasticsK,
	StochasticsFastD,
	StochasticsFastK,
	StochRSI
}

public enum D3SpotPriceType
{
	High_Low,
	Open_Close,
	SMA1,
	SMA2,
	EMA,
//	JMA_MASM
}
//  Converted from NT7 to NT8-B3 by the Ninjascript team 8/10/15
namespace NinjaTrader.NinjaScript.Indicators
{
	public class D3SpotterV3B : Indicator
	{
		private D3SpotIndicatorMethod method = D3SpotIndicatorMethod.StochasticsFastK; //Changed from RSI - WH 10/06/2017
		private D3SpotPriceType pType = D3SpotPriceType.High_Low;
		private bool useDefaultPlot = true; // Default setting for UseDefaultPlot
		private bool showAlerts = false;
		
		//		Results
		private double		foundValue;
		private double		foundAvg;
		private double		foundDiff;
		private double		foundStochD;
		private double		foundStochK;
		
		private int			rsi_Period		= 14;
		private int			mom_Period		= 14;
		private int			rsi_Smooth		= 3;
		private int			rvi_Period		= 14;
		private int			cci_Period		= 14;
		private int			mfi_Period		= 14;
		private int			roc_Period		= 14;
		private int			stochrsi_Period	= 14;
//		private int			cci_jma_masm_Period	= 7;
		
//		private int			smi_EMAPeriod1 	= 25;
//		private int			smi_EMAPeriod2 	= 1;
//		private int			smi_Range 		= 13;
//		private int			smi_SMIEMAPeriod = 25;
		
		//		MACD variables
		private int			macd_Fast 		= 12; 
		private int 		macd_Slow 		= 26; 
		private int 		macd_Smooth 	= 9; 
		
		//		 Stochastics and Fast Stochastics
		private int			stoch_PeriodD	= 7;
		private int			stoch_PeriodK	= 14;
		private int			stoch_Smooth	= 3;
		private int			stochfast_PeriodD= 3;
		private int			stochfast_PeriodK= 14;

		private bool initDone = false;
		private ISeries<double> Indicator;
		private int 		[] HighBarsAgo;
		private int 		[] LowBarsAgo;	
		private int 		ThisHigh;
		private int			ThisLow;
		private int 		QHLength	 		= 0;
		private int			QLLength	 		= 0; 
		private int 		queueLength 		= 3;	//WH changed from QueueLength to queueLength so that could include parameter @ Ln1297-1304
		private int 		scanWidth 			= 10; //Lookback period for finding Higher Highs and Lower Lows - WH changed from 30 to 10
		private int 		rdivlinelookbackperiod = 30; //24/09/2017 WH added - LookbackPeriod for regular divergence line
		private int 		hdivlinelookbackperiod = 30; //24/09/2017 WH added - LookbackPeriod for hidden divergence line	- set to same as scanWidth if you do not want hidden diverge lines & 2-3x higher to see them	
		private int 		A 					= 1;		
		private int 		BarsAgo;
		private double 		priceDiffLimit 		= 0.0;
		private double 		indicatorDiffLimit  = 0.0;
		
		private DashStyleHelper divergenceDashStyle 	= DashStyleHelper.Dot;
		private int divergenceLineWidth 				= 2; //WH changed to 2 from 3
		private int markerDistanceFactor 				= 1;
		private Brush divergenceColor 					= Brushes.DarkMagenta;
		private Brush hiddenDivergenceColor 			= Brushes.DarkBlue;
		private Brush lowerDotColor 					= Brushes.Cyan;
		private Brush upperDotColor 					= Brushes.Salmon; //Changed from yellow - WH 10/06/2017
		
		private string myAlert1 = @"C:\Program Files (x86)\NinjaTrader 8\sounds\Alert1.wav";
		private string myAlert2 = @"C:\Program Files (x86)\NinjaTrader 8\sounds\Alert2.wav";
		
//		private int			do_Period			= 14;
//		private int 		do_Smooth1 			= 5;
//        private int 		do_Smooth2 			= 3;
//        private int 		do_Smooth3			= 9;	
		
		string dot="l"; // wingdings - added by WH 09/06/2017 to reduce the size of the dot by using wingdings so Draw.Text instead of Draw.dot
		private int dotsize = 13; // default size of wingdings dot
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Divergence spotter Ver 3.0 - based on the work of David Anderson (5/2009)";
				Name						= "D3SpotterV3B";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= false;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= false;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Left; //Changed from right to left
				IsSuspendedWhileInactive	= false;  // set to false because of alerts
				
				AddPlot(Brushes.Green, "IndicPlot0");
				AddPlot(Brushes.DarkViolet, "IndicPlot1");
				AddPlot(Brushes.Red, "IndicPlot2");
				AddPlot(new Stroke(Brushes.Navy, 2), PlotStyle.Bar, "IndicPlot3");
				AddLine(Brushes.DarkGray, 0, "IndicLine0");
				AddLine(Brushes.DarkGray, 0, "IndicLine1");
				AddLine(Brushes.DarkGray, 0, "IndicLine2");
				AddLine(Brushes.DarkGray, 0, "IndicLine3");
				AddLine(Brushes.DarkGray, 0, "IndicLine4");
				AddLine(Brushes.DarkGray, 500, "InidicLine5");
				AddLine(Brushes.DarkGray, -500, "IndicLine6");
				
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
			
			else if (State == State.DataLoaded) //WH added 09/06/2017
			{				
				Name = ""; //See State == State.SetDefaults to set name so it appears in indicator list
			}
			
		}

//		public override string DisplayName
//		{
//    		get { return " D3SpotterV3B";}
//		}	
		
		
		protected override void OnBarUpdate()
		{
			double PriceDiff, IndicatorDiff;
		
			if (CurrentBar < ScanWidth) return;
			
			if (initDone == false)
			{
				switch (Method) 
					{
						case D3SpotIndicatorMethod.RSI:
							InitRSI();
							break;
				
						case D3SpotIndicatorMethod.MACD:
							InitMACD();
							break;
				
						case D3SpotIndicatorMethod.MACDdiff:
							InitMACD();
							break;
				
						case D3SpotIndicatorMethod.MACDhistOnly:
							InitMACDhistOnly();
							break;
					
						case D3SpotIndicatorMethod.CCI:
							InitCCI();
							break;
						
//						case D3SpotIndicatorMethod.CCI_JMA_MASM:
//							InitCCI_JMA_MASM();
//							break;
				
						case D3SpotIndicatorMethod.StochasticsD:
							InitStochastics();
							break;
				
						case D3SpotIndicatorMethod.StochasticsK:
							InitStochastics();
							break;

						case D3SpotIndicatorMethod.StochasticsFastD:
							InitStochastics();
							break;
				
						case D3SpotIndicatorMethod.StochasticsFastK:
							InitStochastics();
							break;
				
						case D3SpotIndicatorMethod.StochRSI:
							InitStochRSI();
							break;
					
						case D3SpotIndicatorMethod.MFI:
							InitMFI();
							break;

						case D3SpotIndicatorMethod.ROC:
							InitROC();
							break;
					
						case D3SpotIndicatorMethod.RVI:
							InitRVI();
							break;
					
//						case D3SpotIndicatorMethod.SMI:
//							InitSMI();
//							break;
					
//						case D3SpotIndicatorMethod.DO:
//							InitDO();
//							break;
					
						case D3SpotIndicatorMethod.Momentum:
							InitMomentum();
							break;				
					}
				} 
			
			switch (Method) 
			{
				case D3SpotIndicatorMethod.RSI:	InitRSI(); PlotRSI(); break;										
					
				case D3SpotIndicatorMethod.MACD:	InitMACD(); PlotMACD(); break;										
				
				case D3SpotIndicatorMethod.MACDdiff:	InitMACD(); PlotMACDdiff(); break;								
				
				case D3SpotIndicatorMethod.MACDhistOnly:	InitMACDhistOnly(); PlotMACDhistOnly(); break;				
				
				case D3SpotIndicatorMethod.CCI:	InitCCI(); PlotCCI(); break;
					
//				case D3SpotIndicatorMethod.CCI_JMA_MASM: InitCCI_JMA_MASM(); PlotCCI_JMA_MASM(); break;
				
				case D3SpotIndicatorMethod.StochasticsD:  InitStochastics(); PlotStochasticsD(); break;				

				case D3SpotIndicatorMethod.StochasticsK: InitStochastics(); PlotStochasticsK(); break;					

				case D3SpotIndicatorMethod.StochasticsFastD:  InitStochastics(); PlotStochasticsFastD(); break;			
					
				case D3SpotIndicatorMethod.StochasticsFastK: InitStochastics(); PlotStochasticsFastK(); break;			
				
				case D3SpotIndicatorMethod.StochRSI: InitStochRSI(); PlotStochRSI(); break;								
					
				case D3SpotIndicatorMethod.MFI: InitMFI(); PlotMFI(); break;											
					
				case D3SpotIndicatorMethod.ROC: InitROC(); PlotROC(); break;											
					
				case D3SpotIndicatorMethod.RVI: InitRVI(); PlotRVI(); break;											
					
//				case D3SpotIndicatorMethod.SMI: InitSMI(); PlotSMI(); break;											
					
//				case D3SpotIndicatorMethod.DO: InitDO(); PlotDO(); break;												
					
				case D3SpotIndicatorMethod.Momentum: InitMomentum(); PlotMomentum(); break;								
					
			}
			
//--------------------------------------------------------------------
			
			switch (PType) 
			{
				case D3SpotPriceType.High_Low:	
					ThisHigh = HighestBar(High, ScanWidth);
					break;
					
				case D3SpotPriceType.Open_Close: 
					ThisHigh = HighestBar(Close, ScanWidth);
					break;
				
				case D3SpotPriceType.SMA1:
					ThisHigh = HighestBar(SMA(High,1), ScanWidth);
					break;
					
				case D3SpotPriceType.EMA:
					ThisHigh = HighestBar(EMA(High,1), ScanWidth);
					break;
					
//				case D3SpotPriceType.JMA_MASM:
//					ThisHigh = HighestBar(JMA_MASM(High,1,0), ScanWidth);
//					break;
			}
			
			if (ThisHigh == A) 
			{

				if (ShowAlerts == true) 
				{
					Alert("MyAlrt1" + CurrentBar.ToString(), Priority.High, "D3SpotterV3B: High Found", MyAlert1, 10, Brushes.Black, Brushes.Yellow);
				}
				
				for (int i = QueueLength-1; i >= 1; i--) 
				{
					HighBarsAgo[i] = HighBarsAgo[i-1];
				}
			
				HighBarsAgo[0] = CurrentBar - A;
				
				//WH used Draw.Text with wingding for dot which can be resized, instead of Draw.Dot, which has larger dot fixed to bar size 
				Draw.Text(this, "Hdot" + CurrentBar.ToString(), true, dot, A, High[A] + (TickSize * MarkerDistanceFactor),0, UpperDotColor, new SimpleFont("Wingdings",dotsize), TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
//				Draw.Dot(this, "Hdot" + CurrentBar.ToString(), true, A, High[A] + (TickSize * MarkerDistanceFactor), UpperDotColor);
				DrawOnPricePanel = false;
				
				Draw.Text(this, "IHdot" + CurrentBar.ToString(), true, dot, A, Indicator[A],0, UpperDotColor, new SimpleFont("Wingdings",dotsize), TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
//				Draw.Dot(this, "IHdot" + CurrentBar.ToString(), true, A, Indicator[A], UpperDotColor);
				DrawOnPricePanel = true;
				
				if (++QHLength >= 2) 
				{
					for(int i = 0; i < Math.Min(QHLength, QueueLength); i++) 
					{
						BarsAgo = CurrentBar - HighBarsAgo[i];
						
						IndicatorDiff	= Indicator[A] - Indicator[BarsAgo];

						switch (PType) 
						{
							case D3SpotPriceType.High_Low:	
								PriceDiff	= High[A] - High[BarsAgo];
								break;
								
							case D3SpotPriceType.Open_Close: 
								PriceDiff	= Close[A] - Close[BarsAgo];
								break;
							
							case D3SpotPriceType.SMA1:
								PriceDiff	= SMA(High, 1)[A] - SMA(High, 1)[BarsAgo];
								break;
								
							default :
								PriceDiff	= High[A] - High[BarsAgo];
								break;
						}
						//WH adjusted original code 23/09/2017
//						if (((IndicatorDiff < IndicatorDiffLimit) && (PriceDiff >= PriceDiffLimit)) || ((IndicatorDiff > IndicatorDiffLimit) 
//							&& (PriceDiff <= PriceDiffLimit))) 
						if (IndicatorDiff < IndicatorDiffLimit && (PriceDiff > PriceDiffLimit || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) 							
						{							
//							if ((BarsAgo - A) < ScanWidth)
							if ((BarsAgo - A) < rdivlinelookbackperiod) //WH changed above line to this
							{
								if (ShowAlerts == true) 
								{
									Alert("MyAlrt2" + CurrentBar.ToString(), Priority.High, "D3SpotterV3B: Divergence Found", MyAlert2, 10, Brushes.Black, Brushes.Red);
								}
								
								Draw.Line(this, "high"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, High[BarsAgo] + (TickSize * MarkerDistanceFactor), A, 
									High[A] + (TickSize * MarkerDistanceFactor), divergenceColor, divergenceDashStyle, divergenceLineWidth);	
								
								Draw.TriangleDown(this, "MyTriDown"+CurrentBar.ToString(), true, 0, High[0] + (TickSize * MarkerDistanceFactor), Brushes.Red);

								DrawOnPricePanel = false;	
								Draw.Line(this, "IH"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Indicator[BarsAgo], A, Indicator[A], divergenceColor, 
									divergenceDashStyle, divergenceLineWidth);
								
								DrawOnPricePanel = true;
								
							}	
						}
						else if (IndicatorDiff > IndicatorDiffLimit && (PriceDiff < PriceDiffLimit || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) //WH Added to give different color to hidden divergence
						{							
//							if ((BarsAgo - A) < ScanWidth)
							if ((BarsAgo - A) < hdivlinelookbackperiod) //WH changed above line to this
							{
								if (ShowAlerts == true) 
								{
									Alert("MyAlrt2" + CurrentBar.ToString(), Priority.High, "D3SpotterV3B: Divergence Found", MyAlert2, 10, Brushes.Black, Brushes.Red);
								}
								
								Draw.Line(this, "high"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, High[BarsAgo] + (TickSize * MarkerDistanceFactor), A, 
									High[A] + (TickSize * MarkerDistanceFactor), hiddenDivergenceColor, divergenceDashStyle, divergenceLineWidth);	
								
								Draw.TriangleDown(this, "MyTriDown"+CurrentBar.ToString(), true, 0, High[0] + (TickSize * MarkerDistanceFactor), Brushes.Red);

								DrawOnPricePanel = false;	
								Draw.Line(this, "IH"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Indicator[BarsAgo], A, Indicator[A], hiddenDivergenceColor, 
									divergenceDashStyle, divergenceLineWidth);
								
								DrawOnPricePanel = true;
								
							}	
						}
					}	
				}	
			}
			
			switch (PType) 
			{
				case D3SpotPriceType.High_Low:	
					ThisLow = LowestBar(Low, ScanWidth);
					break;
					
				case D3SpotPriceType.Open_Close: 
					ThisLow = LowestBar(Close, ScanWidth);
					break;
					
				case D3SpotPriceType.SMA1:
					ThisLow = LowestBar(SMA(Low,1), ScanWidth);
					break;
					
				case D3SpotPriceType.EMA:
					ThisLow = LowestBar(EMA(Low,1), ScanWidth);
					break;
					
//				case D3SpotPriceType.JMA_MASM:
//					ThisLow = LowestBar(JMA_MASM(Low,1,0), ScanWidth);
//					break;
			}
				
			if (ThisLow == A) 
			{					
				for (int i = QueueLength-1; i >= 1; i--) 
				{
					LowBarsAgo[i] = LowBarsAgo[i-1];
				}
			
				LowBarsAgo[0] = CurrentBar - A;
				
				//WH used Draw.Text with wingding for dot which can be resized, instead of Draw.Dot, which has larger dot fixed to bar size 
				Draw.Text(this, "Ldot" + CurrentBar.ToString(), true, dot, A, Low[A] - (TickSize * MarkerDistanceFactor),0, LowerDotColor, new SimpleFont("Wingdings",dotsize), TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
//				Draw.Dot(this, "Ldot" + CurrentBar.ToString(), true, A, Low[A] - (TickSize * MarkerDistanceFactor), LowerDotColor);
				DrawOnPricePanel = false;
				
				Draw.Text(this, "ILdot" + CurrentBar.ToString(), true, dot, A, Indicator[A],0, LowerDotColor, new SimpleFont("Wingdings",dotsize), TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
//				Draw.Dot(this, "ILdot" + CurrentBar.ToString(), true, A, Indicator[A], LowerDotColor);
				DrawOnPricePanel = true;

				if (ShowAlerts == true) 
				{
					Alert("MyAlrt1" + CurrentBar.ToString(), Priority.High, "D3SpotterV3B: Low Found", MyAlert1, 10, Brushes.Black, Brushes.Yellow);
				}
				
				if (++QLLength >= 2) 
				{
					for(int i = 0; i < Math.Min(QLLength, QueueLength); i++) 
					{
						BarsAgo = CurrentBar - LowBarsAgo[i];
						
						IndicatorDiff 	= Indicator[A] - Indicator[BarsAgo];
						switch (PType) 
						{
							case D3SpotPriceType.High_Low:	
								PriceDiff 		= Low[A] - Low[BarsAgo];	
								break;
								
							case D3SpotPriceType.Open_Close: 
								PriceDiff 		= Close[A] - Close[BarsAgo];	
								break;
							
							case D3SpotPriceType.SMA1:
								PriceDiff 		= SMA(Close,1)[A] - SMA(Close,1)[BarsAgo];	
								break;
								
							default:
								PriceDiff 		= Low[A] - Low[BarsAgo];	
								break;
						}	
						
						//WH adjusted original code 23/09/2017
//						if (((IndicatorDiff > IndicatorDiffLimit) && (PriceDiff <= PriceDiffLimit)) || ((IndicatorDiff < IndicatorDiffLimit) 
//							&& (PriceDiff >= PriceDiffLimit))) 
						if (IndicatorDiff > IndicatorDiffLimit && (PriceDiff < PriceDiffLimit || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) 							 
						{	
//							if ((BarsAgo - A) < ScanWidth)
							if ((BarsAgo - A) < rdivlinelookbackperiod) //WH changed above line to this
							{  
								Draw.Line(this, "low"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Low[BarsAgo] - (TickSize * MarkerDistanceFactor), 
									A, Low[A] - (TickSize * MarkerDistanceFactor), DivergenceColor, DivergenceDashStyle, DivergenceLineWidth);
									
								Draw.TriangleUp(this, "MyTriUp"+CurrentBar.ToString(), true, 0, Low[0] - (TickSize * MarkerDistanceFactor), Brushes.Green);
									
								DrawOnPricePanel = false;
								
								Draw.Line(this, "Ilow"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Indicator[BarsAgo],
									A, Indicator[A], divergenceColor, divergenceDashStyle, divergenceLineWidth);
								
								DrawOnPricePanel = true;
								
								if (ShowAlerts == true) {
									Alert("MyAlrt2" + CurrentBar.ToString(), Priority.High, "D3SpotterV3B: Divergence Found", MyAlert2, 10, Brushes.Black, Brushes.Red);
								}
							}
						}
						else if (IndicatorDiff < IndicatorDiffLimit && (PriceDiff > PriceDiffLimit || PriceDiff.ApproxCompare(PriceDiffLimit)==0)) 
						{	
//							if ((BarsAgo - A) < ScanWidth)
							if ((BarsAgo - A) < hdivlinelookbackperiod) //WH changed above line to this
							{  
								Draw.Line(this, "low"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Low[BarsAgo] - (TickSize * MarkerDistanceFactor), 
									A, Low[A] - (TickSize * MarkerDistanceFactor), hiddenDivergenceColor, DivergenceDashStyle, DivergenceLineWidth);
									
								Draw.TriangleUp(this, "MyTriUp"+CurrentBar.ToString(), true, 0, Low[0] - (TickSize * MarkerDistanceFactor), Brushes.Green);
									
								DrawOnPricePanel = false;
								
								Draw.Line(this, "Ilow"+CurrentBar.ToString() + BarsAgo.ToString(), true, BarsAgo, Indicator[BarsAgo],
									A, Indicator[A], hiddenDivergenceColor, divergenceDashStyle, divergenceLineWidth);
								
								DrawOnPricePanel = true;
								
								if (ShowAlerts == true) {
									Alert("MyAlrt2" + CurrentBar.ToString(), Priority.High, "D3SpotterV3B: Divergence Found", MyAlert2, 10, Brushes.Black, Brushes.Red);
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

		#region Configured Indicators
	

		private void InitRSI()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Green;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Orange;
				Plots[1].Width = 1;
				Plots[1].DashStyleHelper = DashStyleHelper.Solid;
				Plots[1].PlotStyle = PlotStyle.Line;
				
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;

				Lines[0].Brush = Brushes.DarkViolet;
				Lines[0].Value=30;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.YellowGreen;
				Lines[1].Value=70;
				Lines[1].DashStyleHelper = DashStyleHelper.Solid;
				Lines[1].Width = 1;	
				
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}

		private void PlotRSI()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "RSI", "Using: RSI("+RSI_Period.ToString()+","+RSI_Smooth.ToString()+")", TextPosition.TopLeft);
			DrawOnPricePanel = true;
			
			Indicator = RSI(Close, RSI_Period,RSI_Smooth);
			foundValue = Indicator[0];
			foundAvg = RSI(Close, RSI_Period,RSI_Smooth).Avg[0];
	
			IndicPlot0[0] = foundValue;
			IndicPlot1[0] = foundAvg;
		}

		//
		// The RVI (Relative Volatility Index) was developed by Donald Dorsey as a compliment to and a confirmation of momentum based indicators. When used to confirm other signals, only buy when the RVI is over 50 and only sell when the RVI is under 50.
		//
		private void InitRVI()
		{

			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.DarkOrange;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Transparent;
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;

				Lines[0].Brush = Brushes.LightGray;
				Lines[0].Value=50;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.Transparent;
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}
		
		private void PlotRVI()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "RVI", "Using: RVI("+RVI_Period.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;

			Indicator = RVI(RVI_Period);
			foundValue = Indicator[0];

			IndicPlot0[0] = foundValue;
		}
		
		//
		// The MACD (Moving Average Convergence/Divergence) is a trend following momentum indicator that shows the relationship between two moving averages of prices.
		//
		private void InitMACD()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Green;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.DarkViolet;
				Plots[1].Width = 1;
				Plots[1].DashStyleHelper = DashStyleHelper.Solid;
				Plots[1].PlotStyle = PlotStyle.Line;

				Plots[2].Brush = Brushes.Transparent;
				
				Plots[3].Brush = Brushes.Navy;
				Plots[3].Width = 2;
				Plots[3].DashStyleHelper = DashStyleHelper.Solid;
				Plots[3].PlotStyle = PlotStyle.Bar;


				Lines[0].Brush = Brushes.DarkGray;
				Lines[0].Value = 0;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.Transparent;
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}

		private void InitMACDhistOnly()
		{

			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Navy;
				Plots[0].Width = 2;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;

				Plots[1].Brush = Brushes.Transparent;
				Plots[2].Brush = Brushes.Transparent;
				
				Plots[3].Brush = Brushes.Navy;
				Plots[3].Width = 2;
				Plots[3].DashStyleHelper = DashStyleHelper.Solid;
				Plots[3].PlotStyle = PlotStyle.Bar;


				Lines[0].Brush = Brushes.DarkGray;
				Lines[0].Value = 0;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.Transparent;
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}
		
		private void PlotMACD()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "MACD", "Using: MACD("+Macd_Fast.ToString()+","+Macd_Slow.ToString()+","+Macd_Smooth.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			
			Indicator = MACD(Macd_Fast, Macd_Slow, Macd_Smooth);
			
			foundValue = Indicator[0];
			foundAvg = MACD(Macd_Fast, Macd_Slow, Macd_Smooth).Avg[0];
			foundDiff = MACD(Macd_Fast, Macd_Slow, Macd_Smooth).Diff[0];

			IndicPlot0[0] = foundValue;
			IndicPlot1[0] = foundAvg;
			IndicPlot3[0] = foundDiff;
		}
				
		private void PlotMACDdiff()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "MACDdiff", "Using: MACDdiff("+Macd_Fast.ToString()+","+Macd_Slow.ToString()+","+Macd_Smooth.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			
			Indicator = MACD(Macd_Fast, Macd_Slow, Macd_Smooth).Diff;
			
			foundValue = MACD(Macd_Fast, Macd_Slow, Macd_Smooth)[0];
			foundAvg = MACD(Macd_Fast, Macd_Slow, Macd_Smooth).Avg[0];
			foundDiff = MACD(Macd_Fast, Macd_Slow, Macd_Smooth).Diff[0];

			IndicPlot0[0] = foundValue;
			IndicPlot1[0] = foundAvg;
			IndicPlot3[0] = foundDiff;			
		}

		private void PlotMACDhistOnly()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "MACDhistogram", "Using: MACDhistogram("+Macd_Fast.ToString()+","+Macd_Slow.ToString()+","+Macd_Smooth.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			
			Indicator = MACD(Macd_Fast, Macd_Slow, Macd_Smooth).Diff;
			foundValue = Indicator[0];

			IndicPlot0[0] = foundValue;
			IndicPlot3[0] = foundValue;			
		}

		
		//
		// The Commodity Channel Index (CCI) measures the variation of a security's price from its statistical mean. High values show that prices are unusually high compared to average prices whereas low values indicate that prices are unusually low.
		//
		private void InitCCI()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Orange;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Transparent;
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;


				Lines[0].Brush = Brushes.DarkGray;
				Lines[0].Value = 0;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.DarkGray;
				Lines[1].Value=100;
				Lines[1].DashStyleHelper = DashStyleHelper.Solid;
				Lines[1].Width = 1;

				Lines[2].Brush = Brushes.DarkGray;
				Lines[2].Value = 200;
				Lines[2].DashStyleHelper = DashStyleHelper.Solid;
				Lines[2].Width = 1;

				Lines[3].Brush = Brushes.DarkGray;
				Lines[3].Value = -100;
				Lines[3].DashStyleHelper = DashStyleHelper.Solid;
				Lines[3].Width = 1;

				Lines[4].Brush = Brushes.DarkGray;
				Lines[4].Value = -200;
				Lines[4].DashStyleHelper = DashStyleHelper.Solid;
				Lines[4].Width = 1;

				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}

		private void PlotCCI()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "CCI", "Using: CCI("+CCI_Period.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			
			Indicator = CCI(CCI_Period);
			foundValue = Indicator[0];

			IndicPlot0[0] = foundValue;	
		}

//		private void InitCCI_JMA_MASM()
//		{
//			if (useDefaultPlot == true)
//			{
//				Plots[0].Brush = Brushes.Orange;
//				Plots[0].Width = 1;
//				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
//				Plots[0].PlotStyle = PlotStyle.Line;
				
//				Plots[1].Brush = Brushes.Transparent;
//				Plots[2].Brush = Brushes.Transparent;
//				Plots[3].Brush = Brushes.Transparent;


//				Lines[0].Brush = Brushes.DarkGray;
//				Lines[0].Value = 0;
//				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
//				Lines[0].Width = 1;

//				Lines[1].Brush = Brushes.DarkGray;
//				Lines[1].Value=100;
//				Lines[1].DashStyleHelper = DashStyleHelper.Solid;
//				Lines[1].Width = 1;

//				Lines[2].Brush = Brushes.DarkGray;
//				Lines[2].Value = 200;
//				Lines[2].DashStyleHelper = DashStyleHelper.Solid;
//				Lines[2].Width = 1;

//				Lines[3].Brush = Brushes.DarkGray;
//				Lines[3].Value = -100;
//				Lines[3].DashStyleHelper = DashStyleHelper.Solid;
//				Lines[3].Width = 1;

//				Lines[4].Brush = Brushes.DarkGray;
//				Lines[4].Value = -200;
//				Lines[4].DashStyleHelper = DashStyleHelper.Solid;
//				Lines[4].Width = 1;

//				Lines[5].Brush = Brushes.Transparent;
//				Lines[6].Brush = Brushes.Transparent;
//			}
//		}

//		private void PlotCCI_JMA_MASM()
//		{
//			DrawOnPricePanel = false;
//			Draw.TextFixed(this, "CCI_LMA_MASM", "Using: CCI_JMA_MASM("+CCI_JAM_MASM_Period.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
//			DrawOnPricePanel = true;
			
//			Indicator = CCI_JMA_MASM(CCI_JAM_MASM_Period,0, CCI_JAM_MASM_Period);
//			foundValue = Indicator[0];

//			IndicPlot0[0] = foundValue;	
//		}
		//
		//	Stochastics
		//
		private void InitStochastics()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Transparent; //WH changed from Green to DodgerBlue to Transparent
				Plots[0].Width = 2; //WH changed from 1 to 2
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Transparent; //WH changed from Orange to Transparent
				Plots[1].Width = 2; //WH changed from 1 to 2
				Plots[1].DashStyleHelper = DashStyleHelper.Solid;
				Plots[1].PlotStyle = PlotStyle.Line;
				//Multicoloured plot - added WH 13/07/2017
//				if(StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).K[0]-StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).D[0] >
//					StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).K[1]-StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).D[1])
//				PlotBrushes[1][0] = Brushes.Lime;
//				else if(StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).K[0]-StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).D[0] <
//					StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).K[1]-StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).D[1])
//				PlotBrushes[1][0] = Brushes.Red;
//				else
//				PlotBrushes[1][0] = Brushes.Orange;	
				
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;

				Lines[0].Brush = Brushes.DarkViolet;
				Lines[0].Value = 20;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 2; //WH changed from 1 to 2

				Lines[1].Brush = Brushes.YellowGreen;
				Lines[1].Value = 80;
				Lines[1].DashStyleHelper = DashStyleHelper.Solid;
				Lines[1].Width = 2; //WH changed from 1 to 2
				
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}
		
		private void PlotStochasticsD()
		{
			DrawOnPricePanel = false; //WH changed text position from TopLeft to BottomLeft
			Draw.TextFixed(this, "Stochastics", "Using: StochasticsD("+Stoch_PeriodD.ToString()+","+Stoch_PeriodK.ToString()+","+Stoch_Smooth.ToString()+")", TextPosition.BottomLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;

			Indicator = Stochastics(Stoch_PeriodD, Stoch_PeriodK, Stoch_Smooth).D;
			foundStochD = Indicator[0];
			foundStochK = Stochastics(Stoch_PeriodD, Stoch_PeriodK, Stoch_Smooth).K[0];

			IndicPlot0[0] = foundStochD;
			IndicPlot1[0] = foundStochK;
		}
		
		private void PlotStochasticsK()
		{
			DrawOnPricePanel = false; //WH changed text position from TopLeft to BottomLeft
			Draw.TextFixed(this, "Stochastics", "Using: StochasticsK("+Stoch_PeriodD.ToString()+","+Stoch_PeriodK.ToString()+","+Stoch_Smooth.ToString()+")", TextPosition.BottomLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;

			Indicator = Stochastics(Stoch_PeriodD, Stoch_PeriodK, Stoch_Smooth).K;
			foundStochK = Indicator[0];
			foundStochD = Stochastics(Stoch_PeriodD, Stoch_PeriodK, Stoch_Smooth).D[0];

			IndicPlot0[0] = foundStochD;
			IndicPlot1[0] = foundStochK;
		}

		private void PlotStochasticsFastD()
		{
			DrawOnPricePanel = false; //WH changed text position from TopLeft to BottomLeft
			Draw.TextFixed(this, "Using: StochasticsFast", "StochasticsFastD("+StochFast_PeriodD.ToString()+","+StochFast_PeriodK.ToString()+")", TextPosition.BottomLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;

			Indicator = StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).D;
			foundStochD = Indicator[0];
			foundStochK = StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).K[0];

			IndicPlot0[0] = foundStochD;
			IndicPlot1[0] = foundStochK;
		}
		
		private void PlotStochasticsFastK()
		{
			DrawOnPricePanel = false; //WH changed text position from TopLeft to BottomLeft
			Draw.TextFixed(this, "StochasticsFast", "Using: StochasticsFastK("+StochFast_PeriodD.ToString()+","+StochFast_PeriodK.ToString()+")", TextPosition.BottomLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;

			Indicator = StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).K;
			foundStochK = Indicator[0];
			foundStochD = StochasticsFast(StochFast_PeriodD, StochFast_PeriodK).D[0];

			IndicPlot0[0] = foundStochD;
			IndicPlot1[0] = foundStochK;
		}
		
		//
		//	MFI
		//
		private void InitMFI()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Orange;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Transparent;
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;


				Lines[0].Brush = Brushes.DarkViolet;
				Lines[0].Value = 20;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.YellowGreen;
				Lines[1].Value = 80;
				Lines[1].DashStyleHelper = DashStyleHelper.Solid;
				Lines[1].Width = 1;
				
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}
		
		private void PlotMFI()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "MFI", "Using: MFI("+MFI_Period.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;

			Indicator = MFI(MFI_Period);
			foundValue = Indicator[0];

			IndicPlot0[0] = foundValue;
		}
		
		//
		//	ROC
		//
		private void InitROC()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Blue;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Transparent;
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;


				Lines[0].Brush = Brushes.DarkGray;
				Lines[0].Value = 0;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.Transparent;
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}
		
		private void PlotROC()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "ROC", "Using: ROC("+ROC_Period.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;

			Indicator = ROC(ROC_Period);
			foundValue = Indicator[0];

			IndicPlot0[0] = foundValue;
		}
		
		//
		//	StochRSI
		//
		private void InitStochRSI()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Green;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Transparent;
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;


				Lines[0].Brush = Brushes.Blue;
				Lines[0].Value = 0.5;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.Red;
				Lines[1].Value = 0.8;
				Lines[1].DashStyleHelper = DashStyleHelper.Solid;
				Lines[1].Width = 1;

				Lines[2].Brush = Brushes.Red;
				Lines[2].Value = 0.2;
				Lines[2].DashStyleHelper = DashStyleHelper.Solid;
				Lines[2].Width = 1;


				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}
		
		private void PlotStochRSI()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "Using: StochRSI", "StochRSI("+StochRSI_Period.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			
			Indicator = StochRSI(StochRSI_Period);
			foundValue = Indicator[0];

			IndicPlot0[0] = foundValue;
		}
		
		//
		//	SMI
		//
//		private void InitSMI()
//		{
//			if (useDefaultPlot == true)
//			{
//				Plots[0].Brush = Brushes.Green;
//				Plots[0].Width = 2;
//				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
//				Plots[0].PlotStyle = PlotStyle.Line;
				
//				Plots[1].Brush = Brushes.Orange;
//				Plots[1].Width = 1;
//				Plots[1].DashStyleHelper = DashStyleHelper.Solid;
//				Plots[1].PlotStyle = PlotStyle.Line;
				
//				Plots[2].Brush = Brushes.Transparent;
//				Plots[3].Brush = Brushes.Transparent;


//				Lines[0].Brush = Brushes.DarkGray;
//				Lines[0].Value = 0;
//				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
//				Lines[0].Width = 1;

//				Lines[1].Brush = Brushes.Transparent;
//				Lines[2].Brush = Brushes.Transparent;
//				Lines[3].Brush = Brushes.Transparent;
//				Lines[4].Brush = Brushes.Transparent;
//				Lines[5].Brush = Brushes.Transparent;
//				Lines[6].Brush = Brushes.Transparent;
//			}
//		}
		
//		private void PlotSMI()
//		{
//			DrawOnPricePanel = false;
//			Draw.TextFixed(this, "SMI", "Using: SMI("+SMI_EMAPeriod1.ToString()+","+SMI_EMAPeriod2.ToString()+","+SMI_Range.ToString()
//				+","+SMI_SMIEMAPeriod.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
//			DrawOnPricePanel = true;
			
//			Indicator = SMI(SMI_EMAPeriod1, SMI_EMAPeriod2, SMI_Range, SMI_SMIEMAPeriod);
//			foundValue = Indicator[0];
//			foundAvg = SMI(SMI_EMAPeriod1, SMI_EMAPeriod2, SMI_Range, SMI_SMIEMAPeriod).SMIEMA[0];

//			IndicPlot0[0] = foundValue;
//			IndicPlot1[0] = foundAvg;
//		}

		//
		// "Constance Brown's Derivative Oscillator as pusblished in 'Technical Analysis for the Trading Professional' p. 293")
		//
//		private void InitDO()
//		{
//			if (useDefaultPlot == true)
//			{
//				Plots[0].Brush = Brushes.Black;
//				Plots[0].Width = 2;
//				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
//				Plots[0].PlotStyle = PlotStyle.Line;
				
//				Plots[1].Brush = Brushes.Blue; 
//				Plots[1].Width = 2;
//				Plots[1].DashStyleHelper = DashStyleHelper.Solid;
//				Plots[1].PlotStyle = PlotStyle.Bar;
//				Plots[1].Min = 0;
				
//				Plots[2].Brush = Brushes.Red;
//				Plots[2].Width = 2;
//				Plots[2].DashStyleHelper = DashStyleHelper.Solid;
//				Plots[2].PlotStyle = PlotStyle.Bar;
//				Plots[2].Max = 0;

//				Plots[3].Brush = Brushes.Transparent;


//				Lines[0].Brush = Brushes.DarkOliveGreen;
//				Lines[0].Value = 0;
//				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
//				Lines[0].Width = 1;

//				Lines[1].Brush = Brushes.Transparent;
//				Lines[2].Brush = Brushes.Transparent;
//				Lines[3].Brush = Brushes.Transparent;
//				Lines[4].Brush = Brushes.Transparent;
//				Lines[5].Brush = Brushes.Transparent;
//				Lines[6].Brush = Brushes.Transparent;
//			}
//		}
			
//		private void PlotDO()
//		{
//			DrawOnPricePanel = false;
//			Draw.TextFixed(this, "DO", "Using: DO("+DO_Period.ToString()+","+DO_Smooth1.ToString()+","+DO_Smooth2.ToString()+","+DO_Smooth3.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
//			DrawOnPricePanel = true;

//			Indicator = DerivativeOscillator(DO_Period, DO_Smooth1, DO_Smooth2, DO_Smooth3);
//			foundValue = Indicator[0];

//			IndicPlot0[0] = foundValue;
//			IndicPlot1[0] = foundValue;
//			IndicPlot2[0] = foundValue;
//		}	
		//
		// NinjaTraders Momentum indicator
		//
		private void InitMomentum()
		{
			if (useDefaultPlot == true)
			{
				Plots[0].Brush = Brushes.Green;
				Plots[0].Width = 1;
				Plots[0].DashStyleHelper = DashStyleHelper.Solid;
				Plots[0].PlotStyle = PlotStyle.Line;
				
				Plots[1].Brush = Brushes.Transparent;
				Plots[2].Brush = Brushes.Transparent;
				Plots[3].Brush = Brushes.Transparent;


				Lines[0].Brush = Brushes.DarkViolet;
				Lines[0].Value = 0;
				Lines[0].DashStyleHelper = DashStyleHelper.Solid;
				Lines[0].Width = 1;

				Lines[1].Brush = Brushes.Transparent;
				Lines[2].Brush = Brushes.Transparent;
				Lines[3].Brush = Brushes.Transparent;
				Lines[4].Brush = Brushes.Transparent;
				Lines[5].Brush = Brushes.Transparent;
				Lines[6].Brush = Brushes.Transparent;
			}
		}
			
		private void PlotMomentum()
		{
			DrawOnPricePanel = false;
			Draw.TextFixed(this, "Momentum", "Using: Momentum("+MOM_Period.ToString()+")", TextPosition.TopLeft); //, Color.Black, new Font("Arial", 10), Color.Black, Color.Black, 5);
			DrawOnPricePanel = true;
			
			Indicator = Momentum(Close, MOM_Period);
			foundValue = Indicator[0];

			IndicPlot0[0] = foundValue;
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
		public D3SpotIndicatorMethod Method
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
		
		[Range(1, int.MaxValue)] //WH added 05/07/2017 - see Ln106
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
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="hDiv Line Lookback Bars", Description="hidden divergence line Lookback", Order=7, GroupName="Control")]	//WH Added			
		public int Hdivlinelookbackperiod
		{
			get { return hdivlinelookbackperiod; }
			set { hdivlinelookbackperiod = Math.Max(10, value); }
		}

		[NinjaScriptProperty]
		[Display(Name="Price Type", Description="Price Type", Order=8, GroupName="Control")]						
		public D3SpotPriceType PType
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
		[Display(Name="Color", Description="Color of hidden divergence line plotted", Order=0, GroupName="Divergence")]		//WH added
		public Brush HiddenDivergenceColor 
		{
			get { return hiddenDivergenceColor; }
			set { hiddenDivergenceColor = value;; }
		}	
		[Browsable(false)]
		public string hiddenDivergenceColorSerializable
		{
			get { return Serialize.BrushToString(hiddenDivergenceColor); }
			set { hiddenDivergenceColor = Serialize.StringToBrush(value); }
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
		
		[XmlIgnore]
		[Display(Name="Lower Dots", Order=5, GroupName="Divergence")]		
		public Brush LowerDotColor
		{
			get { return lowerDotColor; }
			set { lowerDotColor = value;; }
		}	
		[Browsable(false)]
		public string lowerDotColorSerializable
		{
			get { return Serialize.BrushToString(lowerDotColor); }
			set { lowerDotColor = Serialize.StringToBrush(value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Marker Distance", Description="Marker distance (ticks) above/below", Order=6, GroupName="Divergence")]		
		public int MarkerDistanceFactor
		{
			get { return markerDistanceFactor; }
			set { markerDistanceFactor = Math.Max(1,value);}
		}
		
		[XmlIgnore]
		[Display(Name="Upper Dots", Order=4, GroupName="Divergence")]		
		public Brush UpperDotColor
		{
			get { return upperDotColor; }
			set { upperDotColor = value;; }
		}		
		[Browsable(false)]
		public string upperDotColorSerializable
		{
			get { return Serialize.BrushToString(upperDotColor); }
			set { upperDotColor = Serialize.StringToBrush(value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="CCI Period", Description="Number of bars used for calculations", Order=1, GroupName="Indicators")]		
		public int CCI_Period
		{
			get { return cci_Period; }
			set { cci_Period = Math.Max(1, value); }
		}
		
//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="CCI_JAM MASM Period", Description="Number of bars used for calculations", Order=1, GroupName="Indicators")]		
//		public int CCI_JAM_MASM_Period
//		{
//			get { return cci_jma_masm_Period; }
//			set { cci_jma_masm_Period = Math.Max(1, value); }
//		}
		
//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="Derivative Osc. Period", Description="Number of bars used for calculations", Order=2, GroupName="Indicators")]		
//		public int DO_Period
//		{
//			get { return do_Period; }
//			set { do_Period = Math.Max(1, value); }
//		}
		
//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="Derivative Osc. Smooth1", Description="Length of smoothing EMA 1", Order=3, GroupName="Indicators")]			
//        public int DO_Smooth1
//        {
//            get { return do_Smooth1; }
//            set { do_Smooth1 = Math.Max(1, value); }
//        }
		
//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="Derivative Osc. Smooth2", Description="Length of smoothing EMA 2", Order=4, GroupName="Indicators")]			
//        public int DO_Smooth2
//        {
//            get { return do_Smooth2; }
//            set { do_Smooth2 = Math.Max(1, value); }
//        }
		
//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="Derivative Osc. Smooth3", Description="Length of final smoothing SMA", Order=5, GroupName="Indicators")]			
//        public int DO_Smooth3
//        {
//            get { return do_Smooth3; }
//            set { do_Smooth3 = Math.Max(1, value); }
//        }
		
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

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="MFI Period", Description="Number of Bars used for calculations", Order=9, GroupName="Indicators")]			
		public int MFI_Period
		{
			get { return mfi_Period; }
			set { mfi_Period = Math.Max(1, value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Momentum Period", Description="Number of Bars used for calculations", Order=10, GroupName="Indicators")]					
		public int MOM_Period
		{
			get { return mom_Period; }
			set { mom_Period = Math.Max(1, value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="ROC Period", Description="Number of Bars used for calculations", Order=11, GroupName="Indicators")]			
		public int ROC_Period
		{
			get { return roc_Period; }
			set { roc_Period = Math.Max(1, value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="RSI Period", Description="Number of Bars used for calculations", Order=12, GroupName="Indicators")]					
		public int RSI_Period
		{
			get { return rsi_Period; }
			set { rsi_Period = Math.Max(1, value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="RSI Smooth", Description="Number of Bars used for smoothing", Order=13, GroupName="Indicators")]					
		public int RSI_Smooth
		{
			get { return rsi_Smooth; }
			set { rsi_Smooth = Math.Max(1, value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="RVI Period", Description="Number of Bars used for calculations", Order=14, GroupName="Indicators")]					
		public int RVI_Period
		{
			get { return rvi_Period; }
			set { rvi_Period = Math.Max(1, value); }
		}

//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="SMI EMA Period 1", Description="First EMA smoothing ( R )", Order=15, GroupName="Indicators")]							
//		public int SMI_EMAPeriod1
//		{
//			get { return smi_EMAPeriod1; }
//			set { smi_EMAPeriod1 = Math.Max(1, value); }
//		}
//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="SMI EMA Period 2", Description="First EMA smoothing ( S )", Order=16, GroupName="Indicators")]							
//		public int SMI_EMAPeriod2
//		{
//			get { return smi_EMAPeriod2; }
//			set { smi_EMAPeriod2 = Math.Max(1, value); }
//		}
		
//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="SMI Range", Description="Range for momentum calculation ( Q )", Order=17, GroupName="Indicators")]							
//		public int SMI_Range
//		{
//			get { return smi_Range; }
//			set { smi_Range = Math.Max(1, value); }
//		}

//		[Range(1, int.MaxValue)]
//		[NinjaScriptProperty]
//		[Display(Name="SMI SMIEMA Period", Description="SMI EMA smoothing period", Order=18, GroupName="Indicators")]							
//		public int SMI_SMIEMAPeriod
//		{
//			get { return smi_SMIEMAPeriod; }
//			set { smi_SMIEMAPeriod = Math.Max(1, value); }
//		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Stochastics PeriodD", Description="Numbers of bars used for moving average over D values", Order=19, GroupName="Indicators")]							
		public int Stoch_PeriodD
		{
			get { return stoch_PeriodD; }
			set { stoch_PeriodD = Math.Max(1, value); }
		}

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Stochastics PeriodK", Description="Numbers of bars used for calculating K values", Order=20, GroupName="Indicators")]							
		public int Stoch_PeriodK
		{
			get { return stoch_PeriodK; }
			set { stoch_PeriodK = Math.Max(1, value); }
		}

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Stochastics Smooth", Description="Numbers of bars for smoothing the slow K values", Order=21, GroupName="Indicators")]							
		public int Stoch_Smooth
		{
			get { return stoch_Smooth; }
			set { stoch_Smooth = Math.Max(1, value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Stochastics Fast PeriodD", Description="Numbers of bars used for moving average over D values", Order=22, GroupName="Indicators")]							
		public int StochFast_PeriodD
		{
			get { return stochfast_PeriodD; }
			set { stochfast_PeriodD = Math.Max(1, value); }
		}

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Stochastics Fast PeriodK", Description="Numbers of bars for calculating the K values", Order=23, GroupName="Indicators")]
		public int StochFast_PeriodK
		{
			get { return stochfast_PeriodK; }
			set { stochfast_PeriodK = Math.Max(1, value); }
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="StochRSI Period", Description="Number of Bars used for calculations", Order=24, GroupName="Indicators")]					
		public int StochRSI_Period
		{
			get { return stochrsi_Period; }
			set { stochrsi_Period = Math.Max(1, value); }
		}

		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private D3SpotterV3B[] cacheD3SpotterV3B;
		public D3SpotterV3B D3SpotterV3B(D3SpotIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, int hdivlinelookbackperiod, D3SpotPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int cCI_Period, int macd_Fast, int macd_Slow, int macd_Smooth, int mFI_Period, int mOM_Period, int rOC_Period, int rSI_Period, int rSI_Smooth, int rVI_Period, int stoch_PeriodD, int stoch_PeriodK, int stoch_Smooth, int stochFast_PeriodD, int stochFast_PeriodK, int stochRSI_Period)
		{
			return D3SpotterV3B(Input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, hdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, cCI_Period, macd_Fast, macd_Slow, macd_Smooth, mFI_Period, mOM_Period, rOC_Period, rSI_Period, rSI_Smooth, rVI_Period, stoch_PeriodD, stoch_PeriodK, stoch_Smooth, stochFast_PeriodD, stochFast_PeriodK, stochRSI_Period);
		}

		public D3SpotterV3B D3SpotterV3B(ISeries<double> input, D3SpotIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, int hdivlinelookbackperiod, D3SpotPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int cCI_Period, int macd_Fast, int macd_Slow, int macd_Smooth, int mFI_Period, int mOM_Period, int rOC_Period, int rSI_Period, int rSI_Smooth, int rVI_Period, int stoch_PeriodD, int stoch_PeriodK, int stoch_Smooth, int stochFast_PeriodD, int stochFast_PeriodK, int stochRSI_Period)
		{
			if (cacheD3SpotterV3B != null)
				for (int idx = 0; idx < cacheD3SpotterV3B.Length; idx++)
					if (cacheD3SpotterV3B[idx] != null && cacheD3SpotterV3B[idx].Method == method && cacheD3SpotterV3B[idx].IndicatorDiffLimit == indicatorDiffLimit && cacheD3SpotterV3B[idx].PriceDiffLimit == priceDiffLimit && cacheD3SpotterV3B[idx].ScanWidth == scanWidth && cacheD3SpotterV3B[idx].QueueLength == queueLength && cacheD3SpotterV3B[idx].Rdivlinelookbackperiod == rdivlinelookbackperiod && cacheD3SpotterV3B[idx].Hdivlinelookbackperiod == hdivlinelookbackperiod && cacheD3SpotterV3B[idx].PType == pType && cacheD3SpotterV3B[idx].ShowAlerts == showAlerts && cacheD3SpotterV3B[idx].MyAlert1 == myAlert1 && cacheD3SpotterV3B[idx].MyAlert2 == myAlert2 && cacheD3SpotterV3B[idx].UseDefaultPlot == useDefaultPlot && cacheD3SpotterV3B[idx].DivergenceDashStyle == divergenceDashStyle && cacheD3SpotterV3B[idx].DivergenceLineWidth == divergenceLineWidth && cacheD3SpotterV3B[idx].MarkerDistanceFactor == markerDistanceFactor && cacheD3SpotterV3B[idx].CCI_Period == cCI_Period && cacheD3SpotterV3B[idx].Macd_Fast == macd_Fast && cacheD3SpotterV3B[idx].Macd_Slow == macd_Slow && cacheD3SpotterV3B[idx].Macd_Smooth == macd_Smooth && cacheD3SpotterV3B[idx].MFI_Period == mFI_Period && cacheD3SpotterV3B[idx].MOM_Period == mOM_Period && cacheD3SpotterV3B[idx].ROC_Period == rOC_Period && cacheD3SpotterV3B[idx].RSI_Period == rSI_Period && cacheD3SpotterV3B[idx].RSI_Smooth == rSI_Smooth && cacheD3SpotterV3B[idx].RVI_Period == rVI_Period && cacheD3SpotterV3B[idx].Stoch_PeriodD == stoch_PeriodD && cacheD3SpotterV3B[idx].Stoch_PeriodK == stoch_PeriodK && cacheD3SpotterV3B[idx].Stoch_Smooth == stoch_Smooth && cacheD3SpotterV3B[idx].StochFast_PeriodD == stochFast_PeriodD && cacheD3SpotterV3B[idx].StochFast_PeriodK == stochFast_PeriodK && cacheD3SpotterV3B[idx].StochRSI_Period == stochRSI_Period && cacheD3SpotterV3B[idx].EqualsInput(input))
						return cacheD3SpotterV3B[idx];
			return CacheIndicator<D3SpotterV3B>(new D3SpotterV3B(){ Method = method, IndicatorDiffLimit = indicatorDiffLimit, PriceDiffLimit = priceDiffLimit, ScanWidth = scanWidth, QueueLength = queueLength, Rdivlinelookbackperiod = rdivlinelookbackperiod, Hdivlinelookbackperiod = hdivlinelookbackperiod, PType = pType, ShowAlerts = showAlerts, MyAlert1 = myAlert1, MyAlert2 = myAlert2, UseDefaultPlot = useDefaultPlot, DivergenceDashStyle = divergenceDashStyle, DivergenceLineWidth = divergenceLineWidth, MarkerDistanceFactor = markerDistanceFactor, CCI_Period = cCI_Period, Macd_Fast = macd_Fast, Macd_Slow = macd_Slow, Macd_Smooth = macd_Smooth, MFI_Period = mFI_Period, MOM_Period = mOM_Period, ROC_Period = rOC_Period, RSI_Period = rSI_Period, RSI_Smooth = rSI_Smooth, RVI_Period = rVI_Period, Stoch_PeriodD = stoch_PeriodD, Stoch_PeriodK = stoch_PeriodK, Stoch_Smooth = stoch_Smooth, StochFast_PeriodD = stochFast_PeriodD, StochFast_PeriodK = stochFast_PeriodK, StochRSI_Period = stochRSI_Period }, input, ref cacheD3SpotterV3B);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.D3SpotterV3B D3SpotterV3B(D3SpotIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, int hdivlinelookbackperiod, D3SpotPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int cCI_Period, int macd_Fast, int macd_Slow, int macd_Smooth, int mFI_Period, int mOM_Period, int rOC_Period, int rSI_Period, int rSI_Smooth, int rVI_Period, int stoch_PeriodD, int stoch_PeriodK, int stoch_Smooth, int stochFast_PeriodD, int stochFast_PeriodK, int stochRSI_Period)
		{
			return indicator.D3SpotterV3B(Input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, hdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, cCI_Period, macd_Fast, macd_Slow, macd_Smooth, mFI_Period, mOM_Period, rOC_Period, rSI_Period, rSI_Smooth, rVI_Period, stoch_PeriodD, stoch_PeriodK, stoch_Smooth, stochFast_PeriodD, stochFast_PeriodK, stochRSI_Period);
		}

		public Indicators.D3SpotterV3B D3SpotterV3B(ISeries<double> input , D3SpotIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, int hdivlinelookbackperiod, D3SpotPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int cCI_Period, int macd_Fast, int macd_Slow, int macd_Smooth, int mFI_Period, int mOM_Period, int rOC_Period, int rSI_Period, int rSI_Smooth, int rVI_Period, int stoch_PeriodD, int stoch_PeriodK, int stoch_Smooth, int stochFast_PeriodD, int stochFast_PeriodK, int stochRSI_Period)
		{
			return indicator.D3SpotterV3B(input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, hdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, cCI_Period, macd_Fast, macd_Slow, macd_Smooth, mFI_Period, mOM_Period, rOC_Period, rSI_Period, rSI_Smooth, rVI_Period, stoch_PeriodD, stoch_PeriodK, stoch_Smooth, stochFast_PeriodD, stochFast_PeriodK, stochRSI_Period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.D3SpotterV3B D3SpotterV3B(D3SpotIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, int hdivlinelookbackperiod, D3SpotPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int cCI_Period, int macd_Fast, int macd_Slow, int macd_Smooth, int mFI_Period, int mOM_Period, int rOC_Period, int rSI_Period, int rSI_Smooth, int rVI_Period, int stoch_PeriodD, int stoch_PeriodK, int stoch_Smooth, int stochFast_PeriodD, int stochFast_PeriodK, int stochRSI_Period)
		{
			return indicator.D3SpotterV3B(Input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, hdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, cCI_Period, macd_Fast, macd_Slow, macd_Smooth, mFI_Period, mOM_Period, rOC_Period, rSI_Period, rSI_Smooth, rVI_Period, stoch_PeriodD, stoch_PeriodK, stoch_Smooth, stochFast_PeriodD, stochFast_PeriodK, stochRSI_Period);
		}

		public Indicators.D3SpotterV3B D3SpotterV3B(ISeries<double> input , D3SpotIndicatorMethod method, double indicatorDiffLimit, double priceDiffLimit, int scanWidth, int queueLength, int rdivlinelookbackperiod, int hdivlinelookbackperiod, D3SpotPriceType pType, bool showAlerts, string myAlert1, string myAlert2, bool useDefaultPlot, DashStyleHelper divergenceDashStyle, int divergenceLineWidth, int markerDistanceFactor, int cCI_Period, int macd_Fast, int macd_Slow, int macd_Smooth, int mFI_Period, int mOM_Period, int rOC_Period, int rSI_Period, int rSI_Smooth, int rVI_Period, int stoch_PeriodD, int stoch_PeriodK, int stoch_Smooth, int stochFast_PeriodD, int stochFast_PeriodK, int stochRSI_Period)
		{
			return indicator.D3SpotterV3B(input, method, indicatorDiffLimit, priceDiffLimit, scanWidth, queueLength, rdivlinelookbackperiod, hdivlinelookbackperiod, pType, showAlerts, myAlert1, myAlert2, useDefaultPlot, divergenceDashStyle, divergenceLineWidth, markerDistanceFactor, cCI_Period, macd_Fast, macd_Slow, macd_Smooth, mFI_Period, mOM_Period, rOC_Period, rSI_Period, rSI_Smooth, rVI_Period, stoch_PeriodD, stoch_PeriodK, stoch_Smooth, stochFast_PeriodD, stochFast_PeriodK, stochRSI_Period);
		}
	}
}

#endregion
