SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

CREATE OR ALTER PROCEDURE [VEN].[sp_ReversoTransaccion]
    @IdVenta INT,
    @IdUsuario INT,
    @Motivo NVARCHAR(200),
    @DetalleJson NVARCHAR(MAX) = NULL -- Si es NULL o '[]', es reverso total. Si tiene items, es reverso parcial.
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @IdTurno INT;
    DECLARE @UsuarioVenta INT;
    DECLARE @FechaVenta DATE;
    DECLARE @YaAnulada BIT;
    DECLARE @IdCondicionPago INT;
    DECLARE @IdPersona INT;
    DECLARE @TasaCambioUsd DECIMAL(18,6);
    DECLARE @TotalVenta DECIMAL(12,2);
    
    -- 1. Obtener datos de la venta
    SELECT 
        @UsuarioVenta = ID_USUARIO,
        @FechaVenta = CAST(FECHA_VENTA AS DATE),
        @YaAnulada = ANULADA,
        @IdCondicionPago = ID_CONDICION_PAGO,
        @IdPersona = ID_PERSONA,
        @TasaCambioUsd = NULLIF(TASA_CAMBIO_USD, 0),
        @TotalVenta = TOTAL_NIO
    FROM VEN.VENTAS
    WHERE ID_VENTA = @IdVenta;

    IF @UsuarioVenta IS NULL
        THROW 50001, 'La factura especificada no existe.', 1;

    IF @YaAnulada = 1
        THROW 50002, 'La factura ya ha sido anulada previamente.', 1;

    IF @FechaVenta <> CAST(GETDATE() AS DATE)
        THROW 50003, 'Solo se pueden reversar transacciones realizadas el día de hoy.', 1;

    IF @UsuarioVenta <> @IdUsuario
        THROW 50004, 'Regla estricta: Solo el cajero que registró la venta puede reversarla.', 1;

    -- Verificar Turno Abierto
    SELECT @IdTurno = ID_TURNO 
    FROM CAJA.TURNOS 
    WHERE ID_USUARIO = @IdUsuario AND ID_ESTADO = 1;

    IF @IdTurno IS NULL
        THROW 50005, 'Debe tener un turno de caja activo para procesar el reverso.', 1;

    BEGIN TRANSACTION;
    
    DECLARE @EsTotal BIT = 1;
    IF @DetalleJson IS NOT NULL AND LEN(@DetalleJson) > 2 AND @DetalleJson <> '[]'
        SET @EsTotal = 0;

    -- Variables para sumar lo que se reversa
    DECLARE @MontoReversoNio DECIMAL(12,2) = 0;
    
    -- Procesar Items
    IF @EsTotal = 1
    BEGIN
        -- REVERSO TOTAL
        SET @MontoReversoNio = @TotalVenta;
        
        -- Marcar todo como devuelto
        UPDATE VEN.VENTA_DETALLE SET DEVUELTO = 1 WHERE ID_VENTA = @IdVenta AND DEVUELTO = 0;
        
        -- Retornar TODO al stock
        UPDATE P
        SET P.STOCK_ACTUAL = P.STOCK_ACTUAL + VD.CANTIDAD
        FROM INV.PRODUCTOS P
        INNER JOIN VEN.VENTA_DETALLE VD ON P.ID_PRODUCTO = VD.ID_PRODUCTO
        WHERE VD.ID_VENTA = @IdVenta;

        -- Marcar Venta como Anulada
        UPDATE VEN.VENTAS 
        SET ANULADA = 1, 
            ID_USUARIO_ANULA = @IdUsuario, 
            MOTIVO_ANULACION = @Motivo, 
            FECHA_ANULACION = GETDATE()
        WHERE ID_VENTA = @IdVenta;
    END
    ELSE
    BEGIN
        -- REVERSO PARCIAL
        -- Iterar sobre el JSON (lista de IDs de detalle)
        DECLARE @IdDetalle INT;
        DECLARE cur CURSOR FOR
        SELECT CAST(value AS INT) FROM OPENJSON(@DetalleJson);
        
        OPEN cur;
        FETCH NEXT FROM cur INTO @IdDetalle;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Verificar que el detalle pertenezca a la venta y no esté devuelto
            DECLARE @Valido BIT = 0;
            DECLARE @IdProd INT;
            DECLARE @Cant INT;
            DECLARE @SubtotalDetalle DECIMAL(12,2);
            
            SELECT @Valido = 1, @IdProd = ID_PRODUCTO, @Cant = CANTIDAD, @SubtotalDetalle = SUBTOTAL_NIO
            FROM VEN.VENTA_DETALLE 
            WHERE ID_DETALLE = @IdDetalle AND ID_VENTA = @IdVenta AND DEVUELTO = 0;
            
            IF @Valido = 1
            BEGIN
                -- Marcar devuelto
                UPDATE VEN.VENTA_DETALLE SET DEVUELTO = 1 WHERE ID_DETALLE = @IdDetalle;
                
                -- Retornar Stock
                UPDATE INV.PRODUCTOS SET STOCK_ACTUAL = STOCK_ACTUAL + @Cant WHERE ID_PRODUCTO = @IdProd;
                
                -- Sumar al monto de reverso
                SET @MontoReversoNio = @MontoReversoNio + @SubtotalDetalle;
            END
            
            FETCH NEXT FROM cur INTO @IdDetalle;
        END
        CLOSE cur;
        DEALLOCATE cur;
        
        -- Poner observacion en la factura
        UPDATE VEN.VENTAS 
        SET OBSERVACION = ISNULL(OBSERVACION + ' | ', '') + 'Reverso Parcial C$ ' + CAST(@MontoReversoNio AS VARCHAR) + ' - Motivo: ' + @Motivo
        WHERE ID_VENTA = @IdVenta;
        
        -- Si devolvimos todo (monto igual), marcarla como anulada
        IF @MontoReversoNio >= @TotalVenta
        BEGIN
            UPDATE VEN.VENTAS 
            SET ANULADA = 1, 
                ID_USUARIO_ANULA = @IdUsuario, 
                MOTIVO_ANULACION = @Motivo, 
                FECHA_ANULACION = GETDATE()
            WHERE ID_VENTA = @IdVenta;
        END
    END

    -- Reversar Pagos/Turno y Creditos si hubo monto reversado
    IF @MontoReversoNio > 0
    BEGIN
        IF @IdCondicionPago = 2 -- Crédito
        BEGIN
            IF @EsTotal = 1
            BEGIN
                UPDATE VEN.CREDITOS SET ESTADO = 'ANULADO' WHERE ID_VENTA = @IdVenta;
                UPDATE ADM.CLIENTES_CREDITO SET SALDO_ACTUAL = SALDO_ACTUAL - @MontoReversoNio WHERE ID_PERSONA = @IdPersona;
            END
            ELSE
            BEGIN
                UPDATE VEN.CREDITOS SET SALDO_PENDIENTE = SALDO_PENDIENTE - @MontoReversoNio WHERE ID_VENTA = @IdVenta;
                UPDATE ADM.CLIENTES_CREDITO SET SALDO_ACTUAL = SALDO_ACTUAL - @MontoReversoNio WHERE ID_PERSONA = @IdPersona;
                
                -- Validar si el crédito bajó de 0, ajustar (en teoría no debería pasar)
            END
        END
        ELSE -- Contado (afecta turno)
        BEGIN
            -- Restar el monto a efectivo en el Turno actual (asumimos devolución en efectivo por simplicidad)
            DECLARE @MontoReversoUsd DECIMAL(12,2) = @MontoReversoNio / @TasaCambioUsd;
            
            UPDATE CAJA.TURNOS 
            SET TOTAL_VENTAS_NIO = TOTAL_VENTAS_NIO - @MontoReversoNio,
                TOTAL_VENTAS_USD = TOTAL_VENTAS_USD - @MontoReversoUsd,
                TOTAL_EFECTIVO_NIO = TOTAL_EFECTIVO_NIO - @MontoReversoNio
            WHERE ID_TURNO = @IdTurno;
            
            -- Registrar el movimiento de egreso para cuadre histórico
            INSERT INTO CAJA.MOVIMIENTOS_VARIOS (ID_TURNO, ID_USUARIO, ID_MONEDA, TIPO, MONTO, CONCEPTO, FECHA)
            VALUES (@IdTurno, @IdUsuario, 1, 'EGRESO', @MontoReversoNio, 'Reverso/Devolución de Fac ' + CAST(@IdVenta AS VARCHAR) + ' - ' + @Motivo, GETDATE());
        END
    END
    
    -- Invalidar Garantías asociadas a los items devueltos
    UPDATE G
    SET ESTADO_GARANTIA = 'CANCELADA'
    FROM GAR.GARANTIAS G
    INNER JOIN VEN.VENTA_DETALLE VD ON G.ID_DETALLE_VENTA = VD.ID_DETALLE
    WHERE VD.ID_VENTA = @IdVenta AND VD.DEVUELTO = 1;

    COMMIT TRANSACTION;
    
    SELECT 1 AS OK;
END;
GO
