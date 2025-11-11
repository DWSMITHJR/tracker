using System;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public interface IToastService
    {
        // These events are kept for backward compatibility but may not be fully functional with Blazored.Toast
        event Action<Toast> OnShow;
        event Action<Guid> OnHide;
        
        void ShowInfo(string message, string title = "Info", bool autoClose = true);
        void ShowSuccess(string message, string title = "Success", bool autoClose = true);
        void ShowWarning(string message, string title = "Warning", bool autoClose = true);
        void ShowError(string message, string title = "Error", bool autoClose = true);
        
        // This method is kept for backward compatibility
        void ShowToast(Toast toast);
        
        // This method is kept for backward compatibility but may not be fully functional with Blazored.Toast
        void HideToast(Guid toastId);
    }
}
