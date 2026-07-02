CREATE PROCEDURE [VEN].[sp_ReversoTransaccion]
    @IdVenta INT,
    @IdUsuario INT,
    @Motivo NVARCHAR(200),
    @DetalleJson NVARCHAR(MAX) = NULL 
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @IdTurno INT;
    DECLARE @UsuarioVenta INT;
    DECLARE @FechaVenta DATE;
    DECLARE @YaAnulada BIT;
    DECLARE @IdCondicionPago INT;
    DECLARE @IdPersona INT;
    DECLARE @TasaCambioUsd DECIMAL(18,6);
    DECLARE @TotalVenta DECIMAL(12,2);
    
    SELECT 
        @UsuarioVenta = ID_USUARIO,
        @FechaVenta = CAST(FECHA_VENTA AS DATE),
        @YaAnulada = ANULADA,
        @IdCondicionPago = ID_CONDICION_PAGO,
        @IdPersona = ID_PERSONA,
        @TasaCambioUsd = NULLIF(TASA_CAMBIO_USD, 0),
        @TotalVenta = TOTAL_BASE
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

    SELECT @IdTurno = ID_TURNO FROM CAJA.TURNOS WHERE ID_USUARIO = @IdUsuario AND ID_ESTADO = 1;

    IF @IdTurno IS NULL
        THROW 50005, 'Debe tener un turno de caja activo para procesar el reverso.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;
        
        DECLARE @EsTotal BIT = 1;
        IF @DetalleJson IS NOT NULL AND LEN(@DetalleJson) > 2 AND @DetalleJson <> '[]'
            SET @EsTotal = 0;

        DECLARE @MontoReversoBase DECIMAL(12,2) = 0;
        
        IF @EsTotal = 1
        BEGIN
            SET @MontoReversoBase = @TotalVenta;
            UPDATE VEN.VENTA_DETALLE SET DEVUELTO = 1 WHERE ID_VENTA = @IdVenta AND DEVUELTO = 0;
            UPDATE P SET P.STOCK_ACTUAL = P.STOCK_ACTUAL + VD.CANTIDAD FROM INV.PRODUCTOS P INNER JOIN VEN.VENTA_DETALLE VD ON P.ID_PRODUCTO = VD.ID_PRODUCTO WHERE VD.ID_VENTA = @IdVenta;
            UPDATE VEN.VENTAS SET ANULADA = 1, ID_USUARIO_ANULA = @IdUsuario, MOTIVO_ANULACION = @Motivo, FECHA_ANULACION = GETDATE() WHERE ID_VENTA = @IdVenta;
        END
        ELSE
        BEGIN
            DECLARE @IdDetalle INT;
            DECLARE cur CURSOR FOR SELECT CAST(value AS INT) FROM OPENJSON(@DetalleJson);
            OPEN cur;
            FETCH NEXT FROM cur INTO @IdDetalle;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                DECLARE @Valido BIT = 0;
                DECLARE @IdProd INT;
                DECLARE @Cant INT;
                DECLARE @SubtotalDetalle DECIMAL(12,2);
                SELECT @Valido = 1, @IdProd = ID_PRODUCTO, @Cant = CANTIDAD, @SubtotalDetalle = SUBTOTAL_BASE FROM VEN.VENTA_DETALLE WHERE ID_DETALLE = @IdDetalle AND ID_VENTA = @IdVenta AND DEVUELTO = 0;
                IF @Valido = 1
                BEGIN
                    UPDATE VEN.VENTA_DETALLE SET DEVUELTO = 1 WHERE ID_DETALLE = @IdDetalle;
                    UPDATE INV.PRODUCTOS SET STOCK_ACTUAL = STOCK_ACTUAL + @Cant WHERE ID_PRODUCTO = @IdProd;
                    SET @MontoReversoBase = @MontoReversoBase + @SubtotalDetalle;
                END
                FETCH NEXT FROM cur INTO @IdDetalle;
            END
            CLOSE cur;
            DEALLOCATE cur;
            UPDATE VEN.VENTAS SET OBSERVACION = ISNULL(OBSERVACION + ' | ', '') + 'Reverso Parcial C$ ' + CAST(@MontoReversoBase AS VARCHAR) + ' - Motivo: ' + @Motivo WHERE ID_VENTA = @IdVenta;
            IF @MontoReversoBase >= @TotalVenta
            BEGIN
                UPDATE VEN.VENTAS SET ANULADA = 1, ID_USUARIO_ANULA = @IdUsuario, MOTIVO_ANULACION = @Motivo, FECHA_ANULACION = GETDATE() WHERE ID_VENTA = @IdVenta;
            END
        END

        IF @MontoReversoBase > 0
        BEGIN
            IF @IdCondicionPago = 2 
            BEGIN
                IF @
