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
	
	public class WTTcRSI2 : Indicator
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
		private int CyclicMemory = 0;
		private int Leveling = 10;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "WTT cRSI 2";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				CycleLength					= 20;
				Vibration					= 10;
				ShowZones					= true;
				LineThickness					= 3;
				Opacity					= 20;
				UpColor					= Brushes.DodgerBlue;
				DnColor					= Brushes.Red;
				AddPlot(Brushes.Red, "CRSI");
				AddPlot(Brushes.DodgerBlue, "CRISUpper");
				AddPlot(Brushes.DimGray, "CRSINetral");
				AddPlot(Brushes.DodgerBlue, "CRSILower");
			}
			else if (State == State.Configure)
			{
				cycleConstant = (CycleLength - 1);
				torque		= 2.0 / (Vibration + 1);
				phasingLag	= (int) Math.Ceiling((Vibration - 1) / 2.0);
				CyclicMemory = CycleLength * 2;
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
			if (CurrentBar == 0)
			{
				down[0]		= 0;
				up[0]		= 0;
				return;
			}
			lastBar = CurrentBar - 1;
			double input0	= Input[0];
			double input1	= Input[1];
			down[0]			= Math.Max(input1 - input0, 0);
			up[0]			= Math.Max(input0 - input1, 0);
						
			if (CurrentBar + 1 < CycleLength) 
			{
				ad+=down[0];
				au+=up[0];
				return;
			}

			if ((CurrentBar + 1) == CycleLength) 
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
						
			if (CurrentBar < CycleLength+phasingLag)
				CRSI[0] = rawRSI;
			else 
				CRSI[0]	= cRSI;
			
			if (CurrentBar < CycleLength+phasingLag+CyclicMemory) return;
			
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
			
			
			CRISUpper[0]=ub;
			CRSILower[0]=db;
			CRSINetral[0]=(double)((ub+db)/2);
			
			/// set short
			if (cRSI >= CRISUpper[0]) {
				cRSIShortLength += 1;
				cRSIShortPrice = MAX(High,5)[0] + 1 * TickSize;
				if ( High[0] > cRSIShortPrice ) {
					cRSIShortPrice = MAX(High,5)[0] + 1 * TickSize;
				}
				drawRectShort(message: "");
				//Print("Lower");
			}
			// reset short 
			if (cRSI  < CRISUpper[0])
			{
				cRSIShortLength = 0;
				cRSIShortPrice = 0.0;
				alertHasTriggeredS = false;
			}
			
			/// set long
			if (cRSI <= CRSILower[0]) {
				cRSILongLength += 1;
				cRSIShortLength = 0;
				cRSILongPrice = MIN(Low, 5)[0] - 1 * TickSize;
				if ( Low[0] < cRSILongPrice ) {
					cRSILongPrice = MIN(Low, 5)[0] - 1 * TickSize;
				}
				drawRectLong(message: "");
				//Print("Upper");
			}
			// reset long 
			if (cRSI  > CRSILower[0] )
			{
				cRSILongLength = 0;
				cRSILongPrice = High[0];
				alertHasTriggeredL = false;
			}
		}

		private void drawRectShort(string message) {
			// draw short crsi rectangle
			if ( cRSIShortLength > 0 ) {
				RemoveDrawObject("ShortRectangle" + lastBar);
				if (  ShowZones ) { 
					Draw.Rectangle(this, "ShortRectangle" + CurrentBar, false, cRSIShortLength, cRSIShortPrice , 0, 
						cRSIShortPrice + (LineThickness * TickSize), Brushes.Transparent, DnColor, Opacity);
				} 
			}
		}
		
		private void drawRectLong(string message) {
			// draw long crsi rectangle
			if ( cRSILongLength > 0 ) {
				RemoveDrawObject("LongRectangle" + lastBar);
				if (  ShowZones ) { 
					Draw.Rectangle(this, "LongRectangle" + CurrentBar, false, cRSILongLength, cRSILongPrice , 0, 
					cRSILongPrice - ( LineThickness * TickSize ), Brushes.Transparent, UpColor, Opacity);
				} 
			}
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="CycleLength", Order=1, GroupName="Parameters")]
		public int CycleLength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Vibration", Order=2, GroupName="Parameters")]
		public int Vibration
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="ShowZones", Order=3, GroupName="Parameters")]
		public bool ShowZones
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="LineThickness", Order=4, GroupName="Parameters")]
		public int LineThickness
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Opacity", Order=5, GroupName="Parameters")]
		public int Opacity
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="UpColor", Order=6, GroupName="Parameters")]
		public Brush UpColor
		{ get; set; }

		[Browsable(false)]
		public string UpColorSerializable
		{
			get { return Serialize.BrushToString(UpColor); }
			set { UpColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="DnColor", Order=7, GroupName="Parameters")]
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
		public Series<double> CRISUpper
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CRSINetral
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> CRSILower
		{
			get { return Values[3]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private WTTcRSI2[] cacheWTTcRSI2;
		public WTTcRSI2 WTTcRSI2(int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return WTTcRSI2(Input, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}

		public WTTcRSI2 WTTcRSI2(ISeries<double> input, int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			if (cacheWTTcRSI2 != null)
				for (int idx = 0; idx < cacheWTTcRSI2.Length; idx++)
					if (cacheWTTcRSI2[idx] != null && cacheWTTcRSI2[idx].CycleLength == cycleLength && cacheWTTcRSI2[idx].Vibration == vibration && cacheWTTcRSI2[idx].ShowZones == showZones && cacheWTTcRSI2[idx].LineThickness == lineThickness && cacheWTTcRSI2[idx].Opacity == opacity && cacheWTTcRSI2[idx].UpColor == upColor && cacheWTTcRSI2[idx].DnColor == dnColor && cacheWTTcRSI2[idx].EqualsInput(input))
						return cacheWTTcRSI2[idx];
			return CacheIndicator<WTTcRSI2>(new WTTcRSI2(){ CycleLength = cycleLength, Vibration = vibration, ShowZones = showZones, LineThickness = lineThickness, Opacity = opacity, UpColor = upColor, DnColor = dnColor }, input, ref cacheWTTcRSI2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.WTTcRSI2 WTTcRSI2(int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.WTTcRSI2(Input, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}

		public Indicators.WTTcRSI2 WTTcRSI2(ISeries<double> input , int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.WTTcRSI2(input, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.WTTcRSI2 WTTcRSI2(int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.WTTcRSI2(Input, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}

		public Indicators.WTTcRSI2 WTTcRSI2(ISeries<double> input , int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.WTTcRSI2(input, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}
	}
}

#endregion
