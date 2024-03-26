const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/products-hub")
    .build();

hubConnection.on("SetIsLoaded", function (data) {
    const messageElement = document.createElement("div");
    messageElement.id = "product-add-message";
    const headerText = document.createTextNode(`${data} был добавлен!`);
    messageElement.appendChild(headerText);
    document.body.appendChild(messageElement);

    setInterval(function () {
        let el = document.getElementById('product-add-message');
        document.body.removeChild(el);
    }, 5000);
});

hubConnection.start();