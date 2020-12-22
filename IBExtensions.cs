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
	public class IBExtensions : Indicator
	{
		private long startTime = 0;
		private	long ibTime = 0;
		private	long endTime = 0;
		private int openingBar = 0;
		private int ibLength = 0;
		private double IB_Low = 0.0;
		private double IB_High = 0.0;
		private int lastBar = 0;
		private double ibRange = 0.0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "IB Extensions";
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
				RTHOpen						= DateTime.Parse("06:30", System.Globalization.CultureInfo.InvariantCulture);
				RTHClose						= DateTime.Parse("13:15", System.Globalization.CultureInfo.InvariantCulture);
				IBClose						= DateTime.Parse("07:30", System.Globalization.CultureInfo.InvariantCulture);
				AddPlot(Brushes.Orange, "IBHighs");
				AddPlot(Brushes.Orange, "IBLow");
				AddPlot(Brushes.Orange, "IBup1");
				AddPlot(Brushes.Orange, "IBup2");
				AddPlot(Brushes.Orange, "IBup3");
				AddPlot(Brushes.Orange, "IBdown1");
				AddPlot(Brushes.Orange, "IBdown2");
				AddPlot(Brushes.Orange, "IBdown3");
				
			}
			else if (State == State.Configure)
			{
				startTime = long.Parse(RTHOpen.ToString("HHmmss"));
			 	endTime = long.Parse(RTHClose.ToString("HHmmss"));
				ibTime = long.Parse(IBClose.ToString("HHmmss"));
				ClearOutputWindow();
			}
		}

		protected override void OnBarUpdate()
		{
			if( CurrentBar < 10 ) { return; }
			lastBar = CurrentBar -1;
			setOpen();
			ibData();
			drawIBLines();
		}

		private void setOpen() { 
			if (ToTime(Time[0]) == startTime ) { 
				openingBar = CurrentBars[0];	
				Draw.VerticalLine(this, "startTime", 0, Brushes.DimGray); 
			}
		}
		
		private void ibData() { 
			if ( ToTime(Time[0]) == ibTime ) { 
				ibLength = CurrentBars[0] - openingBar;
				if ( ibLength> 0 ) {
					IB_Low = MIN(Low, ibLength)[0];
					IB_High = MAX(High, ibLength)[0]; 
                    ibRange = IB_High - IB_Low;
				}
			}
		}
					
		private void drawIBLines()
        {
            if ( ToTime(Time[0]) >= ibTime && ToTime(Time[0]) <= endTime)
            {
				IBup1[0] = IB_High + ibRange;
				Draw.Text(this, "IBup1", "X1", -6, IBup1[0] , Brushes.Orange);
				
				IBup2[0] = IB_High + (ibRange * 2 );;
				Draw.Text(this, "IBup2", "X2", -6, IBup2[0] , Brushes.Orange);
				
				IBup3[0] = IB_High + (ibRange * 3 );
				Draw.Text(this, "IBup3", "X3", -6, IBup3[0] , Brushes.Orange);
				
				IBHighs[0] = IB_High;
				Draw.Text(this, "IB_High", "IB High", -6, IB_High , Brushes.Black);
				
				IBLow[0] = IB_Low;
				//RemoveDrawObject("IB Low"+lastBar);
				Draw.Text(this, "IB Low", "IB Low", -6, IB_Low , Brushes.Black);
				IBdown1[0] = IB_Low - ibRange;
				Draw.Text(this, "IBdown1", "X1", -6, IBdown1[0] , Brushes.Orange);
				IBdown2[0] = IB_Low - (ibRange * 2 );
				Draw.Text(this, "IBdown2", "X2", -6, IBdown2[0] , Brushes.Orange);
				IBdown3[0] = IB_Low - (ibRange * 3 );
				Draw.Text(this, "IBdown3", "X3", -6, IBdown3[0] , Brushes.Orange);
			}
		}
		
					
		#region Properties
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHOpen", Order=1, GroupName="Parameters")]
		public DateTime RTHOpen
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="RTHClose", Order=2, GroupName="Parameters")]
		public DateTime RTHClose
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="IBClose", Order=3, GroupName="Parameters")]
		public DateTime IBClose
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBHighs
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
		public Series<double> IBup1
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBup2
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBup3
		{
			get { return Values[4]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBdown1
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBdown2
		{
			get { return Values[6]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> IBdown3
		{
			get { return Values[7]; }
		}
	
		
		
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private IBExtensions[] cacheIBExtensions;
		public IBExtensions IBExtensions(DateTime rTHOpen, DateTime rTHClose, DateTime iBClose)
		{
			return IBExtensions(Input, rTHOpen, rTHClose, iBClose);
		}

		public IBExtensions IBExtensions(ISeries<double> input, DateTime rTHOpen, DateTime rTHClose, DateTime iBClose)
		{
			if (cacheIBExtensions != null)
				for (int idx = 0; idx < cacheIBExtensions.Length; idx++)
					if (cacheIBExtensions[idx] != null && cacheIBExtensions[idx].RTHOpen == rTHOpen && cacheIBExtensions[idx].RTHClose == rTHClose && cacheIBExtensions[idx].IBClose == iBClose && cacheIBExtensions[idx].EqualsInput(input))
						return cacheIBExtensions[idx];
			return CacheIndicator<IBExtensions>(new IBExtensions(){ RTHOpen = rTHOpen, RTHClose = rTHClose, IBClose = iBClose }, input, ref cacheIBExtensions);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.IBExtensions IBExtensions(DateTime rTHOpen, DateTime rTHClose, DateTime iBClose)
		{
			return indicator.IBExtensions(Input, rTHOpen, rTHClose, iBClose);
		}

		public Indicators.IBExtensions IBExtensions(ISeries<double> input , DateTime rTHOpen, DateTime rTHClose, DateTime iBClose)
		{
			return indicator.IBExtensions(input, rTHOpen, rTHClose, iBClose);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.IBExtensions IBExtensions(DateTime rTHOpen, DateTime rTHClose, DateTime iBClose)
		{
			return indicator.IBExtensions(Input, rTHOpen, rTHClose, iBClose);
		}

		public Indicators.IBExtensions IBExtensions(ISeries<double> input , DateTime rTHOpen, DateTime rTHClose, DateTime iBClose)
		{
			return indicator.IBExtensions(input, rTHOpen, rTHClose, iBClose);
		}
	}
}

#endregion
