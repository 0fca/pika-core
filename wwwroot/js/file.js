const fileHubconnection = new signalR.HubConnectionBuilder().withUrl("/hubs/files",
	                                {
						transport: 4,
						skipNegotiation: false
					})
                                .configureLogging(signalR.LogLevel.Information)
                                .build();


fileHubconnection.on("ReceiveListing", ReceiveListing);


fileHubconnection.start().then(function () {
    console.log("FileHub is ready.");
}).catch(function (err) {
    return console.error(err.toString());
});

function removeFromFiles(name) {
    let files = input.files;

    for (let i = 0; i < files.length; i++) {
        if (name === files[i].name) {
            files[i] = "";
            break;
        }
    }
}

function getSummarySize() {
    let input = document.getElementById("files");
    let files = input.files;
    let sum = 0;

    for (let i = 0; i < files.length; i++) {
        sum += files[i].size;
    }
    return sum;
}

function requestListing(path) {
    fileHubconnection.invoke("List", path).catch(function (err) {
        return M.toast({ html: "<p class='text-danger'>" + err.toString()+"</p>" });
    }); 
}

function returnFileSize(number) {
    if (number < 1024) {
        return number + 'bytes';
    } else if (number >= 1024 && number < 1048576) {
        return (number / 1024).toFixed(1) + 'kB';
    } else if (number >= 1048576) {
        return (number / 1048576).toFixed(1) + 'MB';
    }
}

function getFileList() {
    let input = document.getElementById("file_uploads");
    let files = input.files;
    let result = [];

    for (let i = 0; i < files.length; i++) {
        result[i] = files[i].name;
    }
    return result;
}

function deleteAllFiles() {
    let input = document.getElementById("file_uploads");
    input.value = "";
    let messageLabel = document.getElementById("preview");
    messageLabel.innerText = "No files to be uploaded.";
    resetListOnView();
}

function ReceiveListing(listing) {
    const elem = document.getElementById("pathField");
    const instance = M.Autocomplete.getInstance(elem);
    const pathObject = listing.reduce(function (result, item, index, array) {
        result[item] = "";
        return result;
    }, {})
    
    instance.updateData(pathObject);
}



