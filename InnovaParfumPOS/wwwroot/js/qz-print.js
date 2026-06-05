// Configuración de Seguridad para QZ Tray (Impresión Silenciosa)
const qzCertificate = `-----BEGIN CERTIFICATE-----
MIICrjCCAZagAwIBAgIIBeJKdkgYhD8wDQYJKoZIhvcNAQELBQAwFzEVMBMGA1UEAxMMSW5ub3Zh
VGVjUE9TMB4XDTI2MDQyOTAzNTYwNVoXDTM2MDQyOTAzNTYwNVowFzEVMBMGA1UEAxMMSW5ub3Zh
VGVjUE9TMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAziXhneLzmivwvOIk8yge5b8V
E77aj1cCr7sxfgok0oPvpzo+CWVO/yol71fBOikyWMsry3z2rQEpf2DCCxJ2jG1eTHHxlstlJV8d
P1hsf50W7yFXNE3uq3WXGyKdRYn9/Zrdm8LkIpeA/tV7mY6Fsi9ZXMDIFj/h9B7Wc+H86Lh7Hvyj
WnIVhNW3PDFsSjlu9t3uNC5BVH98a1xlVAPCuDtXwG+ZLuDWPqDv3fnlOwwWPritD2BuFlqTfweX
JFpA3V++sy+aPdsd9TmjoIHmtf1781fO1Vk+zTZ88k5nksYru+P8iFHtFA03xUn2v89n922nipru
22McOgU+c+iUQQIDAQABMA0GCSqGSIb3DQEBCwUAA4IBAQDG/4KG5HQ45Q5E9Ag+va8qQdN+g8oA
/8cQL07yjdE1nCPuedvVLGgQULkQkryjByEMVSCEYb9x8z0yfp0IHMjOib0jJbv64K4o6xEzOqKl
cqFP9A3mmVIr0boN1KgxRvZux+owiROxwXmHIl07yRGn1UMt3S76bu1r2GlhcBQOzmIPGFlK50u3
lgBEEzFc4kf7LuK+/31ZanqRMnrXBYOEguo5kElVbsiqSg3Oqj2shXxO+dcwLCDiBIm+k4louIU8
anPssvo+Qpa3wf2MLcU5jhHWHCrrIIDJ2In/xJtqmXDlVlHaO/LC+UPzkpwXByusuAJNhI5+3pBw
bJvbYnSj
-----END CERTIFICATE-----`;

