#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class RangeHistogramBasic : Indicator
	{
		public Dictionary<double, int> Profile = new Dictionary<double, int>();
		
		public int Max = 0;
		public double MaxId = 0;
		public int TotalSamples = 0;
		public int ValueArea = 1;
		
		protected bool setScale = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"A histogram showing the distribution and standard distribution of bar ranges.";
				Name										= "RangeHistogramBasic";
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
				BarsRequiredToPlot			= 0;
				
				Deviation01	 = 1;
				NormalColor  = Brushes.White;
				ValueColor = Brushes.CornflowerBlue;
				
				textSize			= 11;
				TextColor = Brushes.Black;
				
				LimitByDayOfWeek = false;
				Week = System.DayOfWeek.Monday;
			}
			else if (State == State.Historical)
			{
				if (ChartControl == null) return;
				
				InitializeLifetimeDrawingTools();
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{		
			double rangeInTicks = Range()[0] / TickSize;
			int value = 0;
			
			if(!Profile.TryGetValue(rangeInTicks, out value))
			{
				//Set the initial value
				Profile.Add(rangeInTicks, 1);
				setScale = true;
			}
			else
			{
				//Update existing value
				value++;
				Profile[rangeInTicks] = value;
			}

			TotalSamples++;
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{	
			if(Profile.Count < 1)
				return;
			
			base.OnRender(chartControl, chartScale);				
			if(Bars == null || Bars.Instrument == null || IsInHitTest || CurrentBar < 1) { return; }
			
			
			try
			{
				KeyValuePair<double, int> maxkvp = Profile.First();
				foreach(KeyValuePair<double, int> row in Profile)
				{
					if(row.Value > maxkvp.Value)
					{
						maxkvp = row;
					}
				}
				
				Max = maxkvp.Value;
				MaxId = maxkvp.Key;
				
				int i;
				int valueArea = Max;
				for(i = 1; i < Profile.Count; i++)	
				{
					if(Profile.ContainsKey(MaxId + i))
					{
						valueArea = valueArea + Profile[MaxId + i];
					}
					
					if(Profile.ContainsKey(MaxId - i))
					{
						valueArea = valueArea + Profile[MaxId - i];
					}
					
					if(valueArea > TotalSamples * .8)
					{
						ValueArea = i;
						break;
					}
				}
				
				foreach(KeyValuePair<double, int> row in Profile)
				{
					drawRow(chartControl, chartScale, row.Key, row.Value);
				}
			}
			catch(Exception e)
			{
				Print("Error occured during drawing");
			}
		}
		
		private void drawRow(ChartControl chartControl, ChartScale chartScale, double value, int quantity)
		{
			//Calculate color of this row.
			Brush brushColor = NormalColor;	
			if(value <= MaxId + ValueArea && value >= MaxId - ValueArea)
			{				
				brushColor = ValueColor;
			}
			
				
			//Calculate cell properties
			double y1 = ((chartScale.GetYByValue(value) + chartScale.GetYByValue(value + 1)) / 2) + 1;
			double y2 = ((chartScale.GetYByValue(value) + chartScale.GetYByValue(value - 1)) / 2) - 1;
			
			SharpDX.RectangleF rect = new SharpDX.RectangleF();
			rect.X      = (float)chartControl.CanvasRight;
			rect.Y      = (float)y1;
			rect.Width  = (float)((chartControl.CanvasLeft - chartControl.CanvasRight) * Math.Log(quantity) / Math.Log(Max));
			rect.Height = (float)Math.Abs(y1 - y2);			

			//Draw the row.
			using(SharpDX.Direct2D1.Brush rowBrush =  brushColor.ToDxBrush(RenderTarget))
			{
				RenderTarget.FillRectangle(rect, rowBrush);
				//RenderTarget.FillRectangle(rect, rowBrush);
			}
			
			if(rect.Height > this.MinimumTextHeight)
			{
				RenderTarget.DrawText(string.Format("{0}", quantity), textFormat, rect, TextColor.ToDxBrush(RenderTarget));
			}
		}
		
		public override void OnCalculateMinMax()
		{
			try
			{
				if(Profile.Count < 1)
					return;
				
				MinValue = Profile.Keys.Min();
				MaxValue = Profile.Keys.Max();
			}
			catch(Exception e)
			{
				Print("Error occured calculating min/max");
			}
		}
		
		public int CalculateMinimumTextHeight()
		{
			return getTextHeight("0") - 5;
		}
		
		private int getTextHeight(string text)
		{
			SimpleFont sf = new NinjaTrader.Gui.Tools.SimpleFont("Consolas", textSize);
			
			float textHeight = 0f;
			
			if(text.Length > 0)
			{
				TextFormat tf = new TextFormat(new SharpDX.DirectWrite.Factory(), sf.Family.ToString(), SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, (float)sf.Size);
				TextLayout tl = new TextLayout(Core.Globals.DirectWriteFactory, text, tf, ChartPanel.W, ChartPanel.H);
				
				textHeight = tl.Metrics.Height;
				
				tf.Dispose();
				tl.Dispose();
			}
			
			return (int)textHeight;
		}
		
		public void InitializeLifetimeDrawingTools()
		{		
			textFormat = new TextFormat(Globals.DirectWriteFactory, "Arial", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, textSize)
            {
                TextAlignment = TextAlignment.Trailing,   //TextAlignment.Leading,
                WordWrapping = WordWrapping.NoWrap
            };
		}

		#region Properties
		[NinjaScriptProperty]
		public bool LimitByDayOfWeek
		{get;set;}
		
		[NinjaScriptProperty]
		public System.DayOfWeek Week
		{get;set;}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Deviation01", Description="Values within this range from the mode are considered normal.", Order=1, GroupName="Parameters")]
		public int Deviation01
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Normal values color", GroupName = "Parameters", Order = 9)]
		public Brush NormalColor
		{ get; set; }
		
		[Browsable(false)]
		public string normalColorSerializable
		{
			get { return Serialize.BrushToString(NormalColor); }
			set { NormalColor = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Value area Color", GroupName = "Parameters", Order = 9)]
		public Brush ValueColor
		{ get; set; }
		
		[Browsable(false)]
		public string valueColorSerializable
		{
			get { return Serialize.BrushToString(ValueColor); }
			set { ValueColor = Serialize.StringToBrush(value); }
		}
		
		protected TextFormat textFormat
		{ get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Text Size", GroupName = "Parameters", Order = 7)]
		public int textSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name = "Text Color", GroupName = "Parameters", Order = 9)]
		public Brush TextColor
		{ get; set; }
		
		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}
		
		protected int MinimumTextHeight
		{ get; set; }
		
		#endregion		

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RangeHistogramBasic[] cacheRangeHistogramBasic;
		public RangeHistogramBasic RangeHistogramBasic(bool limitByDayOfWeek, System.DayOfWeek week, int deviation01, Brush normalColor, Brush valueColor, int textSize, Brush textColor)
		{
			return RangeHistogramBasic(Input, limitByDayOfWeek, week, deviation01, normalColor, valueColor, textSize, textColor);
		}

		public RangeHistogramBasic RangeHistogramBasic(ISeries<double> input, bool limitByDayOfWeek, System.DayOfWeek week, int deviation01, Brush normalColor, Brush valueColor, int textSize, Brush textColor)
		{
			if (cacheRangeHistogramBasic != null)
				for (int idx = 0; idx < cacheRangeHistogramBasic.Length; idx++)
					if (cacheRangeHistogramBasic[idx] != null && cacheRangeHistogramBasic[idx].LimitByDayOfWeek == limitByDayOfWeek && cacheRangeHistogramBasic[idx].Week == week && cacheRangeHistogramBasic[idx].Deviation01 == deviation01 && cacheRangeHistogramBasic[idx].NormalColor == normalColor && cacheRangeHistogramBasic[idx].ValueColor == valueColor && cacheRangeHistogramBasic[idx].textSize == textSize && cacheRangeHistogramBasic[idx].TextColor == textColor && cacheRangeHistogramBasic[idx].EqualsInput(input))
						return cacheRangeHistogramBasic[idx];
			return CacheIndicator<RangeHistogramBasic>(new RangeHistogramBasic(){ LimitByDayOfWeek = limitByDayOfWeek, Week = week, Deviation01 = deviation01, NormalColor = normalColor, ValueColor = valueColor, textSize = textSize, TextColor = textColor }, input, ref cacheRangeHistogramBasic);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RangeHistogramBasic RangeHistogramBasic(bool limitByDayOfWeek, System.DayOfWeek week, int deviation01, Brush normalColor, Brush valueColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogramBasic(Input, limitByDayOfWeek, week, deviation01, normalColor, valueColor, textSize, textColor);
		}

		public Indicators.RangeHistogramBasic RangeHistogramBasic(ISeries<double> input , bool limitByDayOfWeek, System.DayOfWeek week, int deviation01, Brush normalColor, Brush valueColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogramBasic(input, limitByDayOfWeek, week, deviation01, normalColor, valueColor, textSize, textColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RangeHistogramBasic RangeHistogramBasic(bool limitByDayOfWeek, System.DayOfWeek week, int deviation01, Brush normalColor, Brush valueColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogramBasic(Input, limitByDayOfWeek, week, deviation01, normalColor, valueColor, textSize, textColor);
		}

		public Indicators.RangeHistogramBasic RangeHistogramBasic(ISeries<double> input , bool limitByDayOfWeek, System.DayOfWeek week, int deviation01, Brush normalColor, Brush valueColor, int textSize, Brush textColor)
		{
			return indicator.RangeHistogramBasic(input, limitByDayOfWeek, week, deviation01, normalColor, valueColor, textSize, textColor);
		}
	}
}

#endregion
