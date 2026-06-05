using System;
using System.Collections.Generic;

namespace InnovaParfumPOS.Backend.Models;

public partial class VClienteDashboardStat
{
    public int TotalClientes { get; set; }

    public int TotalGarantiasActivas { get; set; }

    public int ClientesConComprasRecientes { get; set; }
}