const qzPrivateKey = `-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQDOJeGd4vOaK/C84iTzKB7lvxUT
vtqPVwKvuzF+CiTSg++nOj4JZU7/KiXvV8E6KTJYyyvLfPatASl/YMILEnaMbV5McfGWy2UlXx0/
WGx/nRbvIVc0Te6rdZcbIp1Fif39mt2bwuQil4D+1XuZjoWyL1lcwMgWP+H0HtZz4fzouHse/KNa
chWE1bc8MWxKOW723e40LkFUf3xrXGVUA8K4O1fAb5ku4NY+oO/d+eU7DBY+uK0PYG4WWpN/B5ck
WkDdX76zL5o92x31OaOggea1/XvzV87VWT7NNnzyTmeSxiu74/yIUe0UDTfFSfa/z2f3baeKmu7b
Yxw6BT5z6JRBAgMBAAECggEAROMLvcL1PHOqgrPfPRIPIR71YB/K0VN2Jlsbcs7Y9y/3iZ5A1Mrx
1vqyqoRcoZ7aoClkfR9JHw7gWxxuO1z0GIEDnfAFlypoplBeaaiWuR45Z7dweJ4CP3GQCRVMEMzH
/1Mc8svxiE2wYXfdUbb6nkyMRB53vleinf0gFKFT7j/98EwPt+Q0bRVjg2vVacrYHCd0rCknpXwf
RW18OjuLrR6S1TjoOTy2+FCpk5YuRugnAr5TBAxVYOMBzcVP8m+D2144oTOnWTxn6NSiQXULJAJL
ypGERkFncuZV92KJjH8OGBBsogFjb2qwhhsW7wACvidpIf4rrbNVAKUnCY+vMQKBgQDrZrHKMEFi
HWdqLucpoHZbknlO5biLrIjWNdXlI0WVrIzI+e/jBaUxemf4LLaD6oCmSxdZA8BYS4hVypeNExKY
ZXoNQtvFF1uMeXLi+KIjCov9eaX8bMZpBoEG9h32ZE+0x+jDgH57K1lwt0y3/1SqKmL2KZ2Xy5cl
lXGEVnAd9wKBgQDgL+A9ahv7uTxzMmxuRPLjDl0Jr3YV/lQEILWREsYORWbU3jVNfdqeTUZFfqyE
XtMAWm92tkyy7n47c4xC1at1p8zFaUOB9R/vT/UxpTeRoFhLN/AGCC+MRONGCb4/iSqL0UHpp2Mp
hKl90oSA+8frIYIfAovuswpakKIJ5jexhwKBgQDio/slCZTJ3DmHGCVkBKQvwjSda7tkbvdIFokb
lfwXAQyDK3B1CShlHM8hOBt5oru+X6nZAC2eVQqsXuPO5cAPjhQW1Ho3pid0djHQqUWhqzPhFdBj
2m6lC6gKhcePREEhcx99qycbez8fsLtio6hmNW0WSDb8wP2DKAElQvurPQKBgGbi8fmdgfwzld+U
a5jrwcCcrews/3e2gd5nGIzc3dJc3YWh+Tp4IOX3tuFb8lbJofKOjosfvPF9bKdiLyPZJdhYSyzq
U1YIJkDRJElFdsw5l2vl3x0VkqTAVMGm5Q4JqGIEhkhyTpsWTCb3f2Imqyho92u94nSI7J6FtCfw
5OxvAoGAfpCWALqe/61e+93jsFzNiPLOxwdbokZsO3f+Ln6ij07HyIxRyUgfmYCHgDpMeDJUzmQ7
X5Rvw+Xznx6UHaWeWDUYBYDaOT37FQrH2VdaWgXgAZoyD0QkG1E5wGjBs3PWA+GYxQSE+/tXGsZz
aQjG21+4tFnndaJGco6PoMHN+n4=
-----END PRIVATE KEY-----`;

// Configurar certificados en QZ
qz.security.setCertificatePromise((resolve, reject) => {
    resolve(qzCertificate);
});

qz.security.setSignatureAlgorithm("SHA512");

qz.security.setSignaturePromise((toSign) => {
    return (resolve, reject) => {
        try {
            const pk = KEYUTIL.getKey(qzPrivateKey);
            const sig = new KJUR.crypto.Signature({ "alg": "SHA512withRSA" });
            sig.init(pk);
            sig.updateString(toSign);
            const hex = sig.sign();
            resolve(stob64(hextorstr(hex)));
        } catch (err) {
            console.error("Error firmando solicitud QZ:", err);
            reject(err);
        }
    };
});

// Función para redimensionar imagen usando Canvas
function resizeImage(url, maxWidth, maxHeight) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.crossOrigin = "Anonymous";
        img.onload = () => {
            const canvas = document.createElement("canvas");
            let width = img.width;
            let height = img.height;

            if (width > maxWidth) {
                height *= maxWidth / width;
                width = maxWidth;
            }
            if (height > maxHeight) {
                width *= maxHeight / height;
                height = maxHeight;
            }

            canvas.width = width;
            canvas.height = height;
            const ctx = canvas.getContext("2d");
            ctx.drawImage(img, 0, 0, width, height);
            resolve(canvas.toDataURL("image/png"));
        };
        img.onerror = reject;
        img.src = url;
    });
}

