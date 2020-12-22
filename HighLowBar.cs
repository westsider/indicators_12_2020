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
	public class HighLowBar : Indicator
	{
		int lastBar = 0;
		int spaceRight = -8;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "High Low Bar";
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
				AddPlot(Brushes.DimGray, "Highs");
				AddPlot(Brushes.DimGray, "Lows");
				ABrush					= Brushes.Black;
				ABrush2					= Brushes.Black;
				ABool					= true;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
//			Highs[0] = High[0];
//			Lows[0] = Low[0];
			
			if ( CurrentBar < 5 ) { return; }
			lastBar = CurrentBar - 1;
			string highVal = High[0].ToString("N2");
			Draw.Text(this, "Hi",  highVal.Remove(0,3), spaceRight , High[0] + 2 * TickSize, ABrush);
			
			string lowVal = Low[0].ToString("N2");
			RemoveDrawObject("Lo"+lastBar);
			Draw.Text(this, "Lo"+CurrentBar,   lowVal.Remove(0,3), spaceRight , Low[0] - 2 * TickSize, ABrush);
			
			if (ABool ) {
				// show prior bar
				double atr = ATR(5)[0];
				//if ( High[1] != High[0] ) {
				setText(name: "lastHigh", level: High[1], offset: atr);// }
				//if ( Low[1] != Low[0] ) {
				setText(name: "lastLow", level: Low[1], offset: -atr); //}
			}
			setRange();
			
		}
		
		private void setText(String name, Double level, double offset) {
			RemoveDrawObject(name+lastBar);
			string strVal = level.ToString("N2").Remove(0,3);
			Draw.Text(this, name+CurrentBar,   strVal, spaceRight , level+ offset, ABrush2);
		}

		private void setRange() {
			string name = "range";
			double level = High[0] - ( Range()[0] * 0.5);
			RemoveDrawObject(name+lastBar);
			double tickRange = Range()[0] * 4;
			string strVal =  tickRange.ToString("N0");
			Draw.Text(this, name+CurrentBar,   strVal, spaceRight , level, ABrush2);
		}
		
		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Highs
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Lows
		{
			get { return Values[1]; }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Text Color Current", Order=6, GroupName="Parameters")]
		public Brush ABrush
		{ get; set; }

		[Browsable(false)]
		public string ABrushSerializable
		{
			get { return Serialize.BrushToString(ABrush); }
			set { ABrush = Serialize.StringToBrush(value); }
		}		
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="Text Color Prior", Order=6, GroupName="Parameters")]
		public Brush ABrush2
		{ get; set; }

		[Browsable(false)]
		public string ABrush2Serializable
		{
			get { return Serialize.BrushToString(ABrush2); }
			set { ABrush = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(Name="Show Prior Bar", Order=1, GroupName="Parameters")]
		public bool ABool
		{ get; set; }
		
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private HighLowBar[] cacheHighLowBar;
		public HighLowBar HighLowBar(Brush aBrush, Brush aBrush2, bool aBool)
		{
			return HighLowBar(Input, aBrush, aBrush2, aBool);
		}

		public HighLowBar HighLowBar(ISeries<double> input, Brush aBrush, Brush aBrush2, bool aBool)
		{
			if (cacheHighLowBar != null)
				for (int idx = 0; idx < cacheHighLowBar.Length; idx++)
					if (cacheHighLowBar[idx] != null && cacheHighLowBar[idx].ABrush == aBrush && cacheHighLowBar[idx].ABrush2 == aBrush2 && cacheHighLowBar[idx].ABool == aBool && cacheHighLowBar[idx].EqualsInput(input))
						return cacheHighLowBar[idx];
			return CacheIndicator<HighLowBar>(new HighLowBar(){ ABrush = aBrush, ABrush2 = aBrush2, ABool = aBool }, input, ref cacheHighLowBar);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.HighLowBar HighLowBar(Brush aBrush, Brush aBrush2, bool aBool)
		{
			return indicator.HighLowBar(Input, aBrush, aBrush2, aBool);
		}

		public Indicators.HighLowBar HighLowBar(ISeries<double> input , Brush aBrush, Brush aBrush2, bool aBool)
		{
			return indicator.HighLowBar(input, aBrush, aBrush2, aBool);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.HighLowBar HighLowBar(Brush aBrush, Brush aBrush2, bool aBool)
		{
			return indicator.HighLowBar(Input, aBrush, aBrush2, aBool);
		}

		public Indicators.HighLowBar HighLowBar(ISeries<double> input , Brush aBrush, Brush aBrush2, bool aBool)
		{
			return indicator.HighLowBar(input, aBrush, aBrush2, aBool);
		}
	}
}

#endregion
