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

        private bool _isMayorista;
        public bool IsMayorista
        {
            get => _isMayorista;
            set
            {
                if (_isMayorista != value)
                {
                    _isMayorista = value;
                    OnPropertyChanged();
                    NotifyAll();
                }
            }
        }

        private bool _isCredito;
        public bool IsCredito
        {
            get => _isCredito;
            set
            {
                if (_isCredito != value)
                {
                    _isCredito = value;
                    OnPropertyChanged();
                }
            }
        }

        public int IdTipoVenta => IsMayorista ? 2 : 1; // 1: Minorista, 2: Mayorista
        public int IdCondicionPago => IsCredito ? 2 : 1; // 1: Contado, 2: Credito

        public List<CartItem> Items { get; } = new();

        public event Action? OnCheckoutRequested;

        public int TotalUnits => Items.Sum(i => i.Quantity);
        
        public decimal SubTotalMayorista => Items.Sum(i => i.SubTotalMayorista);
        public decimal SubTotalMinorista => Items.Sum(i => i.SubTotalMinorista);
        
        // El subtotal y total activos dependen del switch
        public decimal SubTotal => IsMayorista ? SubTotalMayorista : SubTotalMinorista;
        public decimal Total => Math.Max(0, SubTotal - (Discount ?? 0));
        public decimal TotalMayorista => Math.Max(0, SubTotalMayorista - (Discount ?? 0));
        public decimal TotalMinorista => Math.Max(0, SubTotalMinorista - (Discount ?? 0));

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
            OnPropertyChanged(nameof(SubTotalMayorista));
            OnPropertyChanged(nameof(SubTotalMinorista));
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TotalMayorista));
            OnPropertyChanged(nameof(TotalMinorista));
            OnPropertyChanged(nameof(Total));
        }

        public void ToggleRegalia(CartItem item)
        {
            item.IsRegalia = !item.IsRegalia;
            NotifyAll();
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.SubTotalMayorista) || e.PropertyName == nameof(CartItem.SubTotalMinorista))
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
        
        public decimal PrecioMayorista { get; set; }
        public decimal PrecioMinorista { get; set; }
        public decimal CostoUnitario { get; set; }

        public int? IdCategoria { get; set; }
        public string? Marca { get; set; }
        public string? Genero { get; set; }
        public string? Origen { get; set; }
        public string? Concentracion { get; set; }
        public int? Ml { get; set; }
        
        public int StockMax { get; set; } = int.MaxValue;
        
        // Properties for IMEI handling
        public bool RequiresImei { get; set; }
        
        private bool _isRegalia;
        public bool IsRegalia
        {
            get => _isRegalia;
            set
            {
                if (_isRegalia != value)
                {
                    _isRegalia = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SubTotalMayorista));
                    OnPropertyChanged(nameof(SubTotalMinorista));
                }
            }
        }
        
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
                    OnPropertyChanged(nameof(SubTotalMayorista));
                    OnPropertyChanged(nameof(SubTotalMinorista));
                }
            }
        }

        public decimal SubTotalMayorista => IsRegalia ? 0 : PrecioMayorista * Quantity;
        public decimal SubTotalMinorista => IsRegalia ? 0 : PrecioMinorista * Quantity;

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

