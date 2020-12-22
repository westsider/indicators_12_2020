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
	public class CycleCounterDX : Indicator
	{
		private Swing Swing1;
		
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
		//private int firstDay = 0;
		
		DateTime dt1;
		//DateTime dt2;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Cycle Counter DX";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
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
				
				NoteLocation			= TextPosition.TopLeft;
				BackgroundColor			= Brushes.DimGray;
				BackgroundOpacity 		= 90;
				FontColor				= Brushes.WhiteSmoke;
				OutlineColor			= Brushes.DimGray;
				NoteFont				= new SimpleFont("Arial", 12);
				
				AreaOpacity 			= 80;
				AreaBrush 				= System.Windows.Media.Brushes.DodgerBlue;
                textSize = 18;
                TextBrush = System.Windows.Media.Brushes.WhiteSmoke;
				
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				Swing1				= Swing(Close, SwingStrength);
				ClearOutputWindow();
//				WTTcRSI1				= WTTcRSI(Close, 20, 10, 40, 10);
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < SwingStrength + 1 ) {  
				dt1 = Time[0]; 
				return;
			} 
			dayCount = Convert.ToInt32((Time[0] - dt1).TotalDays); 
			
			if (Low[SwingStrength] == Swing1.SwingLow[0]) 
			{
				int length = CurrentBar - lastBarNum;
				if ( length > SmallCycleMin && lastBarNum != 0) {
					//Print("Swing Low on " + CurrentBar + " length = " + length);
					cycleLows.Add(length);
				}
				lastBarNum = CurrentBar;
			}
			
			calcStats(debug: false);
			//printHistogram(arr: cycleLows);
			
			Draw.TextFixed(this, "myStatsFixed", 
					bellCurve, 
					NoteLocation, 
					FontColor,  // text color
					NoteFont, 
					OutlineColor, // outline color
					BackgroundColor, 
					BackgroundOpacity);
		}
		
		private void calcStats(bool debug) {
			if (CurrentBar < Count -2) return;
			cycleLows.Sort();	
			if ( debug ) {
				Print("\nArray: " + cycleLows.Count() );
				for (int i = 0; i < cycleLows.Count(); i++) 
				{
					Print(cycleLows[i]);
				}
			}
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			drawHistogram(list: cycleLows, position: "Left", title: "Cycles");
		}
		
		private void drawHistogram(List<double> list, string position, string title) {
			
			if ( list.Count < 2 ) { return; }
			
			SharpDX.Vector2 startPoint;
			SharpDX.Vector2 endPoint;
            float leadingSpace = 30.0f;
			//Print(ChartPanel.W);
			if ( position == "Center") { leadingSpace = ChartPanel.W / 2; }
			if ( position == "Right") { leadingSpace = ChartPanel.W -(ChartPanel.W / 4); }
			
			float halfHeight = ChartPanel.H / 4;
			float maxWidth = ChartPanel.W / 8;
 
            Dictionary<double, double> Profile = listIntoSortedDict(list: list);
            var mode = Profile.OrderByDescending(x => x.Value).FirstOrDefault().Key;
            List<double> arr = list;
            int avg =  Convert.ToInt32( arr.Average());
            double stdDev = StandardDeviation(values: arr);
            double stDevLo = mode - stdDev;
            if (stDevLo < 3) { stDevLo = 3; }
            double stDevHi = mode + stdDev; 

           if (!IsInHitTest)
			{
				SharpDX.Direct2D1.Brush areaBrushDx;
				areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
				SharpDX.Direct2D1.SolidColorBrush pocBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,SharpDX.Color.Red);
                SharpDX.Direct2D1.SolidColorBrush avgBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Goldenrod);
                SharpDX.Direct2D1.SolidColorBrush volBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.Gray);

                int spacer = 20;
				
				float divisor = maxWidth / (float)Profile.Values.Max();

                textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", SharpDX.DirectWrite.FontWeight.Light, 
                    SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, textSize)
                {
                    TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing,   //TextAlignment.Leading,
                    WordWrapping = WordWrapping.NoWrap
                };

                textFormatSmaller = new TextFormat(Globals.DirectWriteFactory, "Arial", SharpDX.DirectWrite.FontWeight.Light,
                    SharpDX.DirectWrite.FontStyle.Normal, SharpDX.DirectWrite.FontStretch.Normal, textSize )
                {
                    TextAlignment = SharpDX.DirectWrite.TextAlignment.Trailing,   //TextAlignment.Leading,
                    WordWrapping = WordWrapping.NoWrap
                };

                SharpDX.Direct2D1.Brush textBrushDx;
                textBrushDx = textBrush.ToDxBrush(RenderTarget);

                string unicodeString = "today";

				
                foreach (KeyValuePair<double, double> row in Profile)
                {
                    //Print(row.Value);
                    float rowSize = (float)row.Value * divisor;
                    spacer += 15;
                    startPoint = new SharpDX.Vector2(ChartPanel.X + leadingSpace, halfHeight + spacer);
                    endPoint = new SharpDX.Vector2(ChartPanel.X + rowSize + leadingSpace, halfHeight + spacer);

                    if ( row.Key == mode)
                    {
                        areaBrushDx = pocBrush;
                    }
                    else if (row.Key == avg)
                    {
                        areaBrushDx = avgBrush;
                    }
                    else if (row.Key < stDevLo || row.Key > stDevHi)
                    {
                        areaBrushDx = volBrush;
                    }
                    else
                    { 
                        areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
                    }

                    drawRow(startPoint: startPoint, endPoint: endPoint, areaBrushDx: areaBrushDx);

                    if (row.Key == mode)
                    {
                        float commonBuffer = 40f;
						//if ((int)row.Key == (int)ibRange ) {  commonBuffer += 40f; }
                        float textStartPos = (float)startPoint.Y - 10f;
                        SharpDX.RectangleF rect = new SharpDX.RectangleF(0f, textStartPos, endPoint.X + commonBuffer, 10f);
                        RenderTarget.DrawText("poc", textFormatSmaller, rect, areaBrushDx);
                    }

                    if(row.Key == avg)
                    { 
						float commonBuffer = 40f;
						//if ((int)row.Key == (int)ibRange ) {  commonBuffer += 40f; }
						if ((int)row.Key == (int)mode ) {  commonBuffer += 40f; }
                        float textStartPos = (float)startPoint.Y - 10f;
                        SharpDX.RectangleF rect = new SharpDX.RectangleF(0f, textStartPos, endPoint.X + commonBuffer, 10f);
                        RenderTarget.DrawText("avg", textFormatSmaller, rect, areaBrushDx);

                    }

                    // value text
                    float textStartPos2 = (float)startPoint.Y - 10f;
                    SharpDX.RectangleF rect2 = new SharpDX.RectangleF(0f, textStartPos2, leadingSpace - 5f, 10f);
                    RenderTarget.DrawText(string.Format("{0}", row.Key), textFormat, rect2, areaBrushDx);
                }

                // end text 
                //areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
                SharpDX.RectangleF rect3 = new SharpDX.RectangleF(0f, halfHeight + spacer + 15f, 245, 10f);
                RenderTarget.DrawText(dayCount + " day " + title + " distribution", textFormat, rect3, areaBrushDx);
    
                areaBrushDx.Dispose();
                textBrushDx.Dispose();
                pocBrush.Dispose();
                avgBrush.Dispose();
                volBrush.Dispose();

            }
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
		
		private static double StandardDeviation(IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
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
		
		[NinjaScriptProperty]
		[Display(Name="Note1Location", Description="Note Location", Order=6, GroupName="Stats")]
		public TextPosition NoteLocation
		{ get; set; }
		
		// quick draw dx
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
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private CycleCounterDX[] cacheCycleCounterDX;
		public CycleCounterDX CycleCounterDX(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation, int textSize)
		{
			return CycleCounterDX(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation, textSize);
		}

		public CycleCounterDX CycleCounterDX(ISeries<double> input, int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation, int textSize)
		{
			if (cacheCycleCounterDX != null)
				for (int idx = 0; idx < cacheCycleCounterDX.Length; idx++)
					if (cacheCycleCounterDX[idx] != null && cacheCycleCounterDX[idx].SwingStrength == swingStrength && cacheCycleCounterDX[idx].SmallCycleMin == smallCycleMin && cacheCycleCounterDX[idx].LargeCycleMin == largeCycleMin && cacheCycleCounterDX[idx].BackgroundColor == backgroundColor && cacheCycleCounterDX[idx].FontColor == fontColor && cacheCycleCounterDX[idx].OutlineColor == outlineColor && cacheCycleCounterDX[idx].NoteFont == noteFont && cacheCycleCounterDX[idx].BackgroundOpacity == backgroundOpacity && cacheCycleCounterDX[idx].NoteLocation == noteLocation && cacheCycleCounterDX[idx].textSize == textSize && cacheCycleCounterDX[idx].EqualsInput(input))
						return cacheCycleCounterDX[idx];
			return CacheIndicator<CycleCounterDX>(new CycleCounterDX(){ SwingStrength = swingStrength, SmallCycleMin = smallCycleMin, LargeCycleMin = largeCycleMin, BackgroundColor = backgroundColor, FontColor = fontColor, OutlineColor = outlineColor, NoteFont = noteFont, BackgroundOpacity = backgroundOpacity, NoteLocation = noteLocation, textSize = textSize }, input, ref cacheCycleCounterDX);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.CycleCounterDX CycleCounterDX(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation, int textSize)
		{
			return indicator.CycleCounterDX(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation, textSize);
		}

		public Indicators.CycleCounterDX CycleCounterDX(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation, int textSize)
		{
			return indicator.CycleCounterDX(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation, textSize);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.CycleCounterDX CycleCounterDX(int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation, int textSize)
		{
			return indicator.CycleCounterDX(Input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation, textSize);
		}

		public Indicators.CycleCounterDX CycleCounterDX(ISeries<double> input , int swingStrength, int smallCycleMin, int largeCycleMin, Brush backgroundColor, Brush fontColor, Brush outlineColor, SimpleFont noteFont, int backgroundOpacity, TextPosition noteLocation, int textSize)
		{
			return indicator.CycleCounterDX(input, swingStrength, smallCycleMin, largeCycleMin, backgroundColor, fontColor, outlineColor, noteFont, backgroundOpacity, noteLocation, textSize);
		}
	}
}

#endregion
