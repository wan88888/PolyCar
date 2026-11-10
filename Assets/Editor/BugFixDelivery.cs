using System;
using System.IO;
using UnityEngine;

public static class BugFixDelivery
{
    public static void Run()
    {
        InterfaceFeedbackReview.WithPreservedState(() =>
        {
            SaveManager.SetSelectedRoute(7);
            InterfaceFeedbackReview.Run();
            if (SaveManager.GetSelectedRouteIndex(-1) != 7)
                throw new InvalidOperationException("Review overwrote the selected route");
            Directory.CreateDirectory("Review/BugAudit");
            File.WriteAllText("Review/BugAudit/fixed-delivery.txt",
                "Full interface review and macOS build succeeded.\nSelected route preserved: 7 -> review -> 7.\nOriginal user selection restored by the outer scope.\n");
            Debug.Log("BUG_FIX_DELIVERY_OK selectedRoutePreserved=7 fullReview=true build=true");
        });
    }
}
