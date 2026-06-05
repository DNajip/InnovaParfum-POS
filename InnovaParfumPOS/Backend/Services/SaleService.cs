using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InnovaParfumPOS.Backend.Services
{
    public class SaleService : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private decimal? _discount = null;
        public decimal? Discount
        {
            get => _discount;
            set
            {
                if (_discount != value)
                {
                    _discount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public List<CartItem> Items { get; } = new();

        public event Action? OnCheckoutRequested;

        public int TotalUnits => Items.Sum(i => i.Quantity);
        public decimal SubTotal => Items.Sum(i => i.SubTotal);
        public decimal Total => Math.Max(0, SubTotal - (Discount ?? 0));

        public void AddItem(CartItem item)
        {
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
            NotifyAll();
        }

        public void Clear()
        {
            foreach (var item in Items)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
            Items.Clear();
            Discount = 0;
            NotifyAll();
        }

        public void RequestCheckout()
        {
            if (Items.Any())
                OnCheckoutRequested?.Invoke();
        }

        public void RemoveItem(CartItem item)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
            NotifyAll();
        }

        public void NotifyAll()
        {
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(TotalUnits));
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(Total));
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.SubTotal))
            {
                NotifyAll();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class CartItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int IdProducto { get; set; }
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal UnitPrice { get; set; }
        public int? IdCategoria { get; set; }
        
        public int StockMax { get; set; } = int.MaxValue;
        
        // Properties for IMEI handling
        public bool RequiresImei { get; set; }
        
        // Data for each unit during checkout
        public List<CheckoutDetailItem> Details { get; set; } = new();

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (StockMax > 0 && value > StockMax)
                {
                    value = StockMax;
                }
                if (value < 1)
                {
                    value = 1;
                }
                if (_quantity != value)
                {
                    _quantity = value;
                    UpdateDetails();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SubTotal));
                }
            }
        }

        public decimal SubTotal => UnitPrice * Quantity;

        private void UpdateDetails()
        {
            // Sync details list with quantity
            while (Details.Count < Quantity)
            {
                Details.Add(new CheckoutDetailItem());
            }
            while (Details.Count > Quantity)
            {
                Details.RemoveAt(Details.Count - 1);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class CheckoutDetailItem
    {
        public string? Imei { get; set; }
        public int IdPeriodoGarantia { get; set; } = 1; // Default to "SIN GARANTIA" or first period
    }
}

