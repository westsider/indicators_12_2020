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
using SharpDX.DirectWrite;
using NinjaTrader.Core;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class CycleCountDXAuto : Indicator
	{
		private Swing Swing1;
		//private WTTcRSI2 WTTcRSI22;
		private int lastBarNum = 0;
		private List<double> cycleLows = new List<double>();
		private string bellCurve = "";
		private int peakFrequency = 0;
		private int peakValue = 0;
		
		// dx drawing
		private System.Windows.Media.Brush	areaBrush;
		private int							areaOpacity;
        private System.Windows.Media.Brush textBrush;
		private int dayCount = 0;  
		DateTime dt1; 
		
		//cRSI
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
		private int CyclicMemory = 0;
		private int Leveling = 10;
		private int lastBar = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Cycle Counter DX Auto";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
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
				
//				NoteLocation			= TextPosition.TopLeft;
				BackgroundColor			= Brushes.DimGray;
//				BackgroundOpacity 		= 90;
//				FontColor				= Brushes.WhiteSmoke;
//				OutlineColor			= Brushes.DimGray;
//				NoteFont				= new SimpleFont("Arial", 12);
				
				AreaOpacity 			= 80;
				AreaBrush 				= System.Windows.Media.Brushes.DodgerBlue;
                textSize = 18;
                TextBrush = System.Windows.Media.Brushes.WhiteSmoke;
				
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
				Swing1				= Swing(Close, SwingStrength);
				ClearOutputWindow();

				avgUp	= new Series<double>(this);
				avgDown = new Series<double>(this);
				down	= new Series<double>(this);
				up		= new Series<double>(this);
				raw 	= new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			
			calcRSI(cycleLen: 20);
//			if ( CurrentBar < SwingStrength + 1 ) {  
//				dt1 = Time[0]; 
//				return;
//			} 
//			dayCount = Convert.ToInt32((Time[0] - dt1).TotalDays); 
			
//			if (Low[SwingStrength] == Swing1.SwingLow[0]) 
//			{
//				int length = CurrentBar - lastBarNum;
//				if ( length > SmallCycleMin && lastBarNum != 0) {
//					//Print("Swing Low on " + CurrentBar + " length = " + length);
//					cycleLows.Add(length);
//				}
//				lastBarNum = CurrentBar;
//			}
			
//			calcStats(debug: false); 
			
//			Draw.TextFixed(this, "myStatsFixed", 
//					bellCurve, 
//					NoteLocation, 
//					FontColor,  // text color
//					NoteFont, 
//					OutlineColor, // outline color
//					BackgroundColor, 
//					BackgroundOpacity);	
		}
		
		private void calcRSI(double cycleLen) {
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
			Print(Time[0] + " " + CRSILower[0] );
		}
		
//		private void calcStats(bool debug) {
//			if (CurrentBar < Count -2) return;
//			cycleLows.Sort();	
//			if ( debug ) {
//				Print("\nArray: " + cycleLows.Count() );
//				for (int i = 0; i < cycleLows.Count(); i++) 
//				{
//					Print(cycleLows[i]);
//				}
//			}
//		}
		
