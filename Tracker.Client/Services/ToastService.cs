using System;
using Blazored.Toast.Services;
using Tracker.Shared.Models;

namespace Tracker.Client.Services
{
    public class ToastService : IToastService
    {
        private readonly IToastService _blazoredToastService;

        public ToastService(IToastService blazoredToastService)
        {
            _blazoredToastService = blazoredToastService;
        }

        // These events are kept for backward compatibility but won't be used
        public event Action<Toast> OnShow = _ => { };
        public event Action<Guid> OnHide = _ => { };

        public void ShowInfo(string message, string title = "Info", bool autoClose = true)
        {
            _blazoredToastService.ShowInfo(message);
        }

        public void ShowSuccess(string message, string title = "Success", bool autoClose = true)
        {
            _blazoredToastService.ShowSuccess(message);
        }

        public void ShowWarning(string message, string title = "Warning", bool autoClose = true)
        {
            _blazoredToastService.ShowWarning(message);
        }

        public void ShowError(string message, string title = "Error", bool autoClose = true)
        {
            _blazoredToastService.ShowError(message);
        }

        // This method is kept for backward compatibility but will use Blazored.Toast internally
        public void ShowToast(Toast toast)
        {
            if (toast == null)
                return;
                
            switch (toast.Level)
            {
                case Tracker.Shared.Models.ToastLevel.Info:
                    ShowInfo(toast.Message, toast.Title, toast.AutoClose);
                    break;
                case Tracker.Shared.Models.ToastLevel.Success:
                    ShowSuccess(toast.Message, toast.Title, toast.AutoClose);
                    break;
                case Tracker.Shared.Models.ToastLevel.Warning:
                    ShowWarning(toast.Message, toast.Title, toast.AutoClose);
                    break;
                case Tracker.Shared.Models.ToastLevel.Error:
                    ShowError(toast.Message, toast.Title, toast.AutoClose);
                    break;
                default:
                    ShowInfo(toast.Message, toast.Title, toast.AutoClose);
                    break;
            }
        }

        // This method is kept for backward compatibility but won't do anything with Blazored.Toast
        public void HideToast(Guid toastId)
        {
            // Blazored.Toast doesn't support hiding specific toasts by ID
            // This method is kept for backward compatibility but won't do anything
        }
    }
}
