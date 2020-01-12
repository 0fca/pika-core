const mediaHubconnection = new signalR.HubConnectionBuilder().withUrl("/hubs/media").build();

mediaHubconnection.on("ReceiveThumb", ReceiveThumb);

mediaHubconnection.start().then(function () {
    console.log("MediaHub is ready.");
    loadThumb(path);
}).catch(function (err) {
    return console.error(err.toString());
});

function loadThumb(path) {
    const imgs = document.getElementById("file-list").querySelectorAll("img");

    for (let i = 0; i < imgs.length; i++) {
        const img = imgs[i];

        if (img.hasAttribute("id")) {
            const id = img.getAttribute("id");
            const text = img.getAttribute("alt");
            mediaHubconnection.invoke("CreateThumb", path.toString() + text, id).catch(err => {
                const img = document.getElementById(id);
                const imgParentLink = img.parentElement;
                const icon = document.createElement("i");
                icon.setAttribute("class", "material-icons");
                icon.innerText = "error";
                imgParentLink.insertBefore(icon, img);
                imgParentLink.removeChild(img);
            });
        }
    }
}

function ReceiveThumb(thumbId) {
    if (thumbId != "") {
        const url = "/Storage/Thumb?id=" + thumbId;
        document.getElementById(thumbId).setAttribute("src", url);
    }
}