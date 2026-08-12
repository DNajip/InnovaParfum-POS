SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [VEN].[sp_ProcesarVenta2]
    @IdUsuario INT,
    @IdPersona INT = NULL,
    @IdTipoVenta INT,
    @IdCondicionPago INT,
    @DescuentoBase DECIMAL(12,2),
    @TasaCambioUsd DECIMAL(18,6),
    @ItemsJson NVARCHAR(MAX),
    @PaymentsJson NVARCHAR(MAX),
    @MonedaVuelto CHAR(3) = 'NIO'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @IdVenta INT;
    DECLARE @FechaActual DATETIME = SYSDATETIME();
    DECLARE @IdTurno INT;

    SELECT @IdTurno = ID_TURNO FROM CAJA.TURNOS WHERE ID_USUARIO = @IdUsuario AND ID_ESTADO = 1;

    IF @IdTurno IS NULL
    BEGIN
        THROW 50001, 'No hay un turno de caja abierto para este usuario.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @SubtotalBase DECIMAL(12,2);
        SELECT @SubtotalBase = SUM(CAST(JSON_VALUE(item.[value], '$.SubTotal') AS DECIMAL(12,2))) FROM OPENJSON(@ItemsJson) AS item;
        DECLARE @TotalVentaBase DECIMAL(12,2) = @SubtotalBase - @DescuentoBase;

        DECLARE @TotalPagadoBase DECIMAL(12,2) = 0;
        SELECT @TotalPagadoBase = ISNULL(SUM(CAST(JSON_VALUE(p.[value], '$.MontoEnMonedaBase') AS DECIMAL(12,2))), 0) FROM OPENJSON(@PaymentsJson) AS p;
        
        DECLARE @SaldoPendiente DECIMAL(12,2) = 0;
        IF @TotalPagadoBase < @TotalVentaBase
        BEGIN
            SET @SaldoPendiente = @TotalVentaBase - @TotalPagadoBase;
        END

        INSERT INTO VEN.VENTAS (ID_TURNO, ID_USUARIO, ID_PERSONA, FECHA_VENTA, TASA_CAMBIO_USD, SUBTOTAL_BASE, DESCUENTO_BASE, TOTAL_BASE, ANULADA, ID_TIPO_VENTA, ID_CONDICION_PAGO, MONEDA_VUELTO)
        VALUES (@IdTurno, @IdUsuario, @IdPersona, @FechaActual, @TasaCambioUsd, @SubtotalBase, @DescuentoBase, @TotalVentaBase, 0, @IdTipoVenta, @IdCondicionPago, @MonedaVuelto);
        SET @IdVenta = SCOPE_IDENTITY();

        IF @IdCondicionPago = 2 AND @SaldoPendiente > 0 AND @IdPersona IS NOT NULL
        BEGIN
            DECLARE @DiasCredito INT = 30;
            SELECT @DiasCredito = DIAS_CREDITO FROM ADM.CLIENTES_CREDITO WHERE ID_PERSONA = @IdPersona;
            DECLARE @FechaVencimiento DATE = DATEADD(DAY, @DiasCredito, @FechaActual);
            
            INSERT INTO VEN.CREDITOS (ID_VENTA, ID_PERSONA, MONTO_ORIGINAL, SALDO_PENDIENTE, FECHA_CREDITO, FECHA_VENCIMIENTO, ESTADO)
            VALUES (@IdVenta, @IdPersona, @SaldoPendiente, @SaldoPendiente, CAST(@FechaActual AS DATE), @FechaVencimiento, 'ACTIVO');

            UPDATE ADM.CLIENTES_CREDITO SET SALDO_ACTUAL = SALDO_ACTUAL + @SaldoPendiente WHERE ID_PERSONA = @IdPersona;
        END

        DECLARE @VueltoTotalBase DECIMAL(12,2) = CASE WHEN @TotalPagadoBase > @TotalVentaBase THEN @TotalPagadoBase - @TotalVentaBase ELSE 0 END;

        IF @VueltoTotalBase > 0
        BEGIN
            IF EXISTS (
                SELECT 1 FROM OPENJSON(@PaymentsJson) pj JOIN CAT.METODOS_PAGO mp ON mp.ID_METODO = CAST(JSON_VALUE(pj.[value], '$.IdMetodoPago') AS INT) WHERE mp.NOMBRE NOT LIKE '%EFECTIVO%'
            )
            BEGIN
                DECLARE @NotaAuto NVARCHAR(200) = 'Vuelto de ' + CAST(@VueltoTotalBase AS NVARCHAR(20)) + ' entregado en efectivo por sobrepago en método no-efectivo.';
                UPDATE VEN.VENTAS SET OBSERVACION = ISNULL(OBSERVACION + ' | ', '') + @NotaAuto WHERE ID_VENTA = @IdVenta;
            END
        END

        DECLARE @UltimoIdPago INT;
        INSERT INTO VEN.PAGOS (ID_VENTA, ID_METODO_PAGO, MONTO_PAGADO, TASA_APLICADA, MONTO_EN_BASE, MONTO_RECIBIDO, VUELTO_BASE, VUELTO_MOSTRADO, COD_REFERENCIA, FECHA_PAGO)
        SELECT @IdVenta, CAST(JSON_VALUE(p.[value], '$.IdMetodoPago') AS INT), CAST(JSON_VALUE(p.[value], '$.Monto') AS DECIMAL(12,2)), CAST(JSON_VALUE(p.[value], '$.TasaCambio') AS DECIMAL(12,4)), CAST(JSON_VALUE(p.[value], '$.MontoEnMonedaBase') AS DECIMAL(12,2)), CAST(JSON_VALUE(p.[value], '$.MontoEnNio') AS DECIMAL(12,2)), 0, 0, JSON_VALUE(p.[value], '$.Referencia'), @FechaActual
        FROM OPENJSON(@PaymentsJson) AS p;

        IF @VueltoTotalBase > 0
        BEGIN
            SELECT TOP 1 @UltimoIdPago = ID_PAGO FROM VEN.PAGOS WHERE ID_VENTA = @IdVenta ORDER BY ID_PAGO DESC;
            DECLARE @VueltoMostrado DECIMAL(12,2) = 0;
            SELECT TOP 1 @VueltoMostrado = ISNULL(CAST(JSON_VALUE(pj.[value], '$.VueltoMostrado') AS DECIMAL(12,2)), 0) FROM OPENJSON(@PaymentsJson) pj WHERE ISNULL(CAST(JSON_VALUE(pj.[value], '$.VueltoMostrado') AS DECIMAL(12,2)), 0) > 0;
            UPDATE VEN.PAGOS SET VUELTO_BASE = @VueltoTotalBase, VUELTO_MOSTRADO = @VueltoMostrado WHERE ID_PAGO = @UltimoIdPago;
        END

        UPDATE T SET 
            T.TOTAL_EFECTIVO_BASE += (ISNULL((SELECT SUM(CAST(JSON_VALUE(pj.[value], '$.MontoEnNio') AS DECIMAL(12,2))) FROM OPENJSON(@PaymentsJson) pj JOIN CAT.METODOS_PAGO mp ON mp.ID_METODO = CAST(JSON_VALUE(pj.[value], '$.IdMetodoPago') AS INT) JOIN CAT.MONEDAS m ON m.ID_MONEDA = mp.ID_MONEDA WHERE mp.NOMBRE LIKE '%EFECTIVO%' AND m.CODIGO = 'NIO'), 0) - (CASE WHEN @MonedaVuelto = 'NIO' THEN ISNULL(@VueltoMostrado, 0) ELSE 0 END)),
            T.TOTAL_EFECTIVO_USD += (ISNULL((SELECT SUM(CAST(JSON_VALUE(pj.[value], '$.Monto') AS DECIMAL(12,2))) FROM OPENJSON(@PaymentsJson) pj JOIN CAT.METODOS_PAGO mp ON mp.ID_METODO = CAST(JSON_VALUE(pj.[value], '$.IdMetodoPago') AS INT) JOIN CAT.MONEDAS m ON m.ID_MONEDA = mp.ID_MONEDA WHERE mp.NOMBRE LIKE '%EFECTIVO%' AND m.CODIGO = 'USD'), 0) - (CASE WHEN @MonedaVuelto = 'USD' THEN ISNULL(@VueltoMostrado, 0) ELSE 0 END)),
            T.TOTAL_TARJETA += ISNULL((SELECT SUM(CAST(JSON_VALUE(pj.[value], '$.MontoEnNio') AS DECIMAL(12,2))) FROM OPENJSON(@PaymentsJson) pj JOIN CAT.METODOS_PAGO mp ON mp.ID_METODO = CAST(JSON_VALUE(pj.[value], '$.IdMetodoPago') AS INT) WHERE mp.NOMBRE LIKE '%TARJETA%'), 0),
            T.TOTAL_TRANSFERENCIA += ISNULL((SELECT SUM(CAST(JSON_VALUE(pj.[value], '$.MontoEnNio') AS DECIMAL(12,2))) FROM OPENJSON(@PaymentsJson) pj JOIN CAT.METODOS_PAGO mp ON mp.ID_METODO = CAST(JSON_VALUE(pj.[value], '$.IdMetodoPago') AS INT) WHERE mp.NOMBRE LIKE '%TRANSFERENCIA%'), 0),
            T.TOTAL_VENTAS_BASE += ISNULL((SELECT SUM(CAST(JSON_VALUE(p.[value], '$.MontoEnMonedaBase') AS DECIMAL(12,2))) FROM OPENJSON(@PaymentsJson) AS p) - @VueltoTotalBase, 0),
            T.TOTAL_VENTAS_USD += (ISNULL((SELECT SUM(CAST(JSON_VALUE(p.[value], '$.MontoEnMonedaBase') AS DECIMAL(12,2))) FROM OPENJSON(@PaymentsJson) AS p) - @VueltoTotalBase, 0) / NULLIF(@TasaCambioUsd, 0))
        FROM CAJA.TURNOS T WHERE T.ID_TURNO = @IdTurno;

        DECLARE @IdProducto INT, @DescSnap NVARCHAR(200), @UnitPrice DECIMAL(12,2), @IdPeriodo INT, @Meses INT, @EsRegalia BIT, @CostoUnitario DECIMAL(12,2);
        DECLARE @IdDetalle INT;
        DECLARE detail_cursor CURSOR FOR
        SELECT CAST(JSON_VALUE(i.[value], '$.IdProducto') AS INT), JSON_VALUE(i.[value], '$.Description'), CAST(JSON_VALUE(i.[value], '$.UnitPrice') AS DECIMAL(12,2)), CAST(JSON_VALUE(d.[value], '$.IdPeriodoGarantia') AS INT), PG.MESES, CAST(ISNULL(JSON_VALUE(i.[value], '$.IsRegalia'), 'false') AS BIT), CAST(ISNULL(JSON_VALUE(i.[value], '$.CostoUnitario'), '0') AS DECIMAL(12,2)) FROM OPENJSON(@ItemsJson) AS i CROSS APPLY OPENJSON(i.[value], '$.Details') AS d JOIN CAT.PERIODOS_GARANTIA PG ON PG.ID_PERIODO = CAST(JSON_VALUE(d.[value], '$.IdPeriodoGarantia') AS INT);

        OPEN detail_cursor;
        FETCH NEXT FROM detail_cursor INTO @IdProducto, @DescSnap, @UnitPrice, @IdPeriodo, @Meses, @EsRegalia, @CostoUnitario;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @StockActual INT;
            SELECT @StockActual = STOCK_ACTUAL FROM INV.PRODUCTOS WHERE ID_PRODUCTO = @IdProducto;
            
            IF @StockActual < 1
            BEGIN
                CLOSE detail_cursor;
                DEALLOCATE detail_cursor;
                THROW 50003, 'Stock agotado', 1;
            END

            UPDATE INV.PRODUCTOS SET STOCK_ACTUAL = STOCK_ACTUAL - 1 WHERE ID_PRODUCTO = @IdProducto;
            DECLARE @FechaVence DATE = NULL;
            IF @Meses > 0 SET @FechaVence = DATEADD(MONTH, @Meses, @FechaActual);

            INSERT INTO VEN.VENTA_DETALLE (ID_VENTA, ID_PRODUCTO, DESCRIPCION_SNAP, CANTIDAD, PRECIO_UNITARIO_BASE, SUBTOTAL_BASE, ID_PERIODO_GARANTIA, FECHA_VENCE_GARANTIA, ES_REGALIA, COSTO_UNITARIO_NIO)
            VALUES (@IdVenta, @IdProducto, @DescSnap, 1, @UnitPrice, @UnitPrice, @IdPeriodo, @FechaVence, @EsRegalia, @CostoUnitario);
            SET @IdDetalle = SCOPE_IDENTITY();

            IF @IdPersona IS NOT NULL AND @Meses > 0
            BEGIN
                INSERT INTO GAR.GARANTIAS (ID_DETALLE_VENTA, ID_PERSONA, ID_PRODUCTO, MESES_GARANTIA, FECHA_INICIO, FECHA_VENCIMIENTO, ESTADO_GARANTIA)
                VALUES (@IdDetalle, @IdPersona, @IdProducto, @Meses, CAST(@FechaActual AS DATE), @FechaVence, 'ACTIVA');
            END
            FETCH NEXT FROM detail_cursor INTO @IdProducto, @DescSnap, @UnitPrice, @IdPeriodo, @Meses, @EsRegalia, @CostoUnitario;
        END

        CLOSE detail_cursor;
        DEALLOCATE detail_cursor;

        COMMIT TRANSACTION;
        SELECT * FROM VEN.VENTAS WHERE ID_VENTA = @IdVenta;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;


