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
	public class Reversal : Indicator
	{
		private double FastMA = 0.0;
		private double SlowMA = 0.0;
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Reversal";
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
				ColorBkg					= true;
				AudioAlert					= true;
				AudioFileShort					= @"C:\Users\trade\Documents\NinjaTrader 8\acme\sound\DowntrendShort.wav";
				AudioFileLong				= @"C:\Users\trade\Documents\NinjaTrader 8\acme\sound\UptrendLong.wav";
				UpColor					= Brushes.DodgerBlue;
				DownColor					= Brushes.Red;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 10 ) { return;}
			
			FastMA = SMA(28)[0];
			SlowMA = SMA(50)[0]; 
			int setup = PriceAbove(); 
		}
		
		private int MAStance() {
			if ( FastMA > SlowMA ) {
				//ColorBkgs(bullish: true);
				return 1;	
			} else {
				//ColorBkgs(bullish: false);
				return -1;	
			}
		}
		
		private int PriceAbove() {
			int answer = 0;
			if ( High[0] < SlowMA && MAStance() == 1 ) {
				ColorBkgs(bullish: true);
				answer = 1;
				int reversal = ReversalBar(bullish: true);
			}
			
			if ( Low[0] > FastMA && MAStance() == -1 ) {
				ColorBkgs(bullish: false);
				answer = -1;
				int reversal = ReversalBar(bullish: false);
			}
			return answer;
		}
		
		private int ReversalBar(bool bullish) {
			int answer = 0;
			if (Close[1] < Open[1] && Close[0] > Open[0] && bullish) {
				answer = 1;
				Draw.TriangleUp(this, "MyTriangleUp"+CurrentBar, false, 0, Low[0] - 2 * TickSize, UpColor);
				AlertTone(bullish: true);
			}
			if (Close[1] > Open[1] && Close[0] < Open[0] && !bullish) {
				answer = -1;
				Draw.TriangleDown(this, "MyTriangleDn"+CurrentBar, false, 0, High[0] + 2 * TickSize, DownColor);
				AlertTone(bullish: false);
			}
			return answer;
		}
		
		private void ColorBkgs(bool bullish) {
			if ( !ColorBkg ) { return;}
			if ( bullish ) { 
				//BackBrush = Brushes.PaleGreen;
				BackBrush  = new SolidColorBrush(Colors.Blue) {Opacity = 0.25};
				BackBrush.Freeze();
			} else {
				BackBrush  = new SolidColorBrush(Colors.Red) {Opacity = 0.25};
				BackBrush.Freeze();
			}
		}
		
		private void AlertTone(bool bullish) {
			if ( !AudioAlert ) { return; }
			if ( bullish ) {
				Alert("AudioFileLong", Priority.High, "Reversal Trade Long", AudioFileLong, 10, Brushes.Black, Brushes.Yellow);  
			} else {
				Alert("AudioFileShort", Priority.High, "Reversal Trade Short", AudioFileShort, 10, Brushes.Black, Brushes.Yellow);  
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="ColorBkg", Order=1, GroupName="Parameters")]
		public bool ColorBkg
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="AudioAlert", Order=2, GroupName="Parameters")]
		public bool AudioAlert
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Audio File Short", Order=3, GroupName="Parameters")]
		public string AudioFileShort
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Audio File Long", Order=4, GroupName="Parameters")]
		public string AudioFileLong
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="UpColor", Order=5, GroupName="Parameters")]
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
		[Display(Name="DownColor", Order=6, GroupName="Parameters")]
		public Brush DownColor
		{ get; set; }

		[Browsable(false)]
		public string DownColorSerializable
		{
			get { return Serialize.BrushToString(DownColor); }
			set { DownColor = Serialize.StringToBrush(value); }
		}			
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Reversal[] cacheReversal;
		public Reversal Reversal(bool colorBkg, bool audioAlert, string audioFileShort, string audioFileLong, Brush upColor, Brush downColor)
		{
			return Reversal(Input, colorBkg, audioAlert, audioFileShort, audioFileLong, upColor, downColor);
		}

		public Reversal Reversal(ISeries<double> input, bool colorBkg, bool audioAlert, string audioFileShort, string audioFileLong, Brush upColor, Brush downColor)
		{
			if (cacheReversal != null)
				for (int idx = 0; idx < cacheReversal.Length; idx++)
					if (cacheReversal[idx] != null && cacheReversal[idx].ColorBkg == colorBkg && cacheReversal[idx].AudioAlert == audioAlert && cacheReversal[idx].AudioFileShort == audioFileShort && cacheReversal[idx].AudioFileLong == audioFileLong && cacheReversal[idx].UpColor == upColor && cacheReversal[idx].DownColor == downColor && cacheReversal[idx].EqualsInput(input))
						return cacheReversal[idx];
			return CacheIndicator<Reversal>(new Reversal(){ ColorBkg = colorBkg, AudioAlert = audioAlert, AudioFileShort = audioFileShort, AudioFileLong = audioFileLong, UpColor = upColor, DownColor = downColor }, input, ref cacheReversal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.Reversal Reversal(bool colorBkg, bool audioAlert, string audioFileShort, string audioFileLong, Brush upColor, Brush downColor)
		{
			return indicator.Reversal(Input, colorBkg, audioAlert, audioFileShort, audioFileLong, upColor, downColor);
		}

		public Indicators.Reversal Reversal(ISeries<double> input , bool colorBkg, bool audioAlert, string audioFileShort, string audioFileLong, Brush upColor, Brush downColor)
		{
			return indicator.Reversal(input, colorBkg, audioAlert, audioFileShort, audioFileLong, upColor, downColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.Reversal Reversal(bool colorBkg, bool audioAlert, string audioFileShort, string audioFileLong, Brush upColor, Brush downColor)
		{
			return indicator.Reversal(Input, colorBkg, audioAlert, audioFileShort, audioFileLong, upColor, downColor);
		}

		public Indicators.Reversal Reversal(ISeries<double> input , bool colorBkg, bool audioAlert, string audioFileShort, string audioFileLong, Brush upColor, Brush downColor)
		{
			return indicator.Reversal(input, colorBkg, audioAlert, audioFileShort, audioFileLong, upColor, downColor);
		}
	}
}

#endregion
