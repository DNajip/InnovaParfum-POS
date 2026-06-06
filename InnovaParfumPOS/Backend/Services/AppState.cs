using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InnovaParfumPOS.Backend.Services;

public class AppState
{
    public event Action? OnChange;

    private string _businessName = "InnovaTec POS";
    public string BusinessName
    {
        get => _businessName;
        set
        {
            if (_businessName != value)
            {
                _businessName = value;
                NotifyStateChanged();
            }
        }
    }

    private string _businessLogo = "images/logo.png";
    public string BusinessLogo
    {
        get => _businessLogo;
        set
        {
            if (_businessLogo != value)
            {
                _businessLogo = value;
                NotifyStateChanged();
            }
        }
    }

    private string _businessRuc = "";
    public string BusinessRuc
    {
        get => _businessRuc;
        set
        {
            if (_businessRuc != value)
            {
                _businessRuc = value;
                NotifyStateChanged();
            }
        }
    }

    private string _businessPhone = "";
    public string BusinessPhone
    {
        get => _businessPhone;
        set
        {
            if (_businessPhone != value)
            {
                _businessPhone = value;
                NotifyStateChanged();
            }
        }
    }

    private string _businessAddress = "";
    public string BusinessAddress
    {
        get => _businessAddress;
        set
        {
            if (_businessAddress != value)
            {
                _businessAddress = value;
                NotifyStateChanged();
            }
        }
    }

    private string _ticketMessage = "¡Gracias por su compra!";
    public string TicketMessage
    {
        get => _ticketMessage;
        set
        {
            if (_ticketMessage != value)
            {
                _ticketMessage = value;
                NotifyStateChanged();
            }
        }
    }

    private string _printerName = "";
    public string PrinterName
    {
        get => _printerName;
        set
        {
            if (_printerName != value)
            {
                _printerName = value;
                NotifyStateChanged();
            }
        }
    }

    private bool _openCashDrawer = true;
    public bool OpenCashDrawer
    {
        get => _openCashDrawer;
        set
        {
            if (_openCashDrawer != value)
            {
                _openCashDrawer = value;
                NotifyStateChanged();
            }
        }
    }

    private DateTime _reportStartDate = DateTime.Today;
    public DateTime ReportStartDate
    {
        get => _reportStartDate;
        set
        {
            if (_reportStartDate != value)
            {
                _reportStartDate = value;
                NotifyStateChanged();
            }
        }
    }

    private DateTime _reportEndDate = DateTime.Today;
    public DateTime ReportEndDate
    {
        get => _reportEndDate;
        set
        {
            if (_reportEndDate != value)
            {
                _reportEndDate = value;
                NotifyStateChanged();
            }
        }
    }

    private string _currencyCode = "NIO";
    public string CurrencyCode
    {
        get => _currencyCode;
        set
        {
            if (_currencyCode != value)
            {
                _currencyCode = value;
                NotifyStateChanged();
            }
        }
    }

    private string _currencySymbol = "C$";
    public string CurrencySymbol
    {
        get => _currencySymbol;
        set
        {
            if (_currencySymbol != value)
            {
                _currencySymbol = value;
                NotifyStateChanged();
            }
        }
    }

    private decimal _exchangeRate = 36.62m;
    public decimal ExchangeRate
    {
        get => _exchangeRate;
        set
        {
            if (_exchangeRate != value)
            {
                _exchangeRate = value;
                NotifyStateChanged();
            }
        }
    }

    private string _colorPrimario = "#0077b6";
    public string ColorPrimario
    {
        get => _colorPrimario;
        set { if (_colorPrimario != value) { _colorPrimario = value; NotifyStateChanged(); } }
    }

    private string _colorSecundario = "#6c757d";
    public string ColorSecundario
    {
        get => _colorSecundario;
        set { if (_colorSecundario != value) { _colorSecundario = value; NotifyStateChanged(); } }
    }

    private string _colorModuloActivo = "#0077b6";
    public string ColorModuloActivo
    {
        get => _colorModuloActivo;
        set { if (_colorModuloActivo != value) { _colorModuloActivo = value; NotifyStateChanged(); } }
    }

    private string _colorTextoMarca = "#0077b6";
    public string ColorTextoMarca
    {
        get => _colorTextoMarca;
        set { if (_colorTextoMarca != value) { _colorTextoMarca = value; NotifyStateChanged(); } }
    }

    private string _colorIconos = "#0077b6";
    public string ColorIconos
    {
        get => _colorIconos;
        set { if (_colorIconos != value) { _colorIconos = value; NotifyStateChanged(); } }
    }

    private string _colorFondoLogin = "#003566";
    public string ColorFondoLogin
    {
        get => _colorFondoLogin;
        set { if (_colorFondoLogin != value) { _colorFondoLogin = value; NotifyStateChanged(); } }
    }

    private string _colorBarraVertical = "#0b192c";
    public string ColorBarraVertical
    {
        get => _colorBarraVertical;
        set { if (_colorBarraVertical != value) { _colorBarraVertical = value; NotifyStateChanged(); } }
    }

    public void UpdateFromDictionary(Dictionary<string, string> settings)
    {
        if (settings.TryGetValue("Empresa_Nombre", out var name)) BusinessName = name;
        if (settings.TryGetValue("Empresa_Logo", out var logo)) BusinessLogo = logo;
        if (settings.TryGetValue("Empresa_RUC", out var ruc)) BusinessRuc = ruc;
        if (settings.TryGetValue("Empresa_Telefono", out var phone)) BusinessPhone = phone;
        if (settings.TryGetValue("Empresa_Direccion", out var address)) BusinessAddress = address;
        if (settings.TryGetValue("Ventas_MensajeTicket", out var msg)) TicketMessage = msg;
        if (settings.TryGetValue("Hardware_Impresora", out var printer)) PrinterName = printer;
        if (settings.TryGetValue("Hardware_AbrirCajon", out var openDrawer)) OpenCashDrawer = openDrawer == "SI";
        
        if (settings.TryGetValue("Moneda_Principal", out var moneda))
        {
            CurrencyCode = moneda;
            CurrencySymbol = moneda == "USD" ? "$" : "C$";
        }
        if (settings.TryGetValue("Moneda_TasaCambio", out var tasa) && decimal.TryParse(tasa, out var parsedTasa))
        {
            ExchangeRate = parsedTasa;
        }

        if (settings.TryGetValue("Color_Primario", out var colPri)) ColorPrimario = colPri;
        if (settings.TryGetValue("Color_Secundario", out var colSec)) ColorSecundario = colSec;
        if (settings.TryGetValue("Color_ModuloActivo", out var colMod)) ColorModuloActivo = colMod;
        if (settings.TryGetValue("Color_FondoLogin", out var colLog)) ColorFondoLogin = colLog;
        if (settings.TryGetValue("Color_BarraVertical", out var colBar)) ColorBarraVertical = colBar;
        if (settings.TryGetValue("Color_TextoMarca", out var colTex)) ColorTextoMarca = colTex;
        if (settings.TryGetValue("Color_Iconos", out var colIco)) ColorIconos = colIco;
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

