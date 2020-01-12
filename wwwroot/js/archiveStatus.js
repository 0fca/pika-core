const connection = new signalR.HubConnectionBuilder().withUrl("/hubs/status").build();


connection.on("ReceiveArchivingStatus", function (message) {
    reloadDownloadPartial(message);
});

connection.on("DownloadStarted", function () {
    hideDownloadBox();
});

connection.on("ArchivingCancelled", function (message) {
    reloadDownloadPartial(message);
});


connection.start().catch(function (err) {
    return console.error(err.toString());
});