//		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
//		{
//			drawHistogram(list: cycleLows, position: "Left", title: "Cycles");
//		}
		
		private void drawHistogram(List<double> list, string position, string title) {
			
//			if ( list.Count < 2 ) { return; }
			
//			SharpDX.Vector2 startPoint;
//			SharpDX.Vector2 endPoint;
//            float leadingSpace = 30.0f;
//			//Print(ChartPanel.W);
//			if ( position == "Center") { leadingSpace = ChartPanel.W / 2; }
//			if ( position == "Right") { leadingSpace = ChartPanel.W -(ChartPanel.W / 4); }
			
//			float halfHeight = ChartPanel.H / 4;
//			float maxWidth = ChartPanel.W / 8;
 
//            Dictionary<double, double> Profile = listIntoSortedDict(list: list);
//            var mode = Profile.OrderByDescending(x => x.Value).FirstOrDefault().Key;
//            List<double> arr = list;
//            int avg =  Convert.ToInt32( arr.Average());
//            double stdDev = StandardDeviation(array: arr);
//            double stDevLo = mode - stdDev;
//            if (stDevLo < 3) { stDevLo = 3; }
//            double stDevHi = mode + stdDev; 

//           if (!IsInHitTest)
//			{
//				SharpDX.Direct2D1.Brush areaBrushDx;
//				areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
//				SharpDX.Direct2D1.SolidColorBrush pocBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,SharpDX.Color.Red);
//                SharpDX.Direct2D1.SolidColorBrush avgBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Goldenrod);
//                SharpDX.Direct2D1.SolidColorBrush volBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Gray);

//                int spacer = 20;
				
//				float divisor = maxWidth / (float)Profile.Values.Max();

//                textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", SharpDX.DirectWrite.FontWeight.Light, 
//                    SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, textSize)
//                {
//                    TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing,   //TextAlignment.Leading,
//                    WordWrapping = WordWrapping.NoWrap
//                };

//                textFormatSmaller = new TextFormat(Globals.DirectWriteFactory, "Arial", SharpDX.DirectWrite.FontWeight.Light,
//                    SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, textSize )
//                {
//                    TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing,   //TextAlignment.Leading,
//                    WordWrapping = WordWrapping.NoWrap
//                };

//                SharpDX.Direct2D1.Brush textBrushDx;
//                textBrushDx = textBrush.ToDxBrush(RenderTarget);

//                string unicodeString = "today";

				
//                foreach (KeyValuePair<double, double> row in Profile)
//                {
//                    //Print(row.Value);
//                    float rowSize = (float)row.Value * divisor;
//                    spacer += 15;
//                    startPoint = new SharpDX.Vector2(ChartPanel.X + leadingSpace, halfHeight + spacer);
//                    endPoint = new SharpDX.Vector2(ChartPanel.X + rowSize + leadingSpace, halfHeight + spacer);

//                    if ( row.Key == mode)
//                    {
//                        areaBrushDx = pocBrush;
//                    }
//                    else if (row.Key == avg)
//                    {
//                        areaBrushDx = avgBrush;
//                    }
//                    else if (row.Key < stDevLo || row.Key > stDevHi)
//                    {
//                        areaBrushDx = volBrush;
//                    }
//                    else
//                    { 
//                        areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
//                    }

//                    drawRow(startPoint: startPoint, endPoint: endPoint, areaBrushDx: areaBrushDx);

//                    if (row.Key == mode)
//                    {
//                        float commonBuffer = 40f;
//						//if ((int)row.Key == (int)ibRange ) {  commonBuffer += 40f; }
//                        float textStartPos = (float)startPoint.Y - 10f;
//                        SharpDX.RectangleF rect = new SharpDX.RectangleF(0f, textStartPos, endPoint.X + commonBuffer, 10f);
//                        RenderTarget.DrawText("poc", textFormatSmaller, rect, areaBrushDx);
//                    }

//                    if(row.Key == avg)
//                    { 
//						float commonBuffer = 40f;
//						//if ((int)row.Key == (int)ibRange ) {  commonBuffer += 40f; }
//						if ((int)row.Key == (int)mode ) {  commonBuffer += 40f; }
//                        float textStartPos = (float)startPoint.Y - 10f;
//                        SharpDX.RectangleF rect = new SharpDX.RectangleF(0f, textStartPos, endPoint.X + commonBuffer, 10f);
//                        RenderTarget.DrawText("avg", textFormatSmaller, rect, areaBrushDx);

//                    }

//                    // value text
//                    float textStartPos2 = (float)startPoint.Y - 10f;
//                    SharpDX.RectangleF rect2 = new SharpDX.RectangleF(0f, textStartPos2, leadingSpace - 5f, 10f);
//                    RenderTarget.DrawText(string.Format("{0}", row.Key), textFormat, rect2, areaBrushDx);
//                }

//                // end text 
//                //areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
//                SharpDX.RectangleF rect3 = new SharpDX.RectangleF(0f, halfHeight + spacer + 15f, 245, 10f);
//                RenderTarget.DrawText(dayCount + " day " + title + " distribution", textFormat, rect3, areaBrushDx);
    
//                areaBrushDx.Dispose();
//                textBrushDx.Dispose();
//                pocBrush.Dispose();
//                avgBrush.Dispose();
//                volBrush.Dispose();

 //           }
		}
		
		private void drawRow(SharpDX.Vector2 startPoint, SharpDX.Vector2 endPoint, SharpDX.Direct2D1.Brush areaBrushDx) {
			RenderTarget.DrawLine(startPoint, endPoint, areaBrushDx, 10);
        }
		
		private Dictionary<double, double>  listIntoSortedDict(List<double> list) {
	
			List<double> arr =   list;
			//if ( debug ) { printList(arr: arr, title: "Sorting Algo"); }
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
			return  ItemCount;
		}
		
		private static double StandardDeviation(IEnumerable<double> array)
        {
            double avg = array.Average();
            return Math.Sqrt(array.Average(v => Math.Pow(v - avg, 2)));
        }
		
		
		#region Properties
		
		
        protected TextFormat textFormat
        { get; set; }

        protected TextFormat textFormatSmaller
        { get; set; }
		
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

//		[NinjaScriptProperty]
//		[XmlIgnore]
//		[Display(Name="Font Color", Description="Font Color", Order=2, GroupName="Stats")]
//		public Brush FontColor
//		{ get; set; }

//		[Browsable(false)]
//		public string FontColorSerializable
//		{
//			get { return Serialize.BrushToString(FontColor); }
//			set { FontColor = Serialize.StringToBrush(value); }
//		}
		
//		[NinjaScriptProperty]
//		[XmlIgnore]
//		[Display(Name="OutlineColor Color", Description="OutlineColor Color", Order=3, GroupName="Stats")]
//		public Brush OutlineColor
//		{ get; set; }

//		[Browsable(false)]
//		public string OutlineColorSerializable
//		{
//			get { return Serialize.BrushToString(OutlineColor); }
//			set { OutlineColor = Serialize.StringToBrush(value); }
//		}
		 
