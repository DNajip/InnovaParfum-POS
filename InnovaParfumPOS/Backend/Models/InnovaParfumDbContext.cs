using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using InnovaParfumPOS.Backend.Services;

namespace InnovaParfumPOS.Backend.Models;

public partial class InnovaParfumDbContext : DbContext
{
    public UserSession? Session { get; set; }

    public InnovaParfumDbContext(DbContextOptions<InnovaParfumDbContext> options)
        : base(options)
    {
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (Session != null && Session.IsAuthenticated)
        {
            // Asegurar que usamos la misma conexión para la sesión y el guardado
            if (Database.GetDbConnection().State != System.Data.ConnectionState.Open)
            {
                await Database.OpenConnectionAsync(cancellationToken);
            }

            await Database.ExecuteSqlRawAsync(
                "EXEC sp_set_session_context 'UsuarioId', @p0; EXEC sp_set_session_context 'Observacion', @p1;",
                new object[] { Session.UserId!, Session.CurrentObservation ?? "Ajuste de sistema" },
                cancellationToken);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<Configuracion> Configuracions { get; set; }

    public virtual DbSet<ConteoDenominacione> ConteoDenominaciones { get; set; }

    public virtual DbSet<Denominacione> Denominaciones { get; set; }

    public virtual DbSet<Empleado> Empleados { get; set; }

    public virtual DbSet<Estado> Estados { get; set; }

    public virtual DbSet<Garantia> Garantias { get; set; }

    public virtual DbSet<Genero> Generos { get; set; }

    public virtual DbSet<MetodosPago> MetodosPagos { get; set; }

    public virtual DbSet<Modulo> Modulos { get; set; }

    public virtual DbSet<Moneda> Monedas { get; set; }

    public virtual DbSet<Movimiento> Movimientos { get; set; }

    public virtual DbSet<Pago> Pagos { get; set; }

    public virtual DbSet<PeriodosGarantium> PeriodosGarantia { get; set; }

    public virtual DbSet<Persona> Personas { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ReclamosGarantium> ReclamosGarantia { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<TipoIdentificacion> TipoIdentificacions { get; set; }

    public virtual DbSet<TipoMovimientoInv> TipoMovimientoInvs { get; set; }

    public virtual DbSet<Turno> Turnos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VGarantiasActiva> VGarantiasActivas { get; set; }

    public virtual DbSet<VResumenTurno> VResumenTurnos { get; set; }

    public virtual DbSet<VResumenVenta> VResumenVentas { get; set; }

    public virtual DbSet<VStockCritico> VStockCriticos { get; set; }

    public virtual DbSet<VClienteDashboardStat> VClienteDashboardStats { get; set; }

    public virtual DbSet<Venta> Ventas { get; set; }

    public virtual DbSet<VentaDetalle> VentaDetalles { get; set; }

    public virtual DbSet<MovimientoVario> MovimientosVarios { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Fallback para diseño o herramientas de CLI si no se inyecta la configuración
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=InnovaTecBD;Integrated Security=True;Encrypt=False;TrustServerCertificate=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("PK_INV_CATEGORIAS");

            entity.ToTable("CATEGORIAS", "INV");

            entity.HasIndex(e => e.Nombre, "UQ__CATEGORI__B21D0AB9C8524333").IsUnique();

            entity.Property(e => e.IdCategoria).HasColumnName("ID_CATEGORIA");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("DESCRIPCION");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Categoria)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CATEGORIA__ID_ES__14270015");
        });

        modelBuilder.Entity<Configuracion>(entity =>
        {
            entity.HasKey(e => e.IdConfig).HasName("PK_ADM_CONFIG");

            entity.ToTable("CONFIGURACION", "ADM");

            entity.HasIndex(e => e.Clave, "UQ__CONFIGUR__107EA0211DA3A0C7").IsUnique();

            entity.Property(e => e.IdConfig).HasColumnName("ID_CONFIG");
            entity.Property(e => e.Clave)
                .HasMaxLength(80)
                .HasColumnName("CLAVE");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .HasColumnName("DESCRIPCION");
            entity.Property(e => e.ModificadoPor).HasColumnName("MODIFICADO_POR");
            entity.Property(e => e.SoloAdmin)
                .HasDefaultValue(true)
                .HasColumnName("SOLO_ADMIN");
            entity.Property(e => e.UltimaModificacion)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("ULTIMA_MODIFICACION");
            entity.Property(e => e.Valor).HasColumnName("VALOR");

            entity.HasOne(d => d.ModificadoPorNavigation).WithMany(p => p.Configuracions)
                .HasForeignKey(d => d.ModificadoPor)
                .HasConstraintName("FK_CONFIG_USUARIO");
        });

        modelBuilder.Entity<ConteoDenominacione>(entity =>
        {
            entity.HasKey(e => e.IdConteo).HasName("PK_CAJA_CONTEO");

            entity.ToTable("CONTEO_DENOMINACIONES", "CAJA");

            entity.Property(e => e.IdConteo).HasColumnName("ID_CONTEO");
            entity.Property(e => e.Cantidad).HasColumnName("CANTIDAD");
            entity.Property(e => e.IdDenominacion).HasColumnName("ID_DENOMINACION");
            entity.Property(e => e.IdTurno).HasColumnName("ID_TURNO");
            entity.Property(e => e.TipoConteo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("TIPO_CONTEO");

            entity.HasOne(d => d.IdDenominacionNavigation).WithMany(p => p.ConteoDenominaciones)
                .HasForeignKey(d => d.IdDenominacion)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CONTEO_DE__ID_DE__46B27FE2");

            entity.HasOne(d => d.IdTurnoNavigation).WithMany(p => p.ConteoDenominaciones)
                .HasForeignKey(d => d.IdTurno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CONTEO_DE__ID_TU__45BE5BA9");
        });

        modelBuilder.Entity<Denominacione>(entity =>
        {
            entity.HasKey(e => e.IdDenominacion).HasName("PK_CAJA_DENOM");

            entity.ToTable("DENOMINACIONES", "CAJA");

            entity.HasIndex(e => new { e.IdMoneda, e.Valor }, "UQ_DENOM").IsUnique();

            entity.Property(e => e.IdDenominacion).HasColumnName("ID_DENOMINACION");
            entity.Property(e => e.IdMoneda).HasColumnName("ID_MONEDA");
            entity.Property(e => e.Orden).HasColumnName("ORDEN");
            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("TIPO");
            entity.Property(e => e.Valor)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("VALOR");

            entity.HasOne(d => d.IdMonedaNavigation).WithMany(p => p.Denominaciones)
                .HasForeignKey(d => d.IdMoneda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DENOMINAC__ID_MO__32AB8735");
        });

        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.HasKey(e => e.IdEmpleado).HasName("PK_ADM_EMPLEADOS");

            entity.ToTable("EMPLEADOS", "ADM");

            entity.HasIndex(e => e.IdPersona, "UQ_EMPLEADO_PERSONA").IsUnique();

            entity.Property(e => e.IdEmpleado).HasColumnName("ID_EMPLEADO");
            entity.Property(e => e.FechaContratacion).HasColumnName("FECHA_CONTRATACION");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.IdPersona).HasColumnName("ID_PERSONA");
            entity.Property(e => e.IdRol).HasColumnName("ID_ROL");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EMPLEADOS__ID_ES__02FC7413");

            entity.HasOne(d => d.IdPersonaNavigation).WithOne(p => p.Empleado)
                .HasForeignKey<Empleado>(d => d.IdPersona)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EMPLEADOS__ID_PE__01142BA1");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EMPLEADOS__ID_RO__02084FDA");
        });

        modelBuilder.Entity<Estado>(entity =>
        {
            entity.HasKey(e => e.IdEstado).HasName("PK_CAT_ESTADOS");

            entity.ToTable("ESTADOS", "CAT");

            entity.HasIndex(e => e.Codigo, "UQ__ESTADOS__CC87E1260AED492B").IsUnique();

            entity.Property(e => e.IdEstado).HasColumnName("ID_ESTADO");
            entity.Property(e => e.Codigo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CODIGO");
            entity.Property(e => e.DescEstado)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("DESC_ESTADO");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_CREACION");
        });

        modelBuilder.Entity<Garantia>(entity =>
        {
            entity.HasKey(e => e.IdGarantia).HasName("PK_GAR_GARANTIAS");

            entity.ToTable("GARANTIAS", "GAR");

            entity.HasIndex(e => e.EstadoGarantia, "IX_GARANTIAS_ESTADO");

            entity.HasIndex(e => e.IdPersona, "IX_GARANTIAS_PERSONA");

            entity.Property(e => e.IdGarantia).HasColumnName("ID_GARANTIA");
            entity.Property(e => e.EstadoGarantia)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ACTIVA")
                .HasColumnName("ESTADO_GARANTIA");
            entity.Property(e => e.FechaInicio).HasColumnName("FECHA_INICIO");
            entity.Property(e => e.FechaVencimiento).HasColumnName("FECHA_VENCIMIENTO");
            entity.Property(e => e.IdDetalleVenta).HasColumnName("ID_DETALLE_VENTA");
            entity.Property(e => e.IdPersona).HasColumnName("ID_PERSONA");
            entity.Property(e => e.IdProducto).HasColumnName("ID_PRODUCTO");
            entity.Property(e => e.MesesGarantia).HasColumnName("MESES_GARANTIA");

            entity.HasOne(d => d.IdDetalleVentaNavigation).WithMany(p => p.Garantia)
                .HasForeignKey(d => d.IdDetalleVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GARANTIAS__ID_DE__662B2B3B");
            entity.HasOne(d => d.IdPersonaNavigation).WithMany(p => p.Garantia)
                .HasForeignKey(d => d.IdPersona)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GARANTIAS__ID_PE__681373AD");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Garantia)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GARANTIAS__ID_PR__690797E6");
        });

        modelBuilder.Entity<Genero>(entity =>
        {
            entity.HasKey(e => e.IdGenero).HasName("PK_CAT_GENEROS");

            entity.ToTable("GENEROS", "CAT");

            entity.Property(e => e.IdGenero).HasColumnName("ID_GENERO");
            entity.Property(e => e.DescGenero)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("DESC_GENERO");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Generos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__GENEROS__ID_ESTA__534D60F1");
        });

        modelBuilder.Entity<MetodosPago>(entity =>
        {
            entity.HasKey(e => e.IdMetodo).HasName("PK_CAT_METODOS_PAGO");

            entity.ToTable("METODOS_PAGO", "CAT");

            entity.HasIndex(e => e.Nombre, "UQ__METODOS___B21D0AB91FC97185").IsUnique();

            entity.Property(e => e.IdMetodo).HasColumnName("ID_METODO");
            entity.Property(e => e.AfectaCaja)
                .HasDefaultValue(true)
                .HasColumnName("AFECTA_CAJA");
            entity.Property(e => e.IdMoneda).HasColumnName("ID_MONEDA");
            entity.Property(e => e.Nombre)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");

            entity.HasOne(d => d.IdMonedaNavigation).WithMany(p => p.MetodosPagos)
                .HasForeignKey(d => d.IdMoneda)
                .HasConstraintName("FK__METODOS_P__ID_MO__5BE2A6F2");
        });

        modelBuilder.Entity<Modulo>(entity =>
        {
            entity.HasKey(e => e.IdModulo).HasName("PK_CAT_MODULOS");

            entity.ToTable("MODULOS", "CAT");

            entity.HasIndex(e => e.Nombre, "UQ__MODULOS__B21D0AB99974EA27").IsUnique();

            entity.Property(e => e.IdModulo).HasColumnName("ID_MODULO");
            entity.Property(e => e.Controller)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("CONTROLLER");
            entity.Property(e => e.Icono)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("ICONO");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");
            entity.Property(e => e.Orden).HasColumnName("ORDEN");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Modulos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MODULOS__ID_ESTA__6A30C649");
        });

        modelBuilder.Entity<Moneda>(entity =>
        {
            entity.HasKey(e => e.IdMoneda).HasName("PK_CAT_MONEDAS");

            entity.ToTable("MONEDAS", "CAT");

            entity.HasIndex(e => e.Codigo, "UQ__MONEDAS__CC87E1261AA8AA08").IsUnique();

            entity.Property(e => e.IdMoneda).HasColumnName("ID_MONEDA");
            entity.Property(e => e.Codigo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("CODIGO");
            entity.Property(e => e.EsBase).HasColumnName("ES_BASE");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");
            entity.Property(e => e.Simbolo)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("SIMBOLO");
        });

        modelBuilder.Entity<Movimiento>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("PK_INV_MOVIMIENTOS");

            entity.ToTable("MOVIMIENTOS", "INV");

            entity.HasIndex(e => e.FechaMov, "IX_MOV_FECHA");

            entity.HasIndex(e => e.IdProducto, "IX_MOV_PRODUCTO");

            entity.Property(e => e.IdMovimiento).HasColumnName("ID_MOVIMIENTO");
            entity.Property(e => e.Cantidad).HasColumnName("CANTIDAD");
            entity.Property(e => e.FechaMov)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_MOV");
            entity.Property(e => e.IdProducto).HasColumnName("ID_PRODUCTO");
            entity.Property(e => e.IdReferencia).HasColumnName("ID_REFERENCIA");
            entity.Property(e => e.IdTipoMov).HasColumnName("ID_TIPO_MOV");
            entity.Property(e => e.Observacion)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("OBSERVACION");
            entity.Property(e => e.RegistradoPor).HasColumnName("REGISTRADO_POR");
            entity.Property(e => e.StockAntes).HasColumnName("STOCK_ANTES");
            entity.Property(e => e.StockDespues).HasColumnName("STOCK_DESPUES");
            entity.Property(e => e.TablaReferencia)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TABLA_REFERENCIA");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MOVIMIENT__ID_PR__2B0A656D");

            entity.HasOne(d => d.IdTipoMovNavigation).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.IdTipoMov)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MOVIMIENT__ID_TI__2BFE89A6");

            entity.HasOne(d => d.RegistradoPorNavigation).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.RegistradoPor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MOVIMIENT__REGIS__2CF2ADDF");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.IdPago).HasName("PK_VEN_PAGOS");

            entity.ToTable("PAGOS", "VEN");

            entity.HasIndex(e => e.IdVenta, "IX_PAGOS_VENTA");

            entity.Property(e => e.IdPago).HasColumnName("ID_PAGO");
            entity.Property(e => e.CodReferencia)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("COD_REFERENCIA");
            entity.Property(e => e.FechaPago)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_PAGO");
            entity.Property(e => e.IdMetodoPago).HasColumnName("ID_METODO_PAGO");
            entity.Property(e => e.IdVenta).HasColumnName("ID_VENTA");
            entity.Property(e => e.MontoEnNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("MONTO_EN_NIO");
            entity.Property(e => e.MontoPagado)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("MONTO_PAGADO");
            entity.Property(e => e.MontoRecibido)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("MONTO_RECIBIDO");
            entity.Property(e => e.TasaAplicada)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("TASA_APLICADA");
            entity.Property(e => e.VueltoNio)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("VUELTO_NIO");

            entity.HasOne(d => d.IdMetodoPagoNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdMetodoPago)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PAGOS__ID_METODO__6166761E");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PAGOS__ID_VENTA__607251E5");
        });

        modelBuilder.Entity<PeriodosGarantium>(entity =>
        {
            entity.HasKey(e => e.IdPeriodo).HasName("PK_CAT_GARANTIA");

            entity.ToTable("PERIODOS_GARANTIA", "CAT");

            entity.Property(e => e.IdPeriodo).HasColumnName("ID_PERIODO");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("DESCRIPCION");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.Meses).HasColumnName("MESES");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.PeriodosGarantia)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PERIODOS___ID_ES__5FB337D6");
        });

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.HasKey(e => e.IdPersona).HasName("PK_ADM_PERSONAS");

            entity.ToTable("PERSONAS", "ADM");

            entity.HasIndex(e => new { e.IdTipoId, e.NumIdentificacion }, "UQ_PERSONA_IDENTIFICACION").IsUnique();

            entity.Property(e => e.IdPersona).HasColumnName("ID_PERSONA");
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false)
                .HasColumnName("DIRECCION");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("EMAIL");
            entity.Property(e => e.EsCliente)
                .HasDefaultValue(true)
                .HasColumnName("ES_CLIENTE");
            entity.Property(e => e.EsEmpleado).HasColumnName("ES_EMPLEADO");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_CREACION");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.IdGenero).HasColumnName("ID_GENERO");
            entity.Property(e => e.IdTipoId).HasColumnName("ID_TIPO_ID");
            entity.Property(e => e.NombreCompleto)
                .HasMaxLength(163)
                .IsUnicode(false)
                .HasComputedColumnSql("(Trim((((([PRIMER_NOMBRE]+' ')+isnull([SEGUNDO_NOMBRE]+' ',''))+[PRIMER_APELLIDO])+' ')+isnull([SEGUNDO_APELLIDO],'')))", true)
                .HasColumnName("NOMBRE_COMPLETO");
            entity.Property(e => e.NumIdentificacion)
                .HasMaxLength(25)
                .IsUnicode(false)
                .HasColumnName("NUM_IDENTIFICACION");
            entity.Property(e => e.PrimerApellido)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("PRIMER_APELLIDO");
            entity.Property(e => e.PrimerNombre)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("PRIMER_NOMBRE");
            entity.Property(e => e.SegundoApellido)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("SEGUNDO_APELLIDO");
            entity.Property(e => e.SegundoNombre)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("SEGUNDO_NOMBRE");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TELEFONO");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Personas)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PERSONAS__ID_EST__7C4F7684");

            entity.HasOne(d => d.IdGeneroNavigation).WithMany(p => p.Personas)
                .HasForeignKey(d => d.IdGenero)
                .HasConstraintName("FK__PERSONAS__ID_GEN__7B5B524B");

            entity.HasOne(d => d.IdTipo).WithMany(p => p.Personas)
                .HasForeignKey(d => d.IdTipoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PERSONAS__ID_TIP__7A672E12");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PK_INV_PRODUCTOS");

            entity.ToTable("PRODUCTOS", "INV", t => t.HasTrigger("Trg_Productos_Auditoria"));

            entity.HasIndex(e => e.CodigoBarras, "UQ__PRODUCTO__9F4646C2D3E8A062").IsUnique();

            entity.Property(e => e.IdProducto).HasColumnName("ID_PRODUCTO");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("ACTIVO");
            entity.Property(e => e.OrigenTipo)
                .HasMaxLength(50)
                .HasColumnName("ORIGEN_TIPO");
            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(100)
                .HasColumnName("CODIGO_BARRAS");
            entity.Property(e => e.Concentracion)
                .HasMaxLength(50)
                .HasColumnName("CONCENTRACION");
            entity.Property(e => e.CreadoPor).HasColumnName("CREADO_POR");

            entity.Property(e => e.EstadoStock)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComputedColumnSql("(case when [STOCK_ACTUAL]<=(0) then 'AGOTADO' when [STOCK_ACTUAL]<=[STOCK_MINIMO] then 'CRITICO' else 'DISPONIBLE' end)", true)
                .HasColumnName("ESTADO_STOCK");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_CREACION");
            entity.Property(e => e.FechaVencimiento).HasColumnType("date").HasColumnName("FECHA_VENCIMIENTO");
                entity.Property(e => e.TipoProducto).HasMaxLength(20).IsUnicode(false).HasDefaultValue("PERFUME").HasColumnName("TIPO_PRODUCTO");
                entity.Property(e => e.FechaDesactivacion)
                .HasColumnName("FECHA_DESACTIVACION");
            entity.Property(e => e.Archivado)
                .HasDefaultValue(false)
                .HasColumnName("ARCHIVADO");
            entity.Property(e => e.IdCategoria).HasColumnName("ID_CATEGORIA");
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .HasColumnName("MARCA");
            entity.Property(e => e.Genero)
                .HasMaxLength(100)
                .HasColumnName("GENERO");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("NOMBRE");
            entity.Property(e => e.PrecioCompra)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("PRECIO_COMPRA");
            entity.Property(e => e.PrecioVenta)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("PRECIO_VENTA");
            entity.Property(e => e.StockActual).HasColumnName("STOCK_ACTUAL");
            entity.Property(e => e.StockMinimo).HasColumnName("STOCK_MINIMO");
            entity.Property(e => e.TipoProducto)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("ARTICULO")
                .HasColumnName("TIPO_PRODUCTO");

            entity.HasOne(d => d.CreadoPorNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.CreadoPor)
                .HasConstraintName("FK__PRODUCTOS__CREAD__1F98B2C1");

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK__PRODUCTOS__ID_CA__1EA48E88");
        });

        modelBuilder.Entity<ReclamosGarantium>(entity =>
        {
            entity.HasKey(e => e.IdReclamo).HasName("PK_GAR_RECLAMOS");

            entity.ToTable("RECLAMOS_GARANTIA", "GAR");

            entity.Property(e => e.IdReclamo).HasColumnName("ID_RECLAMO");
            entity.Property(e => e.DescripcionFalla)
                .HasMaxLength(500)
                .HasColumnName("DESCRIPCION_FALLA");
            entity.Property(e => e.FechaReclamo)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_RECLAMO");
            entity.Property(e => e.FechaResolucion).HasColumnName("FECHA_RESOLUCION");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(4)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.IdGarantia).HasColumnName("ID_GARANTIA");
            entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
            entity.Property(e => e.NotasTecnico)
                .HasMaxLength(500)
                .HasColumnName("NOTAS_TECNICO");
            entity.Property(e => e.ProductoReemplazoId).HasColumnName("PRODUCTO_REEMPLAZO_ID");
            entity.Property(e => e.TipoResolucion)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("TIPO_RESOLUCION");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.ReclamosGarantia)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RECLAMOS___ID_ES__719CDDE7");

            entity.HasOne(d => d.IdGarantiaNavigation).WithMany(p => p.ReclamosGarantia)
                .HasForeignKey(d => d.IdGarantia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RECLAMOS___ID_GA__6EC0713C");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ReclamosGarantia)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RECLAMOS___ID_US__70A8B9AE");

            entity.HasOne(d => d.ProductoReemplazo).WithMany(p => p.ReclamosGarantia)
                .HasForeignKey(d => d.ProductoReemplazoId)
                .HasConstraintName("FK__RECLAMOS___PRODU__6FB49575");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("PK_CAT_ROLES");

            entity.ToTable("ROLES", "CAT");

            entity.HasIndex(e => e.Nombre, "UQ__ROLES__B21D0AB961D97C63").IsUnique();

            entity.Property(e => e.IdRol).HasColumnName("ID_ROL");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Roles)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ROLES__ID_ESTADO__6477ECF3");
        });

        modelBuilder.Entity<TipoIdentificacion>(entity =>
        {
            entity.HasKey(e => e.IdTipo).HasName("PK_CAT_TIPO_ID");

            entity.ToTable("TIPO_IDENTIFICACION", "CAT");

            entity.Property(e => e.IdTipo).HasColumnName("ID_TIPO");
            entity.Property(e => e.DescTipo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("DESC_TIPO");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.TipoIdentificacions)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TIPO_IDEN__ID_ES__4F7CD00D");
        });

        modelBuilder.Entity<TipoMovimientoInv>(entity =>
        {
            entity.HasKey(e => e.IdTipo).HasName("PK_CAT_TIPO_MOV");

            entity.ToTable("TIPO_MOVIMIENTO_INV", "CAT");

            entity.HasIndex(e => e.Nombre, "UQ__TIPO_MOV__B21D0AB9EE563A67").IsUnique();

            entity.Property(e => e.IdTipo).HasColumnName("ID_TIPO");
            entity.Property(e => e.Nombre)
                .HasMaxLength(40)
                .IsUnicode(false)
                .HasColumnName("NOMBRE");
            entity.Property(e => e.Signo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("SIGNO");
        });

        modelBuilder.Entity<Turno>(entity =>
        {
            entity.HasKey(e => e.IdTurno).HasName("PK_CAJA_TURNOS");

            entity.ToTable("TURNOS", "CAJA");

            entity.HasIndex(e => e.IdUsuario, "IX_TURNOS_USUARIO");

            entity.HasIndex(e => e.IdUsuario, "UX_TURNO_ACTIVO")
                .IsUnique()
                .HasFilter("([ID_ESTADO]=(1))");

            entity.Property(e => e.IdTurno).HasColumnName("ID_TURNO");
            entity.Property(e => e.DiferenciaNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("DIFERENCIA_NIO");
            entity.Property(e => e.DiferenciaUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("DIFERENCIA_USD");
            entity.Property(e => e.EstadoCuadre)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ESTADO_CUADRE");
            entity.Property(e => e.FechaApertura)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_APERTURA");
            entity.Property(e => e.FechaCierre).HasColumnName("FECHA_CIERRE");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
            entity.Property(e => e.MontoContadoNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("MONTO_CONTADO_NIO");
            entity.Property(e => e.MontoContadoUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("MONTO_CONTADO_USD");
            entity.Property(e => e.MontoInicialNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("MONTO_INICIAL_NIO");
            entity.Property(e => e.MontoInicialUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("MONTO_INICIAL_USD");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .HasColumnName("OBSERVACIONES");
            entity.Property(e => e.TotalEfectivoNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_EFECTIVO_NIO");
            entity.Property(e => e.TotalEfectivoUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_EFECTIVO_USD");
            entity.Property(e => e.TotalTarjeta)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_TARJETA");
            entity.Property(e => e.TotalTransferencia)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_TRANSFERENCIA");
            entity.Property(e => e.TotalVentasNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_VENTAS_NIO");
            entity.Property(e => e.TotalVentasUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_VENTAS_USD");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Turnos)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TURNOS__ID_ESTAD__40F9A68C");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Turnos)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TURNOS__ID_USUAR__40058253");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PK_ADM_USUARIOS");

            entity.ToTable("USUARIOS", "ADM");

            entity.HasIndex(e => e.Username, "UQ__USUARIOS__B15BE12ED8A23C95").IsUnique();

            entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_CREACION");
            entity.Property(e => e.IdEmpleado).HasColumnName("ID_EMPLEADO");
            entity.Property(e => e.IdEstado)
                .HasDefaultValue(1)
                .HasColumnName("ID_ESTADO");
            entity.Property(e => e.IdRol).HasColumnName("ID_ROL");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(64)
                .HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.PasswordSalt)
                .HasMaxLength(32)
                .HasColumnName("PASSWORD_SALT");
            entity.Property(e => e.UltimoAcceso).HasColumnName("ULTIMO_ACCESO");
            entity.Property(e => e.IntentosFallidos)
                .HasDefaultValue(0)
                .HasColumnName("INTENTOS_FALLIDOS");
            entity.Property(e => e.Username)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("USERNAME");

            entity.HasOne(d => d.IdEmpleadoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdEmpleado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__USUARIOS__ID_EMP__08B54D69");

            entity.HasOne(d => d.IdEstadoNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdEstado)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__USUARIOS__ID_EST__0A9D95DB");

            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdRol)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__USUARIOS__ID_ROL__09A971A2");

            entity.HasMany(d => d.IdModulos).WithMany(p => p.IdUsuarios)
                .UsingEntity<Dictionary<string, object>>(
                    "UsuarioModulo",
                    r => r.HasOne<Modulo>().WithMany()
                        .HasForeignKey("IdModulo")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__USUARIO_M__ID_MO__0F624AF8"),
                    l => l.HasOne<Usuario>().WithMany()
                        .HasForeignKey("IdUsuario")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__USUARIO_M__ID_US__0E6E26BF"),
                    j =>
                    {
                        j.HasKey("IdUsuario", "IdModulo");
                        j.ToTable("USUARIO_MODULOS", "ADM");
                        j.IndexerProperty<int>("IdUsuario").HasColumnName("ID_USUARIO");
                        j.IndexerProperty<int>("IdModulo").HasColumnName("ID_MODULO");
                    });
        });

        modelBuilder.Entity<VGarantiasActiva>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_GARANTIAS_ACTIVAS", "GAR");

            entity.Property(e => e.Cliente)
                .HasMaxLength(163)
                .IsUnicode(false)
                .HasColumnName("CLIENTE");
            entity.Property(e => e.DiasRestantes).HasColumnName("DIAS_RESTANTES");
            entity.Property(e => e.EstadoGarantia)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ESTADO_GARANTIA");
            entity.Property(e => e.FechaInicio).HasColumnName("FECHA_INICIO");
            entity.Property(e => e.FechaVencimiento).HasColumnName("FECHA_VENCIMIENTO");
            entity.Property(e => e.IdGarantia).HasColumnName("ID_GARANTIA");
            entity.Property(e => e.MesesGarantia).HasColumnName("MESES_GARANTIA");

            entity.Property(e => e.Producto)
                .HasMaxLength(150)
                .HasColumnName("PRODUCTO");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("TELEFONO");
        });

        modelBuilder.Entity<VResumenTurno>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_RESUMEN_TURNOS", "CAJA");

            entity.Property(e => e.Cajero)
                .HasMaxLength(163)
                .IsUnicode(false)
                .HasColumnName("CAJERO");
            entity.Property(e => e.DiferenciaNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("DIFERENCIA_NIO");
            entity.Property(e => e.Estado)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("ESTADO");
            entity.Property(e => e.EstadoCuadre)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ESTADO_CUADRE");
            entity.Property(e => e.FechaApertura).HasColumnName("FECHA_APERTURA");
            entity.Property(e => e.FechaCierre).HasColumnName("FECHA_CIERRE");
            entity.Property(e => e.IdTurno).HasColumnName("ID_TURNO");
            entity.Property(e => e.MontoContadoNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("MONTO_CONTADO_NIO");
            entity.Property(e => e.MontoInicialNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("MONTO_INICIAL_NIO");
            entity.Property(e => e.TotalEfectivoNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_EFECTIVO_NIO");
            entity.Property(e => e.TotalEfectivoUsd)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_EFECTIVO_USD");
            entity.Property(e => e.TotalVentasNio)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_VENTAS_NIO");
        });

        modelBuilder.Entity<VResumenVenta>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_RESUMEN_VENTAS", "VEN");

            entity.Property(e => e.Anulada).HasColumnName("ANULADA");
            entity.Property(e => e.AperturaTurno).HasColumnName("APERTURA_TURNO");
            entity.Property(e => e.Cajero)
                .HasMaxLength(163)
                .IsUnicode(false)
                .HasColumnName("CAJERO");
            entity.Property(e => e.Cliente)
                .HasMaxLength(163)
                .IsUnicode(false)
                .HasColumnName("CLIENTE");
            entity.Property(e => e.DescuentoNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("DESCUENTO_NIO");
            entity.Property(e => e.FechaVenta).HasColumnName("FECHA_VENTA");
            entity.Property(e => e.IdVenta).HasColumnName("ID_VENTA");
            entity.Property(e => e.NumeroFactura)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("NUMERO_FACTURA");
            entity.Property(e => e.SubtotalNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("SUBTOTAL_NIO");
            entity.Property(e => e.TotalNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("TOTAL_NIO");
        });

        modelBuilder.Entity<VStockCritico>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("V_STOCK_CRITICO", "INV");

            entity.Property(e => e.Categoria)
                .HasMaxLength(80)
                .IsUnicode(false)
                .HasColumnName("CATEGORIA");
            entity.Property(e => e.EstadoStock)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("ESTADO_STOCK");
            entity.Property(e => e.IdProducto)
                .ValueGeneratedOnAdd()
                .HasColumnName("ID_PRODUCTO");
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .HasColumnName("MARCA");
            entity.Property(e => e.OrigenTipo)
                .HasMaxLength(100)
                .HasColumnName("ORIGEN_TIPO");
            entity.Property(e => e.Concentracion)
                .HasMaxLength(100)
                .HasColumnName("CONCENTRACION");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .HasColumnName("NOMBRE");
            entity.Property(e => e.StockActual).HasColumnName("STOCK_ACTUAL");
            entity.Property(e => e.StockMinimo).HasColumnName("STOCK_MINIMO");
        });

        modelBuilder.Entity<VClienteDashboardStat>(entity =>
        {
            entity.HasNoKey()
                .ToView("V_CLIENTE_DASHBOARD_STATS", "ADM");

            entity.Property(e => e.TotalClientes).HasColumnName("TotalClientes");
            entity.Property(e => e.TotalGarantiasActivas).HasColumnName("TotalGarantiasActivas");
            entity.Property(e => e.ClientesConComprasRecientes).HasColumnName("ClientesConComprasRecientes");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("PK_VEN_VENTAS");

            entity.ToTable("VENTAS", "VEN");

            entity.HasIndex(e => e.FechaVenta, "IX_VENTAS_FECHA");

            entity.HasIndex(e => e.IdPersona, "IX_VENTAS_PERSONA");

            entity.HasIndex(e => e.IdUsuario, "IX_VENTAS_USUARIO");

            entity.Property(e => e.IdVenta).HasColumnName("ID_VENTA");
            entity.Property(e => e.Anulada).HasColumnName("ANULADA");
            entity.Property(e => e.DescuentoNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("DESCUENTO_NIO");
            entity.Property(e => e.FechaAnulacion).HasColumnName("FECHA_ANULACION");
            entity.Property(e => e.FechaVenta)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA_VENTA");
            entity.Property(e => e.IdPersona).HasColumnName("ID_PERSONA");
            entity.Property(e => e.IdTurno).HasColumnName("ID_TURNO");
            entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
            entity.Property(e => e.IdUsuarioAnula).HasColumnName("ID_USUARIO_ANULA");
            entity.Property(e => e.MotivoAnulacion)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("MOTIVO_ANULACION");
            entity.Property(e => e.NumeroFactura)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasComputedColumnSql("('FAC-'+right('000000'+CONVERT([varchar],[ID_VENTA]),(6)))", true)
                .HasColumnName("NUMERO_FACTURA");
            entity.Property(e => e.Observacion)
                .HasMaxLength(300)
                .IsUnicode(false)
                .HasColumnName("OBSERVACION");
            entity.Property(e => e.SubtotalNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("SUBTOTAL_NIO");
            entity.Property(e => e.TasaCambioUsd)
                .HasColumnType("decimal(18, 6)")
                .HasColumnName("TASA_CAMBIO_USD");
            entity.Property(e => e.TotalNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("TOTAL_NIO");

            entity.HasOne(d => d.IdPersonaNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdPersona)
                .HasConstraintName("FK__VENTAS__ID_PERSO__503BEA1C");

            entity.HasOne(d => d.IdTurnoNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdTurno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__VENTAS__ID_TURNO__4E53A1AA");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.VentaIdUsuarioNavigations)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__VENTAS__ID_USUAR__4F47C5E3");

            entity.HasOne(d => d.IdUsuarioAnulaNavigation).WithMany(p => p.VentaIdUsuarioAnulaNavigations)
                .HasForeignKey(d => d.IdUsuarioAnula)
                .HasConstraintName("FK__VENTAS__ID_USUAR__51300E55");
        });

        modelBuilder.Entity<VentaDetalle>(entity =>
        {
            entity.HasKey(e => e.IdDetalle).HasName("PK_VEN_DETALLE");

            entity.ToTable("VENTA_DETALLE", "VEN");

            entity.HasIndex(e => e.IdVenta, "IX_DET_VENTA_ID");

            entity.Property(e => e.IdDetalle).HasColumnName("ID_DETALLE");
            entity.Property(e => e.Cantidad).HasColumnName("CANTIDAD");
            entity.Property(e => e.DescripcionSnap)
                .HasMaxLength(200)
                .HasColumnName("DESCRIPCION_SNAP");
            entity.Property(e => e.DescuentoLineaNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("DESCUENTO_LINEA_NIO");
            entity.Property(e => e.FechaVenceGarantia).HasColumnName("FECHA_VENCE_GARANTIA");
            entity.Property(e => e.IdPeriodoGarantia).HasColumnName("ID_PERIODO_GARANTIA");
            entity.Property(e => e.IdProducto).HasColumnName("ID_PRODUCTO");
            entity.Property(e => e.IdVenta).HasColumnName("ID_VENTA");
            entity.Property(e => e.PrecioUnitarioNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("PRECIO_UNITARIO_NIO");
            entity.Property(e => e.SubtotalNio)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("SUBTOTAL_NIO");

            entity.HasOne(d => d.IdPeriodoGarantiaNavigation).WithMany(p => p.VentaDetalles)
                .HasForeignKey(d => d.IdPeriodoGarantia)
                .HasConstraintName("FK__VENTA_DET__ID_PE__56E8E7AB");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.VentaDetalles)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__VENTA_DET__ID_PR__55F4C372");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.VentaDetalles)
                .HasForeignKey(d => d.IdVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__VENTA_DET__ID_VE__55009F39");
        });

        modelBuilder.Entity<MovimientoVario>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("PK_CAJA_MOV_VARIOS");

            entity.ToTable("MOVIMIENTOS_VARIOS", "CAJA");

            entity.Property(e => e.IdMovimiento).HasColumnName("ID_MOVIMIENTO");
            entity.Property(e => e.Concepto)
                .HasMaxLength(200)
                .HasColumnName("CONCEPTO");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("FECHA");
            entity.Property(e => e.IdMoneda).HasColumnName("ID_MONEDA");
            entity.Property(e => e.IdTurno).HasColumnName("ID_TURNO");
            entity.Property(e => e.IdUsuario).HasColumnName("ID_USUARIO");
            entity.Property(e => e.Monto)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("MONTO");
            entity.Property(e => e.Tipo)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("INGRESO")
                .HasColumnName("TIPO");

            entity.HasOne(d => d.IdMonedaNavigation).WithMany(p => p.MovimientosVarios)
                .HasForeignKey(d => d.IdMoneda)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MOVIMIENT__ID_MO__4E53A1AA");

            entity.HasOne(d => d.IdTurnoNavigation).WithMany(p => p.MovimientosVarios)
                .HasForeignKey(d => d.IdTurno)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MOVIMIENT__ID_TU__4D5F7D71");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.MovimientosVarios)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MOVIMIENT__ID_US__4F47C5E3");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}



