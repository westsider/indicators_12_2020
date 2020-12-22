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
	public class SymbolWatermark : Indicator
	{
		private SharpDX.Vector2					endPoint, startPoint;
		private SharpDX.DirectWrite.TextLayout	textLayout;
		private System.Windows.Media.Brush		textBrush;
		private SharpDX.Direct2D1.Brush			textBrushDx;
		private int								textOpacity;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "SymbolWatermark";
				IsOverlay									= true;
				IsSuspendedWhileInactive					= true;

				FontSize									= 160f;
				TextBrush									= Brushes.Gray;
				TextOpacity									= 30;
			}
			else if (State == State.Configure)
			{
				SetZOrder(-100);
			}
			else if (State == State.DataLoaded)
			{
				SetOpacity();

				startPoint	= new SharpDX.Vector2();
				endPoint	= new SharpDX.Vector2();

				// use the chart control text label font information when creating our object
				SharpDX.DirectWrite.TextFormat chartTextFormat	= ChartControl.Properties.LabelFont.ToDirectWriteTextFormat();
				// create a new TextFormat object using information from the chart labels
				SharpDX.DirectWrite.TextFormat textFormat		= new SharpDX.DirectWrite.TextFormat(Core.Globals.DirectWriteFactory, chartTextFormat.FontFamilyName, chartTextFormat.FontWeight, chartTextFormat.FontStyle, FontSize);
				// calculate the layout of the text to be drawn
				string fullName =  Instrument.FullName;
				var digits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-' };
				var shortName = fullName.TrimEnd(digits);
				textLayout = new SharpDX.DirectWrite.TextLayout(Core.Globals.DirectWriteFactory,
				  shortName, textFormat, 1000, textFormat.FontSize);
			}
		}

		protected override void OnBarUpdate() {	}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			// don't want to be clicked on
			if (IsInHitTest)
			{
				base.OnRender(chartControl, chartScale);
				return;
			}

			startPoint.X	= (float)(chartScale.Width / 2) - (float)(textLayout.Metrics.Width / 2);
			startPoint.Y	= (float)(chartScale.Height / 2) - (float)(textLayout.Metrics.Height / 2);
			endPoint.X		= startPoint.X;
			endPoint.Y		= startPoint.Y;

			RenderTarget.DrawTextLayout(startPoint, textLayout, textBrushDx);
			
			base.OnRender(chartControl, chartScale);
		}
		
		public override void OnRenderTargetChanged()
		{
			if (textBrushDx != null)
				textBrushDx.Dispose();

			if (RenderTarget != null)
			{
				try
				{
					textBrushDx		= TextBrush.ToDxBrush(RenderTarget);
				}
				catch (Exception e) { }
			}
		}

		private void SetOpacity()
		{
			if (TextBrush != null)
			{
				Brush tempBrush		= TextBrush.Clone();
				tempBrush.Opacity	= (double)TextOpacity / 100;
				tempBrush.Freeze();
				textBrush			= tempBrush;
			}
		}

		[NinjaScriptProperty, Range(0, float.MaxValue)]
		[Display(Name = "Text size", Order = 3, GroupName = "Parameters")]
		public float FontSize
		{ get; set; }

		[NinjaScriptProperty, XmlIgnore]
		[Display(Name = "Text color", Order = 1, GroupName = "Parameters")]
		public Brush TextBrush
		{
			get { return textBrush; }
			set
			{
				textBrush = value;
				SetOpacity();
			}
		}

		[Browsable(false)]
		public string TextBrushSerialize
		{
			get { return Serialize.BrushToString(textBrush); }
			set { textBrush = Serialize.StringToBrush(value); }
		}

		[NinjaScriptProperty, Range(0, 100)]
		[Display(Name = "Text opacity", Description = "Values 0 - 100", Order = 2, GroupName = "Parameters")]
		public int TextOpacity
		{
			get { return textOpacity; }
			set
			{
				textOpacity = value;
				SetOpacity();
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SymbolWatermark[] cacheSymbolWatermark;
		public SymbolWatermark SymbolWatermark(float fontSize, Brush textBrush, int textOpacity)
		{
			return SymbolWatermark(Input, fontSize, textBrush, textOpacity);
		}

		public SymbolWatermark SymbolWatermark(ISeries<double> input, float fontSize, Brush textBrush, int textOpacity)
		{
			if (cacheSymbolWatermark != null)
				for (int idx = 0; idx < cacheSymbolWatermark.Length; idx++)
					if (cacheSymbolWatermark[idx] != null && cacheSymbolWatermark[idx].FontSize == fontSize && cacheSymbolWatermark[idx].TextBrush == textBrush && cacheSymbolWatermark[idx].TextOpacity == textOpacity && cacheSymbolWatermark[idx].EqualsInput(input))
						return cacheSymbolWatermark[idx];
			return CacheIndicator<SymbolWatermark>(new SymbolWatermark(){ FontSize = fontSize, TextBrush = textBrush, TextOpacity = textOpacity }, input, ref cacheSymbolWatermark);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SymbolWatermark SymbolWatermark(float fontSize, Brush textBrush, int textOpacity)
		{
			return indicator.SymbolWatermark(Input, fontSize, textBrush, textOpacity);
		}

		public Indicators.SymbolWatermark SymbolWatermark(ISeries<double> input , float fontSize, Brush textBrush, int textOpacity)
		{
			return indicator.SymbolWatermark(input, fontSize, textBrush, textOpacity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SymbolWatermark SymbolWatermark(float fontSize, Brush textBrush, int textOpacity)
		{
			return indicator.SymbolWatermark(Input, fontSize, textBrush, textOpacity);
		}

		public Indicators.SymbolWatermark SymbolWatermark(ISeries<double> input , float fontSize, Brush textBrush, int textOpacity)
		{
			return indicator.SymbolWatermark(input, fontSize, textBrush, textOpacity);
		}
	}
}

#endregion
