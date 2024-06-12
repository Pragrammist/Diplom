$(function () {
    function toggleMain() {
        $("main").toggleClass("hide");
    }

    function toggleRightPannel() {
        $("#right-pannel").toggleClass("hide-right-pannel");
    }

    function toggleLeftPannel() {
        $("#left-small-pannel").toggleClass("hide-left-pannel");
        $("#left-pannel").toggleClass("hide-left-pannel");
    }

    $(".left-pannel-toggler").on("click", function () {
        toggleMain();
        toggleLeftPannel();
    })

    $(".right-pannel-toggler").on("click", function () {
        toggleMain();
        toggleRightPannel();
    })
})

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