window.qzPrintInvoice = async (invoice, printerName) => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }

        let printer = printerName;
        
        if (!printer) {
            // Fallback si no viene nombre específico
            try {
                const printers = await qz.printers.find("EPSON");
                if (printers && printers.length > 0) printer = printers[0];
            } catch (e) {}

            if (!printer) {
                try {
                    printer = await qz.printers.getDefault();
                } catch (e) {}
            }
        }

        if (!printer) {
            throw new Error("No se configuró ninguna impresora válida.");
        }

        // Redimensionar el logo a 150px de ancho máximo antes de imprimir
        let base64Logo = null;
        try {
            const logoPath = invoice.logoPath || (window.location.origin + '/images/logo.png');
            const dataUrl = await resizeImage(logoPath, 150, 150);
            base64Logo = dataUrl.split(',')[1];
        } catch (e) {
            console.error("No se pudo procesar el logo:", e);
        }

        const config = qz.configs.create(printer, { encoding: 'ISO-8859-1' });

        const ESC = '\x1B';
        const GS = '\x1D';
        
        const init = ESC + '@';
        const center = ESC + 'a' + '\x01';
        const left = ESC + 'a' + '\x00';
        const right = ESC + 'a' + '\x02';
        const boldOn = ESC + 'E' + '\x01';
        const boldOff = ESC + 'E' + '\x00';
        const doubleSize = ESC + '!' + '\x30'; 
        const normalSize = ESC + '!' + '\x00'; 
        const cut = GS + 'V' + '\x41' + '\x00'; 
        const openDrawer = '\x1B' + '\x70' + '\x00' + '\x19' + '\xFA'; // Comando ESC/POS para abrir cajon

        let data = [init];

        // Abrir cajón si está configurado
        if (invoice.abrirCajon) {
            data.push(openDrawer);
        }

        data.push(center);

        // Añadir logo
        if (base64Logo) {
            data.push({ 
                type: 'pixel', 
                format: 'image', 
                flavor: 'base64', 
                data: base64Logo,
                options: { language: 'ESCPOS', dotDensity: 'double' } 
            });
            data.push("\n");
        }

        data = data.concat([
            boldOn + (invoice.nombreNegocio || "INNOVATEC POS") + "\n" + boldOff,
            (invoice.ruc ? "RUC: " + invoice.ruc + "\n" : ""),
            (invoice.direccion ? invoice.direccion + "\n" : ""),
            (invoice.telefono ? "Tel: " + invoice.telefono + "\n" : ""),
            "------------------------------------------------\n",
            left,
            `Factura: ${invoice.numeroFactura}\n`,
            `Fecha:   ${invoice.fecha}\n`,
            `Cliente: ${invoice.cliente}\n`,
            `Cajero:  ${invoice.vendedor}\n`,
            "------------------------------------------------\n",
            boldOn,
            "Cant Descripcion                 Total\n",
            boldOff,
            "------------------------------------------------\n"
        ]);

        invoice.detalles.forEach(d => {
            // Asegurar que la cantidad sea entera y tenga un ancho de 4
            let cantStr = Math.floor(d.cantidad).toString().padEnd(4);
            // Reducir descripción a 26 caracteres para evitar desbordamiento
            let descStr = d.descripcion.substring(0, 26).padEnd(26);
            // Símbolo de moneda + total formateado (ancho total de la columna ~15)
            let totalStr = (invoice.simboloMoneda || "C$ ") + d.total.toFixed(2).padStart(12);
            
            data.push(`${cantStr} ${descStr} ${totalStr}\n`);
            
            if (d.imeis && d.imeis.length > 0) {
                data.push(`      IMEI: ${d.imeis.join(', ')}\n`);
            }
        });

        data.push("------------------------------------------------\n");
        
        data.push(right);
        data.push(`Subtotal:  ${invoice.simboloMoneda || "C$"} ${invoice.subtotal.toFixed(2)}\n`);
        if (invoice.descuento > 0) {
            data.push(`Descuento: ${invoice.simboloMoneda || "C$"} ${invoice.descuento.toFixed(2)}\n`);
        }
        data.push(boldOn);
        data.push(`TOTAL:      ${invoice.simboloMoneda || "C$"} ${invoice.total.toFixed(2)}\n`);
        data.push(boldOff);
        
        data.push(center);
        data.push("\n" + (invoice.mensajeTicket || "Gracias por su compra!") + "\n\n\n\n");
        data.push(cut);

        await qz.print(config, data);

        
    } catch (err) {
        console.error("Error en QZ Tray:", err);
        throw err;
    }
};

