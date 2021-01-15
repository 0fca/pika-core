const mediaHubconnection = new signalR.HubConnectionBuilder()
                        .withUrl("/hubs/media", {
                            transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
                        })
                .configureLogging(signalR.LogLevel.None)
				.build();

mediaHubconnection.onclose(async () => {
    await start();
});

mediaHubconnection.on("ReceiveThumb", ReceiveThumb);

mediaHubconnection.start().then(function () {
    console.log("MediaHub is ready.");
    loadThumb(listingPath, 1);
}).catch(function (err) {
    return console.error(err.toString());
});

async function start() {
    await mediaHubconnection.start();
}

function setErrorIcon(guid, err) {
    const imgEl = document.getElementById(guid);
    const imgParentLink = imgEl.parentElement;
    const icon = document.createElement("i");
    icon.setAttribute("class", "material-icons");
    icon.setAttribute("title", "There was an error loading a thumb...");
    icon.innerText = "error";
    imgParentLink.insertBefore(icon, imgEl);
    imgParentLink.removeChild(imgEl);
    console.log(err);
}

function loadThumb(path, s) {
    const imgs = document.getElementById("file-list").querySelectorAll("source");

    for (let i = 0; i < imgs.length; i++) {
        const img = imgs[i];

        if (img.hasAttribute("id")) {
            const guid = img.getAttribute("id");
            const text = img.getAttribute("data-name");
            const systemPath = path.toString() + directorySeparator +text;

            mediaHubconnection.invoke("CreateThumb", systemPath, guid, 1).catch(err => {
                setErrorIcon(guid, err.toString());
            });
        }
    }
}

function ReceiveThumb(isSuccess, thumbId) {
    if (isSuccess) {
        const url = "/Core/Storage/Thumb?id=" + thumbId;
        document.getElementById(thumbId).setAttribute("srcset", encodeURI(url));
    }else{
        setErrorIcon(thumbId, "Error receiving thumb for: "+thumbId);
    }
}
