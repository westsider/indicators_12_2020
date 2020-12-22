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
	public class OneTick : Indicator
	{ 
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "One Tick";
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
				UpColor										= Brushes.CornflowerBlue;
				DownColor									= Brushes.Red;
				AString										= "smsAlert2.wav";
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 20 ) { return; }
			//Add your custom indicator logic here. if high > eith bollinger or keltner
			double upperValue = Bollinger(2, 14).Upper[0];
			double upperK = KeltnerChannel(1.5, 10).Upper[0];
			if (High[0] >= upperValue || High[0] >= upperK  ) {
				if ( Close[0] <= Open[0] ) {
					BarBrush = DownColor;
					CandleOutlineBrush = DownColor;
					sendAlert( message: "sell", sound: AString);
				}
			}
			
			double lowerValue = Bollinger(2, 14).Lower[0];
			double lowerK = KeltnerChannel(1.5, 10).Lower[0];
			if (Low[0] <= lowerValue || Low[0] <= lowerK  ) {
				if (Close[0] >= Open[0]) {
					BarBrush = UpColor;
					CandleOutlineBrush = UpColor;
					sendAlert( message: "buy", sound: AString);
				}
			}
		}
		
		private void sendAlert(string message, string sound ) {
			message += " " + Bars.Instrument.MasterInstrument.Name;
			Alert("myAlert"+CurrentBar, Priority.High, message, NinjaTrader.Core.Globals.InstallDir+@"\sounds\"+ sound,10, Brushes.Black, Brushes.Yellow);  
			if (CurrentBar < Count -2) return;
		}

		#region Properties
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="UpColor", Order=1, GroupName="Parameters")]
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
		[Display(Name="DownColor", Order=2, GroupName="Parameters")]
		public Brush DownColor
		{ get; set; }

		[Browsable(false)]
		public string DownColorSerializable
		{
			get { return Serialize.BrushToString(DownColor); }
			set { DownColor = Serialize.StringToBrush(value); }
		}			
		
		[NinjaScriptProperty]
		[Display(Name="Sound Name", Order=3, GroupName="Parameters")]
		public string AString
		{ get; set; }
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OneTick[] cacheOneTick;
		public OneTick OneTick(Brush upColor, Brush downColor, string aString)
		{
			return OneTick(Input, upColor, downColor, aString);
		}

		public OneTick OneTick(ISeries<double> input, Brush upColor, Brush downColor, string aString)
		{
			if (cacheOneTick != null)
				for (int idx = 0; idx < cacheOneTick.Length; idx++)
					if (cacheOneTick[idx] != null && cacheOneTick[idx].UpColor == upColor && cacheOneTick[idx].DownColor == downColor && cacheOneTick[idx].AString == aString && cacheOneTick[idx].EqualsInput(input))
						return cacheOneTick[idx];
			return CacheIndicator<OneTick>(new OneTick(){ UpColor = upColor, DownColor = downColor, AString = aString }, input, ref cacheOneTick);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OneTick OneTick(Brush upColor, Brush downColor, string aString)
		{
			return indicator.OneTick(Input, upColor, downColor, aString);
		}

		public Indicators.OneTick OneTick(ISeries<double> input , Brush upColor, Brush downColor, string aString)
		{
			return indicator.OneTick(input, upColor, downColor, aString);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OneTick OneTick(Brush upColor, Brush downColor, string aString)
		{
			return indicator.OneTick(Input, upColor, downColor, aString);
		}

		public Indicators.OneTick OneTick(ISeries<double> input , Brush upColor, Brush downColor, string aString)
		{
			return indicator.OneTick(input, upColor, downColor, aString);
		}
	}
}

#endregion
