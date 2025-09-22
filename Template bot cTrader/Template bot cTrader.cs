//+------------------------------------------------------------------+
//| Template bot cTrader - Version 0.1                               |
//| By Rayman223                                                     |
//+------------------------------------------------------------------+

using System;
using System.Linq;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RaymanBot : Robot
    {
        // === STRATEGY PARAMETERS ===
        [Parameter("Min Lot", Group = "Money Management", DefaultValue = 1, MinValue = 0.01)]
        public double MinLotSize { get; set; }

        [Parameter("Fixed Lot", Group = "Money Management", DefaultValue = 1, MinValue = 0.01)]
        public double LotSize { get; set; }

        [Parameter("Use Dynamic Lot?", Group = "Money Management", DefaultValue = true)]
        public bool UseDynamicLot { get; set; }

        [Parameter("Risk Per Trade %", Group = "Money Management", DefaultValue = 1.8, MinValue = 0.1, MaxValue = 2)]
        public double RiskPercent { get; set; }

        [Parameter("Max open positions", Group = "SL/TP", DefaultValue = 4, MaxValue = 10, MinValue = 1, Step = 1)]
        public int MaxOpenPosition { get; set; }

        [Parameter("Stop Loss (pips)", Group = "SL/TP", DefaultValue = 9, MaxValue = 100, MinValue = 1, Step = 1)]
        public int StopLossPips { get; set; }

        [Parameter("Take Profit (pips)", Group = "SL/TP", DefaultValue = 26, MaxValue = 100, MinValue = 1, Step = 1)]
        public int TakeProfitPips { get; set; }

        // Nomber of pips for new SL after Break-even
        [Parameter("Trailing Stop (pips)", Group = "SL/TP", DefaultValue = 12, MaxValue = 100, MinValue = 1, Step = 1)]
        public int TrailingStopPips { get; set; }

        // Number of pips when new SL on price
        [Parameter("Break-even Trigger (pips)", Group = "SL/TP", DefaultValue = 7, MaxValue = 20, MinValue = 1, Step = 1)]
        public int BreakEvenTriggerPips { get; set; }
        // Margin add to the price for the new SL
        [Parameter("Break-even Margin (pips)", Group = "SL/TP", DefaultValue = 1, MinValue = 0, MaxValue = 100, Step = 0.1)]
        public int BreakEvenMarginPips { get; set; }

        [Parameter("Total loss", Group = "Risk", DefaultValue = 200, MaxValue = 1000, MinValue = 0, Step = 1)]
        public int MaxLoss { get; set; }

        [Parameter("Max Allowed Spread (pips)", Group = "Settings", DefaultValue = 0.4, MaxValue = 0.7, MinValue = 0, Step = 0.1)]
        public double MaxAllowedSpread { get; set; }

        [Parameter("Rollover Hour (UTC)", Group = "Settings", DefaultValue = 20, MinValue = 0, MaxValue = 23)]
        public int RolloverHour { get; set; }

        [Parameter("Close Before Weekend (hours)", Group = "Filters", DefaultValue = 1, MinValue = 0, MaxValue = 2, Step = 1)]
        public int CloseBeforeWeekendHours { get; set; }

        [Parameter("Enable TimeFrame 15M", Group = "Backtest Settings", DefaultValue = true)]
        public bool EnableTimeFrame15M { get; set; }

        [Parameter("Enable TimeFrame 30M", Group = "Backtest Settings", DefaultValue = true)]
        public bool EnableTimeFrame30M { get; set; }

        [Parameter("Enable TimeFrame 1H", Group = "Backtest Settings", DefaultValue = true)]
        public bool EnableTimeFrame1H { get; set; }

        private string lastLogMessage = string.Empty;
        private double GlobalBalanceValue => GlobalBalance();
        private string lastLogSource = string.Empty;
        private double GlobalBalance()
        {
            return Math.Floor(History
                .Where(h => h.Label == "RaymanBot")
                .Sum(h => h.NetProfit) * 100) / 100;
        }

        protected override void OnStart()
        {
            // Display market opening and closing hours
            DisplayMarketHours();

            // Get Indicator(s) for the selected timeframe
            
            // Get Indicator(s) for xxM timeframe
            
            // Get Indicator(s) for xxH timeframe
            

            // Validate parameters
            ValidateParameters();

            Log("Bot started successfully", "Info");
        }

        protected override void OnBarClosed()
        {
            double price = Bars.ClosePrices.LastValue;
            double pricePrev = Bars.ClosePrices.Last(1);
            double GlobalBalanceValue = GlobalBalance();

            if (GlobalBalanceValue <= -MaxLoss)
            {
                Log($"Maximum loss reached. No more positions will be opened ({GlobalBalanceValue} €).", "Warning");
                // If no more positions are open, stop the robot
                if (Positions.Count(p => p.Label == "RaymanBot") == 0)
                {
                    Log("Robot stopped due to maximum loss.", "Warning");
                    Stop();
                }
                return;
            }

            // Check if it is a buy position
            if (IsBuyCondition(price, pricePrev))
            {
                ManagePositions(TradeType.Buy, price);
            }

            // Check if it is a sell position
            if (IsSellCondition(price, pricePrev))
            {
                ManagePositions(TradeType.Sell, price);
            }
        }

        protected override void OnTick()
        {
            // Close positions before rollover
            ClosePositionsBeforeRollover();

            // Manage trailing stop and break-even adjustments
            ManageBreakEven();
            ManageTrailingStop();
        }

        private bool IsBuyCondition(double price, double pricePrev)
        {
            // If open position => do nothing
            if (!CanOpenNewPosition(TradeType.Buy))
                return false;

            return CheckIndicatorConditionsForTradeType(TradeType.Buy, price, pricePrev);
        }

        private bool IsSellCondition(double price, double pricePrev)
        {
            // If open position => do nothing
            if (!CanOpenNewPosition(TradeType.Sell))
                return false;

            return CheckIndicatorConditionsForTradeType(TradeType.Sell, price, pricePrev);
        }

        private bool CanOpenNewPosition(TradeType tradeType)
        {
            // Check number of open positions
            // If the number of open positions exceeds the maximum allowed, do not open a new position
            int openPositionsCount = Positions.Count(p => p.SymbolName == SymbolName);
            if (openPositionsCount >= MaxOpenPosition)
            {
                Log("Too many positions opened", "Warning");
                return false;
            }

            // Check if a position was closed on the current candle
            var lastClosedPosition = History
                .Where(h => h.Label == "RaymanBot" && h.SymbolName == SymbolName)
                .OrderByDescending(h => h.ClosingTime)
                .FirstOrDefault();

            if (lastClosedPosition != null && lastClosedPosition.ClosingTime >= Bars.OpenTimes.LastValue)
            {
                Log($"One position was closed on the current candle : {lastClosedPosition.TradeType} | {lastClosedPosition.SymbolName} | {lastClosedPosition.NetProfit:F2} €", "Warning");
                return false;
            }

            // Check if the spread is acceptable
            if (!IsSpreadAcceptable())
                return false;

            // Check if the current time is close to the rollover time
            if (StopOpenPositionsBeforeRollover())
                return false;

            // If there is an existing position in the same direction, check if it is in deficit
            foreach (var position in Positions.FindAll("RaymanBot", SymbolName))
            {
                if (position.TradeType == tradeType)
                {
                    double deficitInPips = (position.TradeType == TradeType.Buy)
                        ? (position.EntryPrice - Symbol.Bid) / Symbol.PipSize
                        : (Symbol.Ask - position.EntryPrice) / Symbol.PipSize;

                    if (deficitInPips >= 5)
                    {
                        Log($"Cannot open a new position: an existing {tradeType} position is in deficit by {deficitInPips:F2} pips.", "Warning");
                        return false;
                    }
                }
            }

            return true;
        }

        private void ClosePositions(TradeType tradeType, bool closeAll = false)
        {
            foreach (var position in Positions)
            {
                if (closeAll || position.TradeType == tradeType)
                {
                    ClosePosition(position);
                    Log($"Closed position : {position.TradeType} | {position.SymbolName} | {position.NetProfit:F2} €", "Info");
                }
            }
        }

        private void OpenPosition(TradeType tradeType, double price)
        {
            var volume = UseDynamicLot ? GetDynamicVolume() : Symbol.QuantityToVolumeInUnits(LotSize);

            double sl = tradeType == TradeType.Buy
                ? price - StopLossPips * Symbol.PipSize
                : price + StopLossPips * Symbol.PipSize;

            double tp = tradeType == TradeType.Buy
                ? price + TakeProfitPips * Symbol.PipSize
                : price - TakeProfitPips * Symbol.PipSize;

            // Conversion des niveaux SL et TP en pips
            int slInPips = (int)Math.Round(Math.Abs(price - sl) / Symbol.PipSize);
            int tpInPips = (int)Math.Round(Math.Abs(price - tp) / Symbol.PipSize);

            try
            {
                ExecuteMarketOrder(tradeType, SymbolName, volume, "RaymanBot", slInPips, tpInPips);
            }
            catch (Exception ex)
            {
                Log($"Error during order execution : {ex.Message}", "Error");
            }
        }

        private long GetDynamicVolume()
        {
            double riskAmount = Account.Balance * (RiskPercent / 100);
            double pipValue = Symbol.PipValue;

            if (StopLossPips <= 0 || pipValue <= 0 || MinLotSize <= 0)
            {
                throw new ArgumentException("StopLossPips, PipValue or MinLotSize is invalid.");
            }

            double volumeInLots = riskAmount / (StopLossPips * pipValue);
            volumeInLots = Math.Round(volumeInLots, 2);

            // Check if volume is less than minimum lot size
            if (volumeInLots < MinLotSize)
            {
                Log($"Calculated volume ({volumeInLots}) is less than the minimal size lot ({MinLotSize}). Utilisation of the minimal size lot.", "Warning");
                volumeInLots = MinLotSize;
            }

            return (long)Symbol.QuantityToVolumeInUnits((long)volumeInLots);
        }

        private void ManageBreakEven()
        {
            foreach (var position in Positions.FindAll("RaymanBot", SymbolName))
            {
                double distance = position.TradeType == TradeType.Buy
                    ? Symbol.Bid - position.EntryPrice
                    : position.EntryPrice - Symbol.Ask;

                if (distance >= BreakEvenTriggerPips * Symbol.PipSize)
                {
                    double newStopLoss = position.TradeType == TradeType.Buy
                        ? position.EntryPrice + (BreakEvenMarginPips * Symbol.PipSize)
                        : position.EntryPrice - (BreakEvenMarginPips * Symbol.PipSize) - GetSpreadInPips();

                    if ((position.TradeType == TradeType.Buy && newStopLoss > position.StopLoss) ||
                        (position.TradeType == TradeType.Sell && newStopLoss < position.StopLoss))
                    {
                        position.ModifyStopLossPrice(newStopLoss);
                        Log($"Break-even adjusted | distance={distance} | New SL={newStopLoss} | Entry={position.EntryPrice}", "Info");
                    }
                }
            }
        }

        private void ManageTrailingStop()
        {
            foreach (var position in Positions.FindAll("RaymanBot", SymbolName))
            {
                double currentPrice = position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask;
                //double breakEvenPrice = position.EntryPrice;
                double breakEvenPrice = position.TradeType == TradeType.Buy
                    ? position.EntryPrice + BreakEvenMarginPips * Symbol.PipSize
                    : position.EntryPrice - (BreakEvenMarginPips * Symbol.PipSize) - Symbol.Spread;

                // Vérifie si le prix a dépassé le niveau de Break-even
                if ((position.TradeType == TradeType.Buy && currentPrice > breakEvenPrice) ||
                    (position.TradeType == TradeType.Sell && currentPrice < breakEvenPrice))
                {
                    // Calcule le nouveau Stop Loss pour rester TrailingStopPips pips en dessous du prix actuel
                    double newStopLoss = position.TradeType == TradeType.Buy
                        ? currentPrice - TrailingStopPips * Symbol.PipSize
                        : currentPrice + TrailingStopPips * Symbol.PipSize;

                    double CurrentPips = (newStopLoss - position.EntryPrice) / Symbol.PipSize;
                    // Applique le nouveau Stop Loss uniquement s'il est plus favorable
                    if ((position.TradeType == TradeType.Buy && newStopLoss > position.StopLoss && newStopLoss > breakEvenPrice) ||
                        (position.TradeType == TradeType.Sell && newStopLoss < position.StopLoss && newStopLoss < breakEvenPrice))
                    {
                        position.ModifyStopLossPrice(newStopLoss);
                        Log($"Trailing Stop ajusted | New SL={newStopLoss} | Actual price={currentPrice} | Entry={position.EntryPrice} | Pips={CurrentPips}", "Info");
                    }
                }
            }
        }

        private bool StopOpenPositionsBeforeRollover()
        {
            TimeSpan currentTime = Server.Time.TimeOfDay;
            TimeSpan rolloverTime = new TimeSpan(RolloverHour, 0, 0); // Manual user hour (UTC)
            TimeSpan timeTillClose = Symbol.MarketHours.TimeTillClose(); // Automatic hour

            // Check if within 30 minutes of manual or automatic rollover time
            bool isNearManualRollover = currentTime >= rolloverTime - TimeSpan.FromMinutes(30) && currentTime < rolloverTime;
            bool isNearAutomaticRollover = timeTillClose <= TimeSpan.FromMinutes(30) && timeTillClose > TimeSpan.Zero;

            if (isNearManualRollover || isNearAutomaticRollover)
            {
                return true; // Prevent opening new positions
            }

            return false; // Allow opening new positions
        }

        private void ClosePositionsBeforeRollover()
        {
            /*
                Does not work properly in backtest mode
            */

            // Combine manual and automatic rollover checks
            TimeSpan rolloverTime = new TimeSpan(RolloverHour, 0, 0); // Manual user hour (UTC)
            TimeSpan currentTime = Server.Time.TimeOfDay;
            TimeSpan timeTillClose = Symbol.MarketHours.TimeTillClose(); // Automatic hour

            // Check if within 5 minutes of manual or automatic rollover time
            if ((currentTime >= rolloverTime - TimeSpan.FromMinutes(5) && currentTime <= rolloverTime) ||
            (timeTillClose <= TimeSpan.FromMinutes(5) && timeTillClose > TimeSpan.Zero))
            {
                Log($"Closing positions to avoid swap fees. Current time: {currentTime}, Rollover time: {rolloverTime}, Time till close: {timeTillClose}.", "Warning");

                // Close all positions opened by the bot
                foreach (var position in Positions.FindAll("RaymanBot", SymbolName))
                {
                    ClosePosition(position);
                }
            }
        }

        private void ManagePositions(TradeType tradeType, double price)
        {
            ClosePositions(tradeType == TradeType.Buy ? TradeType.Sell : TradeType.Buy);
            OpenPosition(tradeType, price);
        }

        private void ValidateParameters()
        {
            if (StopLossPips <= 0 || TakeProfitPips <= 0)
                throw new ArgumentException("Stop Loss and Take Profit must be greater than 0.");

            if (MaxOpenPosition <= 0)
                throw new ArgumentException("The maximum number of open positions must be greater than 0.");

            if (RiskPercent <= 0 || RiskPercent > 100)
                throw new ArgumentException("Risk Percent must be between 0 and 100.");
        }

        private void Log(string message, string level = "Info")
        {
            string logSource = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name;
            string formattedMessage = $"[{level}]: {message}";

            // Check if the message is similar to the last log (ignoring dynamic variables)
            if (lastLogMessage != null && AreMessagesSimilar(formattedMessage, lastLogMessage))
            {
                return;
            }

            // Save log and update last log
            lastLogMessage = formattedMessage;
            lastLogSource = logSource;
            Print(formattedMessage);
        }

        private bool AreMessagesSimilar(string currentMessage, string lastMessage)
        {
            // Ignore les parties dynamiques des messages (comme les valeurs numériques)
            string strippedCurrent = System.Text.RegularExpressions.Regex.Replace(currentMessage, @"\d+(\.\d+)?", "");
            string strippedLast = System.Text.RegularExpressions.Regex.Replace(lastMessage, @"\d+(\.\d+)?", "");

            return strippedCurrent == strippedLast;
        }

        private void DisplayMarketHours()
        {
            var TimeTillClose = Symbol.MarketHours.TimeTillClose();
            Log($"TimeTillClose() : {TimeTillClose}", "Info");
            Log($"Current server time : {Server.Time}", "Info");
            Log($"Time zone : {TimeZoneInfo.Local.StandardName}", "Info");
            Log($"Symbol: {SymbolName}", "Info");
        }

        private bool IsSpreadAcceptable()
        {
            double spreadInPips = GetSpreadInPips();

            if (spreadInPips > MaxAllowedSpread)
            {
                Log($"Spread too high : {spreadInPips:F2} pips (Max allow : {MaxAllowedSpread:F2} pips)", "Warning");
                return false;
            }

            return true;
        }

        private void CloseProfitablePositions()
        {
            foreach (var position in Positions.FindAll("RaymanBot", SymbolName))
            {
                if (position.NetProfit > 0)
                {
                    ClosePosition(position);
                    Log($"position closed in profit : {position.TradeType} | {position.SymbolName} | {position.NetProfit:F2} €", "Info");
                }
            }
        }

        private bool CheckIndicatorConditionsForTradeType(TradeType tradeType, double price, double pricePrev)
        {
            bool isBuy = tradeType == TradeType.Buy;

            // Check selected timeframe
            //bool TimeFrame = ToMethodForYourIndicator

            // Check other timeframes
            //bool TimeFrameXXM = ToMethodForYourIndicator
            //bool TimeFrameXXH = ToMethodForYourIndicator

            return TimeFrame &&
                   (!EnableTimeFrameXXM || TimeFrameXXM) &&
                   (!EnableTimeFrameXXH || TimeFrameXXH);
        }

        private double GetSpreadInPips()
        {
            // Symbol.Spread ?
            // make search because used with trailing stop
            return (Symbol.Ask - Symbol.Bid) / Symbol.PipSize;
        }
    }
}
