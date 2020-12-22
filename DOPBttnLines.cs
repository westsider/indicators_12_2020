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

using System.Windows.Media.Imaging;
using System.Net.Mail;
using System.Net.Mime;

using System.Windows.Media.Imaging;
using System.Windows.Controls;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class DOPBttnLines : Indicator
	{
		List<double> KPAList            = new List<double>();
		List<double> CPAList            = new List<double>();  
		NinjaTrader.Gui.Chart.Chart 	chart;
        BitmapFrame 					outputFrame;
		private string name             = "";
        private int firstbar            = 20;
		private int lastBar 			= 0;
		private bool timeSpanelapsed 	= true;

		// tool bar
		private System.Windows.Controls.Button button1 = null;
		private System.Windows.Controls.Button button2 = null;
		private System.Windows.Controls.Button button3 = null;
		private bool IsToolBarButtonAdded;
		private Style s = new Style();
		private Chart chartWindow;
		
		private MIN min;
		private MAX max;
		
        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"DOP Trading levels displayed on a price chart";
				Name										= "DOP Buttons Lines";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right; 
				IsSuspendedWhileInactive					= true;
				Text					= string.Empty;
				KPColor					= Brushes.Orange;
				CPAColor				= Brushes.CornflowerBlue;
				KPAoff					= false; 
				AudioAlertsOn 			= false;
				KPALertSound 			= "Alert1.wav";
				CPAALertSound 			= "Alert2.wav";
				ImportantAreaALertSound = "Alert3.wav";
				SendSMS 				= false;
				SMSAddress 				= "send_alert_to@gmail.com";
				HostEmailAddress        = "your_email@gmail.com";
				HostAddress             = "smtp.gmail.com";
				HostPort                = 587;
				HostUserName            = "user_name";
				HostPassword            = "pass_word";
				WatchArea1              = 2856.00;
				WatchArea2              = 2875.00;
				WatchArea3              = 2900.00;
				
				CPALineWidth = 3;
				KPALineWidth = 1;

				SimpleColsol 				= true;
				ComplexConsol 				= true;
				
				Period						= 3;
			}
			else if (State == State.Configure)
			{
				ClearOutputWindow();
				Dispatcher.BeginInvoke(new Action(() =>
				{
					chart = Window.GetWindow(ChartControl) as Chart;
				}));
				
				s.TargetType = typeof(System.Windows.Controls.Button);
		        s.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
		      	s.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.DimGray));
		      	s.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.WhiteSmoke));
		     	s.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
		      	s.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));
			}
			else if (State == State.Historical)
			{
				if (!IsToolBarButtonAdded) 
				{ 
					AddFirstButton();
			 		AddSecondButton(); 
					AddThirdButton();
				}
			}
			else if (State == State.Terminated)
			{
				DisposeCleanUp();
			}
			else if (State == State.DataLoaded) {
				min = MIN(Low, Period);
				max = MAX(High, Period);
			}
		}

		protected override void OnBarUpdate()
		{
			if ( CurrentBar < firstbar ) { return;}
			lastBar = CurrentBar -1;
			/// sort lines at regular intervals
			if (CurrentBar == firstbar) { GetData(); }
			
			if ( ToTime(Time[0]) == ToTime(DateTime.Parse("09:00", System.Globalization.CultureInfo.InvariantCulture)) ) {
				UpdateChart();
			}
 
			if ( ToTime(Time[0]) == ToTime(DateTime.Parse("11:00", System.Globalization.CultureInfo.InvariantCulture)) ) {
				UpdateChart();
			}
			
			if ( ToTime(Time[0]) == ToTime(DateTime.Parse("13:00", System.Globalization.CultureInfo.InvariantCulture)) ) {
				UpdateChart();
			}
			
			if ( ToTime(Time[0]) == ToTime(DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture)) ) {
				UpdateChart();
			}
			
			DrawLines();
			ScanForAlerts();
            AddLables();
			DrawConsol(minrange: 2.0);
			//DrawConsolAll(minrange: 2.0);
			//ShowReversals();
			ShowHistoricalReversals();
			
        }
		
		private void ShowHistoricalReversals() {
			for (int index = 0; index < CPAList.Count; index++) 
			{
				double ThisLevel = CPAList[index];
				if ( High[0] >= ThisLevel && Low[0] <= ThisLevel ) {
					ShowReversals();
				} 
			}
			
			for (int index = 0; index < KPAList.Count; index++) 
			{
				double ThisLevel = KPAList[index];
				if ( High[0] >= ThisLevel && Low[0] <= ThisLevel ) {
					ShowReversals();
				} 
			}
		}
		private void ShowReversals() {
			double up = Low[0] < min[1] && Close[0] > Close[1] ? 1: 0;
			if (up == 1) {
				Draw.Dot(this, "up"+CurrentBar, false, 0, Low[0] - 1 * TickSize, Brushes.DodgerBlue);
			}
			double down = High[0] > max[1] && Close[0] < Close[1] ? 1: 0;
			if (down == 1) {
				Draw.Dot(this, "down"+CurrentBar, false, 0, High[0] + 1 * TickSize, Brushes.Red);
			}
		}
		
		private void DrawLines() {
			for (int i = 0; i < CPAList.Count(); i++) 
			{
				DrawLine(level: CPAList[i], index: i, KPALine: false);
			}
			if ( KPAoff ) {
				for (int i = 0; i < KPAList.Count(); i++) 
				{
					DrawLine(level: KPAList[i], index: i, KPALine: true);
				}
			}
		}
	
		private void DrawLine(double level, int index, bool KPALine) {
			if (CurrentBar < Count -2) return;

            if ( ! KPALine)
            {
                RemoveDrawObject("CPALine" + index.ToString() + lastBar);
                Draw.ExtendedLine(this, "CPALine" + index.ToString() + CurrentBar, 10, level, 0, level, CPAColor, DashStyleHelper.Solid, CPALineWidth);

            }

            if (KPALine) {
                RemoveDrawObject("KPALine" +index.ToString() + lastBar);
				Draw.ExtendedLine(this, "KPALine"+index.ToString() + CurrentBar, 10, level, 0, level, KPColor, DashStyleHelper.Dash, KPALineWidth);
			}
		}

        private void AddLables()
        {
            int spacer = -5;
            for (int i = 0; i < 30; i++)
            {
                RemoveDrawObject("cpaLabel" + i.ToString());
                RemoveDrawObject("kpaLabel" + i.ToString());
            }
            for (int i = 0; i < CPAList.Count; i++)
            {
                double upSpace = TickSize * (CPALineWidth - 1);
                Draw.Text(this, "cpaLabel" + i.ToString(), "\tCPA " + CPAList[i].ToString("N2"), spacer, CPAList[i] + upSpace, CPAColor);
            }

            if (KPAoff)
            {
                double upSpace = TickSize * (KPALineWidth * 2);
                for (int i = 0; i < KPAList.Count; i++)
                {
                    Draw.Text(this, "kpaLabel" + i.ToString(), "\tKPA " + KPAList[i].ToString("N2"), spacer, KPAList[i] + upSpace, KPColor);
                }
            }
        }

        // draw rect in consol
        private void DrawConsol(double minrange) {
			if (CurrentBar < Count -2) return; 
			if ( !SimpleColsol ) { return;}
			// measure gap to line below
			for (int i = 0; i < CPAList.Count() -1; i++) 
			{
				double dist = CPAList[i + 1] - CPAList[i];
				if ( dist <= minrange ) {
					Draw.Text(this, "CPALineText"+ i.ToString(), dist.ToString("N2"), 0, CPAList[i] + (dist * 0.5), CPAColor);
					RemoveDrawObject("CPALConsol" +i.ToString() + lastBar);
					Draw.RegionHighlightY(this, "CPALConsol"+ i.ToString()+CurrentBar, false, CPAList[i + 1] ,CPAList[i], Brushes.Transparent, CPAColor, 40); 
				} 
			} 
			
            if(KPAoff)
            {
                for (int i = 0; i < KPAList.Count() - 1; i++)
                {
                    double dist = KPAList[i + 1] - KPAList[i];
                    if (dist <= minrange)
                    {
                        Draw.Text(this, "MyTextKPAList" + i.ToString(), dist.ToString("N2"), 0, KPAList[i] + (dist * 0.5), KPColor);
                        RemoveDrawObject("KPAConsol" + i.ToString() + lastBar);
                        Draw.RegionHighlightY(this, "KPAConsol" + i.ToString() + CurrentBar, false, KPAList[i + 1], KPAList[i], Brushes.Transparent, KPColor, 30);
                    }
                }
            }
			
		}
		
		//private void  DrawConsolAll(double minrange) {
		//	if (CurrentBar < Count -2) return;
		//	if ( !ComplexConsol ) { return;}
		//	// combine both lines
		//	List<double> newList = CPAList.Concat(KPAList).ToList();
		//	newList.Sort();
		//	for (int i = 0; i < newList.Count()-1; i++) 
		//	{
		//		double dist = newList[i + 1] - newList[i];
		//		if ( dist <= minrange ) {
		//			Draw.Text(this, "newListText"+ i.ToString(), dist.ToString("N2"), 0, newList[i] + (dist * 0.5), Brushes.DimGray);
		//			RemoveDrawObject("newListConsol" +i.ToString() + lastBar);
		//			Draw.RegionHighlightY(this, "newListConsol"+ i.ToString()+CurrentBar, false, newList[i + 1] ,newList[i], Brushes.Transparent, Brushes.DimGray, 40); 
		//		} 
		//	}
			
		//}
		
		private void UpdateChart() {
            GetData();
            CPAList = CenterShorterList(dataList: CPAList);
            KPAList = CenterShorterList(dataList: KPAList);
        }
		
		private void ScanForAlerts() {
			if (CurrentBar < Count -2) return;
			
			for (int index = 0; index < CPAList.Count; index++) 
			{
				double ThisLevel = CPAList[index];
				if ( High[0] >= ThisLevel && Low[0] <= ThisLevel ) {
					sendAlert(message: "CPA", sound: CPAALertSound);
					//ShowReversals();
				} 
			}
			
			if (KPAoff) {
				for (int index = 0; index < KPAList.Count; index++) 
				{
					double ThisLevel = KPAList[index];
					if ( High[0] >= ThisLevel && Low[0] <= ThisLevel ) {
						sendAlert(message: "KPA", sound: KPALertSound);
						//ShowReversals();
					} 
				}
			}
		}
		
		private void GetData() {
			name = Bars.Instrument.MasterInstrument.Name;
			CPAList.Clear();
			KPAList.Clear();

            // remove spaces from user input text from beginning and end
            Print("");
            Text = Text.Trim(); 
            Print("Original text: " +Text);
			Print("");

            // convert string into 2 price types KPA, CPA
            string[] priceTypes = { "CPA-", "KP-" };
            string[] textInputSeparated = Text.Split(priceTypes, System.StringSplitOptions.RemoveEmptyEntries);
			string KPstring = textInputSeparated.First();
			string CPASstring = textInputSeparated.Last();
			Print("KP as string:\n" + KPstring + "\n");
			Print("CPA as string:\n" + CPASstring + "\n");
			
            // convert KPA string to string[]
			char[] delimiterChars = { ' ', ',', ':', '\t' };
			string[] kpArr = KPstring.Split(delimiterChars);
            // remove empty strings
			kpArr = kpArr.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            // convert string[] to List<double>
            KPAList = kpArr.Select(x => double.Parse(x)).ToList();

            // convert CPA string to string[]
            string[] cpaArr = CPASstring.Split(delimiterChars);
			cpaArr = cpaArr.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            CPAList = cpaArr.Select(x => double.Parse(x)).ToList();

            Print("--------------------------------------------");
			CPAList.Sort();
			KPAList.Sort();
			Print("CPA sorted");
			Print(string.Join(", ", CPAList));
			Print("KPA sorted");
			Print(string.Join(", ", KPAList));
			Print("--------------------------------------------");
		}
		
		/// if list is larger than 10 find the center and split the list 
		private List<double> CenterShorterList(List<double> dataList) {
			if ( dataList.Count > 10 ) {
				
				Print("data before shorten ");
				Print(string.Join(", ", dataList));
				Print("List size is " + dataList.Count + " dividing the list");
				int index = dataList.BinarySearch(Close[0]);
				
				//int index = 16;
				if (index < 0) {	
					index = index * -1;
				}
				
				Print("nearest index is "+index + " for price " + Close[0] + " and line " + dataList[index] );
				
				if ( index <=5 ) {
					Print("index is less than 5 so taking the first 10");
					int last = dataList.Count - 10;
					Print("need to remove last " + last);
					dataList.RemoveRange(10, last);
					Print("The list size is now " + dataList.Count);
					Print(string.Join(", ", dataList));
				} else {
					Print("index is > 5 so taking the middle 10");
					int firstIndex = index - 5;
					int lastIndex = index + 5;
					Print("Array size is " + dataList.Count + " min " + firstIndex + " max" + lastIndex );
					if (firstIndex < 0 ) {
						Print("gone below the first index, shifting up");
						firstIndex = 0;
						lastIndex = 10;
					}
					
					if (lastIndex > dataList.Count ) {
						Print("gone above the last index, shifting down");
						firstIndex = dataList.Count - 10;
						lastIndex = dataList.Count;
					}
					
					IEnumerable<double> result = dataList.Skip(firstIndex).Take(10); // remove first n
					dataList = result.ToList();
					Print("data after shorten ");
					Print(string.Join(", ", result));
					Print(result.Count());
				}	
			}
			return dataList;
		}
			
		private void sendAlert(string message, string sound ) {
            if(CurrentBar < Count - 2) return;
//			if( !timeSpanelapsed ) { return;}
            message += " alert on " + Bars.Instrument.MasterInstrument.Name;
            if (AudioAlertsOn)
            {
                Alert("myAlert" + CurrentBar, Priority.High, message, NinjaTrader.Core.Globals.InstallDir + @"\sounds\" + sound, 10, Brushes.Black, Brushes.Yellow);
            }
			  
			if (SendSMS)
            {
                SendMailChart(name + " Alert", message, HostEmailAddress, SMSAddress, HostAddress, HostPort, HostUserName, HostPassword);
            }
			//LaunchTimer();
		}
		
		private void LaunchTimer() {
			var startTimeSpan = TimeSpan.Zero;
			var periodTimeSpan = TimeSpan.FromMinutes(5);
			timeSpanelapsed = false; 
			Print("Timer start, Alerts are now " + timeSpanelapsed);
			Draw.TextFixed(this, "Alerts", "Alerts are paused for 5 minutes", TextPosition.BottomLeft);
			
			var timer = new System.Threading.Timer((e) =>
			{
				Print("Time up, Alerts are now " + timeSpanelapsed);
			    timeSpanelapsed = true; 
				
			}, null, startTimeSpan, periodTimeSpan);
		}
		
		private void SendMailChart(string Subject, string Body, string From, string To, string Host, int Port, string Username, string Password)
		{
			try	
			{	
				Dispatcher.BeginInvoke(new Action(() =>
				{
						if (chart != null)
				        {
							
							RenderTargetBitmap	screenCapture = chart.GetScreenshot(ShareScreenshotType.Chart);
		                    outputFrame = BitmapFrame.Create(screenCapture);
							
		                    if (screenCapture != null)
		                    {
								PngBitmapEncoder png = new PngBitmapEncoder();
		                        png.Frames.Add(outputFrame);
								System.IO.MemoryStream stream = new System.IO.MemoryStream();
								png.Save(stream);
								stream.Position = 0;
							
								MailMessage theMail = new MailMessage(From, To, Subject, Body);
								System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(stream, "image.png");
								theMail.Attachments.Add(attachment);
							
								SmtpClient smtp = new SmtpClient(Host, Port);
								smtp.EnableSsl = true;
								smtp.Credentials = new System.Net.NetworkCredential(Username, Password);
								string token = Instrument.MasterInstrument.Name + ToDay(Time[0]) + " " + ToTime(Time[0]) + CurrentBar.ToString();
								
								Print("Sending Mail to port " + HostPort);
								smtp.SendAsync(theMail, token);
				            }
						}
				}));
			}
			catch (Exception ex) {
				Print("Sending " + name + "Chart email failed -  " + ex);
			}
		}
		
		#region Buttons
		///---------------------------------------------------------------------------------------------------------------------------------
		/// ///--------------------------------------------		Buttons		----------------------------------------------------------------
		/// ///-----------------------------------------------------------------------------------------------------------------------------
		/// 
		
		private void AddFirstButton()
		{
		  // Use this.Dispatcher to ensure code is executed on the proper thread
		  ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		  {
		      //Obtain the Chart on which the indicator is configured
		      chartWindow = Window.GetWindow(this.ChartControl.Parent) as Chart;
		      if (chartWindow == null)
		      {
		          Print("chartWindow == null");
		          return;
		      }
		      // Instantiate the Button
		      button1 = new System.Windows.Controls.Button();

		      //Set Button Style            
		      button1.Style = s;
			  button1.Content = " audio off ";
			  if ( AudioAlertsOn )
		      	button1.Content = " AUDIO ON ";
		      button1.IsEnabled = true;
		      button1.HorizontalAlignment = HorizontalAlignment.Left;
			  
			  // Assign the Click event 
			  button1.Click += button1_clicked;

		      // Add the Button to the Chart's Toolbar
		      chartWindow.MainMenu.Add(button1);

		      //Prevent the Button From Displaying when WorkSpace Opens if it is not in an active tab
		      button1.Visibility = Visibility.Collapsed;
		      foreach (TabItem tab in this.chartWindow.MainTabControl.Items)
		      {
		          if ((tab.Content as ChartTab).ChartControl == this.ChartControl

		               && tab == this.chartWindow.MainTabControl.SelectedItem)
		          {
		              button1.Visibility = Visibility.Visible;
		          }
		      }
		      IsToolBarButtonAdded = true;
		  }));
		}
		
		
		private void AddSecondButton()
		{
		  // Use this.Dispatcher to ensure code is executed on the proper thread
		  ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		  {
		      //Obtain the Chart on which the indicator is configured
		      chartWindow = Window.GetWindow(this.ChartControl.Parent) as Chart;
		      if (chartWindow == null)
		      {
		          Print("chartWindow == null");
		          return;
		      }

		      // Instantiate the Button
		      button2 = new System.Windows.Controls.Button();

		      //Set Button Style            
		      button2.Style = s;

		      button2.Content = " KP off ";
			  if ( KPAoff )
				  button2.Content = " KP ON ";
		      button2.IsEnabled = true;
		      button2.HorizontalAlignment = HorizontalAlignment.Left;
			  
			  // Assign the Click event 
			  button2.Click += button2_clicked;

		      // Add the Button to the Chart's Toolbar
		      chartWindow.MainMenu.Add(button2);

		      //Prevent the Button From Displaying when WorkSpace Opens if it is not in an active tab
		      button2.Visibility = Visibility.Collapsed;
		      foreach (TabItem tab in this.chartWindow.MainTabControl.Items)
		      {
		          if ((tab.Content as ChartTab).ChartControl == this.ChartControl

		               && tab == this.chartWindow.MainTabControl.SelectedItem)
		          {
		              button2.Visibility = Visibility.Visible;
		          }
		      }
		      IsToolBarButtonAdded = true;
		  }));
		}
		
		private void AddThirdButton()
		{
		  // Use this.Dispatcher to ensure code is executed on the proper thread
		  ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		  {
		      //Obtain the Chart on which the indicator is configured
		      chartWindow = Window.GetWindow(this.ChartControl.Parent) as Chart;
		      if (chartWindow == null)
		      {
		          Print("chartWindow == null");
		          return;
		      }

		      // Instantiate the Button
		      button3 = new System.Windows.Controls.Button();

		      //Set Button Style            
		      button3.Style = s;

		      button3.Content = " SMS off ";
			  if ( SendSMS) 
				  button3.Content = " SMS ON ";
		      button3.IsEnabled = true;
		      button3.HorizontalAlignment = HorizontalAlignment.Left;
			  
			  // Assign the Click event 
			  button3.Click += button3_clicked;

		      // Add the Button to the Chart's Toolbar
		      chartWindow.MainMenu.Add(button3);

		      //Prevent the Button From Displaying when WorkSpace Opens if it is not in an active tab
		      button3.Visibility = Visibility.Collapsed;
		      foreach (TabItem tab in this.chartWindow.MainTabControl.Items)
		      {
		          if ((tab.Content as ChartTab).ChartControl == this.ChartControl

		               && tab == this.chartWindow.MainTabControl.SelectedItem)
		          {
		              button3.Visibility = Visibility.Visible;
		          }
		      }
		      IsToolBarButtonAdded = true;
		  }));
		}
		
		protected void button1_clicked(object sender, RoutedEventArgs e)
		{
			if(AudioAlertsOn)
			{
				button1.Content = " audio off ";
			}
			else 
			{
				button1.Content = " AUDIO ON ";
			}
			AudioAlertsOn = !AudioAlertsOn;
			Print("Button 1 is " + AudioAlertsOn);
			ChartControl.InvalidateVisual();
		}		
		
		protected void button2_clicked(object sender, RoutedEventArgs e)
		{
			if(KPAoff)
			{ 
				button2.Content = " KP off "; 
			}
			else 
			{ 
				button2.Content = " KP ON ";
			}
			KPAoff = !KPAoff;
			Print("Button 2 is " + KPAoff);
			ChartControl.InvalidateVisual();
		}
		
		protected void button3_clicked(object sender, RoutedEventArgs e)
		{
			if(SendSMS)
			{ 
				button3.Content = " sms off "; 
			}
			else 
			{ 
				button3.Content = " SMS ON ";
			}
			SendSMS = !SendSMS;
			Print("Button 3 is " + SendSMS);
			ChartControl.InvalidateVisual();
		}
		
		private void DisposeCleanUp()
		{
		  //ChartWindow Null Check
		  if (chartWindow != null)
		  {
		      //Dispatcher used to Assure Executed on UI Thread
		      ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		      {
		          if (button1 != null)
		          {
					  button1.Click -= button1_clicked;
		              chartWindow.MainMenu.Remove(button1);
		          }
		      }));
			  
		      ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		      {
		          if (button2 != null)
		          {
					  button2.Click -= button2_clicked;
		              chartWindow.MainMenu.Remove(button2);
		          }
		      }));
			  
		      ChartControl.Dispatcher.InvokeAsync((Action)(() =>
		      {
		          if (button3 != null)
		          {
					  button3.Click -= button3_clicked;
		              chartWindow.MainMenu.Remove(button3);
		          }
		      }));
		  }
		}		
		#endregion
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Text from MAP", Order=1, GroupName="All Text")]
		public string Text
		{ get; set; }

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="KP Color", Order=1, GroupName="Lines")]
		public Brush KPColor
		{ get; set; }

		[Browsable(false)]
		public string KPColorSerializable
		{
			get { return Serialize.BrushToString(KPColor); }
			set { KPColor = Serialize.StringToBrush(value); }
		}			

		[NinjaScriptProperty]
		[XmlIgnore]
		[Display(Name="CPA Color", Order=2, GroupName="Lines")]
		public Brush CPAColor
		{ get; set; }

		[Browsable(false)]
		public string CPASerializable
		{
			get { return Serialize.BrushToString(CPAColor); }
			set { CPAColor = Serialize.StringToBrush(value); }
		}		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="CPA  Line  Width", Description="CPALineWidth", Order=3, GroupName="Lines")]
		public int CPALineWidth
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="KPA  Line  Width", Description="CPALineWidth", Order=4, GroupName="Lines")]
		public int KPALineWidth
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="KPA Off", Order=5, GroupName="Lines")]
		public bool KPAoff
		{ get; set; }
		

		/// <summary>
		/// ///////  Consolidation 
		/// </summary>
		[NinjaScriptProperty]
		[Display(Name="Show Simple Colsol", Order=1, GroupName="Consolidation")]
		public bool SimpleColsol
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Show Complex Consol", Order=2, GroupName="Consolidation")]
		public bool ComplexConsol
		{ get; set; }
		
		/// <summary>
		/// ///////  AUDIO ALERTS
		/// </summary>

		[NinjaScriptProperty]
		[Display(Name="Audio Alerts On", Order=1, GroupName="Alerts")]
		public bool AudioAlertsOn
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="KPA  Sound", Order=2, GroupName="Alerts")]
		public string KPALertSound
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="CPA  Sound", Order=3, GroupName="Alerts")]
		public string CPAALertSound
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Display(Name="Important Area Sound", Order=4, GroupName="Alerts")]
		public string ImportantAreaALertSound
		{ get; set; }
		
				
		/// <summary>
		/// ///////  EMAIL ALERTS
		/// </summary>
		
		[NinjaScriptProperty]
		[Display(Name="Mail Alerts On", Order=1, GroupName="Email Host")]
		public bool SendSMS
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Alert Address", Order=2, GroupName="Email Host")]
		public string SMSAddress
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Host Email Address", Order=3, GroupName="Email Host")]
		public string HostEmailAddress
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Server Address", Order=4, GroupName="Email Host")]
		public string HostAddress
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Host Port", Description="Host Port", Order=5, GroupName="Email Host")]
		public int HostPort
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Host UserName", Order=6, GroupName="Email Host")]
		public string HostUserName
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Host Password", Order=7, GroupName="Email Host")]
		public string HostPassword
		{ get; set; }
		
		
		/// <summary>
		/// ///////  WATCH AREAS
		/// </summary>

		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Watch Area 1", Description="WatchArea1", Order=1, GroupName="Watch Areas")]
		public double WatchArea1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Watch Area 2", Description="WatchArea2", Order=2, GroupName="Watch Areas")]
		public double WatchArea2
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="Watch Area 3", Description="WatchArea3", Order=3, GroupName="Watch Areas")]
		public double WatchArea3
		{ get; set; }
		
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "Reversals", Order = 0)]
		public int Period
		{ get; set; }
			
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DOPBttnLines[] cacheDOPBttnLines;
		public DOPBttnLines DOPBttnLines(string text, Brush kPColor, Brush cPAColor, int cPALineWidth, int kPALineWidth, bool kPAoff, bool simpleColsol, bool complexConsol, bool audioAlertsOn, string kPALertSound, string cPAALertSound, string importantAreaALertSound, bool sendSMS, string sMSAddress, string hostEmailAddress, string hostAddress, int hostPort, string hostUserName, string hostPassword, double watchArea1, double watchArea2, double watchArea3, int period)
		{
			return DOPBttnLines(Input, text, kPColor, cPAColor, cPALineWidth, kPALineWidth, kPAoff, simpleColsol, complexConsol, audioAlertsOn, kPALertSound, cPAALertSound, importantAreaALertSound, sendSMS, sMSAddress, hostEmailAddress, hostAddress, hostPort, hostUserName, hostPassword, watchArea1, watchArea2, watchArea3, period);
		}

		public DOPBttnLines DOPBttnLines(ISeries<double> input, string text, Brush kPColor, Brush cPAColor, int cPALineWidth, int kPALineWidth, bool kPAoff, bool simpleColsol, bool complexConsol, bool audioAlertsOn, string kPALertSound, string cPAALertSound, string importantAreaALertSound, bool sendSMS, string sMSAddress, string hostEmailAddress, string hostAddress, int hostPort, string hostUserName, string hostPassword, double watchArea1, double watchArea2, double watchArea3, int period)
		{
			if (cacheDOPBttnLines != null)
				for (int idx = 0; idx < cacheDOPBttnLines.Length; idx++)
					if (cacheDOPBttnLines[idx] != null && cacheDOPBttnLines[idx].Text == text && cacheDOPBttnLines[idx].KPColor == kPColor && cacheDOPBttnLines[idx].CPAColor == cPAColor && cacheDOPBttnLines[idx].CPALineWidth == cPALineWidth && cacheDOPBttnLines[idx].KPALineWidth == kPALineWidth && cacheDOPBttnLines[idx].KPAoff == kPAoff && cacheDOPBttnLines[idx].SimpleColsol == simpleColsol && cacheDOPBttnLines[idx].ComplexConsol == complexConsol && cacheDOPBttnLines[idx].AudioAlertsOn == audioAlertsOn && cacheDOPBttnLines[idx].KPALertSound == kPALertSound && cacheDOPBttnLines[idx].CPAALertSound == cPAALertSound && cacheDOPBttnLines[idx].ImportantAreaALertSound == importantAreaALertSound && cacheDOPBttnLines[idx].SendSMS == sendSMS && cacheDOPBttnLines[idx].SMSAddress == sMSAddress && cacheDOPBttnLines[idx].HostEmailAddress == hostEmailAddress && cacheDOPBttnLines[idx].HostAddress == hostAddress && cacheDOPBttnLines[idx].HostPort == hostPort && cacheDOPBttnLines[idx].HostUserName == hostUserName && cacheDOPBttnLines[idx].HostPassword == hostPassword && cacheDOPBttnLines[idx].WatchArea1 == watchArea1 && cacheDOPBttnLines[idx].WatchArea2 == watchArea2 && cacheDOPBttnLines[idx].WatchArea3 == watchArea3 && cacheDOPBttnLines[idx].Period == period && cacheDOPBttnLines[idx].EqualsInput(input))
						return cacheDOPBttnLines[idx];
			return CacheIndicator<DOPBttnLines>(new DOPBttnLines(){ Text = text, KPColor = kPColor, CPAColor = cPAColor, CPALineWidth = cPALineWidth, KPALineWidth = kPALineWidth, KPAoff = kPAoff, SimpleColsol = simpleColsol, ComplexConsol = complexConsol, AudioAlertsOn = audioAlertsOn, KPALertSound = kPALertSound, CPAALertSound = cPAALertSound, ImportantAreaALertSound = importantAreaALertSound, SendSMS = sendSMS, SMSAddress = sMSAddress, HostEmailAddress = hostEmailAddress, HostAddress = hostAddress, HostPort = hostPort, HostUserName = hostUserName, HostPassword = hostPassword, WatchArea1 = watchArea1, WatchArea2 = watchArea2, WatchArea3 = watchArea3, Period = period }, input, ref cacheDOPBttnLines);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DOPBttnLines DOPBttnLines(string text, Brush kPColor, Brush cPAColor, int cPALineWidth, int kPALineWidth, bool kPAoff, bool simpleColsol, bool complexConsol, bool audioAlertsOn, string kPALertSound, string cPAALertSound, string importantAreaALertSound, bool sendSMS, string sMSAddress, string hostEmailAddress, string hostAddress, int hostPort, string hostUserName, string hostPassword, double watchArea1, double watchArea2, double watchArea3, int period)
		{
			return indicator.DOPBttnLines(Input, text, kPColor, cPAColor, cPALineWidth, kPALineWidth, kPAoff, simpleColsol, complexConsol, audioAlertsOn, kPALertSound, cPAALertSound, importantAreaALertSound, sendSMS, sMSAddress, hostEmailAddress, hostAddress, hostPort, hostUserName, hostPassword, watchArea1, watchArea2, watchArea3, period);
		}

		public Indicators.DOPBttnLines DOPBttnLines(ISeries<double> input , string text, Brush kPColor, Brush cPAColor, int cPALineWidth, int kPALineWidth, bool kPAoff, bool simpleColsol, bool complexConsol, bool audioAlertsOn, string kPALertSound, string cPAALertSound, string importantAreaALertSound, bool sendSMS, string sMSAddress, string hostEmailAddress, string hostAddress, int hostPort, string hostUserName, string hostPassword, double watchArea1, double watchArea2, double watchArea3, int period)
		{
			return indicator.DOPBttnLines(input, text, kPColor, cPAColor, cPALineWidth, kPALineWidth, kPAoff, simpleColsol, complexConsol, audioAlertsOn, kPALertSound, cPAALertSound, importantAreaALertSound, sendSMS, sMSAddress, hostEmailAddress, hostAddress, hostPort, hostUserName, hostPassword, watchArea1, watchArea2, watchArea3, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DOPBttnLines DOPBttnLines(string text, Brush kPColor, Brush cPAColor, int cPALineWidth, int kPALineWidth, bool kPAoff, bool simpleColsol, bool complexConsol, bool audioAlertsOn, string kPALertSound, string cPAALertSound, string importantAreaALertSound, bool sendSMS, string sMSAddress, string hostEmailAddress, string hostAddress, int hostPort, string hostUserName, string hostPassword, double watchArea1, double watchArea2, double watchArea3, int period)
		{
			return indicator.DOPBttnLines(Input, text, kPColor, cPAColor, cPALineWidth, kPALineWidth, kPAoff, simpleColsol, complexConsol, audioAlertsOn, kPALertSound, cPAALertSound, importantAreaALertSound, sendSMS, sMSAddress, hostEmailAddress, hostAddress, hostPort, hostUserName, hostPassword, watchArea1, watchArea2, watchArea3, period);
		}

		public Indicators.DOPBttnLines DOPBttnLines(ISeries<double> input , string text, Brush kPColor, Brush cPAColor, int cPALineWidth, int kPALineWidth, bool kPAoff, bool simpleColsol, bool complexConsol, bool audioAlertsOn, string kPALertSound, string cPAALertSound, string importantAreaALertSound, bool sendSMS, string sMSAddress, string hostEmailAddress, string hostAddress, int hostPort, string hostUserName, string hostPassword, double watchArea1, double watchArea2, double watchArea3, int period)
		{
			return indicator.DOPBttnLines(input, text, kPColor, cPAColor, cPALineWidth, kPALineWidth, kPAoff, simpleColsol, complexConsol, audioAlertsOn, kPALertSound, cPAALertSound, importantAreaALertSound, sendSMS, sMSAddress, hostEmailAddress, hostAddress, hostPort, hostUserName, hostPassword, watchArea1, watchArea2, watchArea3, period);
		}
	}
}

#endregion
