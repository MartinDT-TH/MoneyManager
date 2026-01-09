using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using MoneyManager.Application.Interfaces;

namespace MoneyManager.Infrastructure.Services;

// Input Data Class
public class TransactionData
{
    [LoadColumn(0)] public string VendorName { get; set; } = string.Empty;
    [LoadColumn(1)] public string Category { get; set; } = string.Empty;
}

// Prediction Output Class
public class TransactionPrediction
{
    [ColumnName("PredictedLabel")] public string Category { get; set; } = string.Empty;
}

public class SmartCategorizationService : ICategorizationService
{
    private readonly PredictionEngine<TransactionData, TransactionPrediction> _predictionEngine;

    public SmartCategorizationService()
    {
        var mlContext = new MLContext();

        // 1. Load Dummy Training Data
        var trainingData = new List<TransactionData>
        {
            new() { VendorName = "Highlands Coffee", Category = "Food & Beverage" },
            new() { VendorName = "Starbucks", Category = "Food & Beverage" },
            new() { VendorName = "McDonalds", Category = "Food & Beverage" },
            new() { VendorName = "Pho 24", Category = "Food & Beverage" },
            new() { VendorName = "Grab", Category = "Transportation" },
            new() { VendorName = "Uber", Category = "Transportation" },
            new() { VendorName = "Petrolimex", Category = "Transportation" },
            new() { VendorName = "Netflix", Category = "Entertainment" },
            new() { VendorName = "CGV Cinema", Category = "Entertainment" },
            new() { VendorName = "WinMart", Category = "Groceries" },
            new() { VendorName = "Circle K", Category = "Groceries" }
        };

        var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

        // 2. Build Pipeline
        // Map "Category" (string) to Key (number) -> Featurize "VendorName" -> Train -> Map Key back to String
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(TransactionData.Category))
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", nameof(TransactionData.VendorName)))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        // 3. Train Model
        var model = pipeline.Fit(dataView);

        // 4. Create Prediction Engine
        _predictionEngine = mlContext.Model.CreatePredictionEngine<TransactionData, TransactionPrediction>(model);
    }

    public string PredictCategory(string vendorName)
    {
        if (string.IsNullOrWhiteSpace(vendorName)) return "Uncategorized";

        var prediction = _predictionEngine.Predict(new TransactionData { VendorName = vendorName });
        return prediction.Category;
    }
}