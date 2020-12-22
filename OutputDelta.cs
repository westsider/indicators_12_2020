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
	public class OutputDelta : Indicator
	{
		private OrderFlowCumulativeDelta cumulativeDelta;
		private string csvPath = @"C:\Users\trade\Documents\_Send_To_Mac\Delta_"; 
		private double gxDelta = 0.0;
		private double tenDelta = 0.0;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Output Delta";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= false;
				DrawVerticalGridLines						= false;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				Opening										= DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				SampleEnd									= DateTime.Parse("09:40", System.Globalization.CultureInfo.InvariantCulture);

			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{				
			      // Instantiate the indicator
			      cumulativeDelta = OrderFlowCumulativeDelta(CumulativeDeltaType.BidAsk, CumulativeDeltaPeriod.Session, 0);
			}
			
		}


		protected override void OnBarUpdate()
		{
			AddHeader();
			if (CurrentBars[0] < 10 ) return;
			
			if (BarsInProgress == 0)
			{
				if ( IsFirstTickOfBar ) {
					
					if ( ToTime(Time[0]) == ToTime(Opening) ) {
						//Print("Open \t "+FormatDateTime() + "\t" + cumulativeDelta.DeltaClose[0]);
						gxDelta = cumulativeDelta.DeltaClose[0];
					}
					if (ToTime(Time[0]) == ToTime(SampleEnd)) {
						//Print("Sample \t" + FormatDateTime() + "\t" + cumulativeDelta.DeltaClose[0]);
						tenDelta = cumulativeDelta.DeltaClose[0];
						string thisLine = FormatDateTime() + ", " + gxDelta + ", " + tenDelta;
						AddNewLine(thisLine: thisLine);
					}
				}
			}
			else if (BarsInProgress == 1)
			{
			    // We have to update the secondary series of the hosted indicator to make sure the values we get in BarsInProgress == 0 are in sync
			    cumulativeDelta.Update(cumulativeDelta.BarsArray[1].Count - 1, 1); 
				//Add your custom indicator logic here.
			}
			
		}
		
		private void AddNewLine(string thisLine) {
			Print(thisLine );
			WriteFile(path: csvPath, newLine: thisLine, header: false);
		}
		
		private void AddHeader() {
			if (CurrentBar == 1 && IsFirstTickOfBar) {  
				if ( BarsInProgress == 0  ) {
					SetFileName();
					string header = "Date, GxDelta, 10delta";  // or ibRange
					Print(header);
					WriteFile(path: csvPath, newLine: header, header: true);
				}
				return;
			}
		}
		
				private void WriteFile(string path, string newLine, bool header)
        {
			if ( header ) {
				ClearFile(path: csvPath);
				using (var tw = new StreamWriter(path, true))
	            {
	                tw.WriteLine(newLine); 
	                tw.Close();
	            }
				return;
			}
			
            using (var tw = new StreamWriter(path, true))
            {
                tw.WriteLine(newLine);
                tw.Close();
            }
        }
		
		private void ClearFile(string path)
        {
            try    
			{    
				// Check if file exists with its full path    
				if (File.Exists(path))    
				{    
					// If file found, delete it    
					File.Delete(path);    
					Print("\t\t\tFile deleted.");    
				} 
				else  Print("\t\t\tFile not found");    
			}    
			catch (IOException ioExp)    
			{    
				Print(ioExp.Message);    
			} 
			
        }
		
		private void SetFileName() {
			string inst = Instrument.FullName;
			string instOnly = inst.Remove(inst.Length-6);
			DateTime myDate = DateTime.Today;  // DateTime type
			string prettyDate = myDate.ToString("M_d_yyyy");
			string instDate = instOnly + "_" + prettyDate + ".csv";
			csvPath += instDate;
		}
		
		private string FormatDateTime() {
			DateTime myDate = Time[0];  // DateTime type
			string prettyDate = myDate.ToString("M/d/yyyy") + " " + myDate.ToString("hh:mm");
			return prettyDate;
		}

		#region Properties

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Open", Order=1, GroupName="Parameters")]
		public DateTime Opening
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Sample End", Order=2, GroupName="Parameters")]
		public DateTime SampleEnd
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OutputDelta[] cacheOutputDelta;
		public OutputDelta OutputDelta(DateTime opening, DateTime sampleEnd)
		{
			return OutputDelta(Input, opening, sampleEnd);
		}

		public OutputDelta OutputDelta(ISeries<double> input, DateTime opening, DateTime sampleEnd)
		{
			if (cacheOutputDelta != null)
				for (int idx = 0; idx < cacheOutputDelta.Length; idx++)
					if (cacheOutputDelta[idx] != null && cacheOutputDelta[idx].Opening == opening && cacheOutputDelta[idx].SampleEnd == sampleEnd && cacheOutputDelta[idx].EqualsInput(input))
						return cacheOutputDelta[idx];
			return CacheIndicator<OutputDelta>(new OutputDelta(){ Opening = opening, SampleEnd = sampleEnd }, input, ref cacheOutputDelta);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OutputDelta OutputDelta(DateTime opening, DateTime sampleEnd)
		{
			return indicator.OutputDelta(Input, opening, sampleEnd);
		}

		public Indicators.OutputDelta OutputDelta(ISeries<double> input , DateTime opening, DateTime sampleEnd)
		{
			return indicator.OutputDelta(input, opening, sampleEnd);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OutputDelta OutputDelta(DateTime opening, DateTime sampleEnd)
		{
			return indicator.OutputDelta(Input, opening, sampleEnd);
		}

		public Indicators.OutputDelta OutputDelta(ISeries<double> input , DateTime opening, DateTime sampleEnd)
		{
			return indicator.OutputDelta(input, opening, sampleEnd);
		}
	}
}

#endregion
