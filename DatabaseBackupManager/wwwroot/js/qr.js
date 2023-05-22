window.addEventListener("load", () => {
    const data = document.getElementById("qrCodeData");
    const uri = data ? data.getAttribute('data-url') : undefined;
    
    if(uri)
    {
        new QRCode(document.getElementById("qrCode"),
            {
                text: uri,
                width: 200,
                height: 200
            });
    }
});