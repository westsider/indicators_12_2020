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
	public class VerticalLineAtTime : Indicator
	{
		private long startTime = 0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "VerticalLineAtTime";
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
				LineTime						= DateTime.Parse("07:30", System.Globalization.CultureInfo.InvariantCulture);
				ABrush					= Brushes.DimGray;
			}
			else if (State == State.Configure)
			{ 
				startTime = long.Parse(LineTime.ToString("HHmmss"));
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < 2 ) { return; }
			if (ToTime(Time[0]) >= startTime && ToTime(Time[1]) <= startTime) {
				Draw.VerticalLine(this, "IbTime"+CurrentBar, 0, ABrush);	
			} else if ( ToTime(Time[0]) == startTime) {
				Draw.VerticalLine(this, "IbEnd"+CurrentBar, 0, ABrush);	
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="LineTime", Order=1, GroupName="Parameters")]
		public DateTime LineTime
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Line Color", Order=2, GroupName="Parameters")]
		public Brush ABrush
		{ get; set; }

		[Browsable(false)]
		public string ABrushSerializable
		{
			get { return Serialize.BrushToString(ABrush); }
			set { ABrush = Serialize.StringToBrush(value); }
		}
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private VerticalLineAtTime[] cacheVerticalLineAtTime;
		public VerticalLineAtTime VerticalLineAtTime(DateTime lineTime, Brush aBrush)
		{
			return VerticalLineAtTime(Input, lineTime, aBrush);
		}

		public VerticalLineAtTime VerticalLineAtTime(ISeries<double> input, DateTime lineTime, Brush aBrush)
		{
			if (cacheVerticalLineAtTime != null)
				for (int idx = 0; idx < cacheVerticalLineAtTime.Length; idx++)
					if (cacheVerticalLineAtTime[idx] != null && cacheVerticalLineAtTime[idx].LineTime == lineTime && cacheVerticalLineAtTime[idx].ABrush == aBrush && cacheVerticalLineAtTime[idx].EqualsInput(input))
						return cacheVerticalLineAtTime[idx];
			return CacheIndicator<VerticalLineAtTime>(new VerticalLineAtTime(){ LineTime = lineTime, ABrush = aBrush }, input, ref cacheVerticalLineAtTime);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.VerticalLineAtTime VerticalLineAtTime(DateTime lineTime, Brush aBrush)
		{
			return indicator.VerticalLineAtTime(Input, lineTime, aBrush);
		}

		public Indicators.VerticalLineAtTime VerticalLineAtTime(ISeries<double> input , DateTime lineTime, Brush aBrush)
		{
			return indicator.VerticalLineAtTime(input, lineTime, aBrush);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.VerticalLineAtTime VerticalLineAtTime(DateTime lineTime, Brush aBrush)
		{
			return indicator.VerticalLineAtTime(Input, lineTime, aBrush);
		}

		public Indicators.VerticalLineAtTime VerticalLineAtTime(ISeries<double> input , DateTime lineTime, Brush aBrush)
		{
			return indicator.VerticalLineAtTime(input, lineTime, aBrush);
		}
	}
}

#endregion
