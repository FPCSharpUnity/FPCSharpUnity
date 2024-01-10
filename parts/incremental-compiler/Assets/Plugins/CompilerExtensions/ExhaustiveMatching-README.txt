Built from:
https://github.com/WalkerCodeRanger/ExhaustiveMatching
0050b02c463c1c1a165285162a42f2c4a55ea1e9

Our changes:
	ExhaustiveMatching.Analyzer/ExpressionAnalyzer.cs
		From:
           		var isExhaustiveMatchFailedException = exceptionType.Equals(exhaustiveMatchFailedExceptionType, SymbolEqualityComparer.IncludeNullability);
          		var isInvalidEnumArgumentException = exceptionType.Equals(invalidEnumArgumentExceptionType, SymbolEqualityComparer.IncludeNullability);
		To:
            		var isExhaustiveMatchFailedException = exceptionType.Equals(exhaustiveMatchFailedExceptionType, SymbolEqualityComparer.Default);
			var isInvalidEnumArgumentException = exceptionType.Equals(invalidEnumArgumentExceptionType, SymbolEqualityComparer.Default);