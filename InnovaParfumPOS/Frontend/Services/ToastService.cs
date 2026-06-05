using System;

namespace InnovaParfumPOS.Frontend.Services
{
    public class ToastService
    {
        public event Action<string, string, string>? OnShow;

        public void ShowSuccess(string message, string title = "Operación Exitosa")
        {
            OnShow?.Invoke(message, title, "success");
        }

        public void ShowError(string message, string title = "Error")
        {
            OnShow?.Invoke(message, title, "danger");
        }

        public void ShowInfo(string message, string title = "Información")
        {
            OnShow?.Invoke(message, title, "info");
        }

        public void ShowWarning(string message, string title = "Advertencia")
        {
            OnShow?.Invoke(message, title, "warning");
        }
    }
}

