# Template bot cTrader

A robust and extensible trading bot template for cTrader, written in C#.  
Created by Rayman223.

---

## Overview

This project provides a solid foundation for developing your own automated trading strategies on the cTrader platform.  
It includes advanced position management, risk controls, logging, and is designed for easy integration of your custom indicators and entry/exit logic.

This project is still in development, but you are welcome to use it as a starting point for your own trading bots.  
Contributions and modifications are highly encouraged—feel free to adapt, extend, or improve the bot to suit your specific needs.  
If you make enhancements or fix issues, consider sharing your changes with the community to help others benefit as well!
---

## Features

- **Flexible Money Management**
  - Fixed or dynamic lot sizing
  - Risk per trade configuration
  - Minimum lot size enforcement

- **Position Management**
  - Maximum open positions limit
  - Automatic closing of profitable positions
  - Close all positions before rollover or weekends

- **Risk Controls**
  - Stop Loss, Take Profit, Trailing Stop, and Break-even logic
  - Maximum allowed spread filter
  - Global loss limit (bot stops trading after a defined loss)

- **Timeframe Filters**
  - Enable/disable trading based on multiple timeframes (e.g., 15M, 30M, 1H)
  - Easily extendable to add your own timeframe or indicator logic

- **Logging**
  - Informative logs for all major actions and warnings
  - Duplicate log suppression for cleaner output

- **Extensibility**
  - Clearly separated methods for entry/exit conditions
  - Easy to plug in your own indicators and strategies

---

## Getting Started

### Prerequisites

- [cTrader Automate](https://help.ctrader.com/ctrader-automate/) (formerly cAlgo)
- .NET Framework (compatible with cTrader)
- Visual Studio or Visual Studio Code

### Installation

1. **Clone the repository:**
   ```sh
   git clone https://github.com/rayman223/template-bot-ctrader.git
   ```

2. **Open the project:**
   - Open the `.cs` file in Visual Studio or VS Code.

3. **Build the project:**
   - Use the build tools in your IDE or the cTrader Automate editor.

4. **Load the bot in cTrader:**
   - Compile and add the resulting `.algo` file to your cTrader platform.

---

## Configuration

All parameters can be set from the cTrader UI or directly in the code via attributes.  
Key parameters include:

- **Money Management**
  - `MinLotSize`: Minimum lot size allowed
  - `LotSize`: Fixed lot size (if dynamic lot is disabled)
  - `UseDynamicLot`: Enable/disable dynamic lot calculation
  - `RiskPercent`: Percentage of balance to risk per trade

- **Trade Management**
  - `MaxOpenPosition`: Maximum number of simultaneous open positions
  - `StopLossPips`, `TakeProfitPips`, `TrailingStopPips`: SL/TP/TS in pips
  - `BreakEvenTriggerPips`: Distance in pips to trigger break-even

- **Risk & Filters**
  - `MaxLoss`: Maximum allowed loss before bot stops trading
  - `MaxAllowedSpread`: Maximum spread (in pips) allowed to open a trade
  - `RolloverHour`: Hour (UTC) to avoid swap fees
  - `CloseBeforeWeekendHours`: Hours before weekend to close positions

- **Timeframe Settings**
  - `EnableTimeFrame15M`, `EnableTimeFrame30M`, `EnableTimeFrame1H`: Enable/disable trading on specific timeframes

---

## How to Customize

### 1. Add Your Indicators

In the `OnStart()` method, instantiate your indicators as needed.  
Example:
```csharp
protected override void OnStart()
{
    // Example: myIndicator = Indicators.MyCustomIndicator(...);
}
```

### 2. Define Entry/Exit Logic

Edit the `CheckIndicatorConditionsForTradeType` method to implement your strategy:
```csharp
private bool CheckIndicatorConditionsForTradeType(TradeType tradeType, double price, double pricePrev)
{
    // Example: return myIndicator.Result.Last(1) > myIndicator.Result.Last(2);
}
```

### 3. Adjust Risk and Money Management

Modify the parameters at the top of the file or via the cTrader UI to suit your risk profile.

---

## Example Parameter Block

```csharp
[Parameter("Min Lot", Group = "Money Management", DefaultValue = 1, MinValue = 0.01)]
public double MinLotSize { get; set; }

[Parameter("Risk Per Trade %", Group = "Money Management", DefaultValue = 1.8, MinValue = 0.1, MaxValue = 2)]
public double RiskPercent { get; set; }

[Parameter("Max open positions", Group = "SL/TP", DefaultValue = 4, MaxValue = 10, MinValue = 1, Step = 1)]
public int MaxOpenPosition { get; set; }
```

---

## Logging

All major actions and warnings are logged using the `Log` method.  
Duplicate messages are suppressed for clarity.  
Logs can be viewed in the cTrader Automate log output.

---

## Limitations & Notes

- The template does not include any trading strategy by default.  
  You must implement your own indicator logic in the provided methods.
- Some features (like closing before rollover) may not work as expected in backtesting mode due to platform limitations (still in dev).
- Always test your strategy on a demo account before running it live.

---

## Disclaimer

This bot is provided as a template for educational and development purposes.  
Trading involves risk. The author is not responsible for any financial losses incurred.  
Use at your own risk.

---

## License

MIT License

---

## Author

Rayman223

---