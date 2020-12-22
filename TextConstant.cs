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
	public class TextConstant : Indicator
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Text Upper Fixed";
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
				Line1					= @"first";
				Line2					= @"two";
				Line3					= @"three";
				Line4					= @"four";
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			var message = "\t" + Line1 + "\n\t" + Line2 + "\n\t" + Line3 + "\n\t" +  Line4;
			// Instantiate a TextFixed object
			TextFixed myTF = Draw.TextFixed(this, "tag1", message, TextPosition.TopLeft);
			// Draw.TextFixed(this, 
			// Change the object's TextPosition
			//myTF.AreaBrush = 

		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Line1", Order=1, GroupName="Parameters")]
		public string Line1
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Line2", Order=2, GroupName="Parameters")]
		public string Line2
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Line3", Order=3, GroupName="Parameters")]
		public string Line3
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Line4", Order=4, GroupName="Parameters")]
		public string Line4
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TextConstant[] cacheTextConstant;
		public TextConstant TextConstant(string line1, string line2, string line3, string line4)
		{
			return TextConstant(Input, line1, line2, line3, line4);
		}

		public TextConstant TextConstant(ISeries<double> input, string line1, string line2, string line3, string line4)
		{
			if (cacheTextConstant != null)
				for (int idx = 0; idx < cacheTextConstant.Length; idx++)
					if (cacheTextConstant[idx] != null && cacheTextConstant[idx].Line1 == line1 && cacheTextConstant[idx].Line2 == line2 && cacheTextConstant[idx].Line3 == line3 && cacheTextConstant[idx].Line4 == line4 && cacheTextConstant[idx].EqualsInput(input))
						return cacheTextConstant[idx];
			return CacheIndicator<TextConstant>(new TextConstant(){ Line1 = line1, Line2 = line2, Line3 = line3, Line4 = line4 }, input, ref cacheTextConstant);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TextConstant TextConstant(string line1, string line2, string line3, string line4)
		{
			return indicator.TextConstant(Input, line1, line2, line3, line4);
		}

		public Indicators.TextConstant TextConstant(ISeries<double> input , string line1, string line2, string line3, string line4)
		{
			return indicator.TextConstant(input, line1, line2, line3, line4);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TextConstant TextConstant(string line1, string line2, string line3, string line4)
		{
			return indicator.TextConstant(Input, line1, line2, line3, line4);
		}

		public Indicators.TextConstant TextConstant(ISeries<double> input , string line1, string line2, string line3, string line4)
		{
			return indicator.TextConstant(input, line1, line2, line3, line4);
		}
	}
}

#endregion
