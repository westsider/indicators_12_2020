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
using System.IO;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class ReadIBandExt : Indicator
	{
		private string pathForIB;
		
		struct TodaysLevels { 
			public double ibHigh;
			public double ibLow;
			public bool inYestRange;
			public double excursionLow;
			public double excursionHigh;
			public double Y_Low;
			public double Y_High;
		}
		
		private TodaysLevels todaysLevels = new TodaysLevels(); 
		
		private long startTime = 0;
		private	long endTime = 0;
		private	long ibTime = 0;
		private int openingBar = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Read IB and Ext";
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
				FileName					= @"IBandExt.csv";
				pathForIB					= NinjaTrader.Core.Globals.UserDataDir + FileName;
				LineWeight					= 2;
				Opcaity						= 20;
				RectangleCOlor					= Brushes.DimGray;
				TextColor					= Brushes.DimGray;
				IBHighColor					= Brushes.DimGray;
				IBLowColor					= Brushes.DimGray;
				IBextColor					= Brushes.DimGray;
				YestHighColor					= Brushes.Red;
				YestLowColor					= Brushes.DodgerBlue;
				
				RTHopen						= DateTime.Parse("06:31", System.Globalization.CultureInfo.InvariantCulture);
				IB							= DateTime.Parse("07:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHclose					= DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture);
				
				AddPlot(Brushes.Orange, "IBhigh");
				AddPlot(Brushes.Orange, "IBLow");
				AddPlot(Brushes.Orange, "IBUpper");
				AddPlot(Brushes.Orange, "IBLower");
				AddPlot(Brushes.Orange, "YestHigh");
				AddPlot(Brushes.Orange, "YestLow");
			}
			else if (State == State.Configure)
			{
				startTime = long.Parse(RTHopen.ToString("HHmmss"));
			 	endTime = long.Parse(RTHclose.ToString("HHmmss"));
				ibTime = long.Parse(IB.ToString("HHmmss"));
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{
			if( CurrentBar < 20 ) { return; }
			if (ToTime(Time[0]) >= startTime && ToTime(Time[1]) <= startTime  ) { openingBar = CurrentBar;}
			
			if (ToTime(Time[0]) >= ibTime  && ToTime(Time[1]) <= ibTime ) { 
				ReadFile(path: pathForIB, debug: true);
				int ibLength = CurrentBar - openingBar;
				drawRectLong(name: "IB", Top:  todaysLevels.ibHigh, Bottom: todaysLevels.ibLow, Length: ibLength, colors: RectangleCOlor);
				string openMessage = "Outside range";
				if (todaysLevels.inYestRange ) {
					openMessage = "In Range";
				}
				Draw.Text(this, "MyText", openMessage, ibLength / 2, todaysLevels.ibHigh + 2 * TickSize, RectangleCOlor);
			}

			if (ToTime(Time[0]) >= ibTime && ToTime(Time[0]) <= endTime  ) {
				IBhigh[0] = todaysLevels.ibHigh;
				Plots[0].Brush = IBHighColor;
	    		Plots[0].Width = LineWeight;
				
				IBLow[0] = todaysLevels.ibLow;
				Plots[1].Brush = IBLowColor;
	    		Plots[1].Width = LineWeight;
				
				IBUpper[0] = todaysLevels.excursionHigh;
				Plots[2].Brush = IBHighColor;
	    		Plots[2].Width = LineWeight;
				Plots[2].DashStyleHelper = DashStyleHelper.Dash;
				
				IBLower[0] = todaysLevels.excursionLow;
				Plots[3].Brush = IBLowColor;
	    		Plots[3].Width = LineWeight;
				Plots[3].DashStyleHelper = DashStyleHelper.Dash;
			}
			
			if (ToTime(Time[0]) >= startTime && ToTime(Time[0]) <= endTime  ) {
				YestHigh[0] = todaysLevels.Y_High;
				Plots[4].Brush = YestHighColor;
	    		Plots[4].Width = LineWeight;
				Plots[4].DashStyleHelper = DashStyleHelper.Dash;
				
				YestLow[0] = todaysLevels.Y_Low;
				Plots[5].Brush = YestLowColor;
	    		Plots[5].Width = LineWeight;
				Plots[5].DashStyleHelper = DashStyleHelper.Dash;
			}
		}
		
		private void drawRectLong(string name, double Top, double Bottom, int Length, Brush colors)
        {
            //RemoveDrawObject("name" + lastBar);
            Draw.Rectangle(this, "name", false, Length, Top, 0,
                Bottom, Brushes.Transparent, colors, Opcaity);
        }
		
		private void ReadFile(string path, bool debug)
        {	
            string line;
	        using (StreamReader reader = new StreamReader(path))
	        {
	            line = reader.ReadLine();
	        }
			string[] parts = line.Split(',');
			//Print(line);
			int counter = 0;
			foreach( string i in parts) {
				//Print(i);
				counter+= 1;
				if ( counter == 1 ) {
					todaysLevels.ibHigh = Convert.ToDouble(i);
				}
				if ( counter == 2 ) {
					todaysLevels.ibLow = Convert.ToDouble(i);
				}
				if ( counter == 3 ) {
					//Print("bool "+ i);
					todaysLevels.inYestRange = Convert.ToBoolean(i);
				}
				if ( counter == 4 ) {
					todaysLevels.excursionHigh = Convert.ToDouble(i);
				}
				if ( counter == 5 ) {
					todaysLevels.excursionLow = Convert.ToDouble(i);
				}
				if ( counter == 6 ) {
					todaysLevels.Y_High = Convert.ToDouble(i);
				}
				if ( counter == 7 ) {
					todaysLevels.Y_Low = Convert.ToDouble(i);
				}
			}
			
			if ( debug ) { Print(Time[0].ToShortDateString() + " " + todaysLevels.ibHigh + " " + todaysLevels.ibLow + " " + todaysLevels.inYestRange + " " + todaysLevels.excursionHigh 
			+ " " + todaysLevels.excursionLow + " " + todaysLevels.Y_High + " " + todaysLevels.Y_Low); }
        }
		

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="FileName", Order=1, GroupName="Parameters")]
		public string FileName
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="LineWeight", Order=2, GroupName="Parameters")]
		public int LineWeight
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Opcaity", Order=3, GroupName="Parameters")]
		public int Opcaity
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="RectangleCOlor", Order=4, GroupName="Parameters")]
		public Brush RectangleCOlor
		{ get; set; }

		[Browsable(false)]
		public string RectangleCOlorSerializable
		{
			get { return Serialize.BrushToString(RectangleCOlor); }
			set { RectangleCOlor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="TextColor", Order=5, GroupName="Parameters")]
		public Brush TextColor
		{ get; set; }

		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="IBHighColor", Order=6, GroupName="Parameters")]
		public Brush IBHighColor
		{ get; set; }

		[Browsable(false)]
		public string IBHighColorSerializable
		{
			get { return Serialize.BrushToString(IBHighColor); }
			set { IBHighColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="IBLowColor", Order=7, GroupName="Parameters")]
		public Brush IBLowColor
		{ get; set; }

		[Browsable(false)]
		public string IBLowColorSerializable
		{
			get { return Serialize.BrushToString(IBLowColor); }
			set { IBLowColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="IBextColor", Order=8, GroupName="Parameters")]
		public Brush IBextColor
		{ get; set; }

		[Browsable(false)]
		public string IBextColorSerializable
		{
			get { return Serialize.BrushToString(IBextColor); }
			set { IBextColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="YestHighColor", Order=9, GroupName="Parameters")]
		public Brush YestHighColor
		{ get; set; }

		[Browsable(false)]
		public string YestHighColorSerializable
		{
			get { return Serialize.BrushToString(YestHighColor); }
			set { YestHighColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="YestLowColor", Order=10, GroupName="Parameters")]
		public Brush YestLowColor
		{ get; set; }

		[Browsable(false)]
		public string YestLowColorSerializable
		{
			get { return Serialize.BrushToString(YestLowColor); }
			set { YestLowColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHopen", Order=11, GroupName="Parameters")]
		public DateTime RTHopen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="IB", Order=12, GroupName="Parameters")]
		public DateTime IB
		{ get; set; }
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHclose", Order=13, GroupName="Parameters")]
		public DateTime RTHclose
		{ get; set; }
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBhigh
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBLow
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBUpper
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBLower
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YestHigh
		{
			get { return Values[4]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> YestLow
		{
			get { return Values[5]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ReadIBandExt[] cacheReadIBandExt;
		public ReadIBandExt ReadIBandExt(string fileName, int lineWeight, int opcaity, Brush rectangleCOlor, Brush textColor, Brush iBHighColor, Brush iBLowColor, Brush iBextColor, Brush yestHighColor, Brush yestLowColor, DateTime rTHopen, DateTime iB, DateTime rTHclose)
		{
			return ReadIBandExt(Input, fileName, lineWeight, opcaity, rectangleCOlor, textColor, iBHighColor, iBLowColor, iBextColor, yestHighColor, yestLowColor, rTHopen, iB, rTHclose);
		}

		public ReadIBandExt ReadIBandExt(ISeries<double> input, string fileName, int lineWeight, int opcaity, Brush rectangleCOlor, Brush textColor, Brush iBHighColor, Brush iBLowColor, Brush iBextColor, Brush yestHighColor, Brush yestLowColor, DateTime rTHopen, DateTime iB, DateTime rTHclose)
		{
			if (cacheReadIBandExt != null)
				for (int idx = 0; idx < cacheReadIBandExt.Length; idx++)
					if (cacheReadIBandExt[idx] != null && cacheReadIBandExt[idx].FileName == fileName && cacheReadIBandExt[idx].LineWeight == lineWeight && cacheReadIBandExt[idx].Opcaity == opcaity && cacheReadIBandExt[idx].RectangleCOlor == rectangleCOlor && cacheReadIBandExt[idx].TextColor == textColor && cacheReadIBandExt[idx].IBHighColor == iBHighColor && cacheReadIBandExt[idx].IBLowColor == iBLowColor && cacheReadIBandExt[idx].IBextColor == iBextColor && cacheReadIBandExt[idx].YestHighColor == yestHighColor && cacheReadIBandExt[idx].YestLowColor == yestLowColor && cacheReadIBandExt[idx].RTHopen == rTHopen && cacheReadIBandExt[idx].IB == iB && cacheReadIBandExt[idx].RTHclose == rTHclose && cacheReadIBandExt[idx].EqualsInput(input))
						return cacheReadIBandExt[idx];
			return CacheIndicator<ReadIBandExt>(new ReadIBandExt(){ FileName = fileName, LineWeight = lineWeight, Opcaity = opcaity, RectangleCOlor = rectangleCOlor, TextColor = textColor, IBHighColor = iBHighColor, IBLowColor = iBLowColor, IBextColor = iBextColor, YestHighColor = yestHighColor, YestLowColor = yestLowColor, RTHopen = rTHopen, IB = iB, RTHclose = rTHclose }, input, ref cacheReadIBandExt);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ReadIBandExt ReadIBandExt(string fileName, int lineWeight, int opcaity, Brush rectangleCOlor, Brush textColor, Brush iBHighColor, Brush iBLowColor, Brush iBextColor, Brush yestHighColor, Brush yestLowColor, DateTime rTHopen, DateTime iB, DateTime rTHclose)
		{
			return indicator.ReadIBandExt(Input, fileName, lineWeight, opcaity, rectangleCOlor, textColor, iBHighColor, iBLowColor, iBextColor, yestHighColor, yestLowColor, rTHopen, iB, rTHclose);
		}

		public Indicators.ReadIBandExt ReadIBandExt(ISeries<double> input , string fileName, int lineWeight, int opcaity, Brush rectangleCOlor, Brush textColor, Brush iBHighColor, Brush iBLowColor, Brush iBextColor, Brush yestHighColor, Brush yestLowColor, DateTime rTHopen, DateTime iB, DateTime rTHclose)
		{
			return indicator.ReadIBandExt(input, fileName, lineWeight, opcaity, rectangleCOlor, textColor, iBHighColor, iBLowColor, iBextColor, yestHighColor, yestLowColor, rTHopen, iB, rTHclose);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ReadIBandExt ReadIBandExt(string fileName, int lineWeight, int opcaity, Brush rectangleCOlor, Brush textColor, Brush iBHighColor, Brush iBLowColor, Brush iBextColor, Brush yestHighColor, Brush yestLowColor, DateTime rTHopen, DateTime iB, DateTime rTHclose)
		{
			return indicator.ReadIBandExt(Input, fileName, lineWeight, opcaity, rectangleCOlor, textColor, iBHighColor, iBLowColor, iBextColor, yestHighColor, yestLowColor, rTHopen, iB, rTHclose);
		}

		public Indicators.ReadIBandExt ReadIBandExt(ISeries<double> input , string fileName, int lineWeight, int opcaity, Brush rectangleCOlor, Brush textColor, Brush iBHighColor, Brush iBLowColor, Brush iBextColor, Brush yestHighColor, Brush yestLowColor, DateTime rTHopen, DateTime iB, DateTime rTHclose)
		{
			return indicator.ReadIBandExt(input, fileName, lineWeight, opcaity, rectangleCOlor, textColor, iBHighColor, iBLowColor, iBextColor, yestHighColor, yestLowColor, rTHopen, iB, rTHclose);
		}
	}
}

#endregion
