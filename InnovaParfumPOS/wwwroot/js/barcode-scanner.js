var globalBarcodeString = "";
var globalLastKeyPressTime = 0;
var globalDotNetReference = null;

window.registerBarcodeScanner = function(dotNetObj) {
    globalDotNetReference = dotNetObj;
    document.addEventListener('keydown', window.handleBarcodeKeyDown);
};

window.unregisterBarcodeScanner = function() {
    document.removeEventListener('keydown', window.handleBarcodeKeyDown);
    globalDotNetReference = null;
};

window.handleBarcodeKeyDown = function(e) {
    const currentTime = new Date().getTime();
    
    // Si ha pasado más de 50ms desde la última tecla, asumimos que no es la pistola
    if (currentTime - globalLastKeyPressTime > 50) {
        globalBarcodeString = "";
    }

    if (e.key === 'Enter') {
        if (globalBarcodeString.length >= 3) {
            if (globalDotNetReference) {
                // Prevenir comportamientos por defecto del Enter si es un escaneo válido
                if (document.activeElement && document.activeElement.tagName === 'INPUT') {
                    e.preventDefault();
                }
                globalDotNetReference.invokeMethodAsync('OnBarcodeScanned', globalBarcodeString);
            }
        }
        globalBarcodeString = "";
    } else if (e.key.length === 1 && !e.ctrlKey && !e.altKey && !e.metaKey) {
        globalBarcodeString += e.key;
    }

    globalLastKeyPressTime = currentTime;
};
