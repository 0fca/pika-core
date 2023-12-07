'use strict';
let hubConnection = null;
let event = null;

async function invokeReceiveListing(search, categoryId, bucketId, e) {
    try {
        await hubConnection.invoke("List", search, categoryId, bucketId);
        event = e;
    } catch (err) {
        console.error(err);
    }
}

function onReceiveListing(listing) {
    if (event !== null) {
        localStorage.setItem("data", JSON.stringify(listing));
        document.dispatchEvent(event);
    }
}

function onStart() {
    console.log("Connection to Storage hub started!");
}

function onStartError() {
    console.error("Couldnt connect to hub!");
}

function createSignalRConnection() {
    return new signalR.HubConnectionBuilder()
        .withUrl("/hubs/storage")
        .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
        .configureLogging(signalR.LogLevel.Critical)
        .withAutomaticReconnect()
        .build();
}

function connectToFilesHub() {
    const connection = createSignalRConnection();
    connection.start()
        .then(onStart)
        .catch(onStartError);
    return connection;
}

function registerCallbacks() {
    const connection = connectToFilesHub();
    connection.on('ReceiveListing', onReceiveListing);
    hubConnection = connection;
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

function returnFileSize(number) {
    if (number < 1024) {
        return number + 'B';
    } else if (number >= 1024 && number < 1048576) {
        return (number / 1024).toFixed(1) + 'kB';
    } else if (number >= 1048576) {
        return (number / 1048576).toFixed(1) + 'MB';
    } else if (number >= 1073741824) {
        return (number / 1073741824).toFixed(1) + 'GB';
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