window.qzListPrinters = async () => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }
        return await qz.printers.find();
    } catch (err) {
        console.error("Error al listar impresoras:", err);
        return [];
    }
};

window.qzTestPrint = async (printerName, businessName) => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }
        
        const config = qz.configs.create(printerName);
        const data = [
            '\x1B' + '@',          // Init
            '\x1B' + 'a' + '\x01', // Center
            '\x1B' + 'E' + '\x01', // Bold on
            (businessName || "InnovaTec POS") + "\n",
            '\x1B' + 'E' + '\x00', // Bold off
            "PRUEBA DE IMPRESION\n",
            "--------------------------------\n",
            "QZ Tray Conectado: OK\n",
            "Impresora: " + printerName + "\n",
            "Fecha: " + new Date().toLocaleString() + "\n",
            "--------------------------------\n",
            "\n\n\n\n",
            '\x1D' + 'V' + '\x41' + '\x00' // Cut
        ];
        
        await qz.print(config, data);
        return true;
    } catch (err) {
        console.error("Error en prueba de impresión:", err);
        throw err;
    }
};

window.qzOpenDrawer = async (printerName) => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }
        
        let printer = printerName;
        if (!printer) {
            try {
                printer = await qz.printers.getDefault();
            } catch (e) {}
        }
        
        if (!printer) return;

        const config = qz.configs.create(printer);
        const openDrawer = '\x1B' + '\x70' + '\x00' + '\x19' + '\xFA'; 
        await qz.print(config, [openDrawer]);
    } catch (err) {
        console.error("Error al abrir gaveta:", err);
    }
};

window.qzPrintShiftClosing = async (shift, config_negocio) => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }
        
        let printer = config_negocio.printerName;
        if (!printer) {
            try {
                printer = await qz.printers.getDefault();
            } catch (e) {}
        }
        
        if (!printer) {
            throw new Error("No hay impresora configurada.");
        }

        const config = qz.configs.create(printer, { encoding: 'ISO-8859-1' });
        const ESC = '\x1B';
        const GS = '\x1D';
        const init = ESC + '@';
        const center = ESC + 'a' + '\x01';
        const left = ESC + 'a' + '\x00';
        const boldOn = ESC + 'E' + '\x01';
        const boldOff = ESC + 'E' + '\x00';
        const cut = GS + 'V' + '\x41' + '\x00';

        let data = [init, center, boldOn, (config_negocio.nombreNegocio || "INNOVATEC POS") + "\n", boldOff];
        data.push("REPORTE DE CIERRE DE CAJA (Z)\n");
        data.push("------------------------------------------------\n");
        data.push(left);
        data.push(`Turno #:  ${shift.idTurno}\n`);
        data.push(`Usuario:  ${shift.cajero || ''}\n`);
        data.push(`Apertura: ${new Date(shift.fechaApertura).toLocaleString()}\n`);
        data.push(`Cierre:   ${new Date().toLocaleString()}\n`);
        data.push("------------------------------------------------\n");
        
        data.push(boldOn + "RESUMEN DE VENTAS\n" + boldOff);
        data.push(`Total Ventas:      C$ ${shift.totalVentasNio.toFixed(2)}\n`);
        data.push(`Efectivo NIO:      C$ ${shift.totalEfectivoNio.toFixed(2)}\n`);
        data.push(`Efectivo USD:      $  ${shift.totalEfectivoUsd.toFixed(2)}\n`);
        data.push(`Tarjeta:           C$ ${shift.totalTarjeta.toFixed(2)}\n`);
        data.push(`Transferencia:     C$ ${shift.totalTransferencia.toFixed(2)}\n`);
        data.push("------------------------------------------------\n");

        data.push(boldOn + "CUADRE DE CAJA\n" + boldOff);
        data.push(`Monto Inicial:     C$ ${shift.montoInicialNio.toFixed(2)} | $ ${shift.montoInicialUsd.toFixed(2)}\n`);
        data.push(`Ingresos Manuales: C$ ${(shift.montoManualNio || 0).toFixed(2)} | $ ${(shift.montoManualUsd || 0).toFixed(2)}\n`);
        
        const esperadoNio = shift.montoInicialNio + shift.totalEfectivoNio + (shift.montoManualNio || 0);
        const esperadoUsd = shift.montoInicialUsd + shift.totalEfectivoUsd + (shift.montoManualUsd || 0);
        
        data.push(`Total Esperado:    C$ ${esperadoNio.toFixed(2)} | $ ${esperadoUsd.toFixed(2)}\n`);
        data.push(`Total Contado:     C$ ${shift.montoContadoNio.toFixed(2)} | $ ${shift.montoContadoUsd.toFixed(2)}\n`);
        
        data.push(boldOn);
        data.push(`DIFERENCIA NIO:    C$ ${(shift.diferenciaNio || 0).toFixed(2)}\n`);
        data.push(`DIFERENCIA USD:    $  ${(shift.diferenciaUsd || 0).toFixed(2)}\n`);
        data.push(boldOff);
        
        if (shift.observaciones) {
            data.push("\nOBSERVACIONES:\n" + shift.observaciones + "\n");
        }
        
        data.push("\n\n\n");
        data.push(center + "_______________________\n");
        data.push("Firma del Cajero\n\n\n\n");
        data.push(cut);

        await qz.print(config, data);
    } catch (err) {
        console.error("Error al imprimir arqueo:", err);
    }
};

