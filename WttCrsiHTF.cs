// 
// Copyright (C) 2017 CC BY, www.whentotrade.com / Lars von Thienen
// Book: Decoding The Hidden Market Rhythm - Part 1 (2017)
// Chapter 4 "Fine-tuning technical indicators"
// Link: https://www.amazon.com/dp/1974658244
//
// Usage: 
// You need to derive the dominant cycle as input parameter for the cycle length as described in chapter 4.
//
// License: 
// This work is licensed under a Creative Commons Attribution 4.0 International License.
// You are free to share the material in any medium or format and remix, transform, and build upon the material for any purpose, 
// even commercially. You must give appropriate credit to the authors book and website, provide a link to the license, and indicate 
// if changes were made. You may do so in any reasonable manner, but not in any way that suggests the licensor endorses you or your use. 
//

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

using System.Windows.Media.Imaging;
using System.Net.Mail;
using System.Net.Mime;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class WttCrsiHTF : Indicator
	{
		private Series<double> avgDown;
		private Series<double> avgUp;
		private Series<double> down;
		private Series<double> up;
		private Series<double> raw;
		private double cycleConstant;
		private double ad=0;
		private double au=0;
		private double torque;
		private int	phasingLag;
		
		private string ES_ZoneApproaching = "ES_ZoneApproaching.wav";
		private string NQ_ZoneApproaching = "NQ_ZoneApproaching.wav";
		private string name = "ES";
		private bool SendSMSAlert = true;
		private bool ShowDots = true;
		
		// show zones
		private int cRSIShortLength = 0;
		private	int cRSILongLength = 0;
		private	double cRSILongPrice = 0;
		private	double cRSIShortPrice = 0;
		private int lastBar = 0;
		
		
		private string ES_EnteringLongZone = "ES_EnteringLongZone.wav";
		private string ES_EnteringShortZone = "ES_EnteringShortZone.wav";
		private string NQ_EnteringLongZone = "NQ_EnteringLongZone.wav";
		private string NQ_EnteringShortZone = "NQ_EnteringShortZone.wav";
		private string sound = "";
		private bool	alertHasTriggeredS = false;
		private bool	alertHasTriggeredL = false;
		
		NinjaTrader.Gui.Chart.Chart 	chart;
        BitmapFrame 					outputFrame;
		//private bool ScreenShotSent 	= false;
//		private double cRSI = 0.0;
//		private double ub = 0.0;
//		private double db = 0.0;
//		private double rawRSI = 0.0;
		
		protected override void OnStateChange()
		{ 
			if (State == State.SetDefaults)
			{
				Description									= @"Cyclic Smoothed RSI Indicator";
				Name										= "WTT cRSI HTF";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;
				
				munutesHtf 					= 4500;
				CycleLength					= 20;
				Vibration				 	= 10;
				CyclicMemory				= 40; 
				Leveling					= 10;
				ShowZones					= true;
				LineThickness 				= 5;
				Opacity						= 30;
				UpColor					= Brushes.DodgerBlue;
				DnColor					= Brushes.Red;
				SendAlert 	= false;
				AddPlot(Brushes.Red, "CRSI");
				AddPlot(Brushes.Blue, "cRSIUpper");
				AddPlot(Brushes.Gray, "cRSINeutral");
				AddPlot(Brushes.Blue, "cRSILower");
			}
			else if (State == State.Configure)
			{
				cycleConstant = (CycleLength - 1);
				torque		= 2.0 / (Vibration + 1);
				phasingLag	= (int) Math.Ceiling((Vibration - 1) / 2.0);
				
				Dispatcher.BeginInvoke(new Action(() =>
				{
					chart = Window.GetWindow(ChartControl) as Chart;
				})); 
				
				AddDataSeries(Data.BarsPeriodType.Tick, munutesHtf);
			}
			else if (State == State.DataLoaded)
			{
				//init
				avgUp	= new Series<double>(this);
				avgDown = new Series<double>(this);
				down	= new Series<double>(this);
				up		= new Series<double>(this);
				raw 	= new Series<double>(this);
			}

		}

		protected override void OnBarUpdate()
		{			
			if (CurrentBars[1] == 0)
			{
				down[0]		= 0;
				up[0]		= 0;
				return;
			}
			
			if (CurrentBars[1] <= BarsRequiredToPlot || CurrentBars[1] <= BarsRequiredToPlot )
        		return;
			
			lastBar = CurrentBars[1] - 1;
			double input0	= BarsArray[1][0];
			double input1	= BarsArray[1][1];
			down[0]			= Math.Max(input1 - input0, 0);
			up[0]			= Math.Max(input0 - input1, 0);
						
			if (CurrentBars[1] + 1 < CycleLength) 
			{
				ad+=down[0];
				au+=up[0];
				return;
			}

			if ((CurrentBars[1] + 1) == CycleLength) 
			{
				// initial load
				avgDown[0]	= ad/CycleLength;
				avgUp[0]	= au/CycleLength;
			}  
			else 
			{
				// RSI prep
				avgDown[0]	= (avgDown[1] * cycleConstant + down[0]) / CycleLength;
				avgUp[0]	= (avgUp[1] * cycleConstant + up[0]) / CycleLength;
			}

			double avgDown0	= avgDown[0];
			double rawRSI	= avgDown0 == 0 ? 100 : 100 - 100 / (1 + avgUp[0] / avgDown0);
			double cRSI = torque * (2 * rawRSI - raw[phasingLag]) + (1-torque) * CRSI[1];
			
			raw[0] = rawRSI;
						
			if (BarsInProgress == 0)
			if (CurrentBars[1] < CycleLength+phasingLag)
				CRSI[0] = rawRSI;
			else 
				CRSI[0]	= cRSI;
			
			if (CurrentBars[1] < CycleLength+phasingLag+CyclicMemory) return;
			
			double ub; double db; double lmax; double lmin; double ratio;
			double testvalue; int above; int below; double mstep;
			double aperc = (double)Leveling / 100;
			
			lmax=-999999; lmin=999999;
			for (int i=0; i<CyclicMemory; i++){
				if (CRSI[i]>lmax) lmax=CRSI[i]; 
				else if (CRSI[i]<lmin) lmin=CRSI[i];
			}
			
			mstep=(lmax-lmin)/100;

			db=0;
			for (int steps=0; steps<=100; steps++)
			{
				testvalue=lmin+(mstep*steps);
				above=0; below=0;
				
				for (int m=0; m<CyclicMemory; m++)
					if (CRSI[m]>=testvalue) above++; else below++;
					
				ratio=(double)below / (double)CyclicMemory;
				if (ratio>=aperc)  { db=testvalue; break; }
			}
			
			
			ub=0;
			for (int steps=0; steps<=100; steps++)
			{
				testvalue=lmax-(mstep*steps);
				above=0; below=0;
				
				for (int m=0; m<CyclicMemory; m++)
					if (CRSI[m]>=testvalue) above++; else below++;
					
				ratio=(double)above / (double)CyclicMemory;
				if (ratio>=aperc)  { ub=testvalue; break; }
			}
			
			cRSIUpper[0]=ub;
			cRSILower[0]=db;
			cRSINeutral[0]=(double)((ub+db)/2);	
				
				/// set short
				if (cRSI >= cRSIUpper[0]) {
					cRSIShortLength += 1;
					cRSIShortPrice = MAX(Highs[1],5)[0] + 1 * TickSize;
					
					if ( Highs[1][0] > cRSIShortPrice ) {
						cRSIShortPrice = MAX(Highs[1],5)[0] + 1 * TickSize;
					}
					drawRectShort(message: "");
				}
				// reset short 
				if (cRSI  < cRSIUpper[0])
				{
					cRSIShortLength = 0;
					cRSIShortPrice = 0.0;
					alertHasTriggeredS = false;
				}
				
				/// set long
				if (cRSI <= cRSILower[0]) {
					cRSILongLength += 1;
					cRSIShortLength = 0;
					cRSILongPrice = MIN(Lows[1], 5)[0] - 1 * TickSize;
					if ( Lows[1][0] < cRSILongPrice ) {
						cRSILongPrice = MIN(Lows[1], 5)[0] - 1 * TickSize;
					}
					drawRectLong(message: "");
				}
				// reset long 
				if (cRSI  > cRSILower[0] )
				{
					cRSILongLength = 0;
					cRSILongPrice = Highs[1][0];
					alertHasTriggeredL = false;
				}
			//}
		}
		
		private void sendAlert(string message, string sound) {
			Alert("myAlert"+CurrentBars[1], Priority.High, message, NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ sound,10, Brushes.Black, Brushes.Yellow);  
			if (CurrentBars[1] < Count -2) return;
				SendMailChart(name + " Alert",message,"ticktrade10@gmail.com","13103824522@tmomail.net","smtp.gmail.com",587,"ticktrade10","WH2403wh");
		}

		private void SendMailChart(string Subject, string Body, string From, string To, string Host, int Port, string Username, string Password)
		{
			
			try	
			{	

				Dispatcher.BeginInvoke(new Action(() =>
				{
				
						if (chart != null)
				        {
							
							RenderTargetBitmap	screenCapture = chart.GetScreenshot(ShareScreenshotType.Chart);
		                    outputFrame = BitmapFrame.Create(screenCapture);
							
		                    if (screenCapture != null)
		                    {
		                       
								PngBitmapEncoder png = new PngBitmapEncoder();
		                        png.Frames.Add(outputFrame);
								System.IO.MemoryStream stream = new System.IO.MemoryStream();
								png.Save(stream);
								stream.Position = 0;
							
								MailMessage theMail = new MailMessage(From, To, Subject, Body);
								System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(stream, "image.png");
								theMail.Attachments.Add(attachment);
							
								SmtpClient smtp = new SmtpClient(Host, Port);
								smtp.EnableSsl = true;
								smtp.Credentials = new System.Net.NetworkCredential(Username, Password);
								string token = Instrument.MasterInstrument.Name + ToDay(Time[0]) + " " + ToTime(Time[0]) + CurrentBars[1].ToString();
								
								Print("Sending Mail from " + name);
								smtp.SendAsync(theMail, token);
		                  
				            }
						}
			
			    
				}));

				
				
			}
			catch (Exception ex) {
				
				Print("Sending " + name + "Chart email failed -  " + ex);
			
			}
		}
		
		private void drawRectShort(string message) {
			// draw short crsi rectangle
			if ( cRSIShortLength > 0 ) {
				RemoveDrawObject("ShortRectangle" + lastBar);
				if (  ShowZones ) { 
					Draw.Rectangle(this, "ShortRectangle" + CurrentBars[1], false, cRSIShortLength, cRSIShortPrice , 0, 
						cRSIShortPrice + (LineThickness * TickSize), Brushes.Transparent, Brushes.Red, Opacity);
				}
				if ( SendAlert ) {
					if (name == "ES") {
						sound = ES_EnteringShortZone;
					} else {
						sound = NQ_EnteringShortZone;
					} 	
					alertHasTriggeredS = true;
				}
			}
		}
		
		private void drawRectLong(string message) {
			// draw long crsi rectangle
			if ( cRSILongLength > 0 ) {
				RemoveDrawObject("LongRectangle" + lastBar);
				if (  ShowZones ) { 
					Draw.Rectangle(this, "LongRectangle" + CurrentBars[1], false, cRSILongLength, cRSILongPrice , 0, 
					cRSILongPrice - ( LineThickness * TickSize ), Brushes.Transparent, Brushes.DodgerBlue, Opacity);
				}
				if ( SendAlert ) {
					if (name == "ES") {
						sound = ES_EnteringLongZone;
					} else {
						sound = NQ_EnteringLongZone;
					} 	
					alertHasTriggeredL = true;
				}
			}
		}
		
		#region Properties
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="HTF Ticks", Description="munutesHtf", Order=1, GroupName="HTF")]
		public int munutesHtf
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, int.MaxValue)]
		[Display(Name="CycleLength", Description="Dominant Cycle Length", Order=1, GroupName="Parameters")]
		public int CycleLength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(2, 100)]
		[Display(Name="Vibration", Description="Vibration", Order=2, GroupName="Parameters")]
		public int Vibration
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, 200)]
		[Display(Name="CyclicMemory", Description="Cyclic Memory", Order=3, GroupName="Parameters")]
		public int CyclicMemory
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(5, 100)]
		[Display(Name="Leveling", Description="Leveling Sensor", Order=4, GroupName="Parameters")]
		public int Leveling
		{ get; set; }


		
		[Display(Name="ShowZones", Order=5, GroupName="Parameters")]
		public bool ShowZones
		{ get; set; }
		

		[Range(1, 100)]
		[Display(Name="Opacity", Description="Opacity", Order=6, GroupName="Parameters")]
		public int Opacity
		{ get; set; }
		

		[Display(Name="Up Color", Description="Chop zone color.", Order=7, GroupName="Parameters")]
		public Brush UpColor
		{ get; set; }
		
		[Browsable(false)]
		public string UpColorSerializable
		{
			get { return Serialize.BrushToString(UpColor); }
			set { UpColor = Serialize.StringToBrush(value); }
		}
		

		[Display(Name="Dn Color", Description="Chop zone color.", Order=8, GroupName="Parameters")]
		public Brush DnColor
		{ get; set; }
		
		[Browsable(false)]
		public string DnColorSerializable
		{
			get { return Serialize.BrushToString(DnColor); }
			set { DnColor = Serialize.StringToBrush(value); }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CRSI
		{
			get { return Values[0]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> cRSIUpper
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> cRSINeutral
		{
			get { return Values[2]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> cRSILower
		{
			get { return Values[3]; }
		}
		

		[Range(1, int.MaxValue)]
		[Display(Name="Line Thickness", Order=6, GroupName="Parameters")]
		public int LineThickness
		{ get; set; }
		
		[Display(Name="SendAlert", Order=7, GroupName="Parameters")]
		public bool SendAlert
		{ get; set; }
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private WttCrsiHTF[] cacheWttCrsiHTF;
		public WttCrsiHTF WttCrsiHTF(int munutesHtf, int cycleLength, int vibration, int cyclicMemory, int leveling)
		{
			return WttCrsiHTF(Input, munutesHtf, cycleLength, vibration, cyclicMemory, leveling);
		}

		public WttCrsiHTF WttCrsiHTF(ISeries<double> input, int munutesHtf, int cycleLength, int vibration, int cyclicMemory, int leveling)
		{
			if (cacheWttCrsiHTF != null)
				for (int idx = 0; idx < cacheWttCrsiHTF.Length; idx++)
					if (cacheWttCrsiHTF[idx] != null && cacheWttCrsiHTF[idx].munutesHtf == munutesHtf && cacheWttCrsiHTF[idx].CycleLength == cycleLength && cacheWttCrsiHTF[idx].Vibration == vibration && cacheWttCrsiHTF[idx].CyclicMemory == cyclicMemory && cacheWttCrsiHTF[idx].Leveling == leveling && cacheWttCrsiHTF[idx].EqualsInput(input))
						return cacheWttCrsiHTF[idx];
			return CacheIndicator<WttCrsiHTF>(new WttCrsiHTF(){ munutesHtf = munutesHtf, CycleLength = cycleLength, Vibration = vibration, CyclicMemory = cyclicMemory, Leveling = leveling }, input, ref cacheWttCrsiHTF);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.WttCrsiHTF WttCrsiHTF(int munutesHtf, int cycleLength, int vibration, int cyclicMemory, int leveling)
		{
			return indicator.WttCrsiHTF(Input, munutesHtf, cycleLength, vibration, cyclicMemory, leveling);
		}

		public Indicators.WttCrsiHTF WttCrsiHTF(ISeries<double> input , int munutesHtf, int cycleLength, int vibration, int cyclicMemory, int leveling)
		{
			return indicator.WttCrsiHTF(input, munutesHtf, cycleLength, vibration, cyclicMemory, leveling);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.WttCrsiHTF WttCrsiHTF(int munutesHtf, int cycleLength, int vibration, int cyclicMemory, int leveling)
		{
			return indicator.WttCrsiHTF(Input, munutesHtf, cycleLength, vibration, cyclicMemory, leveling);
		}

		public Indicators.WttCrsiHTF WttCrsiHTF(ISeries<double> input , int munutesHtf, int cycleLength, int vibration, int cyclicMemory, int leveling)
		{
			return indicator.WttCrsiHTF(input, munutesHtf, cycleLength, vibration, cyclicMemory, leveling);
		}
	}
}

#endregion
