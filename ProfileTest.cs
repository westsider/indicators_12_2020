//
// Copyright (C) 2020, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
public class ProfileTest : Indicator
{
		// These are WPF Brushes which are pushed and exposed to the UI by default
		// And allow users to configure a custom value of their choice
		// We will later convert the user defined brush from the UI to SharpDX Brushes for rendering purposes
		private System.Windows.Media.Brush	areaBrush;
		private System.Windows.Media.Brush	textBrush;
		private System.Windows.Media.Brush	smallAreaBrush;
		private int							areaOpacity;
		private SMA							mySma;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionSampleCustomRender;
				Name						= "Profile Test";
				Calculate					= Calculate.OnBarClose;
				DisplayInDataBox			= false;
				IsOverlay					= true;
				IsChartOnly					= true;
				IsSuspendedWhileInactive	= true;
				ScaleJustification			= ScaleJustification.Right;
				AreaBrush = System.Windows.Media.Brushes.DodgerBlue;
				TextBrush = System.Windows.Media.Brushes.DodgerBlue;
				SmallAreaBrush = System.Windows.Media.Brushes.Crimson;
				AreaOpacity = 20;
				AddPlot(System.Windows.Media.Brushes.Crimson, NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameSampleCustomRender);
			}
			else if (State == State.DataLoaded)
			{
				mySma = SMA(20);
			}
			else if (State == State.Historical)
			{
				SetZOrder(-1); // default here is go below the bars and called in State.Historical
			}
		}

		protected override void OnBarUpdate()
		{
			Value[0] = mySma[0];
		}

	protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
	{
		SharpDX.Vector2 startPoint;
		SharpDX.Vector2 endPoint;
		
		float start = ChartPanel.X ;
		float halfHeight = ChartPanel.H / 2;
		Print("Start Point " + start + " height "  +  ChartPanel.H + " half height " +   halfHeight );

		if (!IsInHitTest)
		{

			SharpDX.Direct2D1.Brush areaBrushDx;
			areaBrushDx = areaBrush.ToDxBrush(RenderTarget);
			SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget,
				SharpDX.Color.DodgerBlue);
			
			for (int index = 0; index < 5; index++) 
			{
				int spacer = index * 20;
				startPoint = new SharpDX.Vector2(start, halfHeight  + spacer);
				endPoint = new SharpDX.Vector2(ChartPanel.X + ChartPanel.W, halfHeight  + spacer);
				drawRow(startPoint: startPoint, endPoint: endPoint, areaBrushDx: areaBrushDx);
			}
			
			areaBrushDx.Dispose();
		}
	}
	
	private void drawRow(SharpDX.Vector2 startPoint, SharpDX.Vector2 endPoint, SharpDX.Direct2D1.Brush areaBrushDx) {
		RenderTarget.DrawLine(startPoint, endPoint, areaBrushDx, 10);
	}

	#region Properties
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
	[Display(ResourceType = typeof(Custom.Resource), Name = "SmallAreaColor", GroupName = "NinjaScriptGeneral")]
	public System.Windows.Media.Brush SmallAreaBrush
	{
		get { return smallAreaBrush; }
		set { smallAreaBrush = value; }
	}

	[Browsable(false)]
	public string SmallAreaBrushSerialize
	{
		get { return Serialize.BrushToString(SmallAreaBrush); }
		set { SmallAreaBrush = Serialize.StringToBrush(value); }
	}

	[Browsable(false)]
	[XmlIgnore]
	public Series<double> TestPlot
	{
		get { return Values[0]; }
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
		private ProfileTest[] cacheProfileTest;
		public ProfileTest ProfileTest()
		{
			return ProfileTest(Input);
		}

		public ProfileTest ProfileTest(ISeries<double> input)
		{
			if (cacheProfileTest != null)
				for (int idx = 0; idx < cacheProfileTest.Length; idx++)
					if (cacheProfileTest[idx] != null &&  cacheProfileTest[idx].EqualsInput(input))
						return cacheProfileTest[idx];
			return CacheIndicator<ProfileTest>(new ProfileTest(), input, ref cacheProfileTest);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ProfileTest ProfileTest()
		{
			return indicator.ProfileTest(Input);
		}

		public Indicators.ProfileTest ProfileTest(ISeries<double> input )
		{
			return indicator.ProfileTest(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ProfileTest ProfileTest()
		{
			return indicator.ProfileTest(Input);
		}

		public Indicators.ProfileTest ProfileTest(ISeries<double> input )
		{
			return indicator.ProfileTest(input);
		}
	}
}

#endregion