window.qzPrintLabelPixel = async (base64Data, printerName, widthMm, heightMm) => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }

        const config = qz.configs.create(printerName, {
            size: { width: widthMm, height: heightMm },
            units: 'mm',
            colorType: 'blackwhite',
            interpolation: 'nearest-neighbor',
            density: 203
        });

        const data = [
            {
                type: 'pixel',
                format: 'image',
                flavor: 'base64',
                data: base64Data
            }
        ];

        await qz.print(config, data);
    } catch (err) {
        console.error("Error al imprimir etiqueta (pixel):", err);
        throw err;
    }
};

window.qzPrintPdf = async (base64Pdf, printerName) => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }

        const config = qz.configs.create(printerName);
        const data = [
            {
                type: 'pixel',
                format: 'pdf',
                flavor: 'base64',
                data: base64Pdf
            }
        ];

        await qz.print(config, data);
    } catch (err) {
        console.error("Error al imprimir PDF vía QZ:", err);
        throw err;
    }
};

window.qzPrintLabelPdf = async (base64Pdf, printerName, templateType, quantity = 1) => {
    try {
        if (!qz.websocket.isActive()) {
            await qz.websocket.connect({ retries: 1, delay: 1 });
        }

        // Definir altura según la plantilla seleccionada
        let heightMm = 20;
        if (templateType === 'mediana') heightMm = 30;
        if (templateType === 'grande') heightMm = 35; // Reducido de 40 a 35 para ahorrar papel

        // Configuración estricta para evitar que la impresora de recibos (ej. EPSON TM-T20III) 
        // rote el PDF horizontal y lo escale gigante.
        const config = qz.configs.create(printerName, {
            size: { width: 80, height: heightMm },
            units: 'mm',
            margins: 0,
            scaleContent: false // FUNDAMENTAL: Evita que deforme la etiqueta
        });

        const data = [
            {
                type: 'pixel',
                format: 'pdf',
                flavor: 'base64',
                data: base64Pdf
            }
        ];

        // Enviar trabajos por separado para que la impresora corte entre cada etiqueta
        for (let i = 0; i < quantity; i++) {
            await qz.print(config, data);
        }
    } catch (err) {
        console.error("Error al imprimir Etiqueta PDF vía QZ:", err);
        throw err;
    }
};



