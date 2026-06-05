# Plan Maestro Detallado: Centro de Inteligencia Comercial

Este documento detalla la estrategia de implementación del Módulo de Reportes Inteligentes para InnovaTec POS, diseñado para ser el núcleo analítico y de toma de decisiones del negocio.

## Resumen del Objetivo
Construir un módulo de Business Intelligence (BI) que permita al dueño del negocio detectar pérdidas, optimizar ventas y controlar operaciones en tiempo real, con una interfaz premium y 100% responsiva.

---

## 1. Backend: Capa de Inteligencia de Datos

### [NEW] [ReportService.cs](file:///e:/Programación/Antigravity/InnovaTecPOS/InnovaTecPOS/Backend/Services/ReportService.cs)
Se encargará de la lógica de agregación y cálculos complejos.
- **Métodos Principales**:
  - `GetDashboardExecutiveAsync(DateTime start, DateTime end)`: Retorna KPIs globales (Ventas Brutas, Utilidad Neta, Ticket Promedio, Clientes Nuevos).
  - `GetSalesTrendsAsync(DateTime start, DateTime end, string unit)`: Retorna datos para gráficos de líneas (agrupados por hora, día o mes).
  - `GetInventoryInsightsAsync()`: Identifica stock crítico y productos con nula rotación.
  - `GetCashierAuditAsync(DateTime start, DateTime end)`: Resumen de ventas, descuentos aplicados y arqueos por usuario.
- **Lógica de Utilidad**: Cálculo dinámico comparando `PrecioVenta` vs `PrecioCompra` en los detalles de factura.

### [NEW] [ReportDTOs.cs](file:///e:/Programación/Antigravity/InnovaTecPOS/InnovaTecPOS/Backend/Models/ReportDTOs.cs)
- `ExecutiveDashboardDTO`: Contenedor de todos los KPIs del dashboard.
- `TrendPointDTO`: Estructura para puntos de gráficas (Fecha, Valor NIO, Valor USD).
- `KpiChangeDTO`: Estructura para comparar periodos (Valor actual, Valor anterior, % de cambio).

---

## 2. Frontend: Interfaz y Experiencia de Usuario

### Estructura de Navegación
Implementaremos un sub-layout específico para Reportes que incluya un sidebar interno.

#### [NEW] [InnovaReportSidebar.razor](file:///e:/Programación/Antigravity/InnovaTecPOS/InnovaTecPOS/Frontend/Components/UI/InnovaReportSidebar.razor)
- Navegación lateral con iconos para:
  - **Dashboard**: Vista general de salud del negocio.
  - **Ventas**: Desglose financiero y métodos de pago.
  - **Inventario**: Rotación y stock crítico.
  - **Clientes**: Análisis de fidelidad.
  - **Arqueos**: Auditoría de cierres de caja.
  - **Garantías**: Calidad y devoluciones.

### Filtrado de Fechas (Patrón "Facturas")
Reutilizaremos la lógica de selección de rango de fechas del módulo de Facturas pero potenciada:
- **Inputs**: `Desde` y `Hasta` vinculados a `DateTime`.
- **Rango Rápido**: Botones para "Hoy", "Últimos 7 días", "Mes Actual" y "Personalizado".
- **Sincronización**: Al cambiar las fechas, todos los widgets del dashboard se actualizarán automáticamente.

---

## 3. Visualizaciones y Widgets Premium

### Dashboard General (`ReportDashboard.razor`)
- **Fila de KPIs**: Tarjetas con sombras suaves y gradientes que muestran métricas clave.
- **Gráfico de Tendencia**: SVG dinámico para representar el flujo de ventas NIO/USD.
- **Ventas por Hora**: Una rejilla (Heatmap) que resalta visualmente las horas pico de mayor venta.

### Análisis de Ventas (`ReportVentas.razor`)
- **Gráfico de Dona**: Distribución de ventas por método de pago (Efectivo, Tarjeta, Transferencia).
- **Ventas por Moneda**: Detalle de ingresos con la tasa de cambio promedio del periodo aplicada.
- **Tabla de Resumen Diario**: Filas que desglosan ventas brutas, devoluciones y utilidades netas por día.

---

## 4. Estilos y Diseño (`app.css`)
- **Grid Layout**: Uso de CSS Grid para organizar los widgets de manera flexible.
- **Responsividad**: 
  - Escritorio: 3 o 4 columnas de widgets.
  - Tablet: 2 columnas.
  - Móvil: 1 columna, sidebar convertido en menú de pestañas.
- **Animaciones**: Transiciones suaves al cargar datos y efectos de "hover" en los KPIs.

## 5. Plan de Verificación
1. **Consistencia de Datos**: Comparar los resultados del `ReportService` con los totales de `Ventas` manuales.
2. **Cálculo de Utilidad**: Verificar que la resta de costos sea correcta incluso con cambios de precio históricos.
3. **Pruebas de Carga**: Asegurar que el dashboard cargue en menos de 2 segundos para rangos de 30 días.
