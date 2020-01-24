const mediaHubconnection = new signalR.HubConnectionBuilder().withUrl("/hubs/media",
				{                   
					transport: 2,
					skipNegotiation: false
			   	})
	            		.configureLogging(signalR.LogLevel.Information)
				.build();

async function start() {
    try {
        await mediaHubconnection.start();
        console.log("connected");
    } catch (err) {
        console.log(err);
        setTimeout(() => start(), 5000);
    }
};

mediaHubconnection.onclose(async () => {
    await start();
});

mediaHubconnection.on("ReceiveThumb", ReceiveThumb);

mediaHubconnection.start().then(function () {
    console.log("MediaHub is ready.");
    loadThumb(path, 1);
}).catch(function (err) {
    return console.error(err.toString());
});

function loadThumb(path, s) {
    const imgs = document.getElementById("file-list").querySelectorAll("img");

    for (let i = 0; i < imgs.length; i++) {
        const img = imgs[i];

        if (img.hasAttribute("id")) {
            const guid = img.getAttribute("id");
            const text = img.getAttribute("alt");
            const systemPath = path.toString() + text;
            
            mediaHubconnection.invoke("CreateThumb", systemPath, guid, 1).catch(err => {
                const img = document.getElementById(guid);
                const imgParentLink = img.parentElement;
                const icon = document.createElement("i");
                icon.setAttribute("class", "material-icons");
                icon.setAttribute("title", "There was an error loading a thumb...");
                icon.innerText = "error";
                imgParentLink.insertBefore(icon, img);
                imgParentLink.removeChild(img);
		        console.log(err.toString());
            });
        }
    }
}

function ReceiveThumb(thumbId) {
    if (thumbId !== "") {
        const url = "/Storage/Thumb?id=" + thumbId;
        document.getElementById(thumbId).setAttribute("src", url);
    }
}