//		[NinjaScriptProperty]
//		[Display(Name="Note Font", Description="Note Font", Order=4, GroupName="Stats")]
//		public SimpleFont NoteFont
//		{ get; set; }
		
//		[NinjaScriptProperty]
//		[Range(1, int.MaxValue)]
//		[Display(Name="Background Opacity", Description="Background Opacity", Order=5, GroupName="Stats")]
//		public int BackgroundOpacity
//		{ get; set; }
		
//		[NinjaScriptProperty]
//		[Display(Name="Note1Location", Description="Note Location", Order=6, GroupName="Stats")]
//		public TextPosition NoteLocation
//		{ get; set; }
		
		/// quick draw dx
		[Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Text Size", GroupName = "Parameters", Order = 10)]
        public int textSize
        { get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral")]
		public System.Windows.Media.Brush AreaBrush
		{
			get { return areaBrush; }
			set
			{
				areaBrush = value;
				if (areaBrush != null)
				{
					if (areaBrush.IsFrozen)
						areaBrush = areaBrush.Clone();
					areaBrush.Opacity = areaOpacity / 100d;
					areaBrush.Freeze();
				}
			}
		}

		[Browsable(false)]
		public string AreaBrushSerialize
		{
			get { return Serialize.BrushToString(AreaBrush); }
			set { AreaBrush = Serialize.StringToBrush(value); }
		}
			
		
		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral")]
		public int AreaOpacity
		{
			get { return areaOpacity; }
			set
			{
				areaOpacity = Math.Max(0, Math.Min(100, value));
				if (areaBrush != null)
				{
					System.Windows.Media.Brush newBrush		= areaBrush.Clone();
					newBrush.Opacity	= areaOpacity / 100d;
					newBrush.Freeze();
					areaBrush			= newBrush;
				}
			}
		}

        [XmlIgnore]
        [Display(ResourceType = typeof(Custom.Resource), Name = "TextColor", GroupName = "NinjaScriptGeneral")]
        public System.Windows.Media.Brush TextBrush
        {
            get { return textBrush; }
            set { textBrush = value; }
        }

        [Browsable(false)]
        public string TextBrushSerialize
        {
            get { return Serialize.BrushToString(TextBrush); }
            set { TextBrush = Serialize.StringToBrush(value); }
        }
		
		// cRSI
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
		private CycleCountDXAuto[] cacheCycleCountDXAuto;
		public CycleCountDXAuto CycleCountDXAuto(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, int textSize, int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return CycleCountDXAuto(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, textSize, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}

		public CycleCountDXAuto CycleCountDXAuto(ISeries<double> input, int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, int textSize, int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			if (cacheCycleCountDXAuto != null)
				for (int idx = 0; idx < cacheCycleCountDXAuto.Length; idx++)
					if (cacheCycleCountDXAuto[idx] != null && cacheCycleCountDXAuto[idx].SwingStrength == swingStrength && cacheCycleCountDXAuto[idx].SmallCycleMin == smallCycleMin && cacheCycleCountDXAuto[idx].LargeCycleMin == largeCycleMin && cacheCycleCountDXAuto[idx].BackgroundColor == backgroundColor && cacheCycleCountDXAuto[idx].textSize == textSize && cacheCycleCountDXAuto[idx].CycleLength == cycleLength && cacheCycleCountDXAuto[idx].Vibration == vibration && cacheCycleCountDXAuto[idx].ShowZones == showZones && cacheCycleCountDXAuto[idx].LineThickness == lineThickness && cacheCycleCountDXAuto[idx].Opacity == opacity && cacheCycleCountDXAuto[idx].UpColor == upColor && cacheCycleCountDXAuto[idx].DnColor == dnColor && cacheCycleCountDXAuto[idx].EqualsInput(input))
						return cacheCycleCountDXAuto[idx];
			return CacheIndicator<CycleCountDXAuto>(new CycleCountDXAuto(){ SwingStrength = swingStrength, SmallCycleMin = smallCycleMin, LargeCycleMin = largeCycleMin, BackgroundColor = backgroundColor, textSize = textSize, CycleLength = cycleLength, Vibration = vibration, ShowZones = showZones, LineThickness = lineThickness, Opacity = opacity, UpColor = upColor, DnColor = dnColor }, input, ref cacheCycleCountDXAuto);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CycleCountDXAuto CycleCountDXAuto(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, int textSize, int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.CycleCountDXAuto(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, textSize, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}

		public Indicators.CycleCountDXAuto CycleCountDXAuto(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, int textSize, int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.CycleCountDXAuto(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, textSize, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CycleCountDXAuto CycleCountDXAuto(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, int textSize, int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.CycleCountDXAuto(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, textSize, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}

		public Indicators.CycleCountDXAuto CycleCountDXAuto(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, int textSize, int cycleLength, int vibration, bool showZones, int lineThickness, int opacity, Brush upColor, Brush dnColor)
		{
			return indicator.CycleCountDXAuto(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, textSize, cycleLength, vibration, showZones, lineThickness, opacity, upColor, dnColor);
		}
	}
}

#endregion
